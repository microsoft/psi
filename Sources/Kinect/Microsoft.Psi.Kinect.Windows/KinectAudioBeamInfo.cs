// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    /// <summary>
    /// Defines structure that contains information about the Audio Beam from a Kinect.
    /// </summary>
    public struct KinectAudioBeamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KinectAudioBeamInfo"/> struct.
        /// </summary>
        /// <param name="angle">Direction sensor is set for listening.</param>
        /// <param name="confidence">Confidence in given direction.</param>
        public KinectAudioBeamInfo(float angle, float confidence)
        {
            this.Angle = angle;
            this.Confidence = confidence;
        }

        /// <summary>
        /// Gets the angle.
        /// </summary>
        public float Angle { get; private set; }

        /// <summary>
        /// Gets the confidence.
        /// </summary>
        public float Confidence { get; private set; }
    }
}
