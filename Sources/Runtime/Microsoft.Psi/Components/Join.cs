// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Performs a merge between a pair of streams
    /// </summary>
    /// <typeparam name="TPrimary">The type the messages on the primary stream</typeparam>
    /// <typeparam name="TSecondary">The type messages on the secondary stream</typeparam>
    /// <typeparam name="TOut">The type of output message</typeparam>
    public class Join<TPrimary, TSecondary, TOut> : IProducer<TOut>
    {
        private readonly Queue<Message<TPrimary>> primaryQueue = new Queue<Message<TPrimary>>(); // to be paired
        private readonly Match.Interpolator<TSecondary> interpolator;
        private readonly Func<TPrimary, TSecondary[], TOut> outputCreator;
        private readonly Func<TPrimary, IEnumerable<int>> secondarySelector;
        private Queue<Message<TSecondary>>[] secondaryQueues;
        private Receiver<TSecondary>[] inSecondaries;
        private IEnumerable<int> defaultSecondarySet;
        private Pipeline pipeline;

        // temp buffers
        private TSecondary[] lastValues;
        private MatchResult<TSecondary>[] lastResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="Join{TPrimary, TSecondary, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="interpolator">Interpolator with which to join.</param>
        /// <param name="outputCreator">Mapping function from message pair to output.</param>
        /// <param name="secondaryCount">Number of secondary streams.</param>
        /// <param name="secondarySelector">Selector function mapping primary messages to secondary stream indices.</param>
        public Join(
            Pipeline pipeline,
            Match.Interpolator<TSecondary> interpolator,
            Func<TPrimary, TSecondary[], TOut> outputCreator,
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
            this.secondaryQueues = new Queue<Message<TSecondary>>[secondaryCount];
            this.lastValues = new TSecondary[secondaryCount];
            this.lastResults = new MatchResult<TSecondary>[secondaryCount];
            this.defaultSecondarySet = Enumerable.Range(0, secondaryCount);
            for (int i = 0; i < secondaryCount; i++)
            {
                this.secondaryQueues[i] = new Queue<Message<TSecondary>>();
                var id = i; // needed to make the closure below byval
                var receiver = pipeline.CreateReceiver<TSecondary>(this, (d, e) => this.ReceiveSecondary(id, d, e), "InSecondary" + i);
                this.inSecondaries[i] = receiver;
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
            var lastIndex = this.inSecondaries.Length;
            var count = lastIndex + 1;
            Array.Resize(ref this.inSecondaries, count);
            var newInput = this.inSecondaries[lastIndex] = this.pipeline.CreateReceiver<TSecondary>(this, (d, e) => this.ReceiveSecondary(lastIndex, d, e), "InSecondary" + lastIndex);
            Array.Resize(ref this.secondaryQueues, count);
            this.secondaryQueues[lastIndex] = new Queue<Message<TSecondary>>();
            Array.Resize(ref this.lastResults, count);
            Array.Resize(ref this.lastValues, count);
            this.defaultSecondarySet = Enumerable.Range(0, count);
            return newInput;
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
            this.secondaryQueues[id].Enqueue(Message.Create(clone, e));
            this.Publish();
        }

        private void Publish()
        {
            while (this.primaryQueue.Count > 0)
            {
                var primary = this.primaryQueue.Peek();
                bool ready = true;
                var secondarySet = (this.secondarySelector != null) ? this.secondarySelector(primary.Data) : this.defaultSecondarySet;
                foreach (var iSecondary in secondarySet)
                {
                    var secondaryQueue = this.secondaryQueues[iSecondary];
                    var matchResult = this.interpolator.Match(primary.OriginatingTime, secondaryQueue);
                    if (matchResult.Type == MatchResultType.InsufficientData)
                    {
                        // we need to wait more
                        return;
                    }

                    this.lastResults[iSecondary] = matchResult;
                    this.lastValues[iSecondary] = matchResult.Value;
                    ready = ready && matchResult.Type == MatchResultType.Created;
                }

                // if all secondaries match a value, publish the resulting set
                if (ready)
                {
                    // publish
                    var result = this.outputCreator(primary.Data, this.lastValues);
                    this.Out.Post(result, primary.OriginatingTime);
                    Array.Clear(this.lastValues, 0, this.lastValues.Length);
                }

                // if we got here, all secondaries either successfully match a value, or we have confirmation that they will never be able to match it
                foreach (var iSecondary in secondarySet)
                {
                    var secondaryQueue = this.secondaryQueues[iSecondary];

                    // clear the secondary queue as needed
                    while (secondaryQueue.Peek().OriginatingTime < this.lastResults[iSecondary].ObsoleteTime)
                    {
                        this.InSecondaries[iSecondary].Recycle(secondaryQueue.Dequeue());
                    }
                }

                Array.Clear(this.lastResults, 0, this.lastResults.Length);
                this.InPrimary.Recycle(primary);
                this.primaryQueue.Dequeue();
            }
        }
    }
}