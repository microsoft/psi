// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a XY panel 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class XYPanel3DVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private double height;
        private double width;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [DataMember]
        public double Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [DataMember]
        public double Width
        {
            get { return this.width; }
            set { this.Set(nameof(this.Width), ref this.width, value); }
        }
    }
}
