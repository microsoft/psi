// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Format
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using CsvHelper;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Format serializer/deserializer for CSV.
    /// </summary>
    public class CsvFormat : IFormatSerializer, IPersistentFormatSerializer, IFormatDeserializer, IPersistentFormatDeserializer
    {
        private const string TimeHeader = "_OriginatingTime_";
        private const string ValueHeader = "_Value_";
        private const string ColumnHeader = "_Column{0}_";

        private CsvFormat()
        {
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static CsvFormat Instance { get; } = new CsvFormat();

        /// <inheritdoc />
        public (byte[], int, int) SerializeMessage(dynamic message, DateTime originatingTime)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms))
            using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
            {
                this.WriteCsvHeader(message, csv); // individual messages contain header for field names
                this.WriteCsvRecord(message, originatingTime, csv);
                sw.Flush();
                var bytes = ms.GetBuffer();
                return (bytes, 0, (int)ms.Length);
            }
        }

        /// <inheritdoc />
        public (dynamic, DateTime) DeserializeMessage(byte[] payload, int index, int count)
        {
            using (var ms = new MemoryStream(payload, index, count))
            using (var sr = new StreamReader(ms))
            using (var csv = new CsvReader(sr, CultureInfo.InvariantCulture))
            {
                csv.Read();
                return this.ReadCsvRecord(csv);
            }
        }

        /// <inheritdoc />
        public dynamic PersistHeader(dynamic message, Stream stream)
        {
            var csvWriter = new CsvWriter(new StreamWriter(stream), CultureInfo.InvariantCulture);
            this.WriteCsvHeader(message, csvWriter);
            return csvWriter;
        }

        /// <inheritdoc />
        public void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic csvWriter)
        {
            this.WriteCsvRecord(message, originatingTime, csvWriter);
        }

        /// <inheritdoc />
        public void PersistFooter(Stream stream, dynamic csvWriter)
        {
            // no footer in format, but take the opportunity to close the writer
            csvWriter.Dispose();
        }

        /// <inheritdoc />
        public IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream)
        {
            using (var csv = new CsvReader(new StreamReader(stream), CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {
                    yield return this.ReadCsvRecord(csv);
                }
            }
        }

        private void WriteCsvHeader(dynamic message, CsvWriter writer)
        {
            var type = ((object)message).GetType();
            writer.WriteField(TimeHeader);
            if (type.IsPrimitive || typeof(string).IsAssignableFrom(type))
            {
                // messages comprised of single primitives become _Value_ column
                writer.WriteField(ValueHeader);
            }
            else if (typeof(IEnumerable<double>).IsAssignableFrom(type))
            {
                // special case for double collections; become _Column0_, _Column1_, ...
                var count = (message as IEnumerable<double>).Count();
                for (var i = 0; i < count; i++)
                {
                    writer.WriteField(string.Format(ColumnHeader, i));
                }
            }
            else if (typeof(IDictionary<string, dynamic>).IsAssignableFrom(type))
            {
                // CsvHelper library doesn't handle ExpandoObjects. We do here.
                foreach (var kv in message as IDictionary<string, dynamic>)
                {
                    writer.WriteField(kv.Key);
                }
            }
            else
            {
                writer.WriteHeader(type);
            }

            writer.NextRecord();
        }

        private void WriteCsvRecord(dynamic message, DateTime originatingTime, CsvWriter writer)
        {
            var type = ((object)message).GetType();
            writer.WriteField(originatingTime.ToString("o"));
            if (typeof(string).IsAssignableFrom(type))
            {
                writer.WriteField<string>(message);
            }
            else if (typeof(IEnumerable<double>).IsAssignableFrom(type))
            {
                // special case for double collections
                foreach (var d in message as IEnumerable<double>)
                {
                    writer.WriteField<double>(d);
                }
            }
            else if (typeof(IDictionary<string, dynamic>).IsAssignableFrom(type))
            {
                // CsvHelper library doesn't handle ExpandoObjects. We do here.
                foreach (var kv in message as IDictionary<string, dynamic>)
                {
                    writer.WriteField(kv.Value);
                }
            }
            else
            {
                writer.WriteRecord(message);
            }

            writer.NextRecord();
        }

        private dynamic TryCoerceType(string value)
        {
            // attempt to convert string fields to common primitive types
            if (bool.TryParse(value, out bool b))
            {
                return b;
            }

            if (double.TryParse(value, out double d))
            {
                return d;
            }

            if (DateTime.TryParse(value, out DateTime dt))
            {
                return dt;
            }

            return value; // otherwise, assume string
        }

        private (dynamic, DateTime) ReadCsvRecord(CsvReader reader)
        {
            var dict = reader.GetRecord<dynamic>() as IDictionary<string, dynamic>;
            var time = reader.GetField<DateTime>(TimeHeader).ToUniversalTime();
            if (dict.ContainsKey(ValueHeader))
            {
                // handle messages comprised of single primitives become _Value_ column
                return (this.TryCoerceType(dict[ValueHeader]), time);
            }

            if (dict.ContainsKey(string.Format(ColumnHeader, 0)))
            {
                // handle special case for double collections; become _Column0_, _Column1_, ...
                var count = dict.Count - 1; // not including _Time_ column
                var array = new double[count];
                for (var i = 0; i < count; i++)
                {
                    array[i] = double.Parse(dict[string.Format(ColumnHeader, i)]);
                }

                return (array, time);
            }

            // otherwise, convert to ExpandoObject
            var message = new ExpandoObject() as IDictionary<string, dynamic>;
            dict.Remove(TimeHeader); // not including _Time_ in message itself
            foreach (var key in dict.Keys)
            {
                message[key] = this.TryCoerceType(dict[key]);
            }

            return (message, time);
        }
    }
}