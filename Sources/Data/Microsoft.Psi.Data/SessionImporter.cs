// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines a class used in importing data into a session.
    /// </summary>
    public class SessionImporter
    {
        private readonly Dictionary<string, Importer> importers = new ();

        private SessionImporter(Pipeline pipeline, Session session, bool usePerStreamReaders)
        {
            foreach (var partition in session.Partitions)
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
        {
            return new SessionImporter(pipeline, session, usePerStreamReaders);
        }

        /// <summary>
        /// Determines if any importer contains the named stream.
        /// </summary>
        /// <param name="streamName">The stream to search for.</param>
        /// <returns>true if any importer contains the named stream; otherwise false.</returns>
        public bool Contains(string streamName)
        {
            var all = this.importers.Values.Where(importer => importer.Contains(streamName));
            var count = all.Count();
            if (count > 0)
            {
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
        {
            return this.importers[partitionName].Contains(streamName);
        }

        /// <summary>
        /// Opens the first stream that matched the specified name.
        /// </summary>
        /// <typeparam name="T">The type of stream to open.</typeparam>
        /// <param name="streamName">The name of stream to open.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent out (defaults to disposing <see cref="IDisposable"/> messages.)</param>
        /// <returns>The opened stream.</returns>
        public IProducer<T> OpenStream<T>(string streamName, Func<T> allocator = null, Action<T> deallocator = null)
        {
            var all = this.importers.Values.Where(importer => importer.Contains(streamName));
            var count = all.Count();
            if (count == 1)
            {
                return all.First().OpenStream(streamName, allocator, deallocator);
            }
            else if (count > 1)
            {
                throw new Exception($"Underspecified access to session: multiple partitions contain stream {streamName}");
            }
            else
            {
                throw new Exception($"Cannot find {streamName}");
            }
        }

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
        {
            return this.importers[partitionName].OpenStream(streamName, allocator, deallocator);
        }

        /// <summary>
        /// Gets list of importer names that contain the named stream.
        /// </summary>
        /// <param name="streamName">The stream to search for.</param>
        /// <returns>The list of importer names that contain the named stream.</returns>
        private IEnumerable<string> GetContainingUsages(string streamName)
        {
            return this.importers.Values.Where(importer => importer.Contains(streamName)).Select(i => i.StoreName);
        }
    }
}
