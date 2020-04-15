// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Zip one or more streams (T) into a single stream (Message{T}) while ensuring delivery in originating time order (ordered within single tick by stream ID).
    /// </summary>
    /// <remarks>Messages are produced in originating-time order; potentially delayed in wall-clock time.</remarks>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class Zip<T> : IProducer<Message<T>>
    {
        private readonly Pipeline pipeline;
        private readonly IList<Receiver<T>> inputs = new List<Receiver<T>>();
        private readonly IList<(T data, Envelope envelope, IRecyclingPool<T> recycler)> buffer = new List<(T, Envelope, IRecyclingPool<T>)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Zip{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to which to attach.</param>
        public Zip(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<Message<T>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<Message<T>> Out { get; }

        /// <summary>
        /// Add input receiver.
        /// </summary>
        /// <param name="name">The unique debug name of the receiver.</param>
        /// <returns>Receiver.</returns>
        public Receiver<T> AddInput(string name)
        {
            var syncContext = this.Out.SyncContext; // protect collections from concurrent access
            syncContext.Lock();

            try
            {
                Receiver<T> receiver = null; // captured in receiver action closure
                receiver = this.pipeline.CreateReceiver<T>(this, (m, e) => this.Receive(m, e, receiver.Recycler), name, true);
                receiver.Unsubscribed += _ => this.Publish();
                this.inputs.Add(receiver);
                return receiver;
            }
            finally
            {
                syncContext.Release();
            }
        }

        private void Receive(T clonedData, Envelope e, IRecyclingPool<T> recycler)
        {
            this.buffer.Add((data: clonedData, envelope: e, recycler: recycler));
            this.Publish();
        }

        private void Publish()
        {
            var frontier = this.inputs.Min(i => i.LastEnvelope.OriginatingTime); // earliest last originating time across inputs
            var eligible = from m in this.buffer
                           where frontier > m.envelope.OriginatingTime // later messages seen on _all_ (non-closed) inputs
                           orderby m.envelope.OriginatingTime, m.envelope.SourceId // originating time order (by source ID if collision)
                           select m;
            foreach (var (data, envelope, recycler) in eligible.ToArray())
            {
                var time = this.pipeline.GetCurrentTime();
                if (this.pipeline.FinalOriginatingTime > DateTime.MinValue && time >= this.pipeline.FinalOriginatingTime)
                {
                    // pipeline is closing and these messages would be dropped by the scheduler if posted with current time
                    time = this.Out.LastEnvelope.OriginatingTime.AddTicks(1); // squeeze final messages in before pipeline shutdown if possible
                    if (time > this.pipeline.FinalOriginatingTime)
                    {
                        throw new Exception("Zip has been forced to drop messages due to pipeline shutdown");
                    }
                }

                this.Out.Post(Message.Create(data, envelope), time);
                recycler.Recycle(data);
                this.buffer.Remove((data, envelope, recycler));
            }
        }
    }
}