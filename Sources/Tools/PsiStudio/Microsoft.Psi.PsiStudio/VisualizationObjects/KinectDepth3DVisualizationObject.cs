// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a Kinect depth 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class KinectDepth3DVisualizationObject : Instant3DVisualizationObject<Shared<Image>, KinectDepth3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KinectDepth3DVisualizationObject"/> class.
        /// </summary>
        public KinectDepth3DVisualizationObject()
        {
            this.Visual3D = new KinectDepthVisual(this);
        }

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Color = Colors.Navy;
        }
    }
}
