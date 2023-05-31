// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Psi.Common;

    /// <summary>
    /// UDP network transport.
    /// </summary>
    internal class UdpTransport : ITransport
    {
        private UdpClient client;
        private int port;

        /// <summary>
        /// Gets kind of network transport.
        /// </summary>
        public TransportKind Transport
        {
            get { return TransportKind.Udp; }
        }

        /// <summary>
        /// Start listening on IP port.
        /// </summary>
        public void StartListening()
        {
            this.client = new UdpClient(0);
            this.port = ((IPEndPoint)this.client.Client.LocalEndPoint).Port;
        }

        /// <summary>
        /// Write transport-specific parameter (port number).
        /// </summary>
        /// <param name="writer">Buffer writer to which to write.</param>
        public void WriteTransportParams(BufferWriter writer)
        {
            writer.Write(this.port);
        }

        /// <summary>
        /// Read transport-specific parameter (port number).
        /// </summary>
        /// <param name="reader">Buffer reader from which to read.</param>
        public void ReadTransportParams(BufferReader reader)
        {
            this.port = reader.ReadInt32();
        }

        /// <summary>
        /// Accept new UDP client.
        /// </summary>
        /// <returns>Accepted client.</returns>
        public ITransportClient AcceptClient()
        {
            return new UdpTransportClient(this.client);
        }

        /// <summary>
        /// Connect to remote host.
        /// </summary>
        /// <param name="host">Host name to which to connect.</param>
        /// <returns>Connected client.</returns>
        public ITransportClient Connect(string host)
        {
            this.client = new UdpClient();
            this.client.Connect(host, this.port);
            return new UdpTransportClient(this.client);
        }

        /// <summary>
        /// Dispose of UDP transport.
        /// </summary>
        public void Dispose()
        {
            this.client.Dispose();
            this.client = null;
        }

        internal class UdpTransportClient : ITransportClient
        {
            private UdpClient client;
            private DataChunker chunker;
            private DataUnchunker unchunker;
            private long id = 0;
            private BufferWriter writer = new BufferWriter(0);

            public UdpTransportClient(UdpClient client)
            {
                const int maxDatagramSize = (64 * 1024) - DataChunker.HeaderSize; // see https://en.wikipedia.org/wiki/User_Datagram_Protocol
                this.client = client;
                this.chunker = new DataChunker(maxDatagramSize);
                this.unchunker = new DataUnchunker(maxDatagramSize, x => Trace.WriteLine($"UdpTransport Chunkset: {x}"), x => Trace.WriteLine($"UdpTransport Abandoned: {x}"));
            }

            public Guid ReadSessionId()
            {
                var endpoint = (IPEndPoint)this.client.Client.LocalEndPoint;
                var data = this.client.Receive(ref endpoint);
                this.client.Connect(endpoint);
                if (!this.unchunker.Receive(data) || this.unchunker.Length != 16)
                {
                    throw new IOException($"Expected single session ID packet");
                }

                var bytes = new byte[16];
                Array.Copy(this.unchunker.Payload, bytes, bytes.Length);

                return new Guid(bytes);
            }

            public void WriteSessionId(Guid id)
            {
                Remoting.Transport.WriteSessionId(id, this.Write);
            }

            public Tuple<Envelope, byte[]> ReadMessage()
            {
                var data = this.Read();
                var reader = new BufferReader(data.Item1, data.Item2);
                var envelope = reader.ReadEnvelope();
                var length = reader.ReadInt32();
                var buffer = new byte[length];
                reader.Read(buffer, length);
                return Tuple.Create(envelope, buffer);
            }

            public void WriteMessage(Envelope envelope, byte[] message)
            {
                Remoting.Transport.WriteMessage(envelope, message, this.writer, this.Write);
            }

            public void Dispose()
            {
                this.client.Dispose();
                this.client = null;
            }

            private void Write(byte[] buffer, int size)
            {
                foreach (var chunk in this.chunker.GetChunks(this.id++, buffer, size))
                {
                    var data = chunk.Item1;
                    var len = chunk.Item2;
                    this.client.Send(data, len);
                }
            }

            private Tuple<byte[], int> Read()
            {
                var endpoint = (IPEndPoint)this.client.Client.LocalEndPoint;

                while (!this.unchunker.Receive(this.client.Receive(ref endpoint)))
                {
                }

                return Tuple.Create(this.unchunker.Payload, this.unchunker.Length);
            }
        }
    }
}