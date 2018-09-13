// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Persistence;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a session (collection of partitions) to be reasoned over.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Session
    {
        /// <summary>
        /// Default name of a session.
        /// </summary>
        public const string DefaultName = "Untitled Session";

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="dataset">The dataset that this session belongs to.</param>
        /// <param name="name">The session name.</param>
        internal Session(Dataset dataset, string name = Session.DefaultName)
        {
            this.Dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            this.Name = name;
            this.InternalPartitions = new List<IPartition>();
        }

        [JsonConstructor]
        private Session()
        {
        }

        /// <summary>
        /// Gets the dataset that this session belongs to.
        /// </summary>
        [IgnoreDataMember]
        public Dataset Dataset { get; internal set; }

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        [DataMember]
        public string Name
        {
            get => this.name;
            set
            {
                if (this.Dataset != null && this.Dataset.Sessions.Any(s => s.Name == value))
                {
                    // session names must be unique
                    throw new InvalidOperationException($"Dataset already contains a session named {value}");
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval OriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.InternalPartitions
                    .Where(p => p.OriginatingTimeInterval.Left > DateTime.MinValue && p.OriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(p => p.OriginatingTimeInterval));

        /// <summary>
        /// Gets the collection of partitions in this session.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<IPartition> Partitions => this.InternalPartitions.AsReadOnly();

        [DataMember(Name = "Partitions")]
        private List<IPartition> InternalPartitions { get; set; }

        /// <summary>
        /// Creates and adds an annotation partition from an existing annotation store.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added annotation partition.</returns>
        public AnnotationPartition AddAnnotationPartition(string storeName, string storePath, string partitionName = null)
        {
            var partition = AnnotationPartition.CreateFromExistingStore(this, storeName, storePath, partitionName);
            this.AddPartition(partition);
            return partition;
        }

        /// <summary>
        /// Creates and adds a data partition from an existing data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added data partition.</returns>
        public StorePartition AddStorePartition(string storeName, string storePath, string partitionName = null)
        {
            var partition = StorePartition.CreateFromExistingStore(this, storeName, storePath, partitionName);
            this.AddPartition(partition);
            return partition;
        }

        /// <summary>
        /// Creates and adds an new annotation partition.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="definition">The annotated event definition to use when creating new annoted events in the newly created annotation partition.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added annotation partition.</returns>
        public AnnotationPartition CreateAnnotationPartition(string storeName, string storePath, AnnotatedEventDefinition definition, string partitionName = null)
        {
            var partition = AnnotationPartition.Create(this, storeName, storePath, definition, partitionName);
            this.AddPartition(partition);
            return partition;
        }

        /// <summary>
        /// Asynchronously computes a derived partition for the session.
        /// </summary>
        /// <typeparam name="TParameter">The type of paramater passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">The name of the output data store. Default is null.</param>
        /// <param name="outputPartitionPath">The path of the output partition. Default is null.</param>
        /// <param name="replayDescriptor">The replay descriptor to us</param>
        /// <param name="progress">An object that can be used for reporting progress.</param>
        /// <param name="cancellationToken">A token for cancelling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputPartitionPath = null,
            ReplayDescriptor replayDescriptor = null,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // check for cancellation before making any changes
            cancellationToken.ThrowIfCancellationRequested();

            if (this.Partitions.Any(p => p.Name == outputPartitionName))
            {
                if (overwrite)
                {
                    // remove the partition first
                    this.RemovePartition(this.Partitions.First(p => p.Name == outputPartitionName));
                }
                else
                {
                    // if the overwrite flag is not on, throw
                    throw new Exception($"Session already contains partition with name {outputPartitionName}");
                }
            }

            if (outputStoreName == null)
            {
                // if store name is not explicitly specified, use the output partition name
                outputStoreName = outputPartitionName;
            }

            await Task.Run(
                () =>
                {
                    try
                    {
                        // create and run the pipeline
                        using (var pipeline = Pipeline.Create())
                        {
                            var importer = SessionImporter.Open(pipeline, this);
                            var exporter = Store.Create(pipeline, outputStoreName, outputPartitionPath);

                            computeDerived(pipeline, importer, exporter, parameter);

                            // Add a default replay strategy
                            if (replayDescriptor == null)
                            {
                                replayDescriptor = ReplayDescriptor.ReplayAll;
                            }

                            pipeline.RunAsync(replayDescriptor);

                            var durationTicks = pipeline.ReplayDescriptor.End.Ticks - pipeline.ReplayDescriptor.Start.Ticks;
                            while (!pipeline.WaitAll(100))
                            {
                                // periodically check for cancellation
                                cancellationToken.ThrowIfCancellationRequested();

                                if (progress != null)
                                {
                                    try
                                    {
                                        var currentTime = pipeline.GetCurrentTime();
                                        progress.Report((null, (currentTime.Ticks - pipeline.ReplayDescriptor.Start.Ticks) / (double)durationTicks));
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Workaround for when pipeline ends before call to GetCurrentTime(), until Pipeline supports progress reporting
                                        progress.Report((null, 1.0));
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // if operation was canceled, remove the partially-written store
                        if (StoreCommon.TryGetPathToLatestVersion(outputStoreName, outputPartitionPath, out string storePath))
                        {
                            this.SafeDirectoryDelete(storePath, true);
                        }

                        throw;
                    }
                },
                cancellationToken);

            // add the partition
            this.AddStorePartition(outputStoreName, outputPartitionPath, outputPartitionName);
        }

        /// <summary>
        /// Creates and adds a new data partition.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added data partition.</returns>
        public StorePartition CreateStorePartition(string storeName, string storePath, string partitionName = null)
        {
            var partition = StorePartition.Create(this, storeName, storePath, partitionName);
            this.AddPartition(partition);
            return partition;
        }

        /// <summary>
        /// Removes a specified partition from the session.
        /// </summary>
        /// <param name="partition">The partition to remove.</param>
        public void RemovePartition(IPartition partition)
        {
            this.InternalPartitions.Remove(partition);
        }

        /// <summary>
        /// Adds a partition to this session and updates its originating time interval.
        /// </summary>
        /// <param name="partition">The partition to be added.</param>
        private void AddPartition(IPartition partition)
        {
            if (this.Partitions.Any(p => p.Name == partition.Name))
            {
                // partition names must be unique
                throw new InvalidOperationException($"Session already contains a partition named {partition.Name}");
            }

            this.InternalPartitions.Add(partition);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var partition in this.InternalPartitions)
            {
                partition.Session = this;
            }
        }

        /// <summary>
        /// Due to the runtime's asynchronous behaviour, we may try to
        /// delete our test directory before the runtime has finished
        /// messing with it.  This method will keep trying to delete
        /// the directory until the runtime shuts down
        /// </summary>
        /// <param name="path">The path to the Directory to be deleted</param>
        /// <param name="recursive">Delete all subdirectories and files</param>
        private void SafeDirectoryDelete(string path, bool recursive)
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                try
                {
                    Directory.Delete(path, recursive);
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // Something in the directory is probably still being
                    // accessed by the process under test, so try again shortly.
                    Thread.Sleep(200);
                }
            }

            throw new ApplicationException(string.Format("Unable to delete directory \"{0}\" after multiple attempts", path));
        }
    }
}
