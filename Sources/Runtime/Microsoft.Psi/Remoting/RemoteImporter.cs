// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Importer for remoting over network transport.
    /// </summary>
    public sealed class RemoteImporter : IDisposable
    {
        private readonly Func<string, Importer> importerThunk;
        private readonly long replayEnd;
        private readonly string host;
        private readonly int port;
        private readonly bool allowSequenceRestart;
        private readonly EventWaitHandle connected = new (false, EventResetMode.ManualReset);

        private readonly bool replayRemoteLatestStart; // special replayStart of `DateTime.UtcNow` at exporter side
        private readonly Dictionary<int, int> lastSequenceIdPerStream = new ();

        private PsiStoreWriter storeWriter;
        private long replayStart; // advanced upon each message for restart
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="replay">Time interval to be replayed from remote source.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        public RemoteImporter(Pipeline pipeline, TimeInterval replay, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(name => PsiStore.Open(pipeline, name, null), replay, false, host, port, $"RemoteImporter_{Guid.NewGuid()}", null, allowSequenceRestart)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="replayEnd">End of time interval to be replayed from remote.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        /// <remarks>In this case the start is a special behavior that is `DateTime.UtcNow` _at the sending `RemoteExporter`_.</remarks>
        public RemoteImporter(Pipeline pipeline, DateTime replayEnd, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(name => PsiStore.Open(pipeline, name, null), new TimeInterval(DateTime.MinValue, replayEnd), true, host, port, $"RemoteImporter_{Guid.NewGuid()}", null, allowSequenceRestart)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        /// <remarks>In this case the start is a special behavior that is `DateTime.UtcNow` _at the sending `RemoteExporter`_.</remarks>
        public RemoteImporter(Pipeline pipeline, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(name => PsiStore.Open(pipeline, name, null), new TimeInterval(DateTime.MinValue, DateTime.MaxValue), true, host, port, $"RemoteImporter_{Guid.NewGuid()}", null, allowSequenceRestart)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="importer">Importer to receive remoted streams.</param>
        /// <param name="replay">Time interval to be replayed from remote source.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        public RemoteImporter(Importer importer, TimeInterval replay, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(_ => importer, replay, false, host, port, importer.StoreName, importer.StorePath, allowSequenceRestart)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="importer">Importer to receive remoted streams.</param>
        /// <param name="replayEnd">End of time interval to be replayed from remote.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        /// <remarks>In this case the start is a special behavior that is `DateTime.UtcNow` _at the sending `RemoteExporter`_.</remarks>
        public RemoteImporter(Importer importer, DateTime replayEnd, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(_ => importer, new TimeInterval(DateTime.MinValue, replayEnd), true, host, port, importer.StoreName, importer.StorePath, allowSequenceRestart)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteImporter"/> class.
        /// </summary>
        /// <param name="importer">Importer to receive remoted streams.</param>
        /// <param name="host">Remote host name.</param>
        /// <param name="port">TCP port on which to connect (default 11411).</param>
        /// <param name="allowSequenceRestart">Whether to allow sequence ID restarts upon connection loss/reacquire.</param>
        /// <remarks>In this case the start is a special behavior that is `DateTime.UtcNow` _at the sending `RemoteExporter`_.</remarks>
        public RemoteImporter(Importer importer, string host, int port = RemoteExporter.DefaultPort, bool allowSequenceRestart = true)
            : this(_ => importer, new TimeInterval(DateTime.MinValue, DateTime.MaxValue), true, host, port, importer.StoreName, importer.StorePath, allowSequenceRestart)
        {
        }

        private RemoteImporter(
            Func<string, Importer> importerThunk,
            TimeInterval replay,
            bool replayRemoteLatestStart,
            string host,
            int port,
            string storeName,
            string storePath,
            bool allowSequenceRestart)
        {
            this.importerThunk = importerThunk;
            this.replayStart = replay.Left.Ticks;
            this.replayEnd = replay.Right.Ticks;
            this.replayRemoteLatestStart = replayRemoteLatestStart;
            this.host = host;
            this.port = port;
            this.allowSequenceRestart = allowSequenceRestart;
            this.storeWriter = new PsiStoreWriter(storeName, storePath);
            this.StartMetaClient();
        }

        /// <summary>
        /// Gets importer receiving remoted streams.
        /// </summary>
        public Importer Importer { get; private set; }

        /// <summary>
        /// Gets wait handle for remote connection being established.
        /// </summary>
        /// <remarks>This should be waited on before opening streams.</remarks>
        public EventWaitHandle Connected
        {
            get { return this.connected; }
        }

        /// <summary>
        /// Dispose of remote importer.
        /// </summary>
        public void Dispose()
        {
            this.disposed = true;

            this.storeWriter.Dispose();
            this.storeWriter = null;

            this.connected.Dispose();
        }

        private void StartMetaClient()
        {
            if (this.disposed)
            {
                return;
            }

            // data client will be started once GUID and transport are known
            this.connected.Reset();
            var thread = new Thread(new ThreadStart(this.MetaClientBackground)) { IsBackground = true };
            thread.Start();
        }

        private void MetaClientBackground()
        {
            Guid guid = Guid.Empty;
            try
            {
                var metaClient = new TcpClient();
                metaClient.Connect(this.host, this.port);
                var metaStream = metaClient.GetStream();

                // send protocol version and replay interval
                var buffer = new byte[256];
                var writer = new BufferWriter(buffer);
                writer.Write(RemoteExporter.ProtocolVersion);
                writer.Write(this.replayRemoteLatestStart ? -1 : this.replayStart);
                writer.Write(this.replayEnd);
                metaStream.Write(writer.Buffer, 0, writer.Position);

                // receive ID and transport info
                var reader = new BufferReader(buffer);
                Transport.Read(reader.Buffer, 4, metaStream);
                var len = reader.ReadInt32();
                reader.Reset(len);
                Transport.Read(reader.Buffer, len, metaStream);
                var id = new byte[16];
                reader.Read(id, id.Length);
                var transport = Transport.TransportOfName(reader.ReadString());
                transport.ReadTransportParams(reader);
                guid = new Guid(id);
                Trace.WriteLine($"{nameof(RemoteImporter)} meta client connected (ID={guid})");

                // process metadata updates
                while (!this.disposed)
                {
                    reader.Reset(sizeof(int));
                    Transport.Read(reader.Buffer, sizeof(int), metaStream);
                    var metalen = reader.ReadInt32();
                    if (metalen > 0)
                    {
                        reader.Reset(metalen);
                        Transport.Read(reader.Buffer, metalen, metaStream);
                        var meta = Metadata.Deserialize(reader);
                        if (meta.Kind == MetadataKind.StreamMetadata)
                        {
                            try
                            {
                                this.storeWriter.OpenStream((PsiStreamMetadata)meta);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError($"{nameof(RemoteImporter)} meta update duplicate stream - expected after reconnect (Name={meta.Name}, ID={guid}, Error={ex.Message})");
                            }
                        }
                        else if (meta.Kind == MetadataKind.RuntimeInfo)
                        {
                            this.storeWriter.WriteToCatalog((RuntimeInfo)meta);
                        }
                        else if (meta.Kind == MetadataKind.TypeSchema)
                        {
                            this.storeWriter.WriteToCatalog((TypeSchema)meta);
                        }
                        else
                        {
                            throw new NotSupportedException("Unknown metadata kind.");
                        }

                        Trace.WriteLine($"{nameof(RemoteImporter)} meta update (Name={meta.Name}, ID={guid})");
                    }
                    else
                    {
                        // "intermission" in meta updates
                        this.Importer = this.importerThunk(this.storeWriter.Name); // now that we have a populated catalog
                        this.storeWriter.InitializeStreamOpenedTimes(this.Importer.GetCurrentTime());
                        this.StartDataClient(guid, transport);
                        this.connected.Set();
                    }
                }
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(RemoteImporter)} meta connection error (Message={ex.Message}, ID={guid})");
                this.StartMetaClient(); // restart
            }
        }

        private void StartDataClient(Guid id, ITransport transport)
        {
            if (this.disposed)
            {
                return;
            }

            var dataClient = transport.Connect(this.host);
            dataClient.WriteSessionId(id);

            var thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (!this.disposed)
                    {
                        var data = dataClient.ReadMessage();
                        var envelope = data.Item1;
                        var message = data.Item2;

                        this.replayStart = envelope.OriginatingTime.Ticks + 1; // for restart

                        if (this.allowSequenceRestart)
                        {
                            // patch sequence ID resents (due to exporter process restart)
                            var sourceId = envelope.SourceId;
                            var sequenceId = envelope.SequenceId;
                            if (!this.lastSequenceIdPerStream.ContainsKey(sourceId))
                            {
                                this.lastSequenceIdPerStream.Add(sourceId, sequenceId - 1); // tracking new source
                            }

                            var lastSequenceId = this.lastSequenceIdPerStream[sourceId];
                            if (lastSequenceId >= sequenceId)
                            {
                                sequenceId = lastSequenceId + 1;
                                envelope = new Envelope(envelope.OriginatingTime, envelope.CreationTime, sourceId, sequenceId);
                            }

                            this.lastSequenceIdPerStream[sourceId] = sequenceId;
                        }

                        this.storeWriter.Write(new BufferReader(message), envelope);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{nameof(RemoteImporter)} data connection error (Message={ex.Message}, ID={id})");
                    dataClient.Dispose();
                }
            })) { IsBackground = true };
            thread.Start();
        }
    }
}
