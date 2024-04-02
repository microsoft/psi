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
        /// Adds a partition based on a specified stream reader.
        /// </summary>
        /// <typeparam name="TStreamReader">Type of <see cref="IStreamReader"/> used to read partition.</typeparam>
        /// <param name="streamReader">The stream reader used to read the partition.</param>
        /// <param name="partitionName">An optional partition name (defaults the the stream reader name).</param>
        /// <param name="progress">An optional progress updates receiver.</param>
        /// <returns>The task for adding a partition based on a specified stream raeder.</returns>
        public async Task<Partition<TStreamReader>> AddPartitionAsync<TStreamReader>(
            TStreamReader streamReader,
            string partitionName = null,
            IProgress<(string, double)> progress = null)
            where TStreamReader : IStreamReader
        {
            partitionName ??= streamReader.Name;

            if (streamReader is PsiStoreStreamReader)
            {
                await PsiStore.RepairAsync(streamReader.Name, streamReader.Path, progress: new Progress<double>(t => progress?.Report(($"Repairing store {streamReader.Name} ...", t * 0.95))));
            }

            return await Task.Run(() =>
            {
                progress?.Report(($"Adding partition {partitionName} ...", 0.95));
                var partition = new Partition<TStreamReader>(this, streamReader, partitionName);
                this.AddPartition(partition);
                progress?.Report((string.Empty, 1));
                return partition;
            });
        }

        /// <summary>
        /// Add a partition from a specified \psi store.
        /// </summary>
        /// <param name="storeName">The name of the \psi store.</param>
        /// <param name="storePath">The path to the \psi store.</param>
        /// <param name="partitionName">An optional name for the partition (defaults to the name of the \psi store).</param>
        /// <param name="progress">An optional progress updates receiver.</param>
        /// <returns>The task for adding a partition from a specified \psi store.</returns>
        public async Task<Partition<PsiStoreStreamReader>> AddPartitionFromPsiStoreAsync(
            string storeName,
            string storePath,
            string partitionName = null,
            IProgress<(string, double)> progress = null)
            => await this.AddPartitionAsync(new PsiStoreStreamReader(storeName, storePath), partitionName, progress);

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
            await this.CreateDerivedPsiStorePartitionAsync(
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
        public async Task CreateDerivedPsiStorePartitionAsync<TParameter>(
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
            if (!string.IsNullOrEmpty(outputPartitionName) && (string.IsNullOrEmpty(outputStoreName) || string.IsNullOrEmpty(outputStorePath)))
            {
                throw new InvalidOperationException("If an output partition name is specified, then an output store path and store name also need to be specified.");
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
                        var exporter = outputPartitionName != null ? PsiStore.Create(pipeline, outputStoreName, outputStorePath, createSubdirectory: false) : null;

                        computeDerived(pipeline, importer, exporter, parameter);

                        // Setup the default replay strategy
                        replayDescriptor ??= ReplayDescriptor.ReplayAll;

                        pipeline.RunAsync(replayDescriptor, new Progress<double>(p => progress?.Report((this.Name, p * 0.95))));

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
                        if (outputPartitionName != null)
                        {
                            PsiStore.Delete((outputStoreName, outputStorePath));
                        }

                        throw;
                    }
                },
                cancellationToken);

            // add the partition
            if (outputPartitionName != null)
            {
                await this.AddPartitionFromPsiStoreAsync(
                    outputStoreName,
                    outputStorePath,
                    outputPartitionName,
                    new Progress<(string, double)>(
                        t => progress?.Report((t.Item1, 0.95 + 0.05 * t.Item2))));
            }
        }

        /// <summary>
        /// Asynchronously runs a batch processing task of a specified task on the session.
        /// </summary>
        /// <typeparam name="TBatchProcessingTask">The type of the batch processing task.</typeparam>
        /// <param name="configuration">The batch processing task configuration.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous run batch task operation.</returns>
        public async Task RunBatchProcessingTaskAsync<TBatchProcessingTask>(
            BatchProcessingTaskConfiguration configuration,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
            where TBatchProcessingTask : IBatchProcessingTask
            => await this.RunBatchProcessingTaskAsync<long>(Activator.CreateInstance<TBatchProcessingTask>(), configuration, 0, progress, cancellationToken);

        /// <summary>
        /// Asynchronously runs a specified batch processing task on the session.
        /// </summary>
        /// <param name="batchProcessingTask">The batch processing task to run.</param>
        /// <param name="configuration">The batch processing task configuration.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous run batch task operation.</returns>
        public async Task RunBatchProcessingTaskAsync(
            IBatchProcessingTask batchProcessingTask,
            BatchProcessingTaskConfiguration configuration,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
            => await this.RunBatchProcessingTaskAsync<long>(batchProcessingTask, configuration, 0, progress, cancellationToken);

        /// <summary>
        /// Asynchronously runs a batch processing task of a specified type on the session.
        /// </summary>
        /// <typeparam name="TBatchProcessingTask">The type of the batch processing task.</typeparam>
        /// <typeparam name="TParameter">The type of parameter passed to the batch processing task.</typeparam>
        /// <param name="configuration">The batch processing task configuration.</param>
        /// <param name="parameter">The parameter to be passed to the batch processing task.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous run batch task operation.</returns>
        public async Task RunBatchProcessingTaskAsync<TBatchProcessingTask, TParameter>(
            BatchProcessingTaskConfiguration configuration,
            TParameter parameter,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
            where TBatchProcessingTask : IBatchProcessingTask
            => await this.RunBatchProcessingTaskAsync(Activator.CreateInstance<TBatchProcessingTask>(), configuration, parameter, progress, cancellationToken);

        /// <summary>
        /// Asynchronously runs a specified batch processing task on the session.
        /// </summary>
        /// <typeparam name="TParameter">The type of parameter passed to the batch processing task.</typeparam>
        /// <param name="batchProcessingTask">The batch processing task to run.</param>
        /// <param name="configuration">The batch processing task configuration.</param>
        /// <param name="parameter">The parameter to be passed to the batch processing task.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous run batch task operation.</returns>
        public async Task RunBatchProcessingTaskAsync<TParameter>(
            IBatchProcessingTask batchProcessingTask,
            BatchProcessingTaskConfiguration configuration,
            TParameter parameter,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
        {
            batchProcessingTask.OnStartProcessingSession();
            try
            {
                await this.CreateDerivedPsiStorePartitionAsync(
                    (pipeline, sessionImporter, exporter, parameter) => batchProcessingTask.Run(pipeline, sessionImporter, exporter, configuration),
                    parameter,
                    configuration.OutputPartitionName,
                    overwrite: true,
                    outputStoreName: configuration.OutputStoreName,
                    outputStorePath: configuration.OutputStorePath ?? this.Partitions.First().StorePath,
                    replayDescriptor: configuration.ReplayAllRealTime ? ReplayDescriptor.ReplayAllRealTime : ReplayDescriptor.ReplayAll,
                    deliveryPolicy: configuration.DeliveryPolicySpec,
                    enableDiagnostics: configuration.EnableDiagnostics,
                    progress: progress,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                batchProcessingTask.OnCanceledProcessingSession();
                throw;
            }
            catch (Exception)
            {
                batchProcessingTask.OnExceptionProcessingSession();
                throw;
            }

            batchProcessingTask.OnEndProcessingSession();
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
