// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// Component that exports pipeline clock information over TCP to enable synchronization.
    /// </summary>
    public class RemoteClockExporter : IDisposable
    {
        /// <summary>
        /// Default TCP port used to communicate with <see cref="RemoteClockImporter"/>.
        /// </summary>
        public const int DefaultPort = 11511;

        internal const short ProtocolVersion = 0;

        private TcpListener listener;
        private bool isDisposing;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteClockExporter"/> class.
        /// </summary>
        /// <param name="port">The connection port.</param>
        public RemoteClockExporter(int port = DefaultPort)
        {
            this.Port = port;
            this.listener = new TcpListener(IPAddress.Any, port);
            this.Start();
        }

        /// <summary>
        /// Gets the connection port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this machine hosts the primary pipeline clock.
        /// </summary>
        internal static bool IsPrimaryClockSourceMachine { get; set; } = false;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.isDisposing = true;
            this.listener.Stop();
            this.listener = null;
        }

        private void Start()
        {
            new Thread(new ThreadStart(this.Listen)) { IsBackground = true }.Start();
        }

        private void Listen()
        {
            if (this.listener != null)
            {
                NetworkStream networkStream = null;
                try
                {
                    this.listener.Start();
                    networkStream = this.listener.AcceptTcpClient().GetStream();

                    // clock synchroniztion
                    IsPrimaryClockSourceMachine = true;
                    if (RemoteClockImporter.PrimaryClockSourceMachineName != Environment.MachineName &&
                        RemoteClockImporter.PrimaryClockSourceMachineName.Length > 0)
                    {
                        // client intends to use this machine as the primary clock source. However, a
                        // RemoteClockImporter on this machine also intends to sync with some other machine!
                        throw new ArgumentException(
                            $"A {nameof(RemoteClockImporter)} on this machine is expecting the remote machine ({RemoteClockImporter.PrimaryClockSourceMachineName}) " +
                            $"to serve as the primary clock, but this machine is instead being asked to serve as the primary." +
                            $"There may be only one machine hosting the primary clock.");
                    }

                    // check protocol version
                    using var reader = new BinaryReader(networkStream);
                    var version = reader.ReadInt16();
                    if (version != ProtocolVersion)
                    {
                        throw new IOException($"Unsupported remote clock protocol version: {version}");
                    }

                    using var writer = new BinaryWriter(networkStream);
                    writer.Write(DateTime.UtcNow.Ticks); // current machine time, used by client to sync clocks
                    writer.Write(Environment.MachineName);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{nameof(RemoteClockExporter)} Exception: {ex.Message}");
                }
                finally
                {
                    networkStream?.Dispose();
                    if (!this.isDisposing)
                    {
                        this.listener.Stop();
                        this.Start();
                    }
                }
            }
        }
    }
}
