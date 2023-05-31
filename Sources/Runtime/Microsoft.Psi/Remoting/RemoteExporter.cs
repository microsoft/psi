// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;

    /// <summary>
    /// Exporter for remoting over network transport.
    /// </summary>
    public sealed class RemoteExporter : IDisposable
    {
        internal const short ProtocolVersion = 0;
        internal const int DefaultPort = 11411;
        private const TransportKind DefaultTransport = TransportKind.NamedPipes;

        private readonly int port;
        private readonly TransportKind transport;
        private readonly string name;
        private readonly string path;
        private readonly long maxBytesPerSecond;
        private readonly TcpListener metaListener;
        private readonly ITransport dataTransport;
        private readonly double bytesPerSecondSmoothingWindowSeconds;

        private ConcurrentDictionary<Guid, Connection> connections = new ();
        private bool disposed = false;
        private Thread metaClientThread;
        private Thread dataClientThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="port">TCP port on which to listen (default 11411).</param>
        /// <param name="transport">Transport kind to use.</param>
        /// <param name="maxBytesPerSecond">Maximum bytes/sec quota (default infinite).</param>
        /// <param name="bytesPerSecondSmoothingWindowSeconds">Smoothing window over which to compute bytes/sec (default 5 sec.).</param>
        public RemoteExporter(Pipeline pipeline, int port = DefaultPort, TransportKind transport = DefaultTransport, long maxBytesPerSecond = long.MaxValue, double bytesPerSecondSmoothingWindowSeconds = 5.0)
            : this(PsiStore.Create(pipeline, $"RemoteExporter_{Guid.NewGuid()}", null, true), port, transport, maxBytesPerSecond, bytesPerSecondSmoothingWindowSeconds)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="transport">Transport kind to use.</param>
        /// <param name="maxBytesPerSecond">Maximum bytes/sec quota (default infinite).</param>
        /// <param name="bytesPerSecondSmoothingWindowSeconds">Smoothing window over which to compute bytes/sec (default 5 sec.).</param>
        public RemoteExporter(Pipeline pipeline, TransportKind transport, long maxBytesPerSecond = long.MaxValue, double bytesPerSecondSmoothingWindowSeconds = 5.0)
            : this(pipeline, DefaultPort, transport, maxBytesPerSecond, bytesPerSecondSmoothingWindowSeconds)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="exporter">Exporter to be remoted.</param>
        /// <param name="transport">Transport kind to use.</param>
        public RemoteExporter(Exporter exporter, TransportKind transport)
            : this(exporter, DefaultPort, transport)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="importer">Importer to be remoted.</param>
        /// <param name="transport">Transport kind to use.</param>
        public RemoteExporter(Importer importer, TransportKind transport)
            : this(importer, DefaultPort, transport)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="exporter">Exporter to be remoted.</param>
        /// <param name="port">TCP port on which to listen (default 11411).</param>
        /// <param name="transport">Transport kind to use.</param>
        /// <param name="maxBytesPerSecond">Maximum bytes/sec quota (default infinite).</param>
        /// <param name="bytesPerSecondSmoothingWindowSeconds">Smoothing window over which to compute bytes/sec (default 5 sec.).</param>
        public RemoteExporter(Exporter exporter, int port = DefaultPort, TransportKind transport = DefaultTransport, long maxBytesPerSecond = long.MaxValue, double bytesPerSecondSmoothingWindowSeconds = 5.0)
            : this(exporter.Name, exporter.Path, port, transport, maxBytesPerSecond, bytesPerSecondSmoothingWindowSeconds)
        {
            this.Exporter = exporter;

            // add this as a node in the exporter so that it gets disposed
            exporter.GetOrCreateNode(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="importer">Importer to be remoted.</param>
        /// <param name="port">TCP port on which to listen (default 11411).</param>
        /// <param name="transport">Transport kind to use.</param>
        /// <param name="maxBytesPerSecond">Maximum bytes/sec quota (default infinite).</param>
        /// <param name="bytesPerSecondSmoothingWindowSeconds">Smoothing window over which to compute bytes/sec (default 5 sec.).</param>
        public RemoteExporter(Importer importer, int port = DefaultPort, TransportKind transport = DefaultTransport, long maxBytesPerSecond = long.MaxValue, double bytesPerSecondSmoothingWindowSeconds = 5.0)
            : this(importer.StoreName, importer.StorePath, port, transport, maxBytesPerSecond, bytesPerSecondSmoothingWindowSeconds)
        {
            // used to remote an existing store. this.Exporter remains null

            // add this as a node in the importer so that it gets disposed
            importer.GetOrCreateNode(this);
        }

        private RemoteExporter(string name, string path, int port, TransportKind transport, long maxBytesPerSecond, double bytesPerSecondSmoothingWindowSeconds)
        {
            this.name = name;
            this.path = path;
            this.port = port;
            this.transport = transport;
            this.metaListener = new TcpListener(IPAddress.Any, this.port);
            this.dataTransport = Transport.TransportOfKind(transport);
            this.maxBytesPerSecond = maxBytesPerSecond;
            this.bytesPerSecondSmoothingWindowSeconds = bytesPerSecondSmoothingWindowSeconds;

            this.metaClientThread = new Thread(new ThreadStart(this.AcceptMetaClientsBackground)) { IsBackground = true };
            this.metaClientThread.Start();
            this.dataClientThread = new Thread(new ThreadStart(this.AcceptDataClientsBackground)) { IsBackground = true };
            this.dataClientThread.Start();
        }

        /// <summary>
        /// Gets the TCP port being used.
        /// </summary>
        public int Port => this.port;

        /// <summary>
        /// Gets the transport being used.
        /// </summary>
        public TransportKind TransportKind => this.transport;

        /// <summary>
        /// Gets exporter being remoted.
        /// </summary>
        public Exporter Exporter { get; private set; }

        /// <summary>
        /// Dispose of remote exporter.
        /// </summary>
        public void Dispose()
        {
            this.disposed = true;
            foreach (var connection in this.connections)
            {
                connection.Value.Dispose();
            }

            this.connections = null;
            this.metaClientThread = null;
            this.dataClientThread = null;

            this.metaListener.Stop();
            this.dataTransport.Dispose();
        }

        private void AddConnection(Connection connection)
        {
            if (!this.connections.TryAdd(connection.Id, connection))
            {
                throw new ArgumentException($"Remoting connection already exists (ID={connection.Id}");
            }
        }

        private void RemoveConnection(Guid id)
        {
            if (!this.connections.TryRemove(id, out _))
            {
                throw new ArgumentException($"Remoting connection could not be removed (ID={id})");
            }
        }

        private void AcceptMetaClientsBackground()
        {
            try
            {
                this.metaListener.Start();
                while (!this.disposed)
                {
                    var client = this.metaListener.AcceptTcpClient();
                    Connection connection = null;
                    try
                    {
                        connection = new Connection(client, this.dataTransport, this.name, this.path, this.RemoveConnection, this.Exporter, this.maxBytesPerSecond, this.bytesPerSecondSmoothingWindowSeconds);
                        this.AddConnection(connection);
                        connection.Connect();
                        Trace.WriteLine($"RemoteExporter meta client accepted (ID={connection.Id})");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"RemoteExporter meta connection error (Message={ex.Message}, ID={connection?.Id})");
                        client.Dispose();
                    }
                }
            }
            catch (SocketException se)
            {
                Trace.TraceError($"RemoteExporter meta listener error (Message={se.Message})");
            }
        }

        private void AcceptDataClientsBackground()
        {
            try
            {
                this.dataTransport.StartListening();
                while (!this.disposed)
                {
                    var client = this.dataTransport.AcceptClient();
                    var guid = Guid.Empty;
                    try
                    {
                        guid = client.ReadSessionId();
                        Trace.WriteLine($"RemoteExporter data client accepted (ID={guid})");

                        if (this.connections.TryGetValue(guid, out Connection connection))
                        {
                            connection.JoinBackground(client);
                        }
                        else
                        {
                            throw new IOException($"RemoteExporter error: Invalid remoting connection ID: {guid}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"RemoteExporter data connection error (Message={ex.Message}, ID={guid})");
                        client.Dispose();
                    }
                }
            }
            catch (SocketException se)
            {
                Trace.TraceError($"RemoteExporter data transport error (Message={se.Message})");
            }
        }

        private sealed class Connection : IDisposable
        {
            private readonly Guid id;
            private readonly ITransport dataTransport;
            private readonly Action<Guid> onDisconnect;
            private readonly Exporter exporter;
            private readonly long maxBytesPerSecond;
            private readonly double bytesPerSecondSmoothingWindowSeconds;

            private readonly string storeName;
            private readonly string storePath;

            private TcpClient client;
            private Stream stream;
            private PsiStoreReader storeReader;
            private TimeInterval interval;

            public Connection(TcpClient client, ITransport dataTransport, string name, string path, Action<Guid> onDisconnect, Exporter exporter, long maxBytesPerSecond, double bytesPerSecondSmoothingWindowSeconds)
            {
                this.id = Guid.NewGuid();
                this.client = client;
                this.stream = client.GetStream();
                this.dataTransport = dataTransport;
                this.storeName = name;
                this.storePath = path;
                this.onDisconnect = onDisconnect;
                this.exporter = exporter;
                this.maxBytesPerSecond = maxBytesPerSecond;
                this.bytesPerSecondSmoothingWindowSeconds = bytesPerSecondSmoothingWindowSeconds;
            }

            public Guid Id => this.id;

            public void Connect()
            {
                try
                {
                    // check client version
                    var buffer = new byte[128];
                    Transport.Read(buffer, sizeof(short), this.stream);
                    var reader = new BufferReader(buffer);
                    var version = reader.ReadInt16();
                    if (version != ProtocolVersion)
                    {
                        throw new IOException($"Unsupported remoting protocol version: {version}");
                    }

                    // get replay info
                    var length = sizeof(long) + sizeof(long); // start ticks, end ticks
                    Transport.Read(buffer, length, this.stream);
                    reader.Reset();

                    // get replay interval
                    var startTicks = reader.ReadInt64();
                    if (startTicks == -1)
                    {
                        // special indication of `DateTime.UtcNow` at the exporter end
                        startTicks = DateTime.UtcNow.Ticks;
                    }

                    var start = new DateTime(startTicks);
                    var end = new DateTime(reader.ReadInt64());
                    this.interval = new TimeInterval(start, end);

                    // send ID, stream count, transport and protocol params
                    var writer = new BufferWriter(buffer);
                    writer.Write(0); // length placeholder
                    writer.Write(this.id.ToByteArray());
                    writer.Write(this.dataTransport.Transport.ToString());
                    this.dataTransport.WriteTransportParams(writer);
                    var len = writer.Position;
                    writer.Reset();
                    writer.Write(len - 4);
                    this.stream.Write(writer.Buffer, 0, len);
                    this.storeReader = new PsiStoreReader(this.storeName, this.storePath, this.MetaUpdateHandler, true);
                }
                catch (Exception)
                {
                    this.Disconnect();
                    throw;
                }
            }

            public void JoinBackground(ITransportClient client)
            {
                double avgBytesPerSec = 0;
                var lastTime = DateTime.MinValue;
                var buffer = new byte[0];
                long envelopeSize;
                unsafe
                {
                    envelopeSize = sizeof(Envelope);
                }

                this.storeReader.Seek(this.interval);

                while (true)
                {
                    if (this.storeReader.MoveNext(out Envelope envelope))
                    {
                        var length = this.storeReader.Read(ref buffer);
                        this.exporter.Throttle.Reset();
                        try
                        {
                            client.WriteMessage(envelope, buffer);
                            if (lastTime > DateTime.MinValue /* at least second message */)
                            {
                                if (this.maxBytesPerSecond < long.MaxValue)
                                {
                                    // throttle to arbitrary max BPS
                                    var elapsed = (envelope.OriginatingTime - lastTime).TotalSeconds;
                                    var bytesPerSec = (envelopeSize + length) / elapsed;
                                    double smoothingFactor = 1.0 / (this.bytesPerSecondSmoothingWindowSeconds / elapsed);
                                    avgBytesPerSec = (bytesPerSec * smoothingFactor) + (avgBytesPerSec * (1.0 - smoothingFactor));
                                    if (bytesPerSec > this.maxBytesPerSecond)
                                    {
                                        var wait = (int)(((avgBytesPerSec / this.maxBytesPerSecond) - elapsed) * 1000.0);
                                        if (wait > 0)
                                        {
                                            Thread.Sleep(wait);
                                        }
                                    }
                                }
                            }

                            lastTime = envelope.OriginatingTime;
                        }
                        finally
                        {
                            // writers continue upon failure - meanwhile, remote client may reconnect and resume based on replay interval
                            this.exporter.Throttle.Set();
                        }
                    }
                }
            }

            public void Dispose()
            {
                this.storeReader.Dispose();
                this.storeReader = null;
                this.client.Dispose();
                this.client = null;
                this.stream.Dispose();
                this.stream = null;
            }

            private void Disconnect()
            {
                this.onDisconnect(this.id);
                this.Dispose();
            }

            private void MetaUpdateHandler(IEnumerable<Metadata> meta, RuntimeInfo runtimeInfo)
            {
                try
                {
                    if (this.client.Connected)
                    {
                        var writer = new BufferWriter(0);
                        foreach (var m in meta)
                        {
                            writer.Reset();
                            writer.Write(0); // length placeholder
                            m.Serialize(writer);
                            var len = writer.Position;
                            writer.Reset();
                            writer.Write(len - 4);
                            this.stream.Write(writer.Buffer, 0, len);
                            Trace.WriteLine($"RemoteExporter meta update (Name={m.Name}, ID={this.id})");
                        }

                        writer.Reset();
                        writer.Write(0); // burst "intermission" marker
                        this.stream.Write(writer.Buffer, 0, writer.Position);
                        Trace.WriteLine($"RemoteExporter meta intermission (ID={this.id})");
                    }
                    else
                    {
                        Trace.WriteLine($"RemoteExporter connection closed (ID={this.id})");
                        this.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"RemoteExporter connection error (Message={ex.Message}, ID={this.id})");
                    this.Disconnect();
                }
            }
        }
    }
}
