// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Stream operators and extension methods for Microsoft.Psi.Audio.Windows.
    /// </summary>
    /// <remarks>
    /// These are a collection of extension methods defining various audio operators on <see cref="AudioBuffer"/> streams.
    /// </remarks>
    public static class Operators
    {
        /// <summary>
        /// Resamples an audio stream.
        /// </summary>
        /// <param name="source">A stream of audio to be resampled.</param>
        /// <param name="configuration">The resampler configuration.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of resampled audio.</returns>
        public static IProducer<AudioBuffer> Resample(
            this IProducer<AudioBuffer> source,
            AudioResamplerConfiguration configuration,
            DeliveryPolicy<AudioBuffer> deliveryPolicy = null,
            string name = nameof(Resample))
            => source.PipeTo(new AudioResampler(source.Out.Pipeline, configuration, name), deliveryPolicy);

        /// <summary>
        /// Resamples an audio stream.
        /// </summary>
        /// <param name="source">A stream audio to be resampled.</param>
        /// <param name="outputFormat">The desired audio output format for the resampled stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of resampled audio.</returns>
        public static IProducer<AudioBuffer> Resample(
            this IProducer<AudioBuffer> source,
            WaveFormat outputFormat,
            DeliveryPolicy<AudioBuffer> deliveryPolicy = null,
            string name = nameof(Resample))
            => Resample(source, new AudioResamplerConfiguration() { OutputFormat = outputFormat }, deliveryPolicy, name);
    }
}
