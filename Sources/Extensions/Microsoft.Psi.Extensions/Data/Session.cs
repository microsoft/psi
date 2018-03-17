// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Extensions.Annotations;
    using Microsoft.Psi.Extensions.Base;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a sesion (collection of partitions) to be reasoned over.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Session : ObservableObject
    {
        /// <summary>
        /// Default name of a session.
        /// </summary>
        public const string DefaultName = "Untitled Session";

        private Dataset dataset;
        private string name;
        private TimeInterval originatingTimeInterval;
        private ObservableCollection<IPartition> internalPartitions;
        private ReadOnlyObservableCollection<IPartition> partitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="dataset">The dataset that this session belongs to.</param>
        /// <param name="name">The session name.</param>
        public Session(Dataset dataset, string name = Session.DefaultName)
        {
            this.dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            this.name = name;
            this.originatingTimeInterval = null;
            this.internalPartitions = new ObservableCollection<IPartition>();
            this.partitions = new ReadOnlyObservableCollection<IPartition>(this.internalPartitions);
        }

        [JsonConstructor]
        private Session()
        {
        }

        /// <summary>
        /// Gets the dataset that this session belongs to.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Dataset Dataset
        {
            get { return this.dataset; }
            internal set { this.Set(nameof(this.Dataset), ref this.dataset, value); }
        }

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimeInterval OriginatingTimeInterval
        {
            get { return this.originatingTimeInterval; }
            private set { this.Set(nameof(this.OriginatingTimeInterval), ref this.originatingTimeInterval, value); }
        }

        /// <summary>
        /// Gets the collection of partitions in this session.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ReadOnlyObservableCollection<IPartition> Partitions => this.partitions;

        [DataMember(Name = "Partitions")]
        private ObservableCollection<IPartition> InternalPartitions
        {
            get { return this.internalPartitions; }
            set { this.Set(nameof(this.Partitions), ref this.internalPartitions, value); }
        }

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
        /// Removes this session from the dataset it belongs to.
        /// </summary>
        public void RemoveSession()
        {
            this.dataset.RemoveSession(this);
        }

        private void AddPartition(IPartition partition)
        {
            this.UpdateOriginatingTimeInterval(partition);
            this.InternalPartitions.Add(partition);
        }

        private void UpdateOriginatingTimeInterval(IPartition partition)
        {
            // compute the new originating time interval
            var oldOriginatingTimeInterval = this.OriginatingTimeInterval;
            this.OriginatingTimeInterval = this.Partitions.Count() == 0 ?
                partition.OriginatingTimeInterval :
                TimeInterval.Coverage(new TimeInterval[] { this.OriginatingTimeInterval, partition.OriginatingTimeInterval });
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.OriginatingTimeInterval = TimeInterval.Coverage(this.InternalPartitions.Select(p => p.OriginatingTimeInterval));

            foreach (var partition in this.internalPartitions)
            {
                partition.Session = this;
            }

            this.partitions = new ReadOnlyObservableCollection<IPartition>(this.internalPartitions);
            this.RaisePropertyChanged(nameof(this.Partitions));
        }
    }
}
