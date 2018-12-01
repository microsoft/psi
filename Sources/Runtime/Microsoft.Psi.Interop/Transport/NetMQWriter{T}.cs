// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using Microsoft.Psi.Interop.Serialization;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// NetMQ (ZeroMQ) publisher component.
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    public class NetMQWriter<T> : NetMQWriter, IConsumer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs</param>
        /// <param name="topic">Topic name</param>
        /// <param name="address">Connection string</param>
        /// <param name="serializer">Format serializer with which messages are serialized</param>
        public NetMQWriter(Pipeline pipeline, string topic, string address, IFormatSerializer serializer)
            : base(pipeline, address, serializer)
        {
            this.In = this.AddTopic<T>(topic);
        }

        /// <inheritdoc />
        public Receiver<T> In { get; }
    }
}
