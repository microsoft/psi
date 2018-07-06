// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Generates a sequence of messages at the pace dictated by the pipeline.
    /// Use this base class when your generator has multiple output streams.
    /// Use the static functions of the <see cref="Generators"/> class for the single-stream case.
    /// </summary>
    /// <remarks>
    /// When playing back data from offline sources, it is typically desirable that data not be dropped
    /// even when resource constraints prevent the pipeline from running in real time.
    /// Thus, source components that generate data from offline sources (e.g. from a file)
    /// must be able to slow down production of data as requested by the hosting pipeline.
    /// Since the runtime will not interfere with any threads it doesn’t own (by design),
    /// reading and publishing offline data on a dedicated thread doesn't achieve the desired effect.
    /// Rather, such source components must implement the Generator pattern, in which an internal emitter/receiver
    /// pair is used to yield back to the runtime.
    /// The following example shows how to implement a multi-stream generator:
    /// /include ..\..\Test.Psi\GeneratorSample.cs
    /// </remarks>
    public abstract class Generator : IFiniteSourceComponent
    {
        private readonly Receiver<int> loopBackIn;
        private readonly Emitter<int> loopBackOut;
        private readonly Pipeline pipeline;
        private bool stopped;
        private Action onCompleted;
        private DateTime lastMessageTime = default(DateTime);

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        public Generator(Pipeline pipeline)
        {
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);
            pipeline.RegisterPipelineStopHandler(this, this.OnPipelineStop);
            this.loopBackOut = pipeline.CreateEmitter<int>(this, nameof(this.loopBackOut));
            this.loopBackIn = pipeline.CreateReceiver<int>(this, this.Next, nameof(this.loopBackIn));
            this.loopBackOut.PipeTo(this.loopBackIn);
            this.pipeline = pipeline;
        }

        /// <inheritdoc />
        public void Initialize(Action onCompleted)
        {
            this.onCompleted = onCompleted;
        }

        /// <summary>
        /// Function that gets called to produce more data once the pipeline is ready to consume it.
        /// Override to post data to the appropriate stream.
        /// </summary>
        /// <param name="previous">The timestamp provided by the last invocation. Provided for reference.</param>
        /// <returns>
        /// The timestamp (originating time) of the last message posted.
        /// The next call will occur only after this time (based on the pipeline clock).
        /// If the data being published doesn't come with a timestamp, use pipeline.GetCurrentTime().
        /// </returns>
        protected abstract DateTime GenerateNext(DateTime previous);

        /// <summary>
        /// Stop this component.
        /// </summary>
        protected void Stop()
        {
            this.stopped = true;
        }

        private void OnPipelineStop()
        {
            this.Stop();
        }

        private void OnPipelineStart()
        {
            this.Next(0, default(Envelope));
        }

        private void Next(int last, Envelope envelope)
        {
            if (this.stopped)
            {
                this.onCompleted();
                return;
            }

            var nextMessageTime = this.GenerateNext(envelope.OriginatingTime);
            if (nextMessageTime == DateTime.MaxValue)
            {
                this.stopped = true;
                this.onCompleted();
                return;
            }

            if (nextMessageTime > this.lastMessageTime)
            {
                this.lastMessageTime = nextMessageTime;
            }

            this.loopBackOut.Post(last + 1, this.lastMessageTime);
        }
    }
}