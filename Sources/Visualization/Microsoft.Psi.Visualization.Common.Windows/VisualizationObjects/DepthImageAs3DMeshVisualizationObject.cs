// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a depth image 3D mesh visualization object.
    /// </summary>
    [VisualizationObject("Visualize Depth Image as 3D Mesh")]
    public class DepthImageAs3DMeshVisualizationObject : Instant3DVisualizationObject<Shared<DepthImage>>
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
            this.Visual3D = new DepthImageAs3DMeshVisual3D(this);
            base.InitNew();
        }
    }
}
