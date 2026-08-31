// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Psi.Common;

    /// <summary>
    /// UDP broadcast network transport (one-to-many).
    /// </summary>
    /// <remarks>
    /// Broadcasts messages to all receivers on the subnet via UDP broadcast.
    /// Requires a fixed port known in advance by all participants.
    /// </remarks>
    internal class UdpBroadcastTransport : ITransport
    {
        private readonly IPEndPoint broadcastEndPoint;
        private UdpClient client;
        private int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpBroadcastTransport"/> class.
        /// </summary>
        /// <param name="port">Fixed broadcast port (must be identical on all senders and receivers).</param>
        /// <param name="broadcastAddress">Broadcast IP address (default 255.255.255.255).</param>
        public UdpBroadcastTransport(int port = 11412, string broadcastAddress = "255.255.255.255")
        {
            this.port = port;
            this.broadcastEndPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), port);
        }

        /// <inheritdoc/>
        public TransportKind Transport => TransportKind.UdpBroadcast;

        /// <inheritdoc/>
        /// <remarks>
        /// Receiver side: binds the socket to the fixed broadcast port.
        /// Must be called after <see cref="ReadTransportParams"/> if the port is negotiated.
        /// </remarks>
        public void StartListening()
        {
            this.client = new UdpClient();
            this.client.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);
            this.client.Client.Bind(new IPEndPoint(IPAddress.Any, this.port));
            this.client.EnableBroadcast = true;
        }

        /// <inheritdoc/>
        public void WriteTransportParams(BufferWriter writer)
        {
            writer.Write(this.port);
        }

        /// <inheritdoc/>
        public void ReadTransportParams(BufferReader reader)
        {
            this.port = reader.ReadInt32();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns a client in receive mode on the already-bound broadcast socket.
        /// </remarks>
        public ITransportClient AcceptClient()
        {
            return new UdpBroadcastTransportClient(this.client, isReceiver: true);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Sender side: the <paramref name="host"/> parameter is ignored —
        /// the destination is always <see cref="broadcastEndPoint"/>.
        /// </remarks>
        public ITransportClient Connect(string host)
        {
            this.client = new UdpClient();
            this.client.EnableBroadcast = true;
            this.client.Connect(this.broadcastEndPoint);
            return new UdpBroadcastTransportClient(this.client, isReceiver: false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.client?.Dispose();
            this.client = null;
        }

        internal class UdpBroadcastTransportClient : ITransportClient
        {
            private const int MaxDatagramSize = (64 * 1024) - DataChunker.HeaderSize;

            private readonly UdpClient client;
            private readonly bool isReceiver;
            private readonly DataChunker chunker;
            private readonly DataUnchunker unchunker;
            private readonly BufferWriter writer = new BufferWriter(0);

            private long sendId = 0;
            private Guid sessionFilter = Guid.Empty;

            public UdpBroadcastTransportClient(UdpClient client, bool isReceiver)
            {
                this.client = client;
                this.isReceiver = isReceiver;
                this.chunker = new DataChunker(MaxDatagramSize);
                this.unchunker = new DataUnchunker(
                    MaxDatagramSize,
                    x => Trace.WriteLine($"UdpBroadcastTransport Chunkset: {x}"),
                    x => Trace.WriteLine($"UdpBroadcastTransport Abandoned: {x}"));
            }

            /// <inheritdoc/>
            /// <remarks>
            /// Receiver: reads the session GUID from the incoming broadcast and stores it
            /// in <see cref="sessionFilter"/> to filter subsequent messages.
            /// Unlike <see cref="UdpTransport"/>, no <c>Connect(remoteEp)</c> is performed —
            /// the socket must remain open to all sources.
            /// </remarks>
            public Guid ReadSessionId()
            {
                if (!this.isReceiver)
                {
                    throw new InvalidOperationException("ReadSessionId must be called on the receiver side.");
                }

                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data;

                do
                {
                    data = this.client.Receive(ref remoteEp);
                }
                while (!this.unchunker.Receive(data) || this.unchunker.Length != 16);

                var bytes = new byte[16];
                Array.Copy(this.unchunker.Payload, bytes, 16);
                this.sessionFilter = new Guid(bytes);
                return this.sessionFilter;
            }

            /// <inheritdoc/>
            public void WriteSessionId(Guid id)
            {
                Remoting.Transport.WriteSessionId(id, this.Write);
                this.sessionFilter = id;
            }

            /// <inheritdoc/>
            /// <remarks>
            /// Receiver: reads and reassembles chunks, then filters by session ID.
            /// Packets whose <see cref="Envelope.SourceId"/> does not match
            /// <see cref="sessionFilter"/> are discarded.
            /// </remarks>
            public Tuple<Envelope, byte[]> ReadMessage()
            {
                Envelope envelope;
                byte[] buffer;

                do
                {
                    var data = this.Read();
                    var reader = new BufferReader(data.Item1, data.Item2);
                    envelope = reader.ReadEnvelope();
                    var length = reader.ReadInt32();
                    buffer = new byte[length];
                    reader.Read(buffer, length);
                }
                while (this.sessionFilter != Guid.Empty &&
                       envelope.SourceId != this.sessionFilter.GetHashCode());

                return Tuple.Create(envelope, buffer);
            }

            /// <inheritdoc/>
            public void WriteMessage(Envelope envelope, byte[] message)
            {
                Remoting.Transport.WriteMessage(envelope, message, this.writer, this.Write);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                this.client?.Dispose();
            }

            private void Write(byte[] buffer, int size)
            {
                foreach (var chunk in this.chunker.GetChunks(this.sendId++, buffer, size))
                {
                    this.client.Send(chunk.Item1, chunk.Item2);
                }
            }

            private Tuple<byte[], int> Read()
            {
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);

                while (!this.unchunker.Receive(this.client.Receive(ref remoteEp)))
                {
                }

                return Tuple.Create(this.unchunker.Payload, this.unchunker.Length);
            }
        }
    }
}
