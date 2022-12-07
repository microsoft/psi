// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.MediaCapture
{
    using Windows.Foundation;
    using Windows.Foundation.Collections;
    using Windows.Media.Capture;
    using Windows.Media.Effects;

    /// <summary>
    /// Video effect definition for mixed-reality capture via the PV camera. See
    /// https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers
    /// for more information on enabling mixed-reality capture.
    /// </summary>
    public class MixedRealityCaptureVideoEffect : IVideoEffectDefinition
    {
        private readonly PropertySet properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="MixedRealityCaptureVideoEffect"/> class.
        /// </summary>
        /// <param name="streamType">The capture stream to which this effect is to be applied.</param>
        /// <param name="globalOpacityCoefficient">The opacity of the holograms in range from 0.0 (fully transparent) to 1.0 (fully opaque).</param>
        /// <param name="preferredHologramPerspective">
        /// Value used to indicate which holographic camera view configuration should be captured:
        /// 0 (Display) means that the app won't be asked to render from the photo/video camera,
        /// 1 (PhotoVideoCamera) will ask the app to render from the photo/video camera (if the app supports it).
        /// Only supported on HoloLens 2.
        /// </param>
        public MixedRealityCaptureVideoEffect(
            MediaStreamType streamType = MediaStreamType.VideoRecord,
            float globalOpacityCoefficient = 0.9f,
            MixedRealityCapturePerspective preferredHologramPerspective = MixedRealityCapturePerspective.PhotoVideoCamera)
        {
            this.properties = new ()
            {
                { "StreamType", streamType },
                { "HologramCompositionEnabled", true },
                { "RecordingIndicatorEnabled", false },
                { "VideoStabilizationEnabled", false },
                { "VideoStabilizationBufferLength", 0 },
                { "GlobalOpacityCoefficient", globalOpacityCoefficient },
                { "BlankOnProtectedContent", false },
                { "ShowHiddenMesh", false },
                { "OutputSize", new Size(0, 0) },
                { "PreferredHologramPerspective", (uint)preferredHologramPerspective }, // cast is necessary for this to work
            };
        }

        /// <summary>
        /// Gets the class ID of this video effect definition.
        /// </summary>
        public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";

        /// <summary>
        /// Gets the properties of this video effect definition.
        /// </summary>
        public IPropertySet Properties => this.properties;
    }
}
