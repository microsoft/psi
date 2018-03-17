// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represnts an animated model 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnimatedModel3DVisualizationObjectConfiguration : Instant3DVisualizationObjectConfiguration
    {
        private CoordinateSystem cameraTransform;
        private string source;

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
