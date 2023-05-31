// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// NetMQ (ZeroMQ) publisher component.
    /// </summary>
    public class NetMQWriter : IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly IFormatSerializer serializer;
        private readonly Dictionary<string, Type> topics = new ();

        private PublisherSocket socket;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQWriter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="address">Connection string.</param>
        /// <param name="serializer">Format serializer with which messages are serialized.</param>
        /// <param name="name">An optional name for the component.</param>
        public NetMQWriter(Pipeline pipeline, string address, IFormatSerializer serializer, string name = nameof(NetMQWriter))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Address = address;
            this.serializer = serializer;
            this.socket = new PublisherSocket();
            pipeline.PipelineRun += (s, e) => this.socket.Bind(address);
        }

        /// <summary>
        /// Gets the connection address string.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Gets the topic names and types being published.
        /// </summary>
        public IEnumerable<(string Name, Type Type)> Topics
        {
            get { return this.topics.Select(x => (x.Key, x.Value)); }
        }

        /// <summary>
        /// Add topic receiver.
        /// </summary>
        /// <param name="topic">Topic name.</param>
        /// <typeparam name="T">Message type.</typeparam>
        /// <returns>Receiver to which to pipe messages.</returns>
        public Receiver<T> AddTopic<T>(string topic)
        {
            this.topics.Add(topic, typeof(T));
            return this.pipeline.CreateReceiver<T>(this, (m, e) => this.Receive(m, e, topic), topic);
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

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive<T>(T message, Envelope envelope, string topic)
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
