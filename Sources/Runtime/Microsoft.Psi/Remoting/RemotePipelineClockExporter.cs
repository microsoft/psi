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
    public class RemotePipelineClockExporter : IDisposable
    {
        /// <summary>
        /// Default TCP port used to communicate with <see cref="RemoteClockImporter"/>.
        /// </summary>
        public const int DefaultPort = 11511;

        internal const short ProtocolVersion = 0;

        private TcpListener listener;
        private bool isDisposing;
        private Pipeline pipeline;
        private TimeInterval interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemotePipelineClockExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline sync master.</param>
        /// <param name="port">The connection port.</param>
        /// <param name="interval">Optional for specific time interval.</param>
        public RemotePipelineClockExporter(Pipeline pipeline, int port = DefaultPort, TimeInterval interval = null)
        {
            this.pipeline = pipeline;
            this.Port = port;
            this.interval = interval ?? TimeInterval.Infinite;
            this.listener = new TcpListener(IPAddress.Any, port);
            this.Start();
        }

        /// <summary>
        /// Gets the connection port.
        /// </summary>
        public int Port { get; }

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

                    // check protocol version
                    using var reader = new BinaryReader(networkStream);
                    var version = reader.ReadInt16();
                    if (version != ProtocolVersion)
                    {
                        throw new IOException($"Unsupported remote pipeline clock protocol version: {version}");
                    }

                    using var writer = new BinaryWriter(networkStream);

                    // current pipeline time, used by client to sync clocks
                    if (this.pipeline.IsRunning || this.pipeline.ReplayDescriptor.Start != DateTime.MinValue)
                    {
                        writer.Write(0);
                        if (this.pipeline.IsRunning)
                        {
                            writer.Write(this.pipeline.GetCurrentTime().Ticks);
                        }
                        else
                        {
                            writer.Write(this.pipeline.ReplayDescriptor.Start.Ticks);
                        }
                    }
                    else if (this.pipeline.IsInitial || this.pipeline.IsStarting)
                    {
                        writer.Write(1);
                        writer.Write(Math.Max(this.pipeline.ProposedOriginatingTimeInterval.Left.Ticks, this.interval.Left.Ticks));
                    }
                    else
                    {
                        writer.Write(2); 
                        writer.Write(DateTime.UtcNow.Ticks);
                    }

                    // name of the pipeline
                    writer.Write(this.pipeline.Name);
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{nameof(RemotePipelineClockExporter)} Exception: {ex.Message}");
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
