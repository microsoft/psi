// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Convert a stream to an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <remarks>
        /// This may be traversed while the pipeline runs async, or may collect values to be consumed after pipeline disposal.
        /// </remarks>
        /// <typeparam name="T">Type of messages for the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="condition">Predicate condition while which values will be enumerated (otherwise infinite).</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Enumerable with elements from the source stream.</returns>
        public static IEnumerable<T> ToEnumerable<T>(this IProducer<T> source, Func<T, bool> condition = null, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(ToEnumerable))
            => new StreamEnumerable<T>(source, condition, deliveryPolicy, name);

        /// <summary>
        /// Enumerable stream class.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        public class StreamEnumerable<T> : IEnumerable, IEnumerable<T>, IDisposable
        {
            private readonly StreamEnumerator enumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamEnumerable{T}"/> class.
            /// </summary>
            /// <param name="source">The source stream to enumerate.</param>
            /// <param name="predicate">Predicate (filter) function.</param>
            /// <param name="deliveryPolicy">An optional delivery policy.</param>
            /// <param name="name">An optional name for this operator.</param>
            public StreamEnumerable(IProducer<T> source, Func<T, bool> predicate = null, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(StreamEnumerable<T>))
            {
                this.enumerator = new StreamEnumerator(predicate ?? (_ => true));

                var processor = new Processor<T, T>(
                    source.Out.Pipeline,
                    (d, e, s) =>
                    {
                        this.enumerator.Queue.Enqueue(d.DeepClone());
                        this.enumerator.Enqueued.Set();
                    },
                    name: name);

                source.PipeTo(processor, deliveryPolicy);
                processor.In.Unsubscribed += _ => this.enumerator.Closed.Set();
            }

            /// <inheritdoc />
            public void Dispose()
            {
                this.enumerator.Dispose();
            }

            /// <inheritdoc />
            public IEnumerator GetEnumerator()
            {
                return this.enumerator;
            }

            /// <inheritdoc />
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this.enumerator;
            }

            private class StreamEnumerator : IEnumerator, IEnumerator<T>
            {
                private readonly Func<T, bool> predicate;
                private readonly WaitHandle[] queueUpdated;
                private T current;

                public StreamEnumerator(Func<T, bool> predicate)
                {
                    this.predicate = predicate;
                    this.queueUpdated = new[] { this.Closed, this.Enqueued };
                }

                public ConcurrentQueue<T> Queue { get; } = new ConcurrentQueue<T>();

                public ManualResetEvent Enqueued { get; } = new ManualResetEvent(false);

                public ManualResetEvent Closed { get; } = new ManualResetEvent(false);

                public object Current => this.current;

                T IEnumerator<T>.Current => this.current;

                public void Dispose()
                {
                    this.Enqueued.Dispose();
                }

                public bool MoveNext()
                {
                    while (true)
                    {
                        if (this.Queue.TryDequeue(out this.current))
                        {
                            if (this.Queue.IsEmpty)
                            {
                                this.Enqueued.Reset();
                            }

                            return this.predicate(this.current);
                        }

                        if (WaitHandle.WaitAny(this.queueUpdated) == 0 && this.Queue.IsEmpty)
                        {
                            // enumerator is closed *and* queue is empty
                            return false;
                        }
                    }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
