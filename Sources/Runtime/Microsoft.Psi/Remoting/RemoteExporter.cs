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
        private ConcurrentDictionary<Guid, Connection> connections = new ConcurrentDictionary<Guid, Connection>();
        private ITransport dataTransport;
        private long maxBytesPerSecond;
        private double bytesPerSecondSmoothingWindowSeconds;
        private string name;
        private string path;
        private bool disposed = false;
        private Thread metaClientThread;
        private Thread dataClientThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which to attach.</param>
        /// <param name="port">TCP port on which to listen (default 11411).</param>
        /// <param name="transport">Transport kind to use.</param>
        /// <param name="maxBytesPerSecond">Maximum bytes/sec quota (default infinite).</param>
        /// <param name="bytesPerSecondSmoothingWindowSeconds">Smoothing window over which to compute bytes/sec (default 5 sec.).</param>
        public RemoteExporter(Pipeline pipeline, int port = DefaultPort, TransportKind transport = DefaultTransport, long maxBytesPerSecond = long.MaxValue, double bytesPerSecondSmoothingWindowSeconds = 5.0)
            : this(PsiStore.Create(pipeline, $"RemoteExporter_{Guid.NewGuid().ToString()}", null, true), port, transport, maxBytesPerSecond, bytesPerSecondSmoothingWindowSeconds)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteExporter"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which to attach.</param>
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
        }

        private RemoteExporter(string name, string path, int port, TransportKind transport, long maxBytesPerSecond, double bytesPerSecondSmoothingWindowSeconds)
        {
            this.name = name;
            this.path = path;
            this.port = port;
            this.dataTransport = Transport.TransportOfKind(transport);
            this.maxBytesPerSecond = maxBytesPerSecond;
            this.bytesPerSecondSmoothingWindowSeconds = bytesPerSecondSmoothingWindowSeconds;

            this.metaClientThread = new Thread(new ThreadStart(this.AcceptMetaClientsBackground)) { IsBackground = true };
            this.metaClientThread.Start();
            this.dataClientThread = new Thread(new ThreadStart(this.AcceptDataClientsBackground)) { IsBackground = true };
            this.dataClientThread.Start();
        }

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

            this.dataTransport.Dispose();
        }

        private void AddConnection(Connection connection)
        {
            if (!this.connections.TryAdd(connection.Id, connection))
            {
                throw new ArgumentException($"Remoting connection already exists (ID={connection.Id}");
            }
        }

        private Connection GetConnection(Guid id)
        {
            Connection connection;
            if (!this.connections.TryGetValue(id, out connection))
            {
                throw new ArgumentException($"Remoting connection does not exist (ID={id})");
            }

            return connection;
        }

        private void RemoveConnection(Guid id)
        {
            Connection ignore;
            if (!this.connections.TryRemove(id, out ignore))
            {
                throw new ArgumentException($"Remoting connection could not be removed (ID={id})");
            }
        }

        private void AcceptMetaClientsBackground()
        {
            var metaListener = new TcpListener(IPAddress.Any, this.port);
            metaListener.Start();
            while (!this.disposed)
            {
                var client = metaListener.AcceptTcpClient();
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

        private void AcceptDataClientsBackground()
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

                    Connection connection;
                    if (this.connections.TryGetValue(guid, out connection))
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
                    var buffer = new byte[128];
                    var length = sizeof(short) + sizeof(long) + sizeof(long); // version, start ticks, end ticks
                    for (var i = 0; i < length;)
                    {
                        i += this.stream.Read(buffer, i, length - i);
                    }

                    var reader = new BufferReader(buffer);

                    // check client version
                    var version = reader.ReadInt16();
                    if (version != ProtocolVersion)
                    {
                        throw new IOException($"Unsupported remoting protocol version: {version}");
                    }

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
                Envelope envelope;
                long envelopeSize;
                unsafe
                {
                    envelopeSize = sizeof(Envelope);
                }

                this.storeReader.Seek(this.interval);

                while (true)
                {
                    if (this.storeReader.MoveNext(out envelope))
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

            private void MetaUpdateHandler(IEnumerable<Metadata> meta, RuntimeInfo runtimeVersion)
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
