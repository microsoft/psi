// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.Psi.Common;

    /// <summary>
    /// TCP network transport.
    /// </summary>
    internal class TcpTransport : ITransport
    {
        private TcpListener listener;
        private int port;

        /// <summary>
        /// Gets kind of network transport.
        /// </summary>
        public TransportKind Transport
        {
            get { return TransportKind.Tcp; }
        }

        /// <summary>
        /// Start listening on IP port.
        /// </summary>
        public void StartListening()
        {
            this.listener = new TcpListener(IPAddress.Any, 0);
            this.listener.Start();
            this.port = ((IPEndPoint)this.listener.LocalEndpoint).Port;
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
        /// Accept new TCP client.
        /// </summary>
        /// <returns>Accepted client.</returns>
        public ITransportClient AcceptClient()
        {
            return new TcpTransportClient(this.listener.AcceptTcpClient());
        }

        /// <summary>
        /// Connect to remote host.
        /// </summary>
        /// <param name="host">Host name to which to connect.</param>
        /// <returns>Connected client.</returns>
        public ITransportClient Connect(string host)
        {
            TcpClient client = new TcpClient();
            client.Connect(host, this.port);
            return new TcpTransportClient(client);
        }

        /// <summary>
        /// Dispose of TCP transport.
        /// </summary>
        public void Dispose()
        {
            this.listener.Stop();
        }

        internal class TcpTransportClient : ITransportClient
        {
            private readonly NetworkStream stream;
            private TcpClient client;
            private BufferReader reader = new BufferReader();
            private BufferWriter writer = new BufferWriter(0);

            public TcpTransportClient(TcpClient client)
            {
                this.client = client;
                this.stream = client.GetStream();
            }

            public Guid ReadSessionId()
            {
                return Remoting.Transport.ReadSessionId(this.stream);
            }

            public void WriteSessionId(Guid id)
            {
                Remoting.Transport.WriteSessionId(id, this.Write);
            }

            public Tuple<Envelope, byte[]> ReadMessage()
            {
                return Remoting.Transport.ReadMessage(this.reader, this.stream);
            }

            public void WriteMessage(Envelope envelope, byte[] message)
            {
                Remoting.Transport.WriteMessage(envelope, message, this.writer, this.Write);
            }

            public void Dispose()
            {
                this.stream.Close();
                this.client.Dispose();
                this.client = null;
            }

            private void Write(byte[] buffer, int size)
            {
                this.stream.Write(buffer, 0, size);
                this.stream.Flush();
            }

            private void Read(byte[] buffer, int size)
            {
                Remoting.Transport.Read(buffer, size, this.stream);
            }
        }
    }
}
