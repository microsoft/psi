// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A simple producer component that wakes up on a predefined interval and publishes one message.
    /// </summary>
    /// <typeparam name="TOut">The type of messages published by the generator</typeparam>
    public class Timer<TOut> : Timer, IProducer<TOut>
    {
        private Func<DateTime, TimeSpan, TOut> generator;

        public Timer(Pipeline pipeline, uint timerInterval, Func<DateTime, TimeSpan, TOut> generator)
            : base(pipeline, timerInterval)
        {
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.generator = generator;
        }

        /// <inheritdoc />
        public Emitter<TOut> Out { get; }

        protected override void Generate(DateTime absoluteTime, TimeSpan relativeTime)
        {
            var value = this.generator(absoluteTime, relativeTime);
            this.Out.Post(value, absoluteTime);
        }
    }
}