// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Combines the input messages from multiple inputs into a single output.
    /// </summary>
    /// <typeparam name="TIn">The message type</typeparam>
    /// <typeparam name="TKey">The key type to use to identify the inputs</typeparam>
    public class Merger<TIn, TKey> : IProducer<ValueTuple<TKey, Message<TIn>>>
    {
        private readonly Dictionary<TKey, Receiver<TIn>> inputs = new Dictionary<TKey, Receiver<TIn>>();
        private readonly Pipeline pipeline;
        private readonly Emitter<ValueTuple<TKey, Message<TIn>>> output;

        /// <summary>
        /// Initializes a new instance of the <see cref="Merger{TIn, TKey}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        public Merger(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.output = pipeline.CreateEmitter<ValueTuple<TKey, Message<TIn>>>(this, nameof(this.Out));
        }

        /// <inheritdoc />
        public Emitter<ValueTuple<TKey, Message<TIn>>> Out => this.output;

        /// <summary>
        /// Add a key to which a receiver will be mapped.
        /// </summary>
        /// <param name="key">Key to which to map a receiver.</param>
        /// <returns>Receiver having been mapped.</returns>
        public Receiver<TIn> Add(TKey key)
        {
            if (this.inputs.ContainsKey(key))
            {
                throw new InvalidOperationException($"An input for this key {key} has already been addded.");
            }

            return this.inputs[key] = this.pipeline.CreateReceiver<TIn>(this, m => this.Receive(key, m), key.ToString());
        }

        private void Receive(TKey key, Message<TIn> message)
        {
            // to avoid ordering issues, we post the whole message with a new envelope
            this.output.Post(ValueTuple.Create(key, message), this.pipeline.GetCurrentTime());
        }
    }
}