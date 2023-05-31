// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    /// <summary>
    /// Kinds of supported network transports.
    /// </summary>
    public enum TransportKind
    {
        /// <summary>
        /// Transmission Control Protocol/Internet Protocol.
        /// </summary>
        /// <remarks>No packet loss.</remarks>
        Tcp,

        /// <summary>
        /// User Datagram Protocol/Internet Protocol.
        /// </summary>
        /// <remarks>Possible packet loss.</remarks>
        Udp,

        /// <summary>
        /// Named Pipes protocol.
        /// </summary>
        /// <remarks>No packet loss. Supports security.</remarks>
        NamedPipes,
    }
}