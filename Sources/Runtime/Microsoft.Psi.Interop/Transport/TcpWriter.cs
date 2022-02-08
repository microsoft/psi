// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Component that serializes and writes messages to a remote server over TCP.
    /// </summary>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class TcpWriter<T> : IConsumer<T>, IDisposable
    {
        private readonly IFormatSerializer serializer;

        private TcpListener listener;
        private NetworkStream networkStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpWriter{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">Name by which to refer to the data stream.</param>
        /// <param name="port">The connection port.</param>
        /// <param name="serializer">The serializer to use to serialize messages.</param>
        public TcpWriter(Pipeline pipeline, string name, int port, IFormatSerializer serializer)
        {
            this.serializer = serializer;
            this.Name = name;
            this.Port = port;
            this.In = pipeline.CreateReceiver<T>(this, this.Receive, nameof(this.In));
            this.listener = new TcpListener(IPAddress.Any, port);
            this.Start();
        }

        /// <summary>
        /// Gets the name by which to refer to the data stream.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the connection port.
        /// </summary>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public Receiver<T> In { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.networkStream?.Dispose();
            this.listener.Stop();
            this.listener = null;
        }

        private void Receive(T message, Envelope envelope)
        {
            (var bytes, int offset, int count) = this.serializer.SerializeMessage(message, envelope.OriginatingTime);

            try
            {
                if (this.networkStream != null)
                {
                    this.networkStream.Write(BitConverter.GetBytes(count), 0, sizeof(int));
                    this.networkStream.Write(bytes, offset, count);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"TcpWriter Exception: {ex.Message}");
                this.listener.Stop();
                this.networkStream.Dispose();
                this.networkStream = null;
                this.Start();
            }
        }

        private void Start()
        {
            new Thread(new ThreadStart(this.Listen)) { IsBackground = true }.Start();
        }

        private void Listen()
        {
            if (this.listener != null)
            {
                try
                {
                    this.listener.Start();
                    this.networkStream = this.listener.AcceptTcpClient().GetStream();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"TcpWriter Exception: {ex.Message}");
                }
            }
        }
    }
}
