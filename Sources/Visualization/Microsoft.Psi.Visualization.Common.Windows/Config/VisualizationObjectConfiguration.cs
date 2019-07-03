// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a base class for visualization object configurations.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class VisualizationObjectConfiguration : ObservableObject
    {
        /// <summary>
        /// The name of the visualization object.
        /// </summary>
        private string name;

        /// <summary>
        /// Indicated whether the visualization object is visible.
        /// </summary>
        private bool visible;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObjectConfiguration"/> class.
        /// </summary>
        public VisualizationObjectConfiguration()
        {
            this.visible = true;
        }

        /// <summary>
        /// Gets or sets the name of the visualization object.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the visualization object is visible.
        /// </summary>
        [DataMember]
        public bool Visible
        {
            get { return this.visible; }
            set { this.Set(nameof(this.Visible), ref this.visible, value); }
        }
    }
}
