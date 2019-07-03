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
    /// /include ..\..\Test.Psi\GeneratorSample.cs.
    /// </remarks>
    public abstract class Generator : ISourceComponent
    {
        private readonly Receiver<int> loopBackIn;
        private readonly Emitter<int> loopBackOut;
        private readonly Pipeline pipeline;
        private readonly bool isInfiniteSource;
        private bool stopped;
        private Action<DateTime> notifyCompletionTime;
        private Action notifyCompleted;
        private DateTime finalMessageTime = DateTime.MaxValue;
        private DateTime nextMessageTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="isInfiniteSource">If true, mark this Generator instance as representing an infinite source (e.g., a live-running sensor).
        /// If false (default), it represents a finite source (e.g., Generating messages based on a finite file or IEnumerable).</param>
        public Generator(Pipeline pipeline, bool isInfiniteSource = false)
        {
            this.loopBackOut = pipeline.CreateEmitter<int>(this, nameof(this.loopBackOut));
            this.loopBackIn = pipeline.CreateReceiver<int>(this, this.Next, nameof(this.loopBackIn));
            this.loopBackOut.PipeTo(this.loopBackIn);
            this.pipeline = pipeline;
            this.isInfiniteSource = isInfiniteSource;
        }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.notifyCompletionTime = notifyCompletionTime;
            if (this.isInfiniteSource)
            {
                this.notifyCompletionTime(DateTime.MaxValue);
            }

            var firstEnvelope = default(Envelope);

            if (this.pipeline.ReplayDescriptor.Start == DateTime.MinValue)
            {
                firstEnvelope.OriginatingTime = this.pipeline.GetCurrentTime();
            }
            else
            {
                firstEnvelope.OriginatingTime = this.pipeline.ReplayDescriptor.Start;
            }

            this.Next(0, firstEnvelope);
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.finalMessageTime = finalOriginatingTime;
            this.notifyCompleted = notifyCompleted;

            // if next message would be past the final message time, stop immediately and notify completion
            if (this.nextMessageTime > this.finalMessageTime)
            {
                this.stopped = true;
                this.notifyCompleted();
            }
        }

        /// <summary>
        /// Function that gets called to produce more data once the pipeline is ready to consume it.
        /// Override to post data to the appropriate stream.
        /// </summary>
        /// <param name="previous">The previously returned time, which is also the originating time of the message
        /// that triggered the current call to GenerateNext.</param>
        /// <returns>
        /// The timestamp (originating time) of the next message to be posted back to LoopBackIn.
        /// The next call will occur only after this time (based on the pipeline clock).
        /// </returns>
        protected abstract DateTime GenerateNext(DateTime previous);

        private void Next(int counter, Envelope envelope)
        {
            if (this.stopped)
            {
                return;
            }

            this.nextMessageTime = this.GenerateNext(envelope.OriginatingTime);

            // stop if nextMessageTime is past finalMessageTime or is equal to DateTime.MaxValue (which indicates that there is no more data)
            if (this.nextMessageTime > this.finalMessageTime || this.nextMessageTime == DateTime.MaxValue)
            {
                this.stopped = true;
                this.notifyCompletionTime(envelope.OriginatingTime);

                // additionally notify completed if we have already been requested by the pipeline to stop
                this.notifyCompleted?.Invoke();
                return;
            }

            // Check if the times coming out of GenerateNext are not strictly increasing
            // (but only check once we've gone through this method at least once)
            if ((counter > 0) && (this.nextMessageTime <= envelope.OriginatingTime))
            {
                throw new InvalidOperationException("Generator is creating timestamps out of order. The times returned by GenerateNext are required to be strictly increasing.");
            }

            this.loopBackOut.Post(counter + 1, this.nextMessageTime);
        }
    }
}