// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// IKinectSensor defines the interface used to interact with the Kinect.
    /// </summary>
    public interface IKinectSensor
    {
        /// <summary>
        /// Gets an emitter that emits a stream of KinectBody samples.
        /// </summary>
        Emitter<List<KinectBody>> Bodies { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of image samples for the Kinect's color camera.
        /// </summary>
        Emitter<Shared<Image>> ColorImage { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of image samples for the Kinect's depth camera.
        /// </summary>
        Emitter<Shared<Image>> DepthImage { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of image samples for the Kinect's infrared feed.
        /// </summary>
        Emitter<Shared<Image>> InfraredImage { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of image samples for the Kinect's long exposure infrared feed.
        /// </summary>
        Emitter<Shared<Image>> LongExposureInfraredImage { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of depth device calibration info objects for the Kinect.
        /// </summary>
        Emitter<IDepthDeviceCalibrationInfo> DepthDeviceCalibrationInfo { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of AudioBuffer samples from the Kinect.
        /// </summary>
        Emitter<AudioBuffer> Audio { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of KinectAudioBeamInfo samples from the Kinect.
        /// </summary>
        Emitter<KinectAudioBeamInfo> AudioBeamInfo { get; }

        /// <summary>
        /// Gets an emitter that emits a stream of IList.ulong samples from the Kinect.
        /// </summary>
        Emitter<IList<ulong>> AudioBodyCorrelations { get; }
    }
}