// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;

    /// <summary>
    /// Format deserializer interface.
    /// </summary>
    public interface IFormatDeserializer
    {
        /// <summary>
        /// Deserialize single message and originating time stamp payload.
        /// </summary>
        /// <param name="payload">Payload bytes.</param>
        /// <param name="index">Starting index of message data.</param>
        /// <param name="count">Number of bytes constituting message data.</param>
        /// <returns>Dynamic of primitive or IEnumerable/ExpandoObject of primitive as well as originating time stamp.</returns>
        (dynamic Message, DateTime OriginatingTime) DeserializeMessage(byte[] payload, int index, int count);
    }
}