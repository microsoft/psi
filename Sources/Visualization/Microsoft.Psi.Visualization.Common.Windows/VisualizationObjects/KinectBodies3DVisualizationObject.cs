// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a Kinect bodies 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize Kinect Bodies")]
    public class KinectBodies3DVisualizationObject : Instant3DVisualizationObject<List<KinectBody>>
    {
        private Color color = Colors.White;
        private double inferredJointsOpacity = 0;
        private double size = 0.03;
        private bool showTrackingBillboards = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectBodies3DVisualizationObject"/> class.
        /// </summary>
        public KinectBodies3DVisualizationObject()
        {
            this.Visual3D = new KinectBodiesVisual(this);
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

        /// <summary>
        /// Gets or sets the inferred joints opacity.
        /// </summary>
        [DataMember]
        public double InferredJointsOpacity
        {
            get { return this.inferredJointsOpacity; }
            set { this.Set(nameof(this.InferredJointsOpacity), ref this.inferredJointsOpacity, value); }
        }

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
        /// Gets or sets a value indicating whether to show tracking billboards.
        /// </summary>
        [DataMember]
        public bool ShowTrackingBillboards
        {
            get { return this.showTrackingBillboards; }
            set { this.Set(nameof(this.ShowTrackingBillboards), ref this.showTrackingBillboards, value); }
        }
    }
}
