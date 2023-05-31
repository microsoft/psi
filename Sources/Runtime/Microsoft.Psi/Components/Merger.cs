// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Combines the input messages from multiple inputs; invoking given lambda for each.
    /// </summary>
    /// <typeparam name="TIn">The message type.</typeparam>
    /// <typeparam name="TKey">The key type to use to identify the inputs.</typeparam>
    public class Merger<TIn, TKey>
    {
        private readonly Dictionary<TKey, Receiver<TIn>> inputs = new Dictionary<TKey, Receiver<TIn>>();
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Action<TKey, Message<TIn>> action;
        private readonly object syncRoot = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="Merger{TIn, TKey}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="action">Action invoked for each key/message.</param>
        /// <param name="name">An optional name for the component.</param>
        public Merger(Pipeline pipeline, Action<TKey, Message<TIn>> action, string name = nameof(Merger<TIn, TKey>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.action = action;
        }

        /// <summary>
        /// Add a key to which a receiver will be mapped.
        /// </summary>
        /// <param name="key">Key to which to map a receiver.</param>
        /// <returns>Receiver having been mapped.</returns>
        public Receiver<TIn> Add(TKey key)
        {
            // lock access to the inputs so Merger works concurrently
            lock (this.syncRoot)
            {
                if (this.inputs.ContainsKey(key))
                {
                    throw new InvalidOperationException($"An input for this key {key} has already been added.");
                }

                return this.inputs[key] = this.pipeline.CreateReceiver<TIn>(this, m => this.action(key, m), key.ToString());
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}