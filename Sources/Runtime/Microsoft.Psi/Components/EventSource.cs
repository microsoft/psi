// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using Microsoft.Psi.Executive;

    /// <summary>
    /// A generator component that publishes messages of a specified type whenever an event is raised.
    /// </summary>
    /// <typeparam name="TEventHandler">The event handler delegate type.</typeparam>
    /// <typeparam name="TOut">The output stream type.</typeparam>
    public class EventSource<TEventHandler, TOut> : IProducer<TOut>, ISourceComponent
    {
        private readonly Action<TEventHandler> subscribe;
        private readonly Action<TEventHandler> unsubscribe;
        private readonly TEventHandler eventHandler;
        private readonly Pipeline pipeline;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSource{TEventHandler, TOut}"/> class.
        /// The component will subscribe to an event on startup via the <paramref name="subscribe"/>
        /// delegate, using the supplied <paramref name="converter"/> function to transform the
        /// <see cref="Post"/> action delegate into an event handler compatible with the external
        /// event that is being subscribed to.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="subscribe">The delegate that subscribes to the external event.</param>
        /// <param name="unsubscribe">The delegate that unsubscribes from the external event.</param>
        /// <param name="converter">
        /// A function used to convert the <see cref="Post"/> action delegate into an event
        /// handler of type <typeparamref name="TEventHandler"/> that will be subscribed to the
        /// external event by the <paramref name="subscribe"/> delegate.
        /// </param>
        /// <param name="name">An optional name for the component.</param>
        public EventSource(
            Pipeline pipeline,
            Action<TEventHandler> subscribe,
            Action<TEventHandler> unsubscribe,
            Func<Action<TOut>, TEventHandler> converter,
            string name = nameof(EventSource<TEventHandler, TOut>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.subscribe = subscribe;
            this.unsubscribe = unsubscribe;

            // If the source event is triggered from the execution context of some other receiver, then because the
            // execution context flows all the way through to the event handler, the tracked state object (if tracking
            // is enabled) would represent the owner of the receiver, which would be inconsistent with posting from a
            // pure source (no tracked state object). In order to rectify this, we set the tracked state object to null
            // just prior to the call to this.Post by wrapping it in TrackStateObjectOnContext with a null state object.
            this.eventHandler = converter(PipelineElement.TrackStateObjectOnContext<TOut>(this.Post, null, pipeline));
        }

        /// <summary>
        /// Gets the stream of output messages.
        /// </summary>
        public Emitter<TOut> Out { get; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.subscribe(this.eventHandler);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.unsubscribe(this.eventHandler);
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

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