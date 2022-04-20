// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Rendezvous
{
    using System;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Rendezvous related operators.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Create a rendezvous endpoint from a <see cref="TcpWriter{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of data stream.</typeparam>
        /// <param name="writer"><see cref="TcpWriter{T}"/> from which to create endpoint.</param>
        /// <param name="address">Address with which to create endpoint.</param>
        /// <param name="streamName">The name of the rendezvous stream.</param>
        /// <returns>Rendezvous endpoint.</returns>
        public static Rendezvous.Endpoint ToRendezvousEndpoint<T>(this TcpWriter<T> writer, string address, string streamName)
        {
            // Each TcpWriter is an endpoint emitting a single stream
            return new Rendezvous.TcpSourceEndpoint(address, writer.Port, new[] { new Rendezvous.Stream(streamName, typeof(T)) });
        }

        /// <summary>
        /// Create a <see cref="TcpSource{T}"/> from a <see cref="Rendezvous.TcpSourceEndpoint"/>.
        /// </summary>
        /// <typeparam name="T">Type of data stream.</typeparam>
        /// <param name="endpoint"><see cref="Rendezvous.TcpSourceEndpoint"/> from which to create .</param>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="deallocator">An optional deallocator for the data.</param>
        /// <param name="useSourceOriginatingTimes">An optional parameter indicating whether to use originating times received from the source over the network or to re-timestamp with the current pipeline time upon receiving.</param>
        /// <param name="name">An optional name for the TCP source component.</param>
        /// <returns><see cref="TcpSource{T}"/>.</returns>
        public static TcpSource<T> ToTcpSource<T>(
            this Rendezvous.TcpSourceEndpoint endpoint,
            Pipeline pipeline,
            IFormatDeserializer deserializer,
            Action<T> deallocator = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(TcpSource<T>))
            => new (pipeline, endpoint.Host, endpoint.Port, deserializer, deallocator, useSourceOriginatingTimes, name);

        /// <summary>
        /// Create a rendezvous endpoint from a <see cref="NetMQWriter"/>.
        /// </summary>
        /// <param name="writer"><see cref="NetMQWriter"/> from which to create endpoint.</param>
        /// <returns>Rendezvous endpoint.</returns>
        public static Rendezvous.Endpoint ToRendezvousEndpoint(this NetMQWriter writer)
        {
            // Each NetWriter is an endpoint emitting one or more topics/streams.
            var endpoint = new Rendezvous.NetMQSourceEndpoint(writer.Address);
            foreach (var (name, type) in writer.Topics)
            {
                endpoint.AddStream(new Rendezvous.Stream(name, type));
            }

            return endpoint;
        }

        /// <summary>
        /// Create a rendezvous endpoint from a <see cref="RemoteClockExporter"/>.
        /// </summary>
        /// <param name="exporter"><see cref="RemoteClockExporter"/> from which to create endpoint.</param>
        /// <param name="host">Host address with which to create endpoint.</param>
        /// <returns>Rendezvous endpoint.</returns>
        public static Rendezvous.Endpoint ToRendezvousEndpoint(this RemoteClockExporter exporter, string host)
        {
            return new Rendezvous.RemoteClockExporterEndpoint(host, exporter.Port);
        }

        /// <summary>
        /// Create a <see cref="NetMQSource{T}"/> from a <see cref="Rendezvous.NetMQSourceEndpoint"/>.
        /// </summary>
        /// <typeparam name="T">Type of data stream.</typeparam>
        /// <param name="endpoint"><see cref="Rendezvous.NetMQSourceEndpoint"/> from which to create .</param>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="topic">Topic name.</param>
        /// <param name="deserializer">The deserializer to use to deserialize messages.</param>
        /// <param name="useSourceOriginatingTimes">Flag indicating whether or not to post with originating times received over the socket. If false, we ignore them and instead use pipeline's current time.</param>
        /// <returns><see cref="NetMQSource{T}"/>.</returns>
        public static NetMQSource<T> ToNetMQSource<T>(this Rendezvous.NetMQSourceEndpoint endpoint, Pipeline pipeline, string topic, IFormatDeserializer deserializer, bool useSourceOriginatingTimes = true)
            => new NetMQSource<T>(pipeline, topic, endpoint.Address, deserializer, useSourceOriginatingTimes);

        /// <summary>
        /// Create a rendezvous endpoint from a <see cref="RemoteExporter"/>.
        /// </summary>
        /// <param name="exporter"><see cref="RemoteExporter"/> from which to create endpoint.</param>
        /// <param name="host">Host name with which to create endpoint.</param>
        /// <returns>Rendezvous endpoint.</returns>
        public static Rendezvous.Endpoint ToRendezvousEndpoint(this RemoteExporter exporter, string host)
        {
            // Each RemoteExporter is an endpoint emitting one or more streams.
            var endpoint = new Rendezvous.RemoteExporterEndpoint(host, exporter.Port, exporter.TransportKind);
            foreach (var m in exporter.Exporter.Metadata)
            {
                endpoint.AddStream(new Rendezvous.Stream(m.Name, m.TypeName));
            }

            return endpoint;
        }

        /// <summary>
        /// Create a <see cref="RemoteImporter"/> from a <see cref="Rendezvous.RemoteExporterEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"><see cref="Rendezvous.RemoteExporterEndpoint"/> from which to create .</param>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <returns><see cref="RemoteImporter"/>.</returns>
        public static RemoteImporter ToRemoteImporter(this Rendezvous.RemoteExporterEndpoint endpoint, Pipeline pipeline)
        {
            return new RemoteImporter(pipeline, endpoint.Host, endpoint.Port);
        }

        /// <summary>
        /// Create a <see cref="RemoteClockImporter"/> from a <see cref="Rendezvous.RemoteClockExporterEndpoint"/>.
        /// </summary>
        /// <param name="endpoint"><see cref="Rendezvous.RemoteClockExporterEndpoint"/> from which to create .</param>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <returns><see cref="RemoteClockImporter"/>.</returns>
        public static RemoteClockImporter ToRemoteClockImporter(this Rendezvous.RemoteClockExporterEndpoint endpoint, Pipeline pipeline)
        {
            return new RemoteClockImporter(pipeline, endpoint.Host, endpoint.Port);
        }
    }
}
