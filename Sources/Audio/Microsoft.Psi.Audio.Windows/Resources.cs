// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

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
            PlatformResources.RegisterDefault<Func<Pipeline, IAudioResampler>>(p => new AudioResampler(p));
            PlatformResources.RegisterDefault<Func<Pipeline, WaveFormat, IAudioResampler>>((p, outFormat) => new AudioResampler(p, new AudioResamplerConfiguration { OutputFormat = outFormat }));
            PlatformResources.Register<Func<Pipeline, IAudioResampler>>(nameof(AudioResampler), p => new AudioResampler(p));
        }
    }
}
