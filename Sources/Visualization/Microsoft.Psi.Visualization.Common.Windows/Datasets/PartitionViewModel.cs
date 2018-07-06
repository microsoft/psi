// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Datasets
{
    using System.ComponentModel;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a view model of a partition.
    /// </summary>
    public class PartitionViewModel : ObservableObject
    {
        private IPartition partition;
        private IStreamTreeNode streamTreeRoot;
        private SessionViewModel sessionViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        /// <param name="sessionViewModel">The view model of the session to which this partition belongs.</param>
        /// <param name="partition">The partition for which to create the view model.</param>
        public PartitionViewModel(SessionViewModel sessionViewModel, IPartition partition)
        {
            this.partition = partition;
            this.sessionViewModel = sessionViewModel;
            this.StreamTreeRoot = new StreamTreeNode(this);
            foreach (var stream in this.partition.AvailableStreams)
            {
                this.StreamTreeRoot.AddPath(stream);
            }
        }

        /// <summary>
        /// Gets or sets the partition name.
        /// </summary>
        public string Name
        {
            get => this.partition.Name;
            set
            {
                if (this.partition.Name != value)
                {
                    this.partition.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this partition.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval => this.partition.OriginatingTimeInterval;

        /// <summary>
        /// Gets the session that this partition belongs to.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel SessionViewModel => this.sessionViewModel;

        /// <summary>
        /// Gets the store name of this partition.
        /// </summary>
        public string StoreName => this.partition.StoreName;

        /// <summary>
        /// Gets the store path of this partition.
        /// </summary>
        public string StorePath => this.partition.StorePath;

        /// <summary>
        /// Gets or sets the root stream tree node of this partition.
        /// </summary>
        [Browsable(false)]
        public IStreamTreeNode StreamTreeRoot
        {
            get { return this.streamTreeRoot; }
            set { this.Set(nameof(this.StreamTreeRoot), ref this.streamTreeRoot, value); }
        }

        /// <summary>
        /// Gets the underlying partition.
        /// </summary>
        internal IPartition Partition => this.partition;

        /// <summary>
        /// Removes this partition from the session that it belongs to.
        /// </summary>
        public void RemovePartition()
        {
            this.sessionViewModel.RemovePartition(this);
        }
    }
}
