// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Format
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using MessagePack;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Format serializer/deserializer for MessagePack.
    /// </summary>
    public class MessagePackFormat : IFormatSerializer, IPersistentFormatSerializer, IFormatDeserializer, IPersistentFormatDeserializer
    {
        private MessagePackFormat()
        {
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static MessagePackFormat Instance { get; } = new ();

        /// <inheritdoc />
        public (byte[], int, int) SerializeMessage(dynamic message, DateTime originatingTime)
        {
            var bytes = MessagePackSerializer.Serialize(new { message, originatingTime = originatingTime.Ticks });
            return (bytes, 0, bytes.Length);
        }

        /// <inheritdoc />
        public (dynamic, DateTime) DeserializeMessage(byte[] payload, int index, int count)
        {
            var content = MessagePackSerializer.Deserialize<dynamic>(new ArraySegment<byte>(payload, index, count));
            return (this.NormalizeValue(content["message"]), new DateTime((long)content["originatingTime"]));
        }

        /// <inheritdoc />
        public dynamic PersistHeader(dynamic message, Stream stream)
        {
            return new BinaryWriter(stream);
        }

        /// <inheritdoc />
        public void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state)
        {
            var writer = state as BinaryWriter;
            var payload = this.SerializeMessage(message, originatingTime);
            writer.Write(payload.Item3); // length prefix
            writer.Write(payload.Item1); // bytes
        }

        /// <inheritdoc />
        public void PersistFooter(Stream stream, dynamic state)
        {
            var writer = state as BinaryWriter;
            writer.Write(0); // final zero-length as terminator
            writer.Dispose();
        }

        /// <inheritdoc />
        public IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var length = reader.ReadInt32();
            while (length != 0)
            {
                yield return this.DeserializeMessage(reader.ReadBytes(length), 0, length);
                length = reader.ReadInt32();
            }
        }

        private dynamic NormalizeValue(dynamic value)
        {
            // Console.WriteLine("MessagePackFormat.cs, NormalizeValue - enter");
            if (typeof(IDictionary<object, object>).IsAssignableFrom(((object)value).GetType()))
            {
                // library returns structured values as object dictionary - convert to ExpandoObject
                // Console.WriteLine("MessagePackFormat.cs, NormalizeValue - is dictionary");
                var expando = new ExpandoObject();
                var dict = expando as IDictionary<string, dynamic>;
                foreach (var kv in value as IDictionary<object, object>)
                {
                    // Console.WriteLine("MessagePackFormat.cs, NormalizeValue - kv: '{0}'", kv);
                    dict[kv.Key.ToString()] = this.NormalizeValue(kv.Value); // potentially recursively
                }

                return expando;
            }

            return value;
        }
    }
}