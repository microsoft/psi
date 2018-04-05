// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Psi stream as an `IEnumerable`.
        /// </summary>
        /// <remarks>
        /// This may be traversed while the pipeline runs async, or may collect values to be consumed after pipeline disposal.
        /// </remarks>
        /// <typeparam name="T">Type of Psi stream values</typeparam>
        /// <param name="stream">Psi stream</param>
        /// <param name="condition">Predicate condition while which values will be enumerated (otherwise infinite).</param>
        /// <returns>Enumerable of Psi stream</returns>
        public static IEnumerable<T> ToEnumerable<T>(this IProducer<T> stream, Func<T, bool> condition = null)
        {
            return new StreamEnumerable<T>(stream, condition);
        }

        /// <summary>
        /// Enumerable stream class.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        public class StreamEnumerable<T> : IEnumerable, IEnumerable<T>
        {
            private readonly StreamEnumerator enumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamEnumerable{T}"/> class.
            /// </summary>
            /// <param name="stream">Stream to enumerate.</param>
            /// <param name="predicate">Predicate (filter) function.</param>
            public StreamEnumerable(IProducer<T> stream, Func<T, bool> predicate = null)
            {
                this.enumerator = new StreamEnumerator(predicate ?? (_ => true));
                stream.Do(x =>
                {
                    this.enumerator.Queue.Enqueue(x);
                    this.enumerator.Enqueued.Set();
                });
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

                private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

                private ManualResetEvent enqueued = new ManualResetEvent(false);

                private T current;

                public StreamEnumerator(Func<T, bool> predicate)
                {
                    this.predicate = predicate;
                }

                public ConcurrentQueue<T> Queue => this.queue;

                public ManualResetEvent Enqueued => this.enqueued;

                public object Current => this.current;

                T IEnumerator<T>.Current => this.current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while (true)
                    {
                        if (this.Queue.TryDequeue(out this.current))
                        {
                            return this.predicate(this.current);
                        }

                        this.Enqueued.WaitOne();
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
