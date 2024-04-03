// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Defines a class used in importing data into a session.
    /// </summary>
    public class SessionImporter
    {
        private readonly Dictionary<string, Importer> importers = new ();

        private SessionImporter(Pipeline pipeline, Session session, bool usePerStreamReaders)
        {
            foreach (var partition in session.Partitions.Where(p => p.IsStoreValid))
            {
                var reader = StreamReader.Create(partition.StoreName, partition.StorePath, partition.StreamReaderTypeName);
                var importer = new Importer(pipeline, reader, usePerStreamReaders);
                this.importers.Add(partition.Name, importer);
            }

            this.MessageOriginatingTimeInterval = TimeInterval.Coverage(this.importers.Values.Select(i => i.MessageOriginatingTimeInterval));
            this.MessageCreationTimeInterval = TimeInterval.Coverage(this.importers.Values.Select(i => i.MessageCreationTimeInterval));
            this.StreamTimeInterval = TimeInterval.Coverage(this.importers.Values.Select(i => i.StreamTimeInterval));
            this.Name = session.Name;
        }

        /// <summary>
        /// Gets the name of the session.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in the session.
        /// </summary>
        public TimeInterval MessageOriginatingTimeInterval { get; private set; }

        /// <summary>
        /// Gets the interval between the creation time of the first and last message in the session.
        /// </summary>
        public TimeInterval MessageCreationTimeInterval { get; private set; }

        /// <summary>
        /// Gets the interval between the opened and closed times, across all streams in the session.
        /// </summary>
        public TimeInterval StreamTimeInterval { get; private set; }

        /// <summary>
        /// Gets a dictionary of named importers.
        /// </summary>
        public IReadOnlyDictionary<string, Importer> PartitionImporters => this.importers;

        /// <summary>
        /// Opens a session importer.
        /// </summary>
        /// <param name="pipeline">Pipeline to use for imports.</param>
        /// <param name="session">Session to import into.</param>
        /// <param name="usePerStreamReaders">Optional flag indicating whether to use per-stream readers.</param>
        /// <returns>The newly created session importer.</returns>
        public static SessionImporter Open(Pipeline pipeline, Session session, bool usePerStreamReaders = true)
            => new (pipeline, session, usePerStreamReaders);

        /// <summary>
        /// Determines if any importer contains the specified stream.
        /// </summary>
        /// <param name="streamSpecification">A stream specification in the form of a stream name or [PartitionName]:StreamName.</param>
        /// <returns>true if any importer contains the named stream; otherwise false.</returns>
        public bool Contains(string streamSpecification)
        {
            if (this.TryGetImporterAndStreamName(streamSpecification, out var _, out var _, out var streamSpecificationIsAmbiguous))
            {
                return true;
            }
            else if (streamSpecificationIsAmbiguous)
            {
                // If the stream specification is ambiguous that means multiple streams matching the specification exist.
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a specific importer contains the named stream.
        /// </summary>
        /// <param name="partitionName">Partition name of the specific importer.</param>
        /// <param name="streamName">The stream to search for.</param>
        /// <returns>true if the specific importer contains the named stream; otherwise false.</returns>
        public bool HasStream(string partitionName, string streamName)
            => this.importers[partitionName].Contains(streamName);

        /// <summary>
        /// Opens a specified stream via an importer open stream function.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="streamSpecification">The stream specification in the form of a stream name or [PartitionName]:StreamName.</param>
        /// <param name="openStreamFunc">A function that opens the stream given an importer and optional allocator and deallocator.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator of messages.</param>
        /// <returns>The opened stream.</returns>
        /// <exception cref="Exception">An exception is thrown when the stream specification is ambiguous.</exception>
        public IProducer<T> OpenStream<T>(string streamSpecification, Func<Importer, string, Func<T>, Action<T>, IProducer<T>> openStreamFunc, Func<T> allocator = null, Action<T> deallocator = null)
        {
            if (this.TryGetImporterAndStreamName(streamSpecification, out var importer, out var streamName, out var streamSpecificationIsAmbiguous))
            {
                return openStreamFunc(importer, streamName, allocator, deallocator);
            }
            else if (streamSpecificationIsAmbiguous)
            {
                if (streamSpecification.StartsWith("["))
                {
                    throw new Exception($"The stream specification is ambiguous. To open the stream, please use the {nameof(Importer.OpenStream)} API with a specific partition importer.");
                }
                else
                {
                    throw new Exception($"The stream specification is ambiguous. To open the stream, please use a [PartitionName]:StreamName specification, or use the {nameof(Importer.OpenStream)} API with a specific partition importer.");
                }
            }
            else
            {
                throw new Exception($"Stream specification not found: {streamSpecification}");
            }
        }

        /// <summary>
        /// Opens a specified stream via an importer open stream function, or returns null if the stream does not exist.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="streamSpecification">The stream specification in the form of a stream name or [PartitionName]:StreamName.</param>
        /// <param name="openStreamFunc">A function that opens the stream given an importer and optional allocator and deallocator.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator of messages.</param>
        /// <returns>The opened stream, or null if the stream does not exist.</returns>
        /// <exception cref="Exception">An exception is thrown when the stream specification is ambiguous.</exception>
        public IProducer<T> OpenStreamOrDefault<T>(string streamSpecification, Func<Importer, string, Func<T>, Action<T>, IProducer<T>> openStreamFunc, Func<T> allocator = null, Action<T> deallocator = null)
        {
            if (this.TryGetImporterAndStreamName(streamSpecification, out var importer, out var streamName, out var streamSpecificationIsAmbiguous))
            {
                return openStreamFunc(importer, streamName, allocator, deallocator);
            }
            else if (streamSpecificationIsAmbiguous)
            {
                if (streamSpecification.StartsWith("["))
                {
                    throw new Exception($"The stream specification is ambiguous. To open the stream, please use the {nameof(Importer.OpenStream)} API with a specific partition importer.");
                }
                else
                {
                    throw new Exception($"The stream specification is ambiguous. To open the stream, please use a [PartitionName]:StreamName specification, or use the {nameof(Importer.OpenStream)} API with a specific partition importer.");
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Opens a specified stream.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="streamSpecification">A stream specification in the form of a stream name or [PartitionName]:StreamName.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent out (defaults to disposing <see cref="IDisposable"/> messages.)</param>
        /// <returns>The opened stream.</returns>
        public IProducer<T> OpenStream<T>(string streamSpecification, Func<T> allocator = null, Action<T> deallocator = null)
            => this.OpenStream(streamSpecification, (importer, streamName, allocator, deallocator) => importer.OpenStream(streamName, allocator, deallocator), allocator, deallocator);

        /// <summary>
        /// Opens a specified stream, or returns null if the stream does not exist.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="streamSpecification">A stream specification in the form of a stream name or [PartitionName]:StreamName.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent out (defaults to disposing <see cref="IDisposable"/> messages.)</param>
        /// <returns>The opened stream, or null if no stream with the specified name exists.</returns>
        public IProducer<T> OpenStreamOrDefault<T>(string streamSpecification, Func<T> allocator = null, Action<T> deallocator = null)
            => this.OpenStreamOrDefault(streamSpecification, (importer, streamName, allocator, deallocator) => importer.OpenStream(streamName, allocator, deallocator), allocator, deallocator);

        /// <summary>
        /// Opens the named stream in a specific partition.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="partitionName">The partition to open stream in.</param>
        /// <param name="streamName">The name of stream to open.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent out (defaults to disposing <see cref="IDisposable"/> messages.)</param>
        /// <returns>The opened stream.</returns>
        public IProducer<T> OpenStream<T>(string partitionName, string streamName, Func<T> allocator = null, Action<T> deallocator = null)
            => this.importers[partitionName].OpenStream(streamName, allocator, deallocator);

        /// <summary>
        /// Opens the named stream in a specific partition, if one exists.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="partitionName">The partition to open stream in.</param>
        /// <param name="streamName">The name of stream to open.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent out (defaults to disposing <see cref="IDisposable"/> messages.)</param>
        /// <returns>The opened stream, or null if no stream with the specified name exists in the specified partition.</returns>
        public IProducer<T> OpenStreamOrDefault<T>(string partitionName, string streamName, Func<T> allocator = null, Action<T> deallocator = null)
            => this.importers[partitionName].OpenStreamOrDefault(streamName, allocator, deallocator);

        private bool TryGetImporterAndStreamName(string streamSpecification, out Importer importer, out string streamName, out bool streamSpecificationIsAmbiguous)
        {
            if (streamSpecification.StartsWith("["))
            {
                var matches = Regex.Matches(streamSpecification, @"^\[(.*?)\]\:(.*?)$");
                if (matches.Count == 1)
                {
                    // Determine the partition and stream name within that partition
                    var partitionName = matches[0].Groups[1].Value;
                    streamName = matches[0].Groups[2].Value;

                    // Determine if the same stream specification appears in one of the partitions
                    // i.e. a stream name that starts with the partition name.
                    var importerContainingStreamSpecification = this.importers.Values.FirstOrDefault(importer => importer.AvailableStreams.Any(s => s.Name == streamSpecification));

                    if (importerContainingStreamSpecification != default)
                    {
                        importer = default;
                        streamName = default;
                        streamSpecificationIsAmbiguous = true;
                        return false;
                    }
                    else if (this.importers.TryGetValue(partitionName, out importer))
                    {
                        streamSpecificationIsAmbiguous = false;
                        return true;
                    }
                    else
                    {
                        streamName = default;
                        streamSpecificationIsAmbiguous = false;
                        return false;
                    }
                }
                else
                {
                    throw new Exception($"Invalid stream specification/name: {streamSpecification}");
                }
            }
            else
            {
                var all = this.importers.Values.Where(importer => importer.Contains(streamSpecification));
                var count = all.Count();
                if (count == 1)
                {
                    importer = all.First();
                    streamName = streamSpecification;
                    streamSpecificationIsAmbiguous = false;
                    return true;
                }
                else if (count > 1)
                {
                    importer = default;
                    streamName = default;
                    streamSpecificationIsAmbiguous = true;
                    return false;
                }
                else
                {
                    importer = default;
                    streamName = default;
                    streamSpecificationIsAmbiguous = false;
                    return false;
                }
            }
        }
    }
}
