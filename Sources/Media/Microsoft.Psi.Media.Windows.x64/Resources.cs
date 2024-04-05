// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements methods for registering platform specific resources.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Registers platform specific resources.
        /// </summary>
        public static void RegisterPlatformResources()
        {
            PlatformResources.RegisterDefault<Func<Pipeline, string, uint, uint, PixelFormat, uint, uint, uint, bool, uint, uint, uint, IMpeg4Writer>>(
                (p, fileName, width, height, pixelFormat, frameRateNumerator, frameRateDenominator, targetBitrate, containsAudio, audioBitsPerSample, audioSamplesPerSecond, audioChannels)
                => new Mpeg4Writer(
                    p,
                    fileName,
                    width,
                    height,
                    pixelFormat,
                    frameRateNumerator,
                    frameRateDenominator,
                    targetBitrate,
                    containsAudio,
                    audioBitsPerSample,
                    audioSamplesPerSecond,
                    audioChannels));

            PlatformResources.Register<Func<Pipeline, string, uint, uint, PixelFormat, uint, uint, uint, bool, uint, uint, uint, IMpeg4Writer>>(
                nameof(Mpeg4Writer),
                (p, fileName, width, height, pixelFormat, frameRateNumerator, frameRateDenominator, targetBitrate, containsAudio, audioBitsPerSample, audioSamplesPerSecond, audioChannels)
                => new Mpeg4Writer(
                    p,
                    fileName,
                    width,
                    height,
                    pixelFormat,
                    frameRateNumerator,
                    frameRateDenominator,
                    targetBitrate,
                    containsAudio,
                    audioBitsPerSample,
                    audioSamplesPerSecond,
                    audioChannels));
        }
    }
}
