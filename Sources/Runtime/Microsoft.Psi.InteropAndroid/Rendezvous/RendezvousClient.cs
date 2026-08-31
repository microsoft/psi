// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Client which connects to a <see cref="RendezvousServer"/> and relays <see cref="Rendezvous"/> information.
    /// </summary>
    public class RendezvousClient : RendezvousRelay, IDisposable
    {
        private readonly string serverAddress;
        private readonly int port;
        private readonly EventWaitHandle connected = new (false, EventResetMode.ManualReset);

        private TcpClient client;
        private BinaryReader reader;
        private BinaryWriter writer;
        private bool active = false;
        private string clientAddress = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RendezvousClient"/> class.
        /// </summary>
        /// <param name="serverAddress">TCP address to which to connect.</param>
        /// <param name="port">Optional TCP port to which to connect.</param>
        /// <param name="rendezvous">Optional rendezvous instance to relay.</param>
        public RendezvousClient(string serverAddress, int port = RendezvousServer.DefaultPort, Rendezvous rendezvous = null)
            : base(rendezvous)
        {
            this.serverAddress = serverAddress;
            this.port = port;
            this.connected.Reset();
        }

        /// <summary>
        /// Gets wait handle for server connection being established.
        /// </summary>
        /// <remarks>This should be waited on prior to trusting the processes list.</remarks>
        public EventWaitHandle Connected => this.connected;

        /// <summary>
        /// Gets a value indicating whether the client is active.
        /// </summary>
        public bool IsActive => this.active;

        /// <summary>
        /// Gets the client address (available after connection established).
        /// </summary>
        public string ClientAddress => this.clientAddress;

        /// <summary>
        /// Start rendezvous client (blocking until connection is established).
        /// </summary>
        public void Start()
        {
            if (this.active)
            {
                throw new Exception($"{nameof(RendezvousClient)} already started.");
            }

            this.Rendezvous.ProcessAdded += this.ProcessAdded;
            this.Rendezvous.ProcessRemoved += this.ProcessRemoved;
            while (!this.active)
            {
                try
                {
                    (this.client = new TcpClient()).Connect(this.serverAddress, this.port);
                    var stream = this.client.GetStream();
                    this.reader = new BinaryReader(stream);
                    this.writer = new BinaryWriter(stream);
                    this.writer.Write(RendezvousServer.ProtocolVersion);
                    this.writer.Flush();
                    this.active = true;
                    new Thread(new ThreadStart(this.ReadFromServer)) { IsBackground = true }.Start();
                }
                catch (SocketException ex)
                {
                    Trace.WriteLine($"Failed to connect to {nameof(RendezvousServer)} (retrying): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stop rendezvous client.
        /// </summary>
        public void Stop()
        {
            TryWriteDisconnect(this.writer);
            this.Rendezvous.ProcessAdded -= this.ProcessAdded;
            this.Rendezvous.ProcessRemoved -= this.ProcessRemoved;
            this.active = false;
            this.client?.Close();
            this.client?.Dispose();
            this.client = null;
            this.reader?.Dispose();
            this.reader = null;
            this.writer?.Dispose();
            this.writer = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop();
            this.connected.Dispose();
        }

        private void ReadFromServer()
        {
            try
            {
                var version = this.reader.ReadInt16();
                if (version != RendezvousServer.ProtocolVersion)
                {
                    var ex = new IOException($"{nameof(RendezvousServer)} protocol mismatch ({version})");
                    this.ServerError(ex);
                    throw ex;
                }

                this.clientAddress = this.reader.ReadString();

                // initialize processes before signaling connected
                var count = this.reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    if (!this.ReadProcessUpdate(this.reader))
                    {
                        this.ServerError(new IOException($"{nameof(RendezvousServer)} disconnected."));
                    }
                }

                this.connected.Set();

                do
                {
                    if (!this.ReadProcessUpdate(this.reader))
                    {
                        this.ServerError(new IOException($"{nameof(RendezvousServer)} disconnected."));
                    }
                }
                while (this.active && this.client.Connected);
            }
            catch (Exception ex)
            {
                this.ServerError(ex);
                this.connected.Reset();
            }
        }

        private void NotifyServer(Rendezvous.Process process, Action<Rendezvous.Process, BinaryWriter> action)
        {
            try
            {
                action(process, this.writer);
            }
            catch (Exception ex)
            {
                this.ServerError(ex);
            }
        }

        private void ProcessAdded(object sender, Rendezvous.Process process)
        {
            if (this.writer != null)
            {
                this.NotifyServer(process, WriteAddProcess);
            }
        }

        private void ProcessRemoved(object sender, Rendezvous.Process process)
        {
            if (this.writer != null)
            {
                this.NotifyServer(process, WriteRemoveProcess);
            }
        }

        private void ServerError(Exception ex)
        {
            Trace.WriteLine($"{nameof(RendezvousServer)} error: {ex.Message}");
            this.Stop();
            this.OnError(ex);
        }
    }
}
