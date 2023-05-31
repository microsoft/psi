// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Transport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Persisted file source component.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class FileSource<T> : Generator<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSource{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">File name to which to persist.</param>
        /// <param name="deserializer">Format serializer with which messages are deserialized.</param>
        /// <param name="name">An optional name for the component.</param>
        public FileSource(Pipeline pipeline, string filename, IPersistentFormatDeserializer deserializer, string name = nameof(FileSource<T>))
            : base(pipeline, EnumerateFile(filename, deserializer), GetStartTimeFromFile(filename, deserializer), name: name)
        {
        }

        private static IEnumerator<(T, DateTime)> EnumerateFile(string filename, IPersistentFormatDeserializer deserializer)
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            foreach (var record in deserializer.DeserializeRecords(stream))
            {
                yield return ((T)record.Item1, record.Item2);
            }
        }

        private static DateTime GetStartTimeFromFile(string filename, IPersistentFormatDeserializer deserializer)
        {
            DateTime startTime;
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            (_, startTime) = deserializer.DeserializeRecords(stream).First();
            return startTime;
        }
    }
}
