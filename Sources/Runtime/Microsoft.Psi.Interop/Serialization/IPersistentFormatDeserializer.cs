// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Persistent format deserializer interface.
    /// </summary>
    public interface IPersistentFormatDeserializer
    {
        /// <summary>
        /// Deserialize stream of messages and originating time stamps.
        /// </summary>
        /// <param name="stream">Stream of serialized message data.</param>
        /// <returns>Sequence of dynamic of primitive or IEnumerable/ExpandoObject of primitive as well as originating time stamp.</returns>
        IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream);
    }
}
