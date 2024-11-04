// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Component that maintains rendezvous information.
    /// </summary>
    public class Rendezvous
    {
        /// <summary>
        /// A rendezvous may know about many processes, each with many endpoints, each with many streams.
        /// </summary>
        private readonly ConcurrentDictionary<string, Process> processes = new ();

        private EventHandler<Process> processAdded;

        /// <summary>
        /// Event raised when processes are added.
        /// </summary>
        /// <remarks>Includes processes added before subscription.</remarks>
        public event EventHandler<Process> ProcessAdded
        {
            add
            {
                // inform late-joining handler of currently added processes
                foreach (var p in this.Processes)
                {
                    value.Invoke(this, p);
                }

                this.processAdded += value;
            }

            remove
            {
                this.processAdded -= value;
            }
        }

        /// <summary>
        /// Event raised when processes are removed.
        /// </summary>
        public event EventHandler<Process> ProcessRemoved;

        /// <summary>
        /// Gets the currently known processes.
        /// </summary>
        public IEnumerable<Process> Processes
        {
            get
            {
                return this.processes.Values;
            }
        }

        /// <summary>
        /// Try to add a new process, if not already present.
        /// </summary>
        /// <param name="process">Process to add.</param>
        /// <returns>A value indicating whether the process was added.</returns>
        public bool TryAddProcess(Process process)
        {
            if (this.processes.TryAdd(process.Name, process))
            {
                this.processAdded?.Invoke(this, process);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to remove a process if present.
        /// </summary>
        /// <param name="process">Process to remove.</param>
        /// <returns>A value indicating whether the process was removed.</returns>
        public bool TryRemoveProcess(Process process)
        {
            if (this.processes.TryRemove(process.Name, out _))
            {
                this.ProcessRemoved?.Invoke(this, process);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try to remove a process if present.
        /// </summary>
        /// <param name="processName">Name of process to remove.</param>
        /// <returns>A value indicating whether the process was removed.</returns>
        public bool TryRemoveProcess(string processName)
        {
            if (this.TryGetProcess(processName, out Process process))
            {
                return this.TryRemoveProcess(process);
            }

            return false;
        }

        /// <summary>
        /// Try to get process by name.
        /// </summary>
        /// <param name="processName">Process name.</param>
        /// <param name="process">Process or null if not found.</param>
        /// <returns>A value indicating whether named process found.</returns>
        public bool TryGetProcess(string processName, out Process process)
        {
            return this.processes.TryGetValue(processName, out process);
        }

        /// <summary>
        /// Represents a remoted stream of data.
        /// </summary>
        public class Stream
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Stream"/> class.
            /// </summary>
            /// <param name="streamName">Stream name.</param>
            /// <param name="typeName">Type name of stream data.</param>
            public Stream(string streamName, string typeName)
            {
                this.StreamName = streamName;
                this.TypeName = typeName;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Stream"/> class.
            /// </summary>
            /// <param name="streamName">Stream name.</param>
            /// <param name="type">Type of stream data.</param>
            public Stream(string streamName, Type type)
                : this(streamName, type.AssemblyQualifiedName)
            {
            }

            /// <summary>
            /// Gets the stream name.
            /// </summary>
            public string StreamName { get; private set; }

            /// <summary>
            /// Gets the type name of the stream data.
            /// </summary>
            public string TypeName { get; private set; }
        }

        /// <summary>
        /// Represents an endpoint providing remoted data streams.
        /// </summary>
        public abstract class Endpoint
        {
            private readonly ConcurrentDictionary<string, Stream> streams;

            /// <summary>
            /// Initializes a new instance of the <see cref="Endpoint"/> class.
            /// </summary>
            /// <param name="streams">Endpoint streams.</param>
            public Endpoint(IEnumerable<Stream> streams)
            {
                this.streams = new ConcurrentDictionary<string, Stream>(streams.Select(s => new KeyValuePair<string, Stream>(s.StreamName, s)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Endpoint"/> class.
            /// </summary>
            public Endpoint()
                : this(Enumerable.Empty<Stream>())
            {
            }

            /// <summary>
            /// Gets the streams.
            /// </summary>
            public IEnumerable<Stream> Streams
            {
                get { return this.streams.Values; }
            }

            /// <summary>
            /// Add new stream.
            /// </summary>
            /// <param name="stream">Endpoint stream to add.</param>
            public virtual void AddStream(Stream stream)
            {
                this.streams.TryAdd(stream.StreamName, stream);
            }
        }

        /// <summary>
        /// Represents a simple TCP source endpoint providing a single remoted data stream.
        /// </summary>
        public class TcpSourceEndpoint : Endpoint
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TcpSourceEndpoint"/> class.
            /// </summary>
            /// <param name="host">Host name used by the endpoint.</param>
            /// <param name="port">Port number used by the endpoint.</param>
            /// <param name="stream">Endpoint stream.</param>
            public TcpSourceEndpoint(string host, int port, Stream stream = null)
                : base(stream is null ? Enumerable.Empty<Stream>() : new[] { stream })
            {
                if (string.IsNullOrEmpty(host))
                {
                    throw new ArgumentException("Host must be not null or empty.");
                }

                this.Host = host;
                this.Port = port;
            }

            /// <summary>
            /// Gets the endpoint address.
            /// </summary>
            public string Host { get; private set; }

            /// <summary>
            /// Gets the endpoint port number.
            /// </summary>
            public int Port { get; private set; }

            /// <summary>
            /// Gets the stream (Tcp endpoints have only one).
            /// </summary>
            public Stream Stream => this.Streams.FirstOrDefault();

            /// <inheritdoc/>
            public override void AddStream(Stream stream)
            {
                if (this.Streams.Count() > 0)
                {
                    throw new InvalidOperationException($"Cannot add more than one stream to a single {nameof(TcpSourceEndpoint)}");
                }

                base.AddStream(stream);
            }
        }

        /// <summary>
        /// Represents a NetMQ source endpoint providing remoted data streams.
        /// </summary>
        public class NetMQSourceEndpoint : Endpoint
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NetMQSourceEndpoint"/> class.
            /// </summary>
            /// <param name="address">Address used by the endpoint.</param>
            /// <param name="streams">Endpoint streams.</param>
            public NetMQSourceEndpoint(string address, IEnumerable<Stream> streams)
                : base(streams)
            {
                this.Address = address;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="NetMQSourceEndpoint"/> class.
            /// </summary>
            /// <param name="address">Address used by the endpoint.</param>
            public NetMQSourceEndpoint(string address)
                : this(address, Enumerable.Empty<Stream>())
            {
            }

            /// <summary>
            /// Gets the endpoint address.
            /// </summary>
            public string Address { get; private set; }
        }

        /// <summary>
        /// Represents a remote exporter endpoint providing remoted data streams.
        /// </summary>
        public class RemoteExporterEndpoint : Endpoint
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RemoteExporterEndpoint"/> class.
            /// </summary>
            /// <param name="host">Host name used by the endpoint.</param>
            /// <param name="port">Port used by the endpoint.</param>
            /// <param name="transport">Tranport kind used by the endpoint.</param>
            /// <param name="streams">Endpoint streams.</param>
            public RemoteExporterEndpoint(string host, int port, TransportKind transport, IEnumerable<Stream> streams)
                : base(streams)
            {
                this.Host = host;
                this.Port = port;
                this.Transport = transport;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RemoteExporterEndpoint"/> class.
            /// </summary>
            /// <param name="host">Host name used by the endpoint.</param>
            /// <param name="port">Port used by the endpoint.</param>
            /// <param name="transport">Tranport kind used by the endpoint.</param>
            public RemoteExporterEndpoint(string host, int port, TransportKind transport)
                : this(host, port, transport, Enumerable.Empty<Stream>())
            {
            }

            /// <summary>
            /// Gets the endpoint host name.
            /// </summary>
            public string Host { get; private set; }

            /// <summary>
            /// Gets the endpoint port.
            /// </summary>
            public int Port { get; private set; }

            /// <summary>
            /// Gets the endpoint transport kind.
            /// </summary>
            public TransportKind Transport { get; private set; }
        }

        /// <summary>
        /// Represents a remote clock exporter endpoint providing clock information.
        /// </summary>
        /// <remarks>
        /// Endpoint does not provide any streams. Clock information is exchanged directly.
        /// </remarks>
        public class RemoteClockExporterEndpoint : Endpoint
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RemoteClockExporterEndpoint"/> class.
            /// </summary>
            /// <param name="host">Host name used by the endpoint.</param>
            /// <param name="port">Port used by the endpoint.</param>
            public RemoteClockExporterEndpoint(string host, int port)
            {
                this.Host = host;
                this.Port = port;
            }

            /// <summary>
            /// Gets the endpoint host name.
            /// </summary>
            public string Host { get; private set; }

            /// <summary>
            /// Gets the endpoint port.
            /// </summary>
            public int Port { get; private set; }

            /// <inheritdoc/>
            public override void AddStream(Stream stream)
            {
                throw new InvalidOperationException($"Cannot add streams to a {nameof(RemoteClockExporterEndpoint)}");
            }
        }

        /// <summary>
        /// Represents an application process hosting endpoints.
        /// </summary>
        public class Process
        {
            private readonly List<Endpoint> endpoints;

            /// <summary>
            /// Initializes a new instance of the <see cref="Process"/> class.
            /// </summary>
            /// <param name="name">Unique name by which to refer to the process.</param>
            /// <param name="endpoints">Process endpoints.</param>
            /// <param name="version">Optional process version (allowing negotiation of client compatibility).</param>
            public Process(string name, IEnumerable<Endpoint> endpoints, string version = null)
            {
                this.Name = name;
                this.Version = version ?? string.Empty;
                this.endpoints = endpoints.ToList();
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Process"/> class.
            /// </summary>
            /// <param name="name">Unique name by which to refer to the process.</param>
            /// <param name="version">Optional process version (allowing negotiation of client compatibility).</param>
            public Process(string name, string version = null)
                : this(name, Enumerable.Empty<Endpoint>(), version)
            {
            }

            /// <summary>
            /// Gets the process name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Gets the process version.
            /// </summary>
            public string Version { get; private set; }

            /// <summary>
            /// Gets the endpoints.
            /// </summary>
            public IEnumerable<Endpoint> Endpoints
            {
                get { return this.endpoints; }
            }

            /// <summary>
            /// Add new endpoint.
            /// </summary>
            /// <param name="endpoint">Process endpoint to add.</param>
            public void AddEndpoint(Endpoint endpoint)
            {
                this.endpoints.Add(endpoint);
            }
        }
    }
}
