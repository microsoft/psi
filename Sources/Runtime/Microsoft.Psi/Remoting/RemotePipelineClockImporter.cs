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
    public class RemotePipelineClockImporter : IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly string host;
        private readonly int port;
        private readonly TcpClient client;
        private readonly EventWaitHandle connected = new (false, EventResetMode.ManualReset);

        /// <summary>
        /// Initializes a new instance of the <see cref="RemotePipelineClockImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="host">The host name of the remote clock exporter/server.</param>
        /// <param name="port">The port on which to connect.</param>
        /// <param name="name">An optional name for the component.</param>
        public RemotePipelineClockImporter(Pipeline pipeline, string host, int port = RemotePipelineClockExporter.DefaultPort, string name = nameof(RemotePipelineClockImporter))
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
                    writer.Write(RemotePipelineClockExporter.ProtocolVersion);

                    using var reader = new BinaryReader(networkStream);
                    int state = reader.ReadInt32();
                    var timeAtExporter = reader.ReadInt64();
                    stopwatch.Stop();
                    var timeAtImporter = DateTime.UtcNow.Ticks;
                    var elapsedTime = stopwatch.ElapsedTicks;
                    var pipelineName = reader.ReadString();
                    var timeOffset = TimeSpan.Zero;

                    if (pipelineName != this.pipeline.Name)
                    {
                        switch (state)
                        {
                            case 2:
                            case 0:
                                // Elapsed time includes the complete round trip latency between writing the header and receiving the
                                // remote (exporter) machine's time. We assume that half of the time was from here to the exporter, meaning
                                // that subtracting elapsed / 2 from our current time gives the time as it was on our clock when the exporter
                                // sent it's time. The difference becomes an offset to apply to our pipeline clock to synchronize.
                                timeOffset = TimeSpan.FromTicks(timeAtExporter - (timeAtImporter - (elapsedTime / 2)));
                                Trace.WriteLine($"{nameof(RemotePipelineClockImporter)} clock sync: Local={timeAtImporter} Remote pipeline[{pipelineName}]={timeAtExporter} Latency={elapsedTime} Offset={timeOffset.Ticks}.");
                                break;
                            case 1:
                                this.pipeline.ProposeReplayTime(new TimeInterval(new DateTime(timeAtExporter), DateTime.MaxValue));
                                timeOffset = TimeSpan.FromTicks(elapsedTime / 2);
                                break;
                        }
                    }
                    else
                    {
                        // The "remote" pipeline is actually *this* pipeline. In this case, assume exactly zero offset.
                        Trace.WriteLine($"{nameof(RemotePipelineClockImporter)} clock sync with self ignored ({pipelineName}). Pipeline clock will remain unchanged.");
                    }

                    // synchronize pipeline clock
                    this.pipeline.VirtualTimeOffset = timeOffset;
                    this.connected.Set();
                    completed = true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{nameof(RemotePipelineClockImporter)} Exception: {ex.Message}");
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
