// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// A pass-through component, to be used when writing meta-components.
    /// The encapsulating component (the meta-component) can use this class instead of a receiver.
    /// This allows the code within the meta-component to attach operators to the output of the connector,
    /// while exposing the input property of the connector as a public receiver.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    public class Connector<T> : IProducer<T>, IConsumer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Connector{T}"/> class.
        /// </summary>
        /// <param name="from">The pipeline from which to attach.</param>
        /// <param name="to">The pipeline to which to attach.</param>
        /// <param name="name">The name of the connector</param>
        /// <remarks>Composite components may use the `Connector` to bridge `from` a parent pipeline into a `subpipeline` and/or `from` a subpipeline back out `to` the parent.</remarks>
        public Connector(Pipeline from, Pipeline to, string name)
        {
            this.Out = to.CreateEmitter<T>(this, name);
            this.In = from.CreateReceiver<T>(this, (m, e) => this.Out.Post(m, e.OriginatingTime), name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Connector{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="name">The name of the connector</param>
        public Connector(Pipeline pipeline, string name)
            : this(pipeline, pipeline, name)
        {
        }

        /// <summary>
        /// Gets the connector input.
        /// </summary>
        public Receiver<T> In { get; }

        /// <summary>
        /// Gets the connector output.
        /// </summary>
        public Emitter<T> Out { get; }
    }
}