// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents an instant 3D visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Instant3DVisualizationObjectConfiguration : InstantVisualizationObjectConfiguration
    {
        private Vector3D localOffset;
        private Vector3D localRotation;
        private Vector3D localScale;
        private CoordinateSystem localTransform = new CoordinateSystem();

        /// <summary>
        /// Initializes a new instance of the <see cref="Instant3DVisualizationObjectConfiguration"/> class.
        /// </summary>
        public Instant3DVisualizationObjectConfiguration()
        {
            this.localOffset = new Vector3D(0, 0, 0);
            this.localRotation = new Vector3D(0, 0, 0);
            this.localScale = new Vector3D(1, 1, 1);
            this.localTransform = new CoordinateSystem();
        }

        /// <summary>
        /// Gets or sets the local offset.
        /// </summary>
        [DataMember]
        public Vector3D LocalOffset
        {
            get => this.localOffset;
            set
            {
                this.Set(nameof(this.LocalOffset), ref this.localOffset, value);
                this.UpdateLocalTranformation();
            }
        }

        /// <summary>
        /// Gets or sets the local rotation.
        /// </summary>
        [DataMember]
        public Vector3D LocalRotation
        {
            get => this.localRotation;
            set
            {
                this.Set(nameof(this.LocalRotation), ref this.localRotation, value);
                this.UpdateLocalTranformation();
            }
        }

        /// <summary>
        /// Gets or sets the local scale.
        /// </summary>
        [DataMember]
        public Vector3D LocalScale
        {
            get => this.localScale;
            set
            {
                this.Set(nameof(this.LocalScale), ref this.localScale, value);
                this.UpdateLocalTranformation();
            }
        }

        /// <summary>
        /// Gets or sets the local transform.
        /// </summary>
        [DataMember]
        public CoordinateSystem LocalTransform
        {
            get => this.localTransform;
            set { this.Set(nameof(this.LocalTransform), ref this.localTransform, value); }
        }

        private void UpdateLocalTranformation()
        {
            this.LocalTransform = new CoordinateSystem();
        }
    }
}
