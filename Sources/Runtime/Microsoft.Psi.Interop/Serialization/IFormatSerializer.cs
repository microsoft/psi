// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;

    /// <summary>
    /// Format serializer interface.
    /// </summary>
    public interface IFormatSerializer
    {
        /// <summary>
        /// Serialize single message with originating time stamp.
        /// </summary>
        /// <param name="message">Message of any type.</param>
        /// <param name="originatingTime">Originating time of message.</param>
        /// <returns>Serialized bytes, index and count.</returns>
        (byte[] Bytes, int Index, int Count) SerializeMessage(dynamic message, DateTime originatingTime);
    }
}
