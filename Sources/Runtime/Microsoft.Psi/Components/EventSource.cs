// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A generator component that publishes messages of a specified type whenever an event is raised.
    /// </summary>
    /// <typeparam name="TEventHandler">The event handler delegate type.</typeparam>
    /// <typeparam name="TOut">The output stream type.</typeparam>
    public class EventSource<TEventHandler, TOut> : IProducer<TOut>, IDisposable
    {
        private readonly Action<TEventHandler> subscribe;
        private readonly Action<TEventHandler> unsubscribe;
        private readonly TEventHandler eventHandler;
        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSource{TEventHandler, TOut}"/> class.
        /// The component will subscribe to an event on startup via the <paramref name="subscribe"/>
        /// delegate, using the supplied <paramref name="converter"/> function to transform the
        /// <see cref="Post"/> action delegate into an event handler compatible with the external
        /// event that is being subscribed to.
        /// </summary>
        /// <param name="pipeline">The Psi pipeline.</param>
        /// <param name="subscribe">The delegate that subscribes to the external event.</param>
        /// <param name="unsubscribe">The delegate that unsubscribes from the external event.</param>
        /// <param name="converter">
        /// A function used to convert the <see cref="Post"/> action delegate into an event
        /// handler of type <typeparamref name="TEventHandler"/> that will be subscribed to the
        /// external event by the <paramref name="subscribe"/> delegate.
        /// </param>
        public EventSource(
            Pipeline pipeline,
            Action<TEventHandler> subscribe,
            Action<TEventHandler> unsubscribe,
            Func<Action<TOut>, TEventHandler> converter)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.subscribe = subscribe;
            this.unsubscribe = unsubscribe;
            this.eventHandler = converter(this.Post);
            this.subscribe(this.eventHandler);
        }

        /// <summary>
        /// Gets the stream of output messages.
        /// </summary>
        public Emitter<TOut> Out { get; }

        /// <summary>
        /// Disposes the component.
        /// </summary>
        public void Dispose()
        {
            this.unsubscribe(this.eventHandler);
        }

        /// <summary>
        /// Posts a value on the output stream.
        /// </summary>
        /// <param name="e">The value to post.</param>
        private void Post(TOut e)
        {
            this.Out.Post(e, this.pipeline.GetCurrentTime());
        }
    }
}