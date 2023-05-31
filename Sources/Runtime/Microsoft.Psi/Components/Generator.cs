// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using Microsoft.Psi.Executive;

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
        private readonly string name;
        private readonly PipelineElement node;
        private readonly bool isInfiniteSource;
        private bool stopped;
        private Action<DateTime> notifyCompletionTime;
        private Action notifyCompleted;
        private DateTime finalMessageTime = DateTime.MaxValue;
        private DateTime nextMessageOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="isInfiniteSource">If true, mark this Generator instance as representing an infinite source (e.g., a live-running sensor).
        /// If false (default), it represents a finite source (e.g., Generating messages based on a finite file or IEnumerable).</param>
        /// <param name="name">An optional name for the generator.</param>
        public Generator(Pipeline pipeline, bool isInfiniteSource = false, string name = nameof(Generator))
        {
            this.loopBackOut = pipeline.CreateEmitter<int>(this, nameof(this.loopBackOut));
            this.loopBackIn = pipeline.CreateReceiver<int>(this, this.Next, nameof(this.loopBackIn));
            this.loopBackOut.PipeTo(this.loopBackIn, DeliveryPolicy.Unlimited);
            this.pipeline = pipeline;
            this.name = name;
            this.node = pipeline.GetOrCreateNode(this);
            this.isInfiniteSource = isInfiniteSource;
        }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.notifyCompletionTime = notifyCompletionTime;

            // If this is an infinite source *and* the pipeline does not specify a finite replay interval,
            // then we can notify completion with MaxValue now to notify the pipeline that this component
            // will not terminate until the pipeline is explicitly stopped. Otherwise, we will notify later
            // once we have completed generating all messages within the pipeline's replay interval.
            if (this.isInfiniteSource && this.pipeline.ReplayDescriptor.End == DateTime.MaxValue)
            {
                this.notifyCompletionTime(DateTime.MaxValue);
            }
            else
            {
                // ensure that messages are not posted past the end of the replay descriptor
                this.finalMessageTime = this.pipeline.ReplayDescriptor.End;
            }

            var firstEnvelope = default(Envelope);

            if (this.pipeline.ReplayDescriptor.Start == DateTime.MinValue)
            {
                firstEnvelope.OriginatingTime = this.pipeline.StartTime;
            }
            else
            {
                firstEnvelope.OriginatingTime = this.pipeline.ReplayDescriptor.Start;
            }

            this.loopBackOut.Post(0, firstEnvelope.OriginatingTime);
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            // If the generator has already stopped, call notify completed.
            if (this.stopped)
            {
                notifyCompleted.Invoke();
                return;
            }

            this.finalMessageTime = finalOriginatingTime;
            this.notifyCompleted = notifyCompleted;

            // if next message would be past the final message time, stop immediately and notify completion
            if (this.nextMessageOriginatingTime > this.finalMessageTime)
            {
                this.stopped = true;
                this.notifyCompleted();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Function that gets called to produce more data once the pipeline is ready to consume it.
        /// Override to post data to the appropriate stream.
        /// </summary>
        /// <param name="currentTime">The originating time of the message that triggered the current call to GenerateNext.</param>
        /// <returns>
        /// The originating time of the next message that will trigger the next call to GenerateNext.
        /// The next call will occur only after this time (based on the pipeline clock).
        /// </returns>
        protected abstract DateTime GenerateNext(DateTime currentTime);

        private void Next(int counter, Envelope envelope)
        {
            if (this.stopped)
            {
                return;
            }

            try
            {
                this.nextMessageOriginatingTime = this.GenerateNext(envelope.OriginatingTime);

                // impose strictly increasing times for the loopback message as required by the runtime
                if (this.nextMessageOriginatingTime <= envelope.OriginatingTime)
                {
                    this.nextMessageOriginatingTime = envelope.OriginatingTime.AddTicks(1);
                }

                // stop if nextMessageTime is past finalMessageTime or is equal to DateTime.MaxValue (which indicates that there is no more data)
                if (this.nextMessageOriginatingTime > this.finalMessageTime || this.nextMessageOriginatingTime == DateTime.MaxValue)
                {
                    this.stopped = true;

                    // get the latest message time from all of this component's emitters
                    var finalOriginatingTime = this.node.LastOutputEnvelope.OriginatingTime;

                    // notify the pipeline of the originating time of the final message from this component
                    this.notifyCompletionTime(finalOriginatingTime);

                    // additionally notify completed if we have already been requested by the pipeline to stop
                    this.notifyCompleted?.Invoke();
                    return;
                }

                this.loopBackOut.Post(counter + 1, this.nextMessageOriginatingTime);
            }
            catch
            {
                // If the loopback function throws, we must terminate the generator immediately since we cannot guarantee
                // the delivery of any further loopback messages. The pipeline will attempt to stop all source components
                // (including this generator), so setting this.nextMessageTime to DateTime.MaxValue signals to the component
                // to call notifyCompleted as soon as the pipeline calls its Stop() method, rather than wait for further
                // loopback messages pending completion (since there won't be any forthcoming).
                this.nextMessageOriginatingTime = DateTime.MaxValue;
                throw;
            }
        }
    }
}