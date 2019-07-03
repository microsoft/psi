// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Face
{
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;

    /// <summary>
    /// KinectFace defines the object returned on the KinectSensor.Faces stream.
    /// </summary>
    public class KinectFace
    {
        /// <summary>
        /// Gets or sets the tracking ID used to associate a given face to a KinectBody.
        /// </summary>
        public ulong TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the face's bounding box in pixels relative to the color image.
        /// </summary>
        public RectI FaceBoundingBoxInColorSpace { get; set; }

        /// <summary>
        /// Gets or sets the face's bounding box in pixels relative to the infrared image.
        /// </summary>
        public RectI FaceBoundingBoxInInfraredSpace { get; set; }

        /// <summary>
        /// Gets or sets which facial features are returned by face tracking.
        /// </summary>
        public FaceFrameFeatures FaceFrameFeatures { get; set; }

        /// <summary>
        /// Gets or sets a list of points for each face point. Points are defined in pixels relative to the color image.
        /// </summary>
        public Dictionary<FacePointType, PointF> FacePointsInColorSpace { get; set; }

        /// <summary>
        /// Gets or sets a list of points for each face point. Points are defined in pixels relative to the infared image.
        /// </summary>
        public Dictionary<FacePointType, PointF> FacePointsInInfraredSpace { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of results as to whether some face property has been detected.
        /// </summary>
        public Dictionary<FaceProperty, DetectionResult> FaceProperties { get; set; }

        /// <summary>
        /// Gets or sets the relative orientation of the face as a quaternion.
        /// </summary>
        public Vector4 FaceRotationQuaternion { get; set; }
    }
}
