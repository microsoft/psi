// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;
    using NetMQ;
    using NetMQ.Sockets;

    /// <summary>
    /// NetMQ (ZeroMQ) subscriber component.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class NetMQSource<T> : IProducer<T>, ISourceComponent, IDisposable
    {
        private readonly string topic;
        private readonly string address;
        private readonly IFormatDeserializer deserializer;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly bool useSourceOriginatingTimes;

        private SubscriberSocket socket;
        private NetMQPoller poller;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="topic">Topic name.</param>
        /// <param name="address">Connection string.</param>
        /// <param name="deserializer">Format deserializer with which messages are deserialized.</param>
        /// <param name="useSourceOriginatingTimes">Flag indicating whether or not to post with originating times received over the socket. If false, we ignore them and instead use pipeline's current time.</param>
        /// <param name="name">An optional name for the component.</param>
        public NetMQSource(Pipeline pipeline, string topic, string address, IFormatDeserializer deserializer, bool useSourceOriginatingTimes = true, string name = nameof(NetMQSource<T>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.topic = topic;
            this.address = address;
            this.deserializer = deserializer;
            this.Out = pipeline.CreateEmitter<T>(this, topic);
        }

        /// <inheritdoc />
        public Emitter<T> Out { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Stop();
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.socket = new SubscriberSocket();
            this.socket.Connect(this.address);
            this.socket.Subscribe(this.topic);
            this.socket.ReceiveReady += this.ReceiveReady;
            this.poller = new NetMQPoller();
            this.poller.Add(this.socket);
            this.poller.RunAsync();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Stop();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Stop()
        {
            if (this.socket != null)
            {
                this.poller.Dispose();
                this.socket.Dispose();
                this.socket = null;
            }
        }

        private void ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var frames = new List<byte[]>();
            while (this.socket.TryReceiveMultipartBytes(ref frames, 2))
            {
                var receivedTopic = System.Text.Encoding.Default.GetString(frames[0]);
                if (receivedTopic != this.topic)
                {
                    throw new Exception($"Unexpected topic name received in NetMQSource. Expected {this.topic} but received {receivedTopic}");
                }

                if (frames.Count < 2)
                {
                    throw new Exception($"No payload message received for topic: {this.topic}");
                }

                if (frames.Count > 2)
                {
                    throw new Exception($"Multiple interleaved messages received on topic: {this.topic}. Is the sender on the other side sending messages on multiple threads? You may need to add a lock over there.");
                }

                var (message, originatingTime) = this.deserializer.DeserializeMessage(frames[1], 0, frames[1].Length);
                this.Out.Post(message, this.useSourceOriginatingTimes ? originatingTime : this.pipeline.GetCurrentTime());
            }
        }
    }
}
