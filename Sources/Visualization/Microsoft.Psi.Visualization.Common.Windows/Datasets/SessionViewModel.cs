// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Datasets
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a view model of a session.
    /// </summary>
    public class SessionViewModel : ObservableObject
    {
        private Session session;
        private DatasetViewModel datasetViewModel;
        private ObservableCollection<PartitionViewModel> internalPartitionViewModels;
        private ReadOnlyObservableCollection<PartitionViewModel> partitionViewModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionViewModel"/> class.
        /// </summary>
        /// <param name="datasetViewModel">The view model of the dataset to which this session belongs.</param>
        /// <param name="session">The session for which to create the view model.</param>
        public SessionViewModel(DatasetViewModel datasetViewModel, Session session)
        {
            this.session = session;
            this.datasetViewModel = datasetViewModel;
            this.internalPartitionViewModels = new ObservableCollection<PartitionViewModel>();
            this.partitionViewModels = new ReadOnlyObservableCollection<PartitionViewModel>(this.internalPartitionViewModels);

            foreach (var partition in this.session.Partitions)
            {
                this.internalPartitionViewModels.Add(new PartitionViewModel(this, partition));
            }
        }

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public string Name
        {
            get => this.session.Name;
            set
            {
                if (this.session.Name != value)
                {
                    this.session.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in the underlying session.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval => this.session.OriginatingTimeInterval;

        /// <summary>
        /// Gets the collection of partitions in this session.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<PartitionViewModel> PartitionViewModels => this.partitionViewModels;

        /// <summary>
        /// Gets the underlying session.
        /// </summary>
        internal Session Session => this.session;

        /// <summary>
        /// Creates and adds an annotation partition from an existing annotation store.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added partition view model.</returns>
        public PartitionViewModel AddAnnotationPartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            return this.AddPartition(this.session.AddAnnotationPartition(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Creates and adds a data partition from an existing data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added partition view model.</returns>
        public PartitionViewModel AddStorePartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            return this.AddPartition(this.session.AddStorePartition(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Creates and adds an new annotation partition.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="definition">The annotated event definition to use when creating new annoted events in the newly created annotation partition.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added annotation partition.</returns>
        public PartitionViewModel CreateAnnotationPartition(string storeName, string storePath, AnnotatedEventDefinition definition, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            return this.AddPartition(this.session.CreateAnnotationPartition(storeName, storePath, definition, partitionName));
        }

        /// <summary>
        /// Creates and adds a new data partition.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        /// <returns>The newly added data partition.</returns>
        public PartitionViewModel CreateStorePartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            return this.AddPartition(this.session.CreateStorePartition(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Removes a specified partition from the underlying session.
        /// </summary>
        /// <param name="partitionViewModel">The view model of the partition to be removed.</param>
        public void RemovePartition(PartitionViewModel partitionViewModel)
        {
            this.session.RemovePartition(partitionViewModel.Partition);
            this.internalPartitionViewModels.Remove(partitionViewModel);
        }

        /// <summary>
        /// Removes this session from the dataset it belongs to.
        /// </summary>
        public void RemoveSession()
        {
            this.datasetViewModel.RemoveSession(this);
        }

        private PartitionViewModel AddPartition(IPartition partition)
        {
            var partitionViewModel = new PartitionViewModel(this, partition);
            this.internalPartitionViewModels.Add(partitionViewModel);
            return partitionViewModel;
        }

        private string EnsureUniquePartitionName(string partitionName)
        {
            int suffix = 0;
            string partitionNamePrefix = partitionName;

            // ensure that partition name is unique
            while (this.PartitionViewModels.Any(pvm => pvm.Name == partitionName))
            {
                // append numeric suffix to ensure uniqueness
                partitionName = $"{partitionNamePrefix}_{++suffix}";
            }

            return partitionName;
        }
    }
}
