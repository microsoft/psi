// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Persistent format serializer/deserializer.
    /// </summary>
    /// <typeparam name="T">Type which is serialized/deserialized.</typeparam>
    public class PersistentFormat<T> : IPersistentFormatSerializer, IPersistentFormatDeserializer
    {
        private readonly Func<dynamic, Stream, dynamic> persistHeader;
        private readonly Action<dynamic, DateTime, bool, Stream, dynamic> persistRecord;
        private readonly Action<Stream, dynamic> persistFooter;
        private readonly Func<Stream, IEnumerable<(dynamic, DateTime)>> deserializeRecords;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentFormat{T}"/> class.
        /// </summary>
        /// <param name="persistHeader">Header persistence function.</param>
        /// <param name="persistRecord">Record persistence function.</param>
        /// <param name="persistFooter">Footer persistence function.</param>
        /// <param name="deserializeRecords">Deserialization function.</param>
        public PersistentFormat(
            Func<dynamic, Stream, dynamic> persistHeader,
            Action<dynamic, DateTime, bool, Stream, dynamic> persistRecord,
            Action<Stream, dynamic> persistFooter,
            Func<Stream, IEnumerable<(dynamic, DateTime)>> deserializeRecords)
        {
            this.persistHeader = persistHeader;
            this.persistRecord = persistRecord;
            this.persistFooter = persistFooter;
            this.deserializeRecords = deserializeRecords;
        }

        /// <inheritdoc />
        public dynamic PersistHeader(dynamic message, Stream stream)
        {
            return this.persistHeader(message, stream);
        }

        /// <inheritdoc />
        public void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state)
        {
            this.persistRecord(message, originatingTime, first, stream, state);
        }

        /// <inheritdoc />
        public void PersistFooter(Stream stream, dynamic state)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
