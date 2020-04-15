// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a Kinect depth 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize Kinect Depth Data")]
    public class KinectDepth3DVisualizationObject : Instant3DVisualizationObject<Shared<Image>>
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

        /// <inheritdoc/>
        protected override void InitNew()
        {
            this.Visual3D = new KinectDepthVisual(this);
            base.InitNew();
        }
    }
}
