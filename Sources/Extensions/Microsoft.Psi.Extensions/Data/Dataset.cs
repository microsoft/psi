// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Extensions.Base;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a dataset (collection of sessions) to be reasoned over.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Dataset : ObservableObject
    {
        /// <summary>
        /// Default name of a dataset.
        /// </summary>
        public const string DefaultName = "Untitled Dataset";

        private string name;
        private string filename;
        private TimeInterval originatingTimeInterval;
        private Session currentSession;
        private ObservableCollection<Session> internalSessions;
        private ReadOnlyObservableCollection<Session> sessions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dataset"/> class.
        /// </summary>
        /// <param name="name">The name of the new dataset. Default is <see cref="DefaultName"/>.</param>
        [JsonConstructor]
        public Dataset(string name = Dataset.DefaultName)
        {
            this.name = name;
            this.filename = string.Empty;
            this.originatingTimeInterval = null;
            this.currentSession = null;
            this.internalSessions = new ObservableCollection<Session>();
            this.sessions = new ReadOnlyObservableCollection<Session>(this.internalSessions);
        }

        /// <summary>
        /// Gets or sets the current session for this dataset.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Session CurrentSession
        {
            get { return this.currentSession; }
            set { this.Set(nameof(this.CurrentSession), ref this.currentSession, value); }
        }

        /// <summary>
        /// Gets the filename of this dataset.
        /// </summary>
        [IgnoreDataMember]
        public string FileName
        {
            get { return this.filename; }
            private set { this.Set(nameof(this.FileName), ref this.filename, value); }
        }

        /// <summary>
        /// Gets or sets the name of this dataset.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets the orginating time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimeInterval OriginatingTimeInterval
        {
            get { return this.originatingTimeInterval; }
            set { this.Set(nameof(this.OriginatingTimeInterval), ref this.originatingTimeInterval, value); }
        }

        /// <summary>
        /// Gets the collection of sessions in this dataset.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ReadOnlyObservableCollection<Session> Sessions => this.sessions;

        [Browsable(false)]
        [DataMember(Name = "Sessions")]
        private ObservableCollection<Session> InternalSessions
        {
            get { return this.internalSessions; }
            set { this.Set(nameof(this.Sessions), ref this.internalSessions, value); }
        }

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
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                });
            using (var jsonFile = File.OpenText(filename))
            using (var jsonReader = new JsonTextReader(jsonFile))
            {
                Dataset dataset = serializer.Deserialize<Dataset>(jsonReader);
                dataset.FileName = filename;
                return dataset;
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
        /// <returns>The newly created session.</returns>
        public Session CreateSession()
        {
            var session = new Session(this);
            this.internalSessions.Add(session);
            return session;
        }

        /// <summary>
        /// Removes the specified session from the dataset.
        /// </summary>
        /// <param name="session">The session to remove.</param>
        public void RemoveSession(Session session)
        {
            this.internalSessions.Remove(session);
        }

        /// <summary>
        /// Saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataet into.</param>
        public void Save(string filename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
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
            var partition = session.AddStorePartition(storeName, storePath, partitionName);

            // compute the new originating time interval
            var oldOriginatingTimeInterval = this.OriginatingTimeInterval;
            this.OriginatingTimeInterval = this.Sessions.Count() == 0 ?
                session.OriginatingTimeInterval :
                TimeInterval.Coverage(new TimeInterval[] { this.OriginatingTimeInterval, session.OriginatingTimeInterval });

            // add the session, then raise the event
            this.internalSessions.Add(session);

            // make new session current session
            this.CurrentSession = session;

            return session;
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
            this.OriginatingTimeInterval = TimeInterval.Coverage(this.internalSessions.Select(s => s.OriginatingTimeInterval));
            this.CurrentSession = this.internalSessions.FirstOrDefault();

            foreach (var session in this.internalSessions)
            {
                session.Dataset = this;
            }

            this.sessions = new ReadOnlyObservableCollection<Session>(this.internalSessions);
            this.RaisePropertyChanged(nameof(this.Sessions));
        }
    }
}
