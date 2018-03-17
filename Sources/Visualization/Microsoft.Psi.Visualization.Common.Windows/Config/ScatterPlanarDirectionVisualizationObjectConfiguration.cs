// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a scatter planar direction visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ScatterPlanarDirectionVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private Color color = Colors.DarkGray;
        private double size = 0.5;

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [DataMember]
        public double Size
        {
            get { return this.size; }
            set { this.Set(nameof(this.Size), ref this.size, value); }
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }
    }
}
