// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Persistence;

    /// <summary>
    /// Reads raw messages from a multi-stream store.
    /// </summary>
    internal sealed class MessageImporter : IProducer<Message<BufferReader>>, ISourceComponent
    {
        private readonly Receiver<bool> loopBack; // ignore dispose warning - will be disposed by the Pipeline
        private readonly Emitter<bool> next;
        private readonly StoreReader reader;
        private readonly Pipeline pipeline;
        private Action<DateTime> notifyCompletionTime;
        private long finalTicks = 0;
        private bool stopping;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="reader">The store reader.</param>
        public MessageImporter(Pipeline pipeline, StoreReader reader)
        {
            this.reader = reader;
            this.pipeline = pipeline;
            this.next = pipeline.CreateEmitter<bool>(this, nameof(this.next));
            this.loopBack = pipeline.CreateReceiver<bool>(this, this.Next, nameof(this.loopBack));
            this.next.PipeTo(this.loopBack, DeliveryPolicy.Unlimited);
            this.Out = pipeline.CreateEmitter<Message<BufferReader>>(this, nameof(this.Out));
        }

        public Emitter<Message<BufferReader>> Out { get; }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.notifyCompletionTime = notifyCompletionTime;

            var replay = this.pipeline.ReplayDescriptor;
            this.reader.Seek(replay.Interval, true);
            this.next.Post(true, replay.Start);
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.stopping = true;
            notifyCompleted();
        }

        /// <summary>
        /// Attempts to move the reader to the next message (across all logical storage streams).
        /// </summary>
        /// <param name="moreDataPromised">Indicates whether an absence of messages should be reported as the end of the store.</param>
        /// <param name="env">The envelope of the last message we read.</param>
        private void Next(bool moreDataPromised, Envelope env)
        {
            if (this.stopping)
            {
                return;
            }

            var result = this.reader.MoveNext(out Envelope e);
            if (result)
            {
                int count = this.reader.Read(ref this.buffer);
                var bufferReader = new BufferReader(this.buffer, count);

                // we want messages to be scheduled and delivered based on their original creation time, not originating time
                // the check below is just to ensure we don't fail because of some timing issue when writing the data (since there is no ordering guarantee across streams)
                // note that we are posting a message of a message, and once the outer message is stripped by the splitter, the inner message will still have the correct originating time
                var nextTime = (env.OriginatingTime > e.Time) ? env.OriginatingTime : e.Time;
                this.Out.Post(Message.Create(bufferReader, e), nextTime);
                this.next.Post(true, nextTime.AddTicks(1));
                this.finalTicks = Math.Max(this.finalTicks, Math.Max(e.OriginatingTime.Ticks, nextTime.Ticks));
            }
            else
            {
                // retry at least once, even if there is no active writer
                bool willHaveMoreData = this.reader.IsMoreDataExpected();
                if (willHaveMoreData || moreDataPromised)
                {
                    this.next.Post(willHaveMoreData, env.OriginatingTime.AddTicks(1));
                }
                else
                {
                    this.notifyCompletionTime(new DateTime(this.finalTicks));
                }
            }
        }
    }
}
