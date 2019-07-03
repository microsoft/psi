// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents a Kinect depth 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class KinectDepth3DVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private Color color = Colors.Navy;

        /// <summary>
        /// Gets or sets the mesh color.
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }
    }
}
