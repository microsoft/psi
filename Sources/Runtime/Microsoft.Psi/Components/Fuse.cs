// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Common.Interpolators;

    /// <summary>
    /// Component that fuses multiple streams based on a specified interpolator.
    /// </summary>
    /// <typeparam name="TPrimary">The type the messages on the primary stream.</typeparam>
    /// <typeparam name="TSecondary">The type messages on the secondary stream.</typeparam>
    /// <typeparam name="TInterpolation">The type of the interpolation result on the secondary stream.</typeparam>
    /// <typeparam name="TOut">The type of output message.</typeparam>
    public class Fuse<TPrimary, TSecondary, TInterpolation, TOut> : IProducer<TOut>
    {
        private readonly Pipeline pipeline;
        private readonly Queue<Message<TPrimary>> primaryQueue = new Queue<Message<TPrimary>>(); // to be paired
        private readonly Interpolator<TSecondary, TInterpolation> interpolator;
        private readonly Func<TPrimary, TInterpolation[], TOut> outputCreator;
        private readonly Func<TPrimary, IEnumerable<int>> secondarySelector;
        private (Queue<Message<TSecondary>> Queue, DateTime? ClosedOriginatingTime)[] secondaryQueues;
        private Receiver<TSecondary>[] inSecondaries;
        private bool[] receivedSecondary;
        private IEnumerable<int> defaultSecondarySet;

        // temp buffers
        private TInterpolation[] lastValues;
        private InterpolationResult<TInterpolation>[] lastResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="Fuse{TPrimary, TSecondary, TInterpolation, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="interpolator">Interpolator to use when joining the streams.</param>
        /// <param name="outputCreator">Mapping function from messages to output.</param>
        /// <param name="secondaryCount">Number of secondary streams.</param>
        /// <param name="secondarySelector">Selector function mapping primary messages to a set of secondary stream indices.</param>
        public Fuse(
            Pipeline pipeline,
            Interpolator<TSecondary, TInterpolation> interpolator,
            Func<TPrimary, TInterpolation[], TOut> outputCreator,
            int secondaryCount = 1,
            Func<TPrimary, IEnumerable<int>> secondarySelector = null)
            : base()
        {
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.InPrimary = pipeline.CreateReceiver<TPrimary>(this, this.ReceivePrimary, nameof(this.InPrimary));
            this.interpolator = interpolator;
            this.outputCreator = outputCreator;
            this.secondarySelector = secondarySelector;
            this.inSecondaries = new Receiver<TSecondary>[secondaryCount];
            this.receivedSecondary = new bool[secondaryCount];
            this.secondaryQueues = new ValueTuple<Queue<Message<TSecondary>>, DateTime?>[secondaryCount];
            this.lastValues = new TInterpolation[secondaryCount];
            this.lastResults = new InterpolationResult<TInterpolation>[secondaryCount];
            this.defaultSecondarySet = Enumerable.Range(0, secondaryCount);
            for (int i = 0; i < secondaryCount; i++)
            {
                this.secondaryQueues[i] = (new Queue<Message<TSecondary>>(), null);
                var id = i; // needed to make the closure below byval
                var receiver = pipeline.CreateReceiver<TSecondary>(this, (d, e) => this.ReceiveSecondary(id, d, e), "InSecondary" + i);
                receiver.Unsubscribed += closedOriginatingTime => this.SecondaryClosed(id, closedOriginatingTime);
                this.inSecondaries[i] = receiver;
                this.receivedSecondary[i] = false;
            }
        }

        /// <inheritdoc />
        public Emitter<TOut> Out { get; }

        /// <summary>
        /// Gets primary input receiver.
        /// </summary>
        public Receiver<TPrimary> InPrimary { get; }

        /// <summary>
        /// Gets collection of secondary receivers.
        /// </summary>
        public IList<Receiver<TSecondary>> InSecondaries => this.inSecondaries;

        /// <summary>
        /// Add input receiver.
        /// </summary>
        /// <returns>Receiver.</returns>
        public Receiver<TSecondary> AddInput()
        {
            // use the sync context to protect the queues from concurrent access
            var syncContext = this.Out.SyncContext;
            syncContext.Lock();

            try
            {
                var lastIndex = this.inSecondaries.Length;
                var count = lastIndex + 1;
                Array.Resize(ref this.inSecondaries, count);
                var newInput = this.inSecondaries[lastIndex] = this.pipeline.CreateReceiver<TSecondary>(this, (d, e) => this.ReceiveSecondary(lastIndex, d, e), "InSecondary" + lastIndex);
                newInput.Unsubscribed += closedOriginatingTime => this.SecondaryClosed(lastIndex, closedOriginatingTime);

                Array.Resize(ref this.receivedSecondary, count);
                this.receivedSecondary[count - 1] = false;

                Array.Resize(ref this.secondaryQueues, count);
                this.secondaryQueues[lastIndex] = (new Queue<Message<TSecondary>>(), null);
                Array.Resize(ref this.lastResults, count);
                Array.Resize(ref this.lastValues, count);
                this.defaultSecondarySet = Enumerable.Range(0, count);
                return newInput;
            }
            finally
            {
                syncContext.Release();
            }
        }

        private void ReceivePrimary(TPrimary message, Envelope e)
        {
            var clone = message.DeepClone(this.InPrimary.Recycler);
            this.primaryQueue.Enqueue(Message.Create(clone, e));
            this.Publish();
        }

        private void ReceiveSecondary(int id, TSecondary message, Envelope e)
        {
            var clone = message.DeepClone(this.InSecondaries[id].Recycler);
            this.secondaryQueues[id].Queue.Enqueue(Message.Create(clone, e));
            this.Publish();
        }

        private void SecondaryClosed(int index, DateTime closedOriginatingTime)
        {
            this.secondaryQueues[index].ClosedOriginatingTime = closedOriginatingTime;
            this.Publish();
        }

        private void Publish()
        {
            while (this.primaryQueue.Count > 0)
            {
                var primary = this.primaryQueue.Peek();
                bool ready = true;
                var secondarySet = (this.secondarySelector != null) ? this.secondarySelector(primary.Data) : this.defaultSecondarySet;
                foreach (var secondary in secondarySet)
                {
                    var secondaryQueue = this.secondaryQueues[secondary];
                    var interpolationResult = this.interpolator.Interpolate(primary.OriginatingTime, secondaryQueue.Queue, secondaryQueue.ClosedOriginatingTime);
                    if (interpolationResult.Type == InterpolationResultType.InsufficientData)
                    {
                        // we need to wait longer
                        return;
                    }

                    this.lastResults[secondary] = interpolationResult;
                    this.lastValues[secondary] = interpolationResult.Value;
                    ready = ready && interpolationResult.Type == InterpolationResultType.Created;
                }

                // if all secondaries have an interpolated value, publish the resulting set
                if (ready)
                {
                    // publish
                    var result = this.outputCreator(primary.Data, this.lastValues);
                    this.Out.Post(result, primary.OriginatingTime);
                    Array.Clear(this.lastValues, 0, this.lastValues.Length);
                }

                // if we got here, all secondaries either successfully interpolated a value, or we have confirmation that they will never be able to interpolate
                foreach (var secondary in secondarySet)
                {
                    var secondaryQueue = this.secondaryQueues[secondary];

                    // clear the secondary queue as needed
                    while (secondaryQueue.Queue.Count != 0 && secondaryQueue.Queue.Peek().OriginatingTime < this.lastResults[secondary].ObsoleteTime)
                    {
                        this.InSecondaries[secondary].Recycle(secondaryQueue.Queue.Dequeue());
                    }
                }

                Array.Clear(this.lastResults, 0, this.lastResults.Length);
                this.InPrimary.Recycle(primary);
                this.primaryQueue.Dequeue();
            }
        }
    }
}