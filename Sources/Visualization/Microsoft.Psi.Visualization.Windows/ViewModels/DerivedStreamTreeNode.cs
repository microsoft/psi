// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System.Windows.Media;

    /// <summary>
    /// Implements a node in the dataset tree that represents a derived stream.
    /// </summary>
    /// <remarks>This class acts as a base class for various types of derived stream tree
    /// nodes such as <see cref="DerivedMemberStreamTreeNode"/> or
    /// <see cref="DerivedReceiverDiagnosticsStreamTreeNode{T}"/>.</remarks>
    public abstract class DerivedStreamTreeNode : StreamTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedStreamTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the stream tree node.</param>
        /// <param name="path">The path to the stream tree node.</param>
        /// <param name="name">The name of the stream tree node.</param>
        /// <param name="sourceStreamMetadata">The source stream metadata.</param>
        public DerivedStreamTreeNode(PartitionViewModel partitionViewModel, string path, string name, IStreamMetadata sourceStreamMetadata)
            : base(partitionViewModel, path, name, sourceStreamMetadata)
        {
        }

        /// <inheritdoc/>
        public override string IconSource => this.PartitionViewModel.IsLivePartition ? IconSourcePath.StreamMemberLive : IconSourcePath.StreamMember;

        /// <inheritdoc/>
        public override Brush ForegroundBrush => new SolidColorBrush(Colors.LightGray);

        /// <inheritdoc/>
        protected override void UpdateAuxiliaryInfo()
        {
            this.AuxiliaryInfo = string.Empty;
        }
    }
}
