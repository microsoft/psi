// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Face
{
    using Microsoft.Kinect.Face;

    /// <summary>
    /// Defines settings used to configure the Kinect's Face detector/tracking.
    /// </summary>
    public class KinectFaceDetectorConfiguration
    {
        /// <summary>
        /// Defines the default configuration settings.
        /// </summary>
        public static readonly KinectFaceDetectorConfiguration Default = new KinectFaceDetectorConfiguration();

        /// <summary>
        /// Gets or sets which face detection features are reported.
        /// </summary>
        public FaceFrameFeatures FaceFrameFeatures { get; set; } =
            FaceFrameFeatures.BoundingBoxInColorSpace |
            FaceFrameFeatures.BoundingBoxInInfraredSpace |
            FaceFrameFeatures.FaceEngagement |
            FaceFrameFeatures.Glasses |
            FaceFrameFeatures.Happy |
            FaceFrameFeatures.LeftEyeClosed |
            FaceFrameFeatures.LookingAway |
            FaceFrameFeatures.MouthMoved |
            FaceFrameFeatures.MouthOpen |
            FaceFrameFeatures.PointsInColorSpace |
            FaceFrameFeatures.PointsInInfraredSpace |
            FaceFrameFeatures.RightEyeClosed |
            FaceFrameFeatures.RotationOrientation;
    }
}
