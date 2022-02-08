// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    /// <summary>
    /// Configuration for the <see cref="PhotoVideoCamera"/> component.
    /// </summary>
    public class PhotoVideoCameraConfiguration
    {
        /// <summary>
        /// Gets or sets the settings for the <see cref="PhotoVideoCamera.VideoImage"/> stream, or null to omit.
        /// </summary>
        public StreamSettings VideoStreamSettings { get; set; } = new (); // use defaults

        /// <summary>
        /// Gets or sets the settings for the <see cref="PhotoVideoCamera.PreviewImage"/> stream, or null to omit.
        /// </summary>
        public StreamSettings PreviewStreamSettings { get; set; } = null;

        /// <summary>
        /// Defines the capture settings for the Video or Preview streams.
        /// </summary>
        /// <remarks>
        /// Valid capture profiles for HoloLens2 are as follows.
        ///
        ///    2272x1278 (15,30fps, Video, Preview)
        ///    1952x1100 (15,30fps, Video, Preview)
        ///    1920x1080 (15,30fps, Video, Preview)
        ///    1504x846 (15,30fps, Video, Preview)
        ///    1280x720 (15,30fps, Video, Preview)
        ///    1128x636 (15,30fps, Video only)
        ///    960x540 (15,30fps, Video only)
        ///    896x504 (15,30fps, Video, Preview)
        ///    760x428 (15,30fps, Video only)
        ///    640x360 (15,30fps, Video only)
        ///    500x282 (15,30fps, Video only)
        ///    424x240 (15,30fps, Video only)
        ///
        /// For more info,
        /// see https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2.
        ///
        /// If capturing both Video and Preview streams, the selected capture settings must be supported in the same camera profile.
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
            /// Gets or sets a value indicating whether the camera intrinsics are emitted.
            /// </summary>
            public bool OutputIntrinsics { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the camera pose is emitted.
            /// </summary>
            public bool OutputPose { get; set; } = true;

            /// <summary>
            /// Gets or sets the settings for mixed reality capture, or null to omit holograms on this stream.
            /// </summary>
            public MixedRealityCaptureVideoEffect MixedRealityCapture { get; set; } = null;
        }
    }
}
