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

        /// <summary>
        /// Initializes a new instance of the <see cref="Dataset"/> class.
        /// </summary>
        /// <param name="name">The name of the new dataset. Default is <see cref="DefaultName"/>.</param>
        [JsonConstructor]
        public Dataset(string name = Dataset.DefaultName)
        {
            this.Name = name;
            this.InternalSessions = new List<Session>();
        }

        /// <summary>
        /// Gets or sets the name of this dataset.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [IgnoreDataMember]
        public TimeInterval OriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.InternalSessions
                    .Where(s => s.OriginatingTimeInterval.Left > DateTime.MinValue && s.OriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(s => s.OriginatingTimeInterval));

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
        /// <returns>The newly loaded dataset.</returns>
        public static Dataset Load(string filename)
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
                });
            using (var jsonFile = File.OpenText(filename))
            using (var jsonReader = new JsonTextReader(jsonFile))
            {
                return serializer.Deserialize<Dataset>(jsonReader);
            }
        }

        /// <summary>
        /// Creates a new dataset from an exising data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly created dataset.</returns>
        public static Dataset CreateFromExistingStore(string storeName, string storePath, string partitionName = null)
        {
            var dataset = new Dataset();
            dataset.AddSessionFromExistingStore(storeName, storeName, storePath, partitionName);
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
            return session;
        }

        /// <summary>
        /// Removes the specified session from the dataset.
        /// </summary>
        /// <param name="session">The session to remove.</param>
        public void RemoveSession(Session session)
        {
            this.InternalSessions.Remove(session);
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
                    newSession.AddStorePartition(p.StoreName, p.StorePath);
                }
            }
        }

        /// <summary>
        /// Saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataset into.</param>
        /// <param name="useRelativePaths">Indicates whether to use full or relative store paths.</param>
        public void Save(string filename, bool useRelativePaths = true)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    // pass the dataset filename in the context to allow relative store paths to be computed using the RelativePathConverter
                    Context = useRelativePaths ? new StreamingContext(StreamingContextStates.File, filename) : default(StreamingContext),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });
            using (var jsonFile = File.CreateText(filename))
            using (var jsonWriter = new JsonTextWriter(jsonFile))
            {
                serializer.Serialize(jsonWriter, this);
            }
        }

        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="sessionName">The name of the session.</param>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly added session.</returns>
        public Session AddSessionFromExistingStore(string sessionName, string storeName, string storePath, string partitionName = null)
        {
            var session = new Session(this, sessionName);
            session.AddStorePartition(storeName, storePath, partitionName);
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
                using (var pipeline = Pipeline.Create())
                {
                    var importer = SessionImporter.Open(pipeline, session);

                    var result = new TResult();
                    computeDerived(pipeline, importer, result);

                    var startTime = DateTime.Now;
                    Console.WriteLine($"Computing derived features on {inputPartition.StorePath} ...");
                    pipeline.Run(ReplayDescriptor.ReplayAll);

                    var finishTime = DateTime.Now;
                    Console.WriteLine($" - Time elapsed: {(finishTime - startTime).TotalMinutes:0.00} min.");

                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">The name of the output data store. Default is null.</param>
        /// <param name="outputStorePath">The path of the output data store. Default is null.</param>
        /// <param name="replayDescriptor">The replay descriptor to us.</param>
        /// <param name="cancellationToken">A token for cancelling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync(
            Action<Pipeline, SessionImporter, Exporter> computeDerived,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputStorePath = null,
            ReplayDescriptor replayDescriptor = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.CreateDerivedPartitionAsync<long>(
                (p, si, e, l) => computeDerived(p, si, e),
                0,
                outputPartitionName,
                overwrite,
                outputStoreName,
                outputStorePath,
                replayDescriptor,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten.</param>
        /// <param name="outputStoreName">The name of the output data store.</param>
        /// <param name="outputPathFunction">A function to determine output path from the given Session.</param>
        /// <param name="replayDescriptor">The replay descriptor to us.</param>
        /// <param name="progress">An object that can be used for reporting progress.</param>
        /// <param name="cancellationToken">A token for cancelling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync(
            Action<Pipeline, SessionImporter, Exporter> computeDerived,
            string outputPartitionName,
            bool overwrite,
            string outputStoreName,
            Func<Session, string> outputPathFunction,
            ReplayDescriptor replayDescriptor = null,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.CreateDerivedPartitionAsync<long>(
                (p, si, e, l) => computeDerived(p, si, e),
                0,
                outputPartitionName,
                overwrite,
                outputStoreName,
                outputPathFunction,
                replayDescriptor,
                progress,
                cancellationToken);
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <typeparam name="TParameter">The type of paramater passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten.</param>
        /// <param name="outputStoreName">The name of the output data store.</param>
        /// <param name="outputPathFunction">A function to determine output path from the given Session.</param>
        /// <param name="replayDescriptor">The replay descriptor to us.</param>
        /// <param name="progress">An object that can be used for reporting progress.</param>
        /// <param name="cancellationToken">A token for cancelling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite,
            string outputStoreName,
            Func<Session, string> outputPathFunction,
            ReplayDescriptor replayDescriptor = null,
            IProgress<(string, double)> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            double progressPerSession = 1.0 / this.Sessions.Count;
            for (int i = 0; i < this.Sessions.Count; i++)
            {
                var session = this.Sessions[i];
                await session.CreateDerivedPartitionAsync(
                    computeDerived,
                    parameter,
                    outputPartitionName,
                    overwrite,
                    outputStoreName,
                    outputPathFunction(session),
                    replayDescriptor,
                    new Progress<(string s, double p)>(t => progress?.Report(($"Creating derived partition on session {session.Name}", (i + t.p) * progressPerSession))),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Asynchronously computes a derived partition for each session in the dataset.
        /// </summary>
        /// <typeparam name="TParameter">The type of paramater passed to the action.</typeparam>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">The name of the output data store. Default is null.</param>
        /// <param name="outputStorePath">The path of the output data store. Default is null.</param>
        /// <param name="replayDescriptor">The replay descriptor to us.</param>
        /// <param name="cancellationToken">A token for cancelling the asynchronous task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CreateDerivedPartitionAsync<TParameter>(
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputStorePath = null,
            ReplayDescriptor replayDescriptor = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            int sessionIndex = 0;
            foreach (var session in this.Sessions)
            {
                var inputPartition = session.Partitions.FirstOrDefault();
                await session.CreateDerivedPartitionAsync(
                    computeDerived,
                    parameter,
                    outputPartitionName,
                    overwrite,
                    outputStoreName,
                    (outputStorePath == null) ? inputPartition.StorePath : Path.Combine(outputStorePath, $"{sessionIndex}"),
                    replayDescriptor,
                    null,
                    cancellationToken);

                // increment session index
                sessionIndex++;
            }
        }

        /// <summary>
        /// Adds sessions from data stores located in the specified path.
        /// </summary>
        /// <param name="path">The path that contains the data stores.</param>
        /// <param name="partitionName">The name of the partion to be added when adding a new session. Default is null.</param>
        public void AddSessionsFromExistingStores(string path, string partitionName = null)
        {
            this.AddSessionsFromExistingStores(path, path, partitionName);
        }

        private void AddSessionsFromExistingStores(string rootPath, string currentPath, string partitionName)
        {
            // scan for any psi catalog files
            foreach (var filename in Directory.EnumerateFiles(currentPath, "*.Catalog_000000.psi"))
            {
                var fi = new FileInfo(filename);
                var storeName = fi.Name.Substring(0, fi.Name.Length - ".Catalog_000000.psi".Length);
                var sessionName = (currentPath == rootPath) ? filename : Path.Combine(currentPath, filename).Substring(rootPath.Length);
                sessionName = sessionName.Substring(0, sessionName.Length - fi.Name.Length);
                sessionName = sessionName.Trim('\\');
                this.AddSessionFromExistingStore(sessionName, storeName, currentPath, partitionName);
            }

            // now go through subfolders
            foreach (var directory in Directory.EnumerateDirectories(currentPath))
            {
                this.AddSessionsFromExistingStores(rootPath, Path.Combine(currentPath, directory), partitionName);
            }
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
        }

        /// <summary>
        /// Runs the specified importer action over each session in the dataset.
        /// </summary>
        /// <param name="action">The action to run over a session.</param>
        private void Run(Action<Pipeline, SessionImporter> action)
        {
            foreach (var session in this.Sessions)
            {
                using (var pipeline = Pipeline.Create())
                {
                    var importer = SessionImporter.Open(pipeline, session);
                    action(pipeline, importer);
                    pipeline.Run(new ReplayDescriptor(importer.OriginatingTimeInterval));
                }
            }
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
