// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Base class for <see cref="RendezvousClient"/> and <see cref="RendezvousServer"/>.
    /// </summary>
    public abstract class RendezvousRelay
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RendezvousRelay"/> class.
        /// </summary>
        /// <param name="rendezvous">Optional rendezvous instance to relay.</param>
        public RendezvousRelay(Rendezvous rendezvous = null)
        {
            this.Rendezvous = rendezvous ?? new Rendezvous();
        }

        /// <summary>
        /// Event raised when errors occur.
        /// </summary>
        public event EventHandler<Exception> Error;

        /// <summary>
        /// Gets the underlying rendezvous.
        /// </summary>
        public Rendezvous Rendezvous { get; private set; }

        /// <summary>
        /// Write update to add process.
        /// </summary>
        /// <param name="process">Process to add.</param>
        /// <param name="writer">Writer to which to write update.</param>
        protected static void WriteAddProcess(Rendezvous.Process process, BinaryWriter writer)
        {
            writer.Write((byte)1); // add
            writer.Write(process.Name);
            writer.Write(process.Version);
            writer.Write(process.Endpoints.Count());
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    writer.Write((byte)0); // TcpEndpoint
                    writer.Write(tcpEndpoint.Host);
                    writer.Write(tcpEndpoint.Port);
                }
                else if (endpoint is Rendezvous.NetMQSourceEndpoint netMQEndpoint)
                {
                    writer.Write((byte)1); // NetMQEndpoint
                    writer.Write(netMQEndpoint.Address);
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                {
                    writer.Write((byte)2); // RemoteExporterEndpoint
                    writer.Write(remoteExporterEndpoint.Host);
                    writer.Write(remoteExporterEndpoint.Port);
                    writer.Write((int)remoteExporterEndpoint.Transport);
                }
                else if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockExporterEndpoint)
                {
                    writer.Write((byte)3); // RemoteClockExporterEndpoint
                    writer.Write(remoteClockExporterEndpoint.Host);
                    writer.Write(remoteClockExporterEndpoint.Port);
                }
                else
                {
                    throw new ArgumentException($"Unknown type of Endpoint ({endpoint.GetType().Name}).");
                }

                writer.Write(endpoint.Streams.Count());
                foreach (var stream in endpoint.Streams)
                {
                    writer.Write(stream.StreamName);
                    writer.Write(stream.TypeName);
                }
            }

            writer.Flush();
        }

        /// <summary>
        /// Write update to remove process.
        /// </summary>
        /// <param name="process">Process to remove.</param>
        /// <param name="writer">Writer to which to write update.</param>
        protected static void WriteRemoveProcess(Rendezvous.Process process, BinaryWriter writer)
        {
            writer.Write((byte)2); // remove
            writer.Write(process.Name);
            writer.Flush();
        }

        /// <summary>
        /// Write disconnection signal..
        /// </summary>
        /// <param name="writer">Writer to which to write disconnection signal.</param>
        protected static void TryWriteDisconnect(BinaryWriter writer)
        {
            try
            {
                writer?.Write((byte)0); // disconnect
                writer?.Flush();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Raise error event.
        /// </summary>
        /// <param name="ex">Underlying exception.</param>
        protected void OnError(Exception ex)
        {
            this.Error?.Invoke(this, ex);
        }

        /// <summary>
        /// Read process update record.
        /// </summary>
        /// <param name="reader">Reader from which to read.</param>
        /// <returns>A value indicating whether an update was read, otherwise false indicated disconnection.</returns>
        protected bool ReadProcessUpdate(BinaryReader reader)
        {
            try
            {
                switch (reader.ReadByte())
                {
                    case 0: // disconnect
                        return false;
                    case 1: // add process
                        var process = ReadProcess(reader);
                        this.Rendezvous.TryAddProcess(process);
                        return true;
                    case 2: // remove process
                        var name = reader.ReadString();
                        this.Rendezvous.TryRemoveProcess(name);
                        return true;
                    default:
                        throw new Exception("Unexpected rendezvous action.");
                }
            }
            catch (Exception ex)
            {
                this.OnError(ex);
                return false;
            }
        }

        /// <summary>
        /// Read process.
        /// </summary>
        /// <param name="reader">Reader from which to deserialize.</param>
        /// <returns>Process.</returns>
        private static Rendezvous.Process ReadProcess(BinaryReader reader)
        {
            var processName = reader.ReadString();
            var processVersion = reader.ReadString();
            var process = new Rendezvous.Process(processName, processVersion);

            // read endpoint info
            var endpointCount = reader.ReadInt32();
            for (var i = 0; i < endpointCount; i++)
            {
                Rendezvous.Endpoint endpoint;
                switch (reader.ReadByte())
                {
                    case 0: // TcpEndpoint
                        var address = reader.ReadString();
                        var port = reader.ReadInt32();
                        endpoint = new Rendezvous.TcpSourceEndpoint(address, port);
                        break;
                    case 1: // NetMQEndpoint
                        endpoint = new Rendezvous.NetMQSourceEndpoint(reader.ReadString());
                        break;
                    case 2: // RemoteExporterEndpoint
                        var host = reader.ReadString();
                        port = reader.ReadInt32();
                        var transport = (TransportKind)reader.ReadInt32();
                        endpoint = new Rendezvous.RemoteExporterEndpoint(host, port, transport);
                        break;
                    case 3: // RemoteClockExporerEndpoint
                        host = reader.ReadString();
                        port = reader.ReadInt32();
                        endpoint = new Rendezvous.RemoteClockExporterEndpoint(host, port);
                        break;
                    default:
                        throw new Exception("Unknown type of Endpoint.");
                }

                // read stream info
                var streamCount = reader.ReadInt32();
                for (var j = 0; j < streamCount; j++)
                {
                    var name = reader.ReadString();
                    var typeName = reader.ReadString();
                    endpoint.AddStream(new Rendezvous.Stream(name, typeName));
                }

                process.AddEndpoint(endpoint);
            }

            return process;
        }
    }
}
