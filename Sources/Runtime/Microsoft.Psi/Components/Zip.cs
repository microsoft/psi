// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Zip one or more streams (T) into a single stream while ensuring delivery in originating time order.
    /// </summary>
    /// <remarks>Messages are produced in originating-time order; potentially delayed in wall-clock time.
    /// If multiple messages arrive with the same originating time, they are added in the output array in
    /// the order of stream ids.</remarks>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class Zip<T> : IProducer<T[]>
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly IList<Receiver<T>> inputs = new List<Receiver<T>>();
        private readonly IList<(T data, Envelope envelope, IRecyclingPool<T> recycler)> buffer = new List<(T, Envelope, IRecyclingPool<T>)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Zip{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for this component.</param>
        public Zip(Pipeline pipeline, string name = nameof(Zip<T>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Out = pipeline.CreateEmitter<T[]>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<T[]> Out { get; }

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
                receiver = this.pipeline.CreateReceiver<T>(this, (m, e) => this.Receive(m, e, receiver.Recycler), name);
                receiver.Unsubscribed += _ => this.Publish();
                this.inputs.Add(receiver);
                return receiver;
            }
            finally
            {
                syncContext.Release();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive(T data, Envelope envelope, IRecyclingPool<T> recycler)
        {
            var clonedData = data.DeepClone(recycler);
            this.buffer.Add((data: clonedData, envelope, recycler));
            this.Publish();
        }

        private void Publish()
        {
            // find out the earliest last originating time across inputs
            var frontier = this.inputs.Min(i => i.LastEnvelope.OriginatingTime);

            // get the groups of messages ready to be published
            var eligible = this.buffer
                .Where(m => m.envelope.OriginatingTime <= frontier)
                .OrderBy(m => m.envelope.OriginatingTime)
                .ThenBy(m => m.envelope.SourceId)
                .GroupBy(m => m.envelope.OriginatingTime);

            foreach (var group in eligible.ToArray())
            {
                this.Out.Post(group.Select(t => t.data).ToArray(), group.Key);

                foreach (var (data, envelope, recycler) in group)
                {
                    this.buffer.Remove((data, envelope, recycler));
                    recycler.Recycle(data);
                }
            }
        }
    }
}