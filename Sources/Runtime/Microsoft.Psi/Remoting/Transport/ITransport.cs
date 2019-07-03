// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Interface representing network transport.
    /// </summary>
    internal interface ITransport : IDisposable
    {
        /// <summary>
        /// Gets kind of network transport.
        /// </summary>
        TransportKind Transport { get; }

        /// <summary>
        /// Start listening (e.g. on IP port).
        /// </summary>
        void StartListening();

        /// <summary>
        /// Write transport-specific parameters (e.g. port number, pipe name, ...).
        /// </summary>
        /// <param name="writer">Buffer writer to which to write.</param>
        void WriteTransportParams(BufferWriter writer);

        /// <summary>
        /// Read transport-specific parameters (e.g. port number, pipe name, ...).
        /// </summary>
        /// <param name="reader">Buffer reader from which to read.</param>
        void ReadTransportParams(BufferReader reader);

        /// <summary>
        /// Accept new transport client.
        /// </summary>
        /// <returns>Accepted client.</returns>
        ITransportClient AcceptClient();

        /// <summary>
        /// Connect to remote host.
        /// </summary>
        /// <param name="host">Host name to which to connect.</param>
        /// <returns>Connected client.</returns>
        ITransportClient Connect(string host);
    }
}