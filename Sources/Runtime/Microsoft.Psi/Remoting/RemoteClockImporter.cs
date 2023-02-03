// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi;

    /// <summary>
    /// Component that reads remote clock information over TCP and synchronizes the local pipeline clock.
    /// </summary>
    public class RemoteClockImporter : IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly string host;
        private readonly int port;
        private readonly TcpClient client;
        private readonly EventWaitHandle connected = new (false, EventResetMode.ManualReset);

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteClockImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="host">The host name of the remote clock exporter/server.</param>
        /// <param name="port">The port on which to connect.</param>
        /// <param name="name">An optional name for the component.</param>
        public RemoteClockImporter(Pipeline pipeline, string host, int port = RemoteClockExporter.DefaultPort, string name = nameof(RemoteClockImporter))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.client = new TcpClient();
            this.host = host;
            this.port = port;
            this.connected.Reset();
            new Thread(new ThreadStart(this.SynchronizeLocalPipelineClock)) { IsBackground = true }.Start();
        }

        /// <summary>
        /// Gets wait handle for remote connection being established.
        /// </summary>
        /// <remarks>This should be waited on prior to running the pipeline.</remarks>
        public EventWaitHandle Connected
        {
            get { return this.connected; }
        }

        /// <summary>
        /// Gets or sets machine with which to synchronize pipeline clock.
        /// </summary>
        internal static string PrimaryClockSourceMachineName { get; set; } = string.Empty;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.client.Close();
            this.connected.Dispose();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void SynchronizeLocalPipelineClock()
        {
            var completed = false;
            while (!completed)
            {
                NetworkStream networkStream = null;
                try
                {
                    Trace.WriteLine($"Attempting to connect to {this.host} on port {this.port} ...");
                    this.client.Connect(this.host, this.port);
                    networkStream = this.client.GetStream();
                    Trace.WriteLine($"Connected to {this.host} on port {this.port}.");

                    // send protocol version
                    using var writer = new BinaryWriter(networkStream);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    writer.Write(RemoteClockExporter.ProtocolVersion);

                    using var reader = new BinaryReader(networkStream);
                    var timeAtExporter = reader.ReadInt64();
                    stopwatch.Stop();
                    var timeAtImporter = DateTime.UtcNow.Ticks;
                    var elapsedTime = stopwatch.ElapsedTicks;
                    var machine = reader.ReadString();

                    // Elapsed time includes the complete round trip latency between writing the header and receiving the
                    // remote (exporter) machine's time. We assume that half of the time was from here to the exporter, meaning
                    // that subtracting elapsed / 2 from our current time gives the time as it was on our clock when the exporter
                    // sent it's time. The difference becomes an offset to apply to our pipeline clock to synchronize.
                    var timeOffset = TimeSpan.FromTicks(timeAtExporter - (timeAtImporter - (elapsedTime / 2)));
                    Trace.WriteLine($"{nameof(RemoteClockImporter)} clock sync: Local={timeAtImporter} Remote[{machine}]={timeAtExporter} Latency={elapsedTime} Offset={timeOffset.Ticks}.");
                    if (machine == Environment.MachineName)
                    {
                        // The "remote" machine is actually *this* machine. In this case, assume exactly zero offset.
                        Trace.WriteLine($"{nameof(RemoteClockImporter)} clock sync with self ignored ({machine}). Pipeline clock will remain unchanged.");
                        timeOffset = TimeSpan.Zero;
                    }
                    else if (RemoteClockExporter.IsPrimaryClockSourceMachine)
                    {
                        // An exporter on this machine already thinks that *this* is the primary source, but this importer
                        // is attempting to synchronize with some other machine instead!
                        throw new ArgumentException(
                            $"{nameof(RemoteClockImporter)} treating remote machine ({machine}) as the primary clock source, but this machine ({Environment.MachineName}) is already the " +
                            $"primary. There may be only one machine hosting the primary clock. Check {nameof(RemoteClockImporter)} configurations.");
                    }

                    if (PrimaryClockSourceMachineName != machine && PrimaryClockSourceMachineName.Length > 0)
                    {
                        // Another importer on this machine has already negotiated a clock sync with some machine other than
                        // the one that this importer is syncing with. Importers disagree as to who the primary should be!
                        throw new ArgumentException(
                            $"{nameof(RemoteClockImporter)} treating remote machine ({machine}) as the primary clock source, but another {nameof(RemoteClockImporter)} " +
                            $"is treating a different remote machine ({PrimaryClockSourceMachineName}) as the primary. " +
                            $"There may be only one machine hosting the primary clock. Check {nameof(RemoteClockImporter)} configurations.");
                    }

                    // synchronize pipeline clock
                    this.pipeline.VirtualTimeOffset = timeOffset;
                    this.connected.Set();
                    completed = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{nameof(RemoteClockImporter)} Exception: {ex.Message}");
                }
                finally
                {
                    networkStream?.Dispose();
                    this.client.Close();
                }
            }
        }
    }
}
