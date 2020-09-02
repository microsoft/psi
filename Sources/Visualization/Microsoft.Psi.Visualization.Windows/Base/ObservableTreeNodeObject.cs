// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Base
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// An observable object that can be used in a tree view.
    /// </summary>
    public abstract class ObservableTreeNodeObject : ObservableObject
    {
        private bool isTreeNodeExpanded = false;
        private bool isTreeNodeSelected = false;

        /// <summary>
        /// Gets or sets a value indicating whether the tree view item is expanded.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsTreeNodeExpanded
        {
            get => this.isTreeNodeExpanded;
            set => this.Set(nameof(this.IsTreeNodeExpanded), ref this.isTreeNodeExpanded, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tree view item is selected.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsTreeNodeSelected
        {
            get => this.isTreeNodeSelected;
            set => this.Set(nameof(this.IsTreeNodeSelected), ref this.isTreeNodeSelected, value);
        }
    }
}
