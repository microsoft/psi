// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements an index-based windowing component.
    /// </summary>
    /// <typeparam name="TInput">The type of input messages.</typeparam>
    /// <typeparam name="TOutput">The type of output messages.</typeparam>
    public class RelativeIndexWindow<TInput, TOutput> : ConsumerProducer<TInput, TOutput>
    {
        private readonly IRecyclingPool<Message<TInput>> recycler = RecyclingPool.Create<Message<TInput>>();
        private readonly int bufferSize;
        private readonly int windowSize;
        private readonly int trimLeft;
        private readonly int trimRight;
        private readonly Func<IEnumerable<Message<TInput>>, TOutput> selector;

        private readonly int anchorMessageIndex = 0;
        private readonly Queue<Message<TInput>> buffer = new Queue<Message<TInput>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeIndexWindow{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="relativeIndexInterval">The relative index interval over which to gather messages.</param>
        /// <param name="selector">Select output message from collected window of input messages.</param>
        /// <param name="name">An optional name for the component.</param>
        public RelativeIndexWindow(Pipeline pipeline, IntInterval relativeIndexInterval, Func<IEnumerable<Message<TInput>>, TOutput> selector, string name = nameof(RelativeIndexWindow<TInput, TOutput>))
            : base(pipeline, name)
        {
            if (relativeIndexInterval.IsNegative)
            {
                // normalize to positive form
                relativeIndexInterval = new IntInterval(relativeIndexInterval.RightEndpoint, relativeIndexInterval.LeftEndpoint);
            }

            var left = relativeIndexInterval.LeftEndpoint;
            var right = relativeIndexInterval.RightEndpoint;
            this.trimLeft = left.Point > 0 ? left.Point : 0;
            this.trimRight = right.Point < 0 ? -right.Point : 0;
            this.windowSize = relativeIndexInterval.Span + 1 - (left.Inclusive ? 0 : 1) - (right.Inclusive ? 0 : 1);
            this.bufferSize = this.windowSize + this.trimLeft + this.trimRight;
            this.anchorMessageIndex = left.Point == 0 ? 0 : Math.Abs(left.Point) - (left.Inclusive ? 0 : 1);
            this.selector = selector;
        }

        /// <inheritdoc />
        protected override void Receive(TInput value, Envelope envelope)
        {
            this.buffer.Enqueue(new Message<TInput>(value, envelope).DeepClone(this.recycler)); // clone and add the new message
            if (this.buffer.Count > this.bufferSize)
            {
                var free = this.buffer.Dequeue();
                this.recycler?.Recycle(free);
            }

            // emit buffers of windowSize (otherwise continue accumulating)
            if (this.buffer.Count == this.bufferSize)
            {
                var messages = this.buffer.Skip(this.trimLeft).Take(this.windowSize);
                this.Out.Post(this.selector(messages), this.buffer.Skip(this.anchorMessageIndex).First().OriginatingTime);
            }
        }
    }
}