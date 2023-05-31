// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Named pipes transport.
    /// </summary>
    internal class NamedPipesTransport : ITransport
    {
        private string name;

        /// <summary>
        /// Gets kind of network transport.
        /// </summary>
        public TransportKind Transport
        {
            get { return TransportKind.NamedPipes; }
        }

        /// <summary>
        /// Start listening (really, allocate GUID used as pipe name).
        /// </summary>
        public void StartListening()
        {
            this.name = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Write transport-specific parameter (pipe name).
        /// </summary>
        /// <param name="writer">Buffer writer to which to write.</param>
        public void WriteTransportParams(BufferWriter writer)
        {
            writer.Write(this.name);
        }

        /// <summary>
        /// Read transport-specific parameter (pipe name).
        /// </summary>
        /// <param name="reader">Buffer reader from which to read.</param>
        public void ReadTransportParams(BufferReader reader)
        {
            this.name = reader.ReadString();
        }

        /// <summary>
        /// Accept new named pipes client.
        /// </summary>
        /// <returns>Accepted client.</returns>
        public ITransportClient AcceptClient()
        {
            var server = new NamedPipeServerStream(this.name, PipeDirection.InOut);
            server.WaitForConnection();
            return new NamedPipesTransportClient(server);
        }

        /// <summary>
        /// Connect to remote host.
        /// </summary>
        /// <param name="host">Host name to which to connect.</param>
        /// <returns>Connected client.</returns>
        public ITransportClient Connect(string host)
        {
            var client = new NamedPipeClientStream(host, this.name, PipeDirection.InOut);
            client.Connect();
            client.ReadMode = PipeTransmissionMode.Message;
            return new NamedPipesTransportClient(client);
        }

        /// <summary>
        /// Dispose of named pipes transport.
        /// </summary>
        public void Dispose()
        {
        }

        internal class NamedPipesTransportClient : ITransportClient
        {
            private Stream stream;
            private BufferReader reader = new BufferReader();
            private BufferWriter writer = new BufferWriter(0);

            public NamedPipesTransportClient(Stream stream)
            {
                this.stream = stream;
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
                this.stream.Dispose();
                this.stream = null;
            }

            private void Write(byte[] buffer, int size)
            {
                const int maxPacketSize = 64 * 1024;
                var p = 0;
                do
                {
                    var s = Math.Min(maxPacketSize, size - p);
                    this.stream.Write(buffer, p, s);
                    p += maxPacketSize;
                }
                while (p < size);
                this.stream.Flush();
            }

            private void Read(byte[] buffer, int size)
            {
                Remoting.Transport.Read(buffer, size, this.stream);
            }
        }
    }
}