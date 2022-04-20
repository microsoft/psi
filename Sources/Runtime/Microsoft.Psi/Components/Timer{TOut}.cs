// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A simple producer component that wakes up on a predefined interval and publishes one message.
    /// </summary>
    /// <typeparam name="TOut">The type of messages published by the generator.</typeparam>
    public class Timer<TOut> : Timer, IProducer<TOut>
    {
        private readonly Func<DateTime, TimeSpan, TOut> generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Timer{TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="timerInterval">Time interval with which to produce messages.</param>
        /// <param name="generator">Message generation function.</param>
        /// <param name="name">An optional name for the component.</param>
        public Timer(Pipeline pipeline, uint timerInterval, Func<DateTime, TimeSpan, TOut> generator, string name = nameof(Timer))
            : base(pipeline, timerInterval, name)
        {
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.generator = generator;
        }

        /// <inheritdoc />
        public Emitter<TOut> Out { get; }

        /// <summary>
        /// Generate timer message from current and elapsed time.
        /// </summary>
        /// <param name="absoluteTime">The current (virtual) time.</param>
        /// <param name="relativeTime">The time elapsed since the generator was started.</param>
        protected override void Generate(DateTime absoluteTime, TimeSpan relativeTime)
        {
            var value = this.generator(absoluteTime, relativeTime);
            this.Out.Post(value, absoluteTime);
        }
    }
}