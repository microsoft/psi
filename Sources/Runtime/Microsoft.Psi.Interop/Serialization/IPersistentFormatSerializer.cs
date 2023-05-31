// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;
    using System.IO;

    /// <summary>
    /// Persistent format serializer interface.
    /// </summary>
    public interface IPersistentFormatSerializer
    {
        /// <summary>
        /// Persist header data (if any).
        /// </summary>
        /// <remarks>This is called once at the start of each persisted partition/file.</remarks>
        /// <param name="message">Message of any type.</param>
        /// <param name="stream">Stream of serialized bytes along with delimiter or length-prefix.</param>
        /// <returns>State object of any type; handed back on calls below, simplifying stateless implementation.</returns>
        dynamic PersistHeader(dynamic message, Stream stream);

        /// <summary>
        /// Persist single serialized message with originating time stamp.
        /// </summary>
        /// <remarks>This is called once per message.</remarks>
        /// <param name="message">Message of any type.</param>
        /// <param name="originatingTime">Originating time of message.</param>
        /// <param name="first">Flag indicating whether this is the first record; useful for delimiters in some formats.</param>
        /// <param name="stream">Stream of serialized bytes along with delimiter or length-prefix.</param>
        /// <param name="state">State object of any type; previously returned by header call above.</param>
        void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state);

        /// <summary>
        /// Persist footer data (if any).
        /// </summary>
        /// <remarks>This is called once upon termination of data stream.</remarks>
        /// <param name="stream">Stream of serialized bytes along with delimiter or length-prefix.</param>
        /// <param name="state">State object of any type; previously returned by header call above.</param>
        void PersistFooter(Stream stream, dynamic state);
    }
}
