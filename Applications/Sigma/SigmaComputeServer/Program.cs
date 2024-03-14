// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Speech;
    using Sigma.Diamond;

    /// <summary>
    /// Compute server that implements the compute functionality for Sigma app.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">The application arguments.</param>
        public static void Main(string[] args)
        {
            // Register platform specific resources
            Microsoft.Psi.Audio.Resources.RegisterPlatformResources();
            Microsoft.Psi.Imaging.Resources.RegisterPlatformResources();
            Microsoft.Psi.Speech.Resources.RegisterPlatformResources();

            // Setup the image encoders and decoders to use by the application layer
            Microsoft.Psi.MixedReality.Applications.Resources.ImageToStreamEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 80 };
            Microsoft.Psi.MixedReality.Applications.Resources.PreviewImageToStreamEncoder = new ImageToJpegStreamEncoder() { QualityLevel = 20 };
            Microsoft.Psi.MixedReality.Applications.Resources.ImageFromStreamDecoder = PlatformResources.GetDefault<IImageFromStreamDecoder>();
            Microsoft.Psi.MixedReality.Applications.Resources.DepthImageToStreamEncoder = PlatformResources.GetDefault<IDepthImageToStreamEncoder>();
            Microsoft.Psi.MixedReality.Applications.Resources.DepthImageFromStreamDecoder = PlatformResources.GetDefault<IDepthImageFromStreamDecoder>();
            Microsoft.Psi.MixedReality.Applications.Resources.VoiceActivityDetectorConstructor = PlatformResources.GetDefault<Func<Pipeline, IVoiceActivityDetector>>();
            Microsoft.Psi.MixedReality.Applications.Resources.AudioResamplerConstructor = PlatformResources.GetDefault<Func<Pipeline, IAudioResampler>>();

            SigmaComputeServer.RunLive(new[] { typeof(DiamondConfiguration) });
        }
    }
}