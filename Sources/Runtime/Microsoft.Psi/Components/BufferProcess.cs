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
    public class BufferProcess<TInput, TOutput> : ConsumerProducer<TInput, TOutput>
    {
        private readonly Queue<Message<TInput>> buffer = new Queue<Message<TInput>>();
        private readonly IRecyclingPool<Message<TInput>> recycler = RecyclingPool.Create<Message<TInput>>();
        private readonly int maxSize;

        private Func<Message<TInput>, DateTime, bool> removeCondition;
        private Action<IEnumerable<Message<TInput>>, bool, Emitter<TOutput>> processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferProcess{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="removeCondition">Predicate function determining removal condition.</param>
        /// <param name="processor">Processor function that generates the output when necessary by analyzing the buffer.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        public BufferProcess(Pipeline pipeline, Func<Message<TInput>, DateTime, bool> removeCondition, Action<IEnumerable<Message<TInput>>, bool, Emitter<TOutput>> processor, int maxSize = int.MaxValue)
            : base(pipeline)
        {
            this.maxSize = maxSize;
            pipeline.RegisterPipelineFinalHandler(this, this.StreamClosed);
            this.InitializeLambdas(removeCondition, processor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferProcess{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="processor">Processor function that generates the output when necessary by analyzing the buffer.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        /// <remarks>
        /// The `processor` is given the message buffer and an emitter on which to (optionally) post.
        /// Along with the message buffer and an emitter on which to (optionally) post, the `processor` is given a flag indicating whether
        /// the stream is closing, this is the final call and no future messages are expected.
        /// </remarks>
        public BufferProcess(Pipeline pipeline, Action<IEnumerable<Message<TInput>>, bool, Emitter<TOutput>> processor, int maxSize = int.MaxValue)
            : this(pipeline, null, processor, maxSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferProcess{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        /// <remarks>
        /// This protected constructor leaves the `removeCondition` and `processor` uninitialized.
        /// Subclasses should call `InitializeLambdas(...)`.
        /// This is because of the inconvenience of not being able to refer to instance methods in a `base(...)` call
        /// </remarks>
        protected BufferProcess(Pipeline pipeline, int maxSize = int.MaxValue)
            : this(pipeline, null, null, maxSize)
        {
        }

        /// <summary>
        /// Initializes lambda functions driving behavior.
        /// </summary>
        /// <param name="removeCondition">Predicate function determining removal condition.</param>
        /// <param name="processor">Processor function that generates the output when necessary by analyzing the buffer.</param>
        protected void InitializeLambdas(Func<Message<TInput>, DateTime, bool> removeCondition, Action<IEnumerable<Message<TInput>>, bool, Emitter<TOutput>> processor)
        {
            this.removeCondition = removeCondition;
            this.processor = processor;
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
                this.DequeueBuffer();
            }

            this.ProcessRemoval(message.OriginatingTime);

            // copy the queue to the output buffer and post it
            this.processor(this.buffer, false, this.Out);
        }

        private void DequeueBuffer()
        {
            var free = this.buffer.Dequeue();
            this.recycler?.Recycle(free);
        }

        private void ProcessRemoval(DateTime originatingTime)
        {
            if (this.removeCondition != null)
            {
                while (this.buffer.Count > 0 &&
                       this.removeCondition(this.buffer.Peek(), originatingTime))
                {
                    this.DequeueBuffer();
                }
            }
        }

        private void StreamClosed()
        {
            // give the processor an opportunity now with `final` flag set
            while (this.buffer.Count > 0)
            {
                this.processor(this.buffer, true, this.Out);
                this.ProcessRemoval(DateTime.MaxValue);
                if (this.buffer.Count > 0)
                {
                    // continue processing with trailing buffer
                    this.DequeueBuffer();
                }
            }
        }
    }
}