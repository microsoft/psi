// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System.Collections.Generic;
    using Microsoft.Kinect;

    /// <summary>
    /// KinectBody holds information about a body detected by the Kinect
    /// </summary>
    public class KinectBody
    {
        private static int jointCount = Body.JointCount;

        /// <summary>
        /// Gets the number of joints in body's skeleton
        /// </summary>
        public int JointCount => jointCount;

        /// <summary>
        /// Gets or sets the clipped edges
        /// </summary>
        public FrameEdges ClippedEdges { get; set; }

        /// <summary>
        /// Gets or sets the floor's clip plane
        /// </summary>
        public Vector4 FloorClipPlane { get; set; }

        /// <summary>
        /// Gets or sets confidence in position/pose of left hand
        /// </summary>
        public TrackingConfidence HandLeftConfidence { get; set; }

        /// <summary>
        /// Gets or sets state of left hand
        /// </summary>
        public HandState HandLeftState { get; set; }

        /// <summary>
        /// Gets or sets confidence in position/pose of right hand
        /// </summary>
        public TrackingConfidence HandRightConfidence { get; set; }

        /// <summary>
        /// Gets or sets state of right hand
        /// </summary>
        public HandState HandRightState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the body is restricted
        /// </summary>
        public bool IsRestricted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the body is tracked
        /// </summary>
        public bool IsTracked { get; set; }

        /// <summary>
        /// Gets or sets the joint orientations
        /// </summary>
        public Dictionary<JointType, JointOrientation> JointOrientations { get; set; }

        /// <summary>
        /// Gets or sets the joints
        /// </summary>
        public Dictionary<JointType, Joint> Joints { get; set; }

        /// <summary>
        /// Gets or sets the lean point
        /// </summary>
        public PointF Lean { get; set; }

        /// <summary>
        /// Gets or sets the lean tracking state
        /// </summary>
        public TrackingState LeanTrackingState { get; set; }

        /// <summary>
        /// Gets or sets the body's tracking ID
        /// </summary>
        public ulong TrackingId { get; set; }
    }
}
