// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Sends the input message to at most one of the dynamic outputs, selected using the specified output selector.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TKey">The type of key to use when identifying the correct output.</typeparam>
    public class Splitter<TIn, TKey> : IConsumer<TIn>
    {
        private readonly Dictionary<TKey, Emitter<TIn>> outputs = new ();
        private readonly Func<TIn, Envelope, TKey> outputSelector;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Receiver<TIn> input;

        /// <summary>
        /// Initializes a new instance of the <see cref="Splitter{TIn, TKey}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="outputSelector">Selector function identifying the output.</param>
        /// <param name="name">An optional name for the component.</param>
        public Splitter(Pipeline pipeline, Func<TIn, Envelope, TKey> outputSelector, string name = nameof(Splitter<TIn, TKey>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.outputSelector = outputSelector;
            this.input = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In => this.input;

        /// <summary>
        /// Add emitter mapping.
        /// </summary>
        /// <param name="key">Key to which to map emitter.</param>
        /// <returns>Emitter having been mapped.</returns>
        public Emitter<TIn> Add(TKey key)
        {
            if (this.outputs.ContainsKey(key))
            {
                throw new InvalidOperationException($"An output for this key {key} has already been added.");
            }

            return this.outputs[key] = this.pipeline.CreateEmitter<TIn>(this, key.ToString());
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive(TIn message, Envelope e)
        {
            var key = this.outputSelector(message, e);
            if (this.outputs.ContainsKey(key))
            {
                this.outputs[key].Post(message, e.OriginatingTime);
            }
        }
    }
}