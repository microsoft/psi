// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Server which accepts one or more <see cref="RendezvousClient"/> connections and relays <see cref="Rendezvous"/> information.
    /// </summary>
    public class RendezvousServer : RendezvousRelay, IDisposable
    {
        /// <summary>
        /// Default TCP port on which to listen for clients.
        /// </summary>
        public const int DefaultPort = 13331;

        /// <summary>
        /// Protocol version.
        /// </summary>
        internal const short ProtocolVersion = 2;

        private readonly int port;
        private readonly ConcurrentDictionary<Guid, BinaryWriter> writers = new ();

        private TcpListener listener;
        private bool active = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RendezvousServer"/> class.
        /// </summary>
        /// <param name="port">Optional TCP port on which to listen for clients.</param>
        /// <param name="rendezvous">Optional rendezvous instance to relay.</param>
        public RendezvousServer(int port = DefaultPort, Rendezvous rendezvous = null)
            : base(rendezvous)
        {
            this.port = port;
        }

        /// <summary>
        /// Gets a value indicating whether the server is active.
        /// </summary>
        public bool IsActive => this.active;

        /// <summary>
        /// Start rendezvous client (blocking until connection is established).
        /// </summary>
        public void Start()
        {
            if (this.active)
            {
                throw new Exception($"{nameof(RendezvousServer)} already started.");
            }

            this.Rendezvous.ProcessAdded += (_, process) => this.NotifyClients(process, WriteAddProcess);
            this.Rendezvous.ProcessRemoved += (_, process) => this.NotifyClients(process, WriteRemoveProcess);
            this.listener = new TcpListener(IPAddress.Any, this.port);
            this.active = true;
            new Thread(new ThreadStart(this.ListenForClients)) { IsBackground = true }.Start();
        }

        /// <summary>
        /// Stop rendezvous client.
        /// </summary>
        public void Stop()
        {
            this.active = false;

            foreach (var writer in this.writers.Values)
            {
                TryWriteDisconnect(writer);
                writer.Dispose();
            }

            this.listener?.Stop();
            this.listener = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Stop();
        }

        private void ListenForClients()
        {
            this.listener.Start();
            do
            {
                try
                {
                    var client = this.listener.AcceptTcpClient();
                    var remoteAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                    var stream = client.GetStream();
                    var reader = new BinaryReader(stream);
                    var version = reader.ReadInt16();
                    if (version != ProtocolVersion)
                    {
                        var ex = new IOException($"{nameof(RendezvousClient)} protocol mismatch ({version})");
                        this.ClientError(ex);
                        continue;
                    }

                    var writer = new BinaryWriter(stream);
                    var guid = Guid.NewGuid();
                    this.writers.TryAdd(guid, writer);

                    writer.Write(ProtocolVersion);
                    writer.Write(remoteAddress);
                    writer.Write(this.Rendezvous.Processes.Count());
                    writer.Flush();

                    // notify client of curent process info
                    foreach (var process in this.Rendezvous.Processes)
                    {
                        WriteAddProcess(process, writer);
                    }

                    new Thread(new ParameterizedThreadStart(this.ReadFromClient)) { IsBackground = true }
                        .Start(Tuple.Create(reader, guid));
                }
                catch (Exception ex)
                {
                    this.ClientError(ex);
                }
            }
            while (this.active && this.listener != null);
        }

        private void ReadFromClient(object param)
        {
            var tuple = param as Tuple<BinaryReader, Guid>;
            var reader = tuple.Item1;
            var guid = tuple.Item2;
            try
            {
                do
                {
                    if (!this.ReadProcessUpdate(reader))
                    {
                        Trace.WriteLine($"{nameof(RendezvousClient)} disconnected.");
                        break;
                    }
                }
                while (this.active && this.listener != null);
            }
            catch (Exception ex)
            {
                this.ClientError(ex);
            }

            reader.Dispose();
            if (this.writers.TryRemove(guid, out var writer))
            {
                writer.Dispose();
            }
        }

        private void NotifyClients(Rendezvous.Process process, Action<Rendezvous.Process, BinaryWriter> action)
        {
            foreach (var kv in this.writers)
            {
                var writer = kv.Value;
                try
                {
                    if (writer.BaseStream.CanWrite)
                    {
                        action(process, writer);
                    }
                }
                catch (Exception ex)
                {
                    this.ClientError(ex);
                    if (this.writers.TryRemove(kv.Key, out _))
                    {
                        writer.Dispose();
                    }
                }
            }
        }

        private void ClientError(Exception ex)
        {
            Trace.WriteLine($"{nameof(RendezvousClient)} failed to connect: {ex.Message}");
            if (this.active)
            {
                this.OnError(ex); // note: only invoked on first error
            }
        }
    }
}
