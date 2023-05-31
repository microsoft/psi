// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Helper class for making new formats (implementations of <see cref="IFormatSerializer"/>/<see cref="IFormatDeserializer"/>.
    /// </summary>
    /// <typeparam name="T">Type which is serialized/deserialized.</typeparam>
    public class Format<T> : IFormatSerializer, IFormatDeserializer, IDisposable
    {
        private readonly Func<T, DateTime, (byte[], int, int)> serialize;
        private readonly Func<byte[], int, int, (T, DateTime)> deserialize;
        private readonly MemoryStream memoryStream = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Format{T}"/> class.
        /// </summary>
        /// <param name="serializeFunc">Serialization function.</param>
        /// <param name="deserializeFunc">Deserialization function.</param>
        public Format(
            Func<T, DateTime, (byte[] Bytes, int Index, int Count)> serializeFunc,
            Func<byte[], int, int, (T Message, DateTime OriginatingTime)> deserializeFunc)
        {
            this.serialize = serializeFunc;
            this.deserialize = deserializeFunc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Format{T}"/> class.
        /// </summary>
        /// <param name="serializeAction">Action to serialize using <see cref="BinaryWriter"/>.</param>
        /// <param name="deserializeFunc">Function to deserialize using <see cref="BinaryReader"/> (also given raw payload, offset, length).</param>
        /// <returns>Serialization format.</returns>
        public Format(
            Action<T, BinaryWriter> serializeAction,
            Func<BinaryReader, byte[], int, int, T> deserializeFunc)
        {
            this.memoryStream = new MemoryStream();

            this.serialize = (val, originatingTime) =>
            {
                this.memoryStream.Position = 0;
                using var writer = new BinaryWriter(this.memoryStream, Encoding.UTF8, true);
                writer.Write(originatingTime.ToFileTimeUtc());
                serializeAction(val, writer);
                return (this.memoryStream.GetBuffer(), 0, (int)this.memoryStream.Length);
            };

            this.deserialize = (payload, offset, length) =>
            {
                using var reader = new BinaryReader(new MemoryStream(payload, offset, length), Encoding.UTF8);
                var originatingTime = DateTime.FromFileTimeUtc(reader.ReadInt64());
                var val = deserializeFunc(reader, payload, offset, length);
                return (val, originatingTime);
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Format{T}"/> class.
        /// </summary>
        /// <param name="serializeAction">Action to serialize using <see cref="BinaryWriter"/>.</param>
        /// <param name="deserializeFunc">Function to deserialize using <see cref="BinaryReader"/> (also given raw payload, offset, length).</param>
        /// <returns>Serialization format.</returns>
        public Format(
            Action<T, BinaryWriter> serializeAction,
            Func<BinaryReader, T> deserializeFunc)
            : this(serializeAction, (reader, _, _, _) => deserializeFunc(reader))
        {
        }

        /// <inheritdoc />
        public (byte[] Bytes, int Index, int Count) SerializeMessage(dynamic message, DateTime originatingTime)
        {
            return this.serialize(message, originatingTime);
        }

        /// <inheritdoc />
        public (dynamic Message, DateTime OriginatingTime) DeserializeMessage(byte[] payload, int index, int count)
        {
            return this.deserialize(payload, index, count);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.memoryStream != null)
            {
                this.memoryStream.Dispose();
            }
        }
    }
}
