// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System.ComponentModel;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization;

    /// <summary>
    /// Implements a node in the dataset tree that represents a derived stream container.
    /// </summary>
    public class DerivedStreamContainerTreeNode : StreamContainerTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedStreamContainerTreeNode"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition for the container tree node.</param>
        /// <param name="path">The path to the container tree node.</param>
        /// <param name="name">The name of the container tree node.</param>
        public DerivedStreamContainerTreeNode(PartitionViewModel partitionViewModel, string path, string name)
            : base(partitionViewModel, path, name)
        {
        }

        /// <inheritdoc/>
        public override string IconSource => this.PartitionViewModel.IsLivePartition ? IconSourcePath.GroupLive : IconSourcePath.Group;

        /// <inheritdoc/>
        public override Brush ForegroundBrush => new SolidColorBrush(Colors.LightGray);

        /// <inheritdoc/>
        [Browsable(false)]
        public override long SubsumedMessageCount => 0;

        /// <inheritdoc/>
        [Browsable(false)]
        public override long SubsumedSize => 0;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedOpenedTimeString => string.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedClosedTimeString => string.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedFirstMessageOriginatingTimeString => string.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedFirstMessageCreationTimeString => string.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedLastMessageOriginatingTimeString => string.Empty;

        /// <inheritdoc/>
        [Browsable(false)]
        public override string SubsumedLastMessageCreationTimeString => string.Empty;

        /// <inheritdoc/>
        protected override void UpdateAuxiliaryInfo()
        {
            this.AuxiliaryInfo = string.Empty;
        }
    }
}
