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
    using Microsoft.Psi.Data.Helpers;
    using Microsoft.Psi.Persistence;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a dataset (collection of sessions) to be reasoned over.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Dataset
    {
        /// <summary>
        /// Default name of a dataset.
        /// </summary>
        public const string DefaultName = "Untitled Dataset";

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dataset"/> class.
        /// </summary>
        /// <param name="name">The name of the new dataset. Default is <see cref="DefaultName"/>.</param>
        /// <param name="filename">An optional filename that indicates the location to save the dataset.<see cref="DefaultName"/>.</param>
        /// <param name="autoSave">Whether the dataset automatically autosave changes if a path is given (optional, default is false).</param>
        [JsonConstructor]
        public Dataset(string name = Dataset.DefaultName, string filename = "", bool autoSave = false)
        {
            this.Name = name;
            this.Filename = filename;
            this.AutoSave = autoSave;
            this.InternalSessions = new List<Session>();
            if (this.AutoSave && filename == string.Empty)
            {
                throw new ArgumentException("filename needed to be provided for autosave dataset.");
            }
        }

        /// <summary>
        /// Event raise when the dataset's structure changed.
        /// </summary>
        public event EventHandler DatasetChanged;

        /// <summary>
        /// Gets or sets the name of this dataset.
        /// </summary>
        [DataMember]
        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
                this.OnDatasetChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current save path of this dataset.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether autosave is enabled.
        /// </summary>
        public bool AutoSave { get; set; }

        /// <summary>
        /// Gets a value indicating whether changes to this dataset have been saved.
        /// </summary>
        public bool HasUnsavedChanges { get; private set; } = false;

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval MessageOriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.InternalSessions
                    .Where(s => s.MessageOriginatingTimeInterval.Left > DateTime.MinValue && s.MessageOriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(s => s.MessageOriginatingTimeInterval));

        /// <summary>
        /// Gets the creation time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval MessageCreationTimeInterval =>
            TimeInterval.Coverage(
                this.InternalSessions
                    .Where(s => s.MessageCreationTimeInterval.Left > DateTime.MinValue && s.MessageCreationTimeInterval.Right < DateTime.MaxValue)
                    .Select(s => s.MessageCreationTimeInterval));

        /// <summary>
        /// Gets the stream open-close time interval in this dataset.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval TimeInterval =>
            TimeInterval.Coverage(
                this.InternalSessions
                    .Where(s => s.TimeInterval.Left > DateTime.MinValue && s.TimeInterval.Right < DateTime.MaxValue)
                    .Select(s => s.TimeInterval));

        /// <summary>
        /// Gets the size of the dataset, in bytes.
        /// </summary>
        public long? Size => this.InternalSessions.Sum(p => p.Size);

        /// <summary>
        /// Gets the number of streams in the dataset.
        /// </summary>
        public long? StreamCount => this.InternalSessions.Sum(p => p.StreamCount);

        /// <summary>
        /// Gets the collection of sessions in this dataset.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<Session> Sessions => this.InternalSessions.AsReadOnly();

        [DataMember(Name = "Sessions")]
        private List<Session> InternalSessions { get; set; }

        /// <summary>
        /// Loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <param name="autoSave">A value to indicate whether to enable autosave (optional, default is false).</param>
        /// <returns>The newly loaded dataset.</returns>
        public static Dataset Load(string filename, bool autoSave = false)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Context = new StreamingContext(StreamingContextStates.File, filename),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                });
            using var jsonFile = File.OpenText(filename);
            using var jsonReader = new JsonTextReader(jsonFile);
            var dataset = serializer.Deserialize<Dataset>(jsonReader);
            dataset.AutoSave = autoSave;
            dataset.Filename = filename;
            return dataset;
        }

        /// <summary>
        /// Creates a new dataset from an existing data store.
        /// </summary>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="sessionName">The session name (optional, defaults to streamReader.Name).</param>
        /// <param name="partitionName">The partition name (optional, defaults to streamReader.).</param>
        /// <returns>The newly created dataset.</returns>
        public static Dataset CreateFromStore(IStreamReader streamReader, string sessionName = null, string partitionName = null)
        {
            var dataset = new Dataset();
            dataset.AddSessionFromStore(streamReader, sessionName, partitionName);
            return dataset;
        }

        /// <summary>
        /// Creates a new session within the dataset.
        /// </summary>
        /// <param name="sessionName">The session name.</param>
        /// <returns>The newly created session.</returns>
        public Session CreateSession(string sessionName = Session.DefaultName)
        {
            var session = new Session(this, sessionName);
            this.InternalSessions.Add(session);
            this.OnDatasetChanged();
            return session;
        }

        /// <summary>
        /// Removes the specified session from the dataset.
        /// </summary>
        /// <param name="session">The session to remove.</param>
        public void RemoveSession(Session session)
        {
            this.InternalSessions.Remove(session);
            this.OnDatasetChanged();
        }

        /// <summary>
        /// Appends sessions from an input dataset to this dataset.
        /// </summary>
        /// <param name="inputDataset">The dataset to append from.</param>
        public void Append(Dataset inputDataset)
        {
            foreach (var session in inputDataset.Sessions)
            {
                var newSession = this.CreateSession();
                newSession.Name = session.Name;
                foreach (var p in session.Partitions)
                {
                    newSession.AddStorePartition(StreamReader.Create(p.StoreName, p.StorePath, p.StreamReaderTypeName), p.Name);
                }
            }

            this.OnDatasetChanged();
        }

        /// <summary>
        /// Saves this dataset.
        /// </summary>
        /// <param name="filename">The filename that indicates the location to save the dataset.</param>
        public void SaveAs(string filename)
        {
            this.Filename = filename;
            this.Save();
        }

        /// <summary>
        /// Saves this dataset.
        /// </summary>
        public void Save()
        {
            if (this.Filename == string.Empty)
            {
                throw new ArgumentException("filename to save the dataset must be set before save operation.");
            }

            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    // pass the dataset filename in the context to allow relative store paths to be computed using the RelativePathConverter
                    Context = new StreamingContext(StreamingContextStates.File, this.Filename),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                });
            using var jsonFile = File.CreateText(this.Filename);
            using var jsonWriter = new JsonTextWriter(jsonFile);
            serializer.Serialize(jsonWriter, this);
            this.HasUnsavedChanges = false;
        }

        /// <summary>
        /// Creates and adds an empty session with the specified name to the dataset.
        /// </summary>
        /// <param name="sessionName">The session name.</param>
        /// <returns>The session.</returns>
        public Session AddSession(string sessionName)
        {
            var session = new Session(this, sessionName);
            this.AddSession(session);
            return session;
        }

        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="sessionName">The name of the session (optional, defaults to streamReader.Name).</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly added session.</returns>
        public Session AddSessionFromStore(IStreamReader streamReader, string sessionName = null, string partitionName = null)
        {
            var session = new Session(this, sessionName ?? streamReader.Name);
            session.AddStorePartition(streamReader, partitionName);
            this.AddSession(session);
            return session;
        }

        /// <summary>
        /// Compute derived results for each session in the dataset.
        /// </summary>
        /// <typeparam name="TResult">The type of data of the derived result.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive results.</param>
        /// <returns>List of results.</returns>
        public IReadOnlyList<TResult> ComputeDerived<TResult>(
            Action<Pipeline, SessionImporter, TResult> computeDerived)
            where TResult : class, new()
        {
            var results = new List<TResult>();
            foreach (var session in this.Sessions)
            {
                // the first partition is where we put the data if output is not specified
                var inputPartition = session.Partitions.FirstOrDefault();

                // create and run the pipeline
                using var pipeline = Pipeline.Create();
                var importer = SessionImporter.Open(pipeline, session);

                var result = new TResult();
                computeDerived(pipeline, importer, result);

                var startTime = DateTime.UtcNow;
                Console.WriteLine($"Computing derived features on {inputPartition.StorePath} ...");
                pipeline.Run(ReplayDescriptor.ReplayAll);

                var finishTime = DateTime.UtcNow;
                Console.WriteLine($" - Time elapsed: {(finishTime - startTime).TotalMinutes:0.00} min.");

                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <param name="computeDerived">The action to be invoked to compute derive partitions.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePath">An optional path for the output data store. Default is the path for the first partition in the session.</param>
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
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <param name="computeDerived">The action to be invoked to compute derive partitions.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePathFunction">An optional function to determine output store path for each given session. Default is the path for the first partition in the session.</param>
        /// <param name="replayDescriptor">An optional replay descriptor to use when creating the derived partition.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy to use when creating the derived partition.</param>
        /// <param name="enableDiagnostics">Indicates whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="progress">An optional progress object to be used for reporting progress.</param>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous compute derive partition operation.</returns>
        public async Task CreateDerivedPartitionAsync(
            Action<Pipeline, SessionImporter, Exporter> computeDerived,
            string outputPartitionName,
            bool overwrite,
            string outputStoreName,
            Func<Session, string> outputStorePathFunction,
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
                outputStorePathFunction,
                replayDescriptor,
                deliveryPolicy,
                enableDiagnostics,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <typeparam name="TParameter">The type of parameter passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePath">An optional path for the output data store. Default is the path for the first partition in the session.</param>
        /// <param name="replayDescriptor">The replay descriptor to us.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy to use when creating the derived partition.</param>
        /// <param name="enableDiagnostics">Indicates whether to enable collecting and publishing diagnostics information on the Pipeline.Diagnostics stream.</param>
        /// <param name="progress">An object that can be used for reporting progress.</param>
        /// <param name="cancellationToken">A token for canceling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
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
            await this.CreateDerivedPartitionAsync(
                computeDerived,
                parameter,
                outputPartitionName,
                overwrite,
                outputStoreName,
                _ => outputStorePath,
                replayDescriptor,
                deliveryPolicy,
                enableDiagnostics,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <typeparam name="TParameter">The type of parameter passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The name of the output partition to be created.</param>
        /// <param name="overwrite">An optional flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">An optional name for the output data store. Default is the output partition name.</param>
        /// <param name="outputStorePathFunction">An optional function to determine output store path for each given session. Default is the path for the first partition in the session.</param>
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
            Func<Session, string> outputStorePathFunction,
            ReplayDescriptor replayDescriptor = null,
            DeliveryPolicy deliveryPolicy = null,
            bool enableDiagnostics = false,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default)
        {
            var totalDuration = default(double);
            var sessionStart = this.Sessions.Select(s =>
                {
                    var currentDuration = totalDuration;
                    totalDuration += s.TimeInterval.Span.TotalSeconds;
                    return currentDuration;
                }).ToList();
            var sessionDuration = this.Sessions.Select(s => s.TimeInterval.Span.TotalSeconds).ToList();

            for (int i = 0; i < this.Sessions.Count; i++)
            {
                var session = this.Sessions[i];
                await session.CreateDerivedPsiPartitionAsync(
                    computeDerived,
                    parameter,
                    outputPartitionName,
                    overwrite,
                    outputStoreName ?? outputPartitionName,
                    outputStorePathFunction(session) ?? session.Partitions.First().StorePath,
                    replayDescriptor,
                    deliveryPolicy,
                    enableDiagnostics,
                    progress != null ? new Progress<(string, double)>(tuple => progress.Report((tuple.Item1, (sessionStart[i] + tuple.Item2 * sessionDuration[i]) / totalDuration))) : null,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Adds sessions from data stores located in the specified path.
        /// </summary>
        /// <param name="path">The path that contains the data stores.</param>
        /// <param name="partitionName">The name of the partition to be added when adding a new session. Default is null.</param>
        public void AddSessionsFromPsiStores(string path, string partitionName = null)
        {
            foreach (var store in PsiStoreCommon.EnumerateStores(path))
            {
                this.AddSessionFromPsiStore(store.Name, store.Path, store.Session, partitionName);
            }
        }

        /// <summary>
        /// Method called when structure of the dataset changed.
        /// </summary>
        public virtual void OnDatasetChanged()
        {
            if (this.AutoSave)
            {
                this.Save();
            }
            else
            {
                this.HasUnsavedChanges = true;
            }

            // raise the event.
            EventHandler handler = this.DatasetChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds a session to this dataset and updates its originating time interval.
        /// </summary>
        /// <param name="session">The session to be added.</param>
        private void AddSession(Session session)
        {
            if (this.Sessions.Any(s => s.Name == session.Name))
            {
                // session names must be unique
                throw new InvalidOperationException($"Dataset already contains a session named {session.Name}");
            }

            this.InternalSessions.Add(session);
            this.OnDatasetChanged();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var session in this.InternalSessions)
            {
                session.Dataset = this;
            }
        }
    }
}
