// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implements a buffering component. The component adds each incoming message to the buffer and trims the buffer
    /// by evaluating a remove condition.
    /// </summary>
    /// <typeparam name="TInput">The type of input messages</typeparam>
    /// <typeparam name="TOutput">The type of output messages</typeparam>
    public class BufferSelect<TInput, TOutput> : ConsumerProducer<TInput, TOutput>
    {
        private readonly Queue<Message<TInput>> buffer = new Queue<Message<TInput>>();
        private readonly IRecyclingPool<Message<TInput>> recycler = RecyclingPool.Create<Message<TInput>>();
        private readonly Func<Message<TInput>, DateTime, bool> removeCondition;
        private readonly Func<IEnumerable<Message<TInput>>, ValueTuple<TOutput, DateTime>> selector;
        private readonly int maxSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferSelect{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="removeCondition">Predicate function determining removal condition.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        public BufferSelect(Pipeline pipeline, Func<Message<TInput>, DateTime, bool> removeCondition, Func<IEnumerable<Message<TInput>>, ValueTuple<TOutput, DateTime>> selector, int maxSize = int.MaxValue)
            : base(pipeline)
        {
            this.removeCondition = removeCondition;
            this.selector = selector;
            this.maxSize = maxSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferSelect{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        public BufferSelect(Pipeline pipeline, Func<IEnumerable<Message<TInput>>, ValueTuple<TOutput, DateTime>> selector, int maxSize)
            : base(pipeline)
        {
            this.removeCondition = null;
            this.selector = selector;
            this.maxSize = maxSize;
        }

        /// <inheritdoc />
        protected override void Receive(TInput value, Envelope envelope)
        {
            // clone and add the new message
            var message = new Message<TInput>(value, envelope).DeepClone(this.recycler);
            this.buffer.Enqueue(message);

            // remove any expired messages
            if (this.buffer.Count > this.maxSize)
            {
                var free = this.buffer.Dequeue();
                this.recycler?.Recycle(free);
            }

            if (this.removeCondition != null)
            {
                while (this.buffer.Count > 0 &&
                       this.removeCondition(this.buffer.Peek(), message.OriginatingTime))
                {
                    var free = this.buffer.Dequeue();
                    this.recycler?.Recycle(free);
                }
            }

            // copy the queue to the output buffer and post it
            var val = this.selector(this.buffer);
            this.Out.Post(val.Item1, val.Item2);
        }
    }
}