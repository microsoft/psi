// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Format
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Psi.Interop.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Format serializer/deserializer for JSON.
    /// </summary>
    public class JsonFormat : IFormatSerializer, IPersistentFormatSerializer, IFormatDeserializer, IPersistentFormatDeserializer
    {
        private JsonFormat()
        {
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static JsonFormat Instance { get; } = new JsonFormat();

        /// <inheritdoc />
        public (byte[], int, int) SerializeMessage(dynamic message, DateTime originatingTime)
        {
            // { originatingTime = ..., message = ... }
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { originatingTime, message }));
            return (bytes, 0, bytes.Length);
        }

        /// <inheritdoc />
        public (dynamic, DateTime) DeserializeMessage(byte[] payload, int index, int count)
        {
            // { originatingTime = ..., message = ... }
            var tok = JsonConvert.DeserializeObject<JToken>(Encoding.UTF8.GetString(payload, index, count));
            var originatingTime = tok["originatingTime"].Value<DateTime>();
            var msg = this.JObjectToDynamic(tok["message"]);
            return (msg, originatingTime);
        }

        /// <inheritdoc />
        public dynamic PersistHeader(dynamic message, Stream stream)
        {
            // persisted form as array [<message>,<message>,...]
            stream.WriteByte((byte)'[');
            return null;
        }

        /// <inheritdoc />
        public void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state)
        {
            (byte[], int, int) msg = this.SerializeMessage(message, originatingTime);
            if (!first)
            {
                // commas *before* all but first record in persisted form as array [<message>,<message>,...]
                stream.WriteByte((byte)',');
            }

            stream.Write(msg.Item1, msg.Item2, msg.Item3);
        }

        /// <inheritdoc />
        public void PersistFooter(Stream stream, dynamic state)
        {
            // close array in persisted form [<message>,<message>,...]
            stream.WriteByte((byte)']');
        }

        /// <inheritdoc />
        public IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var obj = this.JObjectToDynamic(JObject.Load(reader));
                    yield return (obj.message, obj.originatingTime);
                }
            }
        }

        private dynamic JObjectToDynamic(JToken tok)
        {
            switch (tok.Type)
            {
                case JTokenType.Object:
                    var dict = new ExpandoObject() as IDictionary<string, dynamic>;
                    tok.Children<JProperty>().ToList().ForEach(p => dict.Add(p.Name, this.JObjectToDynamic(p.Value)));
                    return dict;
                case JTokenType.Array:
                    return tok.Children().Select(this.JObjectToDynamic).ToArray();
                case JTokenType.String:
                    return tok.Value<string>();
                case JTokenType.Float:
                    return tok.Value<double>();
                case JTokenType.Integer:
                    return tok.Value<int>();
                case JTokenType.Boolean:
                    return tok.Value<bool>();
                case JTokenType.Null:
                    return null;
                case JTokenType.Bytes:
                    return tok.Value<byte[]>();
                case JTokenType.Date:
                    return tok.Value<DateTime>();
                case JTokenType.Guid:
                    return tok.Value<Guid>();
                case JTokenType.TimeSpan:
                    return tok.Value<TimeSpan>();
                case JTokenType.Uri:
                    return tok.Value<Uri>();
                default: throw new ArgumentException($"Unexpected JTokenType: {tok}");
            }
        }
    }
}