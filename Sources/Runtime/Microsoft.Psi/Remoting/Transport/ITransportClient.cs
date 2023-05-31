// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;

    /// <summary>
    /// Interface representing a connected transport client.
    /// </summary>
    internal interface ITransportClient : IDisposable
    {
        /// <summary>
        /// Read session ID (GUID) over transport.
        /// </summary>
        /// <returns>ID read.</returns>
        Guid ReadSessionId();

        /// <summary>
        /// Write session ID (GUID) over transport.
        /// </summary>
        /// <param name="id">ID to be written.</param>
        void WriteSessionId(Guid id);

        /// <summary>
        /// Read message envelope and raw bytes over transport.
        /// </summary>
        /// <returns>Envelope and raw message bytes.</returns>
        Tuple<Envelope, byte[]> ReadMessage();

        /// <summary>
        /// Write message envelope and raw bytes over transport.
        /// </summary>
        /// <param name="envelope">Envelope to be written.</param>
        /// <param name="message">Message bytes to be written.</param>
        void WriteMessage(Envelope envelope, byte[] message);
    }
}