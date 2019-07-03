// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Face
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines interface to kinect sensor for handling face detection/tracking.
    /// </summary>
    public interface IKinectFaceDetector
    {
        /// <summary>
        /// Gets and emitter that emits a stream of KinectFace samples.
        /// </summary>
        Emitter<List<KinectFace>> Faces { get; }
    }
}
