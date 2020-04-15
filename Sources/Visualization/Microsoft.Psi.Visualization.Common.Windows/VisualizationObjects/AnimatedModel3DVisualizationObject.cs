// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represnts an animated model 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize Animated Model")]
    public class AnimatedModel3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem>
    {
        private CoordinateSystem cameraTransform;
        private string source;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedModel3DVisualizationObject"/> class.
        /// </summary>
        public AnimatedModel3DVisualizationObject()
        {
            this.Visual3D = new AnimatedModelVisual(this);
        }

        /// <summary>
        /// Gets or sets the camera transform.
        /// </summary>
        [DataMember]
        public CoordinateSystem CameraTransform
        {
            get { return this.cameraTransform; }
            set { this.Set(nameof(this.CameraTransform), ref this.cameraTransform, value); }
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [DataMember]
        public string Source
        {
            get { return this.source; }
            set { this.Set(nameof(this.Source), ref this.source, value); }
        }
    }
}
