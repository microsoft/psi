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
    public class NetMQWriter : IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly IFormatSerializer serializer;

        private PublisherSocket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQWriter"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs</param>
        /// <param name="address">Connection string</param>
        /// <param name="serializer">Format serializer with which messages are serialized</param>
        public NetMQWriter(Pipeline pipeline, string address, IFormatSerializer serializer)
        {
            this.pipeline = pipeline;
            this.serializer = serializer;
            this.socket = new PublisherSocket();
            pipeline.PipelineRun += (s, e) => this.socket.Bind(address);
        }

        /// <summary>
        /// Add topic receiver.
        /// </summary>
        /// <param name="topic">Topic name</param>
        /// <typeparam name="U">Message type</typeparam>
        /// <returns>Receiver to which to pipe messages.</returns>
        public Receiver<U> AddTopic<U>(string topic)
        {
            return this.pipeline.CreateReceiver<U>(this, (m, e) => this.Receive<U>(m, e, topic), topic);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
                this.socket = null;
            }
        }

        private void Receive<U>(U message, Envelope envelope, string topic)
        {
            var (bytes, index, length) = this.serializer.SerializeMessage(message, envelope.OriginatingTime);
            if (index != 0)
            {
                var slice = new byte[length];
                Array.Copy(bytes, index, slice, 0, length);
                bytes = slice;
            }

            this.socket.SendMoreFrame(topic).SendFrame(bytes, length);
        }
    }
}
