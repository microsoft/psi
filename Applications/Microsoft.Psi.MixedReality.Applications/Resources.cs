// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Defines various platform-specific resources.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Gets or sets the image decoder.
        /// </summary>
        public static IImageFromStreamDecoder ImageFromStreamDecoder { get; set; } = null;

        /// <summary>
        /// Gets or sets the image encoder.
        /// </summary>
        public static IImageToStreamEncoder ImageToStreamEncoder { get; set; } = null;

        /// <summary>
        /// Gets or sets the preview image encoder.
        /// </summary>
        public static IImageToStreamEncoder PreviewImageToStreamEncoder { get; set; } = null;

        /// <summary>
        /// Gets or sets the depth image decoder.
        /// </summary>
        public static IDepthImageFromStreamDecoder DepthImageFromStreamDecoder { get; set; } = null;

        /// <summary>
        /// Gets or sets the depth image encoder.
        /// </summary>
        public static IDepthImageToStreamEncoder DepthImageToStreamEncoder { get; set; } = null;

        /// <summary>
        /// Gets or sets the constructor for the voice activity detector.
        /// </summary>
        public static Func<Pipeline, IVoiceActivityDetector> VoiceActivityDetectorConstructor { get; set; } = null;

        /// <summary>
        /// Gets or sets the constructor for the audio resampler.
        /// </summary>
        public static Func<Pipeline, IAudioResampler> AudioResamplerConstructor { get; set; } = null;
    }
}
