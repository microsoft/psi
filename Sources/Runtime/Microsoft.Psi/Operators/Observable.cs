// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Convert a stream to an <see cref="IObservable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of messages for the source stream.</typeparam>
        /// <param name="stream">The source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Observable with elements from the source stream.</returns>
        public static IObservable<T> ToObservable<T>(this IProducer<T> stream, DeliveryPolicy deliveryPolicy = null)
        {
            return new StreamObservable<T>(stream, deliveryPolicy);
        }

        /// <summary>
        /// Observable stream class.
        /// </summary>
        /// <typeparam name="T">Type of stream messages.</typeparam>
        public class StreamObservable<T> : IObservable<T>
        {
            private ConcurrentDictionary<IObserver<T>, IObserver<T>> observers = new ConcurrentDictionary<IObserver<T>, IObserver<T>>();

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamObservable{T}"/> class.
            /// </summary>
            /// <param name="stream">The source stream to observe.</param>
            /// <param name="deliveryPolicy">An optional delivery policy.</param>
            public StreamObservable(IProducer<T> stream, DeliveryPolicy deliveryPolicy = null)
            {
                var processor = new Processor<T, T>(
                    stream.Out.Pipeline,
                    (d, e, s) =>
                    {
                        foreach (var obs in this.observers)
                        {
                            obs.Value.OnNext(d.DeepClone());
                        }

                        s.Post(d, e.OriginatingTime);
                    });

                stream.Out.PipeTo(processor, deliveryPolicy);

                processor.In.Unsubscribed += _ =>
                {
                    foreach (var obs in this.observers)
                    {
                        obs.Value.OnCompleted();
                    }
                };
            }

            /// <summary>
            /// Gets a value indicating whether this observable stream has subscribers.
            /// </summary>
            public bool HasSubscribers => this.observers.Count > 0;

            /// <inheritdoc />
            public IDisposable Subscribe(IObserver<T> observer)
            {
                this.observers.TryAdd(observer, observer);
                return new Unsubscriber(this, observer);
            }

            private class Unsubscriber : IDisposable
            {
                private StreamObservable<T> observable;
                private IObserver<T> observer;

                public Unsubscriber(StreamObservable<T> observable, IObserver<T> observer)
                {
                    this.observable = observable;
                    this.observer = observer;
                }

                public void Dispose()
                {
                    if (this.observer != null)
                    {
                        this.observable.observers.TryRemove(this.observer, out _);
                    }
                }
            }
        }
    }
}
