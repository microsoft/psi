// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Component that implements a stream sampler.
    /// </summary>
    /// <typeparam name="T">The type of messages to sample.</typeparam>
    public class Sampler<T> : ConsumerProducer<T, T>
    {
        private readonly Queue<Message<T>> inputQueue = new Queue<Message<T>>();
        private readonly Match.Interpolator<T> interpolator;
        private readonly TimeSpan samplingInterval;
        private DateTime nextPublishTime;
        private DateTime? closingOriginatingTime = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sampler{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="interpolator">Interpolator used to sample.</param>
        /// <param name="samplingInterval">Sampling interval.</param>
        public Sampler(Pipeline pipeline, Match.Interpolator<T> interpolator, TimeSpan samplingInterval)
            : base(pipeline)
        {
            this.In.Unsubscribed += closingOriginatingTime => this.closingOriginatingTime = closingOriginatingTime;
            this.interpolator = interpolator;
            this.samplingInterval = samplingInterval;
        }

        /// <inheritdoc/>
        protected override void Receive(T message, Envelope e)
        {
            var clone = message.DeepClone(this.In.Recycler);
            this.inputQueue.Enqueue(Message.Create(clone, e));
            this.Publish(e.OriginatingTime);
        }

        private void Publish(DateTime now)
        {
            if (this.nextPublishTime == default(DateTime))
            {
                this.nextPublishTime = now;
            }

            while (this.nextPublishTime <= now)
            {
                var matchResult = this.interpolator.Match(this.nextPublishTime, this.inputQueue, this.closingOriginatingTime);
                if (matchResult.Type == MatchResultType.InsufficientData)
                {
                    // we need to wait more
                    return;
                }

                if (matchResult.Type == MatchResultType.Created)
                {
                    // publish
                    this.Out.Post(matchResult.Value, this.nextPublishTime);
                }

                // clear the queue as needed
                while (this.inputQueue.Peek().OriginatingTime < matchResult.ObsoleteTime)
                {
                    this.In.Recycle(this.inputQueue.Dequeue());
                }

                this.nextPublishTime += this.samplingInterval;
            }
        }
    }
}