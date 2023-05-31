// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.MediaCapture
{
    /// <summary>
    /// Configuration for the <see cref="PhotoVideoCamera"/> component.
    /// </summary>
    public class PhotoVideoCameraConfiguration
    {
        /// <summary>
        /// Gets or sets the settings for the <see cref="PhotoVideoCamera.VideoEncodedImage"/> stream, or null to omit.
        /// </summary>
        public StreamSettings VideoStreamSettings { get; set; } = new (); // use defaults

        /// <summary>
        /// Gets or sets the settings for the <see cref="PhotoVideoCamera.PreviewEncodedImage"/> stream, or null to omit.
        /// </summary>
        public StreamSettings PreviewStreamSettings { get; set; } = null;

        /// <summary>
        /// Defines the capture settings for the Video or Preview streams.
        /// </summary>
        /// <remarks>
        /// If capturing both the Video and Preview streams, the requested resolutions must both
        /// be supported by the same profile. Valid capture profiles for HoloLens2 are as follows.
        ///
        ///  Profile 6B52B017-42C7-4A21-BFE3-23F009149887:
        ///    2272x1278 (15,30fps, Video, Preview)
        ///    896x504 (15,30fps, Video, Preview)
        ///  Profile 6B52B017-42C7-4A21-BFE3-23F009149887:
        ///    2284x1284 (24fps, Video only)
        ///    2284x1284 (15,30fps, Video, Preview)
        ///    1522x856 (24fps, Video only)
        ///    1522x856 (15,30fps, Video, Preview)
        ///  Profile 6B52B017-42C7-4A21-BFE3-23F009149887:
        ///    1952x1100 (15,30fps, Video, Preview)
        ///    1920x1080 (15,30fps, Video, Preview)
        ///    1504x846 (5fps, Video only)
        ///    1504x846 (15,30fps, Video, Preview)
        ///    1280x720 (15,30fps, Video, Preview)
        ///    1128x636 (15,30fps, Video only)
        ///    960x540 (15,30fps, Video only)
        ///    760x428 (15,30fps, Video only)
        ///    640x360 (15,30fps, Video only)
        ///    500x282 (15,30fps, Video only)
        ///    424x240 (15,30fps, Video only)
        ///  Profile B4894D81-62B7-4EEC-8740-80658C4A9D3E:
        ///    2272x1278 (15,30fps, Video, Preview)
        ///    896x504 (15,30fps, Video, Preview)
        ///  Profile C5444A88-E1BF-4597-B2DD-9E1EAD864BB8:
        ///    1952x1100 (60fps, Video only)
        ///    1952x1100 (15,30fps, Video, Preview)
        ///    1920x1080 (15,30fps, Video, Preview)
        ///    1504x846 (5,60fps, Video only)
        ///    1504x846 (15,30fps, Video, Preview)
        ///    1280x720 (15,30fps, Video, Preview)
        ///    1128x636 (15,30fps, Video only)
        ///    960x540 (15,30fps, Video only)
        ///    760x428 (15,30fps, Video only)
        ///    640x360 (15,30fps, Video only)
        ///    500x282 (15,30fps, Video only)
        ///    424x240 (15,30fps, Video only)
        ///  Profile CDF68AD0-7B8D-4DE3-BB64-AC1CE06DA333:
        ///    2284x1284 (24fps, Video only)
        ///    2284x1284 (15,30fps, Video, Preview)
        ///    1522x856 (24fps, Video only)
        ///    1522x856 (15,30fps, Video, Preview)
        ///
        /// For more info,
        /// see https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2.
        ///
        /// Each stream represents a virtual camera in the camera profile and therefore each has its own Instrinsics and Pose streams.
        /// For the HoloLens, since the Video and Preview streams both ultimately originate from the PV camera, the data on the Pose
        /// streams will be identical, representing the PV camera pose. It is therefore only necessary to capture one of the Pose
        /// streams when both Video and Preview capture are enabled. The Intrinsics may be different if the capture resolutions are
        /// different. You may configure whether or not to emit the Pose and/or Intrinsics stream on the Video and Preview streams
        /// by setting the respective OutputPose and OutputIntrinsics configuration parameter.
        /// </remarks>
        public class StreamSettings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StreamSettings"/> class.
            /// </summary>
            public StreamSettings()
            {
            }

            /// <summary>
            /// Gets or sets the capture frame rate.
            /// </summary>
            public int FrameRate { get; set; } = 15;

            /// <summary>
            /// Gets or sets the capture image width.
            /// </summary>
            public int ImageWidth { get; set; } = 1280;

            /// <summary>
            /// Gets or sets the capture image height.
            /// </summary>
            public int ImageHeight { get; set; } = 720;

            /// <summary>
            /// Gets or sets a value indicating whether the BGRA-converted image is emitted.
            /// </summary>
            public bool OutputImage { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether the original NV12-encoded image is emitted.
            /// </summary>
            public bool OutputEncodedImage { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the camera intrinsics are emitted.
            /// </summary>
            public bool OutputCameraIntrinsics { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the camera pose is emitted.
            /// </summary>
            public bool OutputPose { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the BGRA-converted image camera view is emitted.
            /// </summary>
            public bool OutputImageCameraView { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether the original NV12-encoded camera view is emitted.
            /// </summary>
            public bool OutputEncodedImageCameraView { get; set; } = true;

            /// <summary>
            /// Gets or sets the settings for mixed reality capture, or null to omit holograms on this stream.
            /// </summary>
            public MixedRealityCaptureVideoEffect MixedRealityCapture { get; set; } = null;
        }
    }
}
