// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;

    /// <summary>
    /// Represents a Kinect depth 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class KinectDepth3DVisualizationObject : Instant3DVisualizationObject<Shared<Image>, KinectDepth3DVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        protected override void InitNew()
        {
            this.Visual3D = new KinectDepthVisual(this);
            base.InitNew();
        }
    }
}
