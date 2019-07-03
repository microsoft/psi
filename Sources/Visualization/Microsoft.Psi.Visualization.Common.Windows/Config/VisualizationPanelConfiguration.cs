// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a base class for visualization panel configuraions.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class VisualizationPanelConfiguration : ObservableObject
    {
        /// <summary>
        /// The height of the panel.
        /// </summary>
        private double height = 400;

        /// <summary>
        /// The name of the visualization panel.
        /// </summary>
        private string name = "Visualization Panel";

        /// <summary>
        /// The width of the panel.
        /// </summary>
        private double width = 100;

        /// <summary>
        /// Gets or sets the height of the panel.
        /// </summary>
        [DataMember]
        public double Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the name of the visualization panel name.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets the width of the panel.
        /// </summary>
        [DataMember]
        public double Width
        {
            get { return this.width; }
            set { this.Set(nameof(this.Width), ref this.width, value); }
        }
    }
}