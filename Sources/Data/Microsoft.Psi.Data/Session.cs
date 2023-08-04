// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
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
        internal Session(Dataset dataset, string name = null)
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
        /// Event invoked when the structure of the session changed.
        /// </summary>
        public event EventHandler SessionChanged;

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
                this.OnSessionChanged();
            }
        }

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval MessageOriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.InternalPartitions
                    .Where(p => p.MessageOriginatingTimeInterval.Left > DateTime.MinValue && p.MessageOriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(p => p.MessageOriginatingTimeInterval));

        /// <summary>
        /// Gets the creation time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval MessageCreationTimeInterval =>
            TimeInterval.Coverage(
                this.InternalPartitions
                    .Where(p => p.MessageCreationTimeInterval.Left > DateTime.MinValue && p.MessageCreationTimeInterval.Right < DateTime.MaxValue)
                    .Select(p => p.MessageCreationTimeInterval));

        /// <summary>
        /// Gets the stream open-close time interval in this session.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval TimeInterval =>
            TimeInterval.Coverage(
                this.InternalPartitions
                    .Where(p => p.TimeInterval.Left > DateTime.MinValue && p.TimeInterval.Right < DateTime.MaxValue)
                    .Select(p => p.TimeInterval));

        /// <summary>
        /// Gets the session duration.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan Duration => this.TimeInterval.Span;

        /// <summary>
        /// Gets the size of the session, in bytes.
        /// </summary>
        public long? Size => this.InternalPartitions.Sum(p => p.Size);

        /// <summary>
        /// Gets the number of streams in the session.
        /// </summary>
        public long? StreamCount => this.InternalPartitions.Sum(p => p.StreamCount);

        /// <summary>
        /// Gets the collection of partitions in this session.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<IPartition> Partitions => this.InternalPartitions.AsReadOnly();

        [DataMember(Name = "Partitions")]
        private List<IPartition> InternalPartitions { get; set; }

        /// <summary>
        /// Gets the partition specified by a name.
        /// </summary>
        /// <param name="partitionName">The name of the partition.</param>
        /// <returns>The partition with the specified name.</returns>
        public IPartition this[string partitionName] => this.InternalPartitions.FirstOrDefault(p => p.Name == partitionName);

        /// <summary>
        /// Creates and adds a data partition from an existing data store.
        /// </summary>
        /// <typeparam name="TStreamReader">Type of IStreamReader used to read data store.</typeparam>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="partitionName">The partition name. Default is stream reader name.</param>
        /// <returns>The newly added data partition.</returns>
        public Partition<TStreamReader> AddStorePartition<TStreamReader>(TStreamReader streamReader, string partitionName = null)
            where TStreamReader : IStreamReader
        {
            var partition = new Partition<TStreamReader>(this, streamReader, partitionName);
            this.AddPartition(partition);
            return partition;
        }

        /// <summary>
        /// Asynchronously computes a derived partition for this session.
        /// </summary>
        /// <param name="computeDerived">The action to be invoked to compute derive partitions.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePath">An optional path for the output data store. Default is the same path at the first partition in the session.</param>
        /// <param name="replayDescriptor">An optional replay descriptor to use when creating the derived partition.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy to use when creating the derived partition.</param>
        /// <param name="enableDiagnostics">Indicates whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous compute derive partition operation.</returns>
        public async Task CreateDerivedPartitionAsync(
            Action<Pipeline, SessionImporter, Exporter> computeDerived,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputStorePath = null,
            ReplayDescriptor replayDescriptor = null,
            DeliveryPolicy deliveryPolicy = null,
            bool enableDiagnostics = false,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
        {
            await this.CreateDerivedPartitionAsync<long>(
                (p, si, e, _) => computeDerived(p, si, e),
                0,
                outputPartitionName,
                overwrite,
                outputStoreName,
                outputStorePath,
                replayDescriptor,
                deliveryPolicy,
                enableDiagnostics,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for this session.
        /// </summary>
        /// <typeparam name="TParameter">The type of parameter passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePath">An optional path for the output data store. Default is the same path at the first partition in the session.</param>
        /// <param name="replayDescriptor">An optional replay descriptor to use when creating the derived partition.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy to use when creating the derived partition.</param>
        /// <param name="enableDiagnostics">Indicates whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous compute derive partition operation.</returns>
        public async Task CreateDerivedPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite,
            string outputStoreName,
            string outputStorePath,
            ReplayDescriptor replayDescriptor = null,
            DeliveryPolicy deliveryPolicy = null,
            bool enableDiagnostics = false,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
        {
            await this.CreateDerivedPsiPartitionAsync(
                computeDerived,
                parameter,
                outputPartitionName,
                overwrite,
                outputStoreName ?? outputPartitionName,
                outputStorePath ?? this.Partitions.First().StorePath,
                replayDescriptor,
                deliveryPolicy,
                enableDiagnostics,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for the session.
        /// </summary>
        /// <typeparam name="TParameter">The type of parameter passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to compute derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is null.</param>
        /// <param name="outputStorePath">An optional path for the output data store. Default is null.</param>
        /// <param name="replayDescriptor">An optional replay descriptor to use when creating the derived partition.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy to use when creating the derived partition.</param>
        /// <param name="enableDiagnostics">Indicates whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous compute derive partition operation.</returns>
        public async Task CreateDerivedPsiPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite,
            string outputStoreName,
            string outputStorePath,
            ReplayDescriptor replayDescriptor = null,
            DeliveryPolicy deliveryPolicy = null,
            bool enableDiagnostics = false,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (outputStoreName == null || outputStorePath == null)
            {
                throw new InvalidOperationException("The output store path and store name need to be specified.");
            }

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

            await Task.Run(
                () =>
                {
                    try
                    {
                        // create and run the pipeline
                        using var pipeline = Pipeline.Create(enableDiagnostics: enableDiagnostics, deliveryPolicy: deliveryPolicy);
                        var importer = SessionImporter.Open(pipeline, this);
                        var exporter = PsiStore.Create(pipeline, outputStoreName, outputStorePath, createSubdirectory: false);

                        computeDerived(pipeline, importer, exporter, parameter);

                        // Add a default replay strategy
                        if (replayDescriptor == null)
                        {
                            replayDescriptor = ReplayDescriptor.ReplayAll;
                        }

                        pipeline.RunAsync(replayDescriptor, progress != null ? new Progress<double>(p => progress.Report((this.Name, p))) : null);

                        var durationTicks = pipeline.ReplayDescriptor.End.Ticks - pipeline.ReplayDescriptor.Start.Ticks;
                        while (!pipeline.WaitAll(100))
                        {
                            // periodically check for cancellation
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // if operation was canceled, remove the store
                        PsiStore.Delete((outputStoreName, outputStorePath));

                        throw;
                    }
                },
                cancellationToken);

            // add the partition
            this.AddPsiStorePartition(outputStoreName, outputStorePath, outputPartitionName);
        }

        /// <summary>
        /// Removes a specified partition from the session.
        /// </summary>
        /// <param name="partition">The partition to remove.</param>
        public void RemovePartition(IPartition partition)
        {
            this.InternalPartitions.Remove(partition);
            this.OnSessionChanged();
        }

        /// <summary>
        /// Method called when structure of the session changed.
        /// </summary>
        protected virtual void OnSessionChanged()
        {
            this.Dataset?.OnDatasetChanged();
            EventHandler handler = this.SessionChanged;
            handler?.Invoke(this, EventArgs.Empty);
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
            this.OnSessionChanged();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var partition in this.InternalPartitions)
            {
                partition.Session = this;
            }
        }
    }
}
