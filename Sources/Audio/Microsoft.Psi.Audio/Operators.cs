// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Stream operators and extension methods for Microsoft.Psi.Audio.
    /// </summary>
    /// <remarks>
    /// These are a collection of extension methods defining various audio and acoustic operators on <see cref="AudioBuffer"/>
    /// streams, streams of raw audio bytes, streams of floating-point audio samples as well as audio frequency-domain data streams.
    /// </remarks>
    public static class Operators
    {
        /// <summary>
        /// Reframes the bytes in an <see cref="AudioBuffer"/> stream, producing a new <see cref="AudioBuffer"/>
        /// stream where each new <see cref="AudioBuffer"/> has a specified fixed size.
        /// </summary>
        /// <param name="source">A stream containing the input audio.</param>
        /// <param name="frameSizeInBytes">The output frame size in bytes.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the reframed audio.</returns>
        public static IProducer<AudioBuffer> Reframe(this IProducer<AudioBuffer> source, int frameSizeInBytes, DeliveryPolicy<AudioBuffer> deliveryPolicy = null, string name = nameof(Reframe))
            => source.PipeTo(new Reframe(source.Out.Pipeline, frameSizeInBytes, name), deliveryPolicy);

        /// <summary>
        /// Reframes the bytes in an <see cref="AudioBuffer"/> stream, producing a new <see cref="AudioBuffer"/>
        /// stream where each new <see cref="AudioBuffer"/> has a specified fixed duration.
        /// </summary>
        /// <param name="source">A stream containing the input audio.</param>
        /// <param name="frameDuration">The output frame duration.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the reframed audio.</returns>
        public static IProducer<AudioBuffer> Reframe(this IProducer<AudioBuffer> source, TimeSpan frameDuration, DeliveryPolicy<AudioBuffer> deliveryPolicy = null, string name = nameof(Reframe))
            => source.PipeTo(new Reframe(source.Out.Pipeline, frameDuration, name), deliveryPolicy);

        /// <summary>
        /// Transforms an <see cref="AudioBuffer"/> stream to a stream of byte arrays containing the raw audio.
        /// </summary>
        /// <param name="source">A stream of audio buffers.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of byte arrays containing the raw audio.</returns>
        public static IProducer<byte[]> ToByteArray(this IProducer<AudioBuffer> source, DeliveryPolicy<AudioBuffer> deliveryPolicy = null, string name = nameof(ToByteArray))
            => source.Select(x => x.Data, deliveryPolicy, name);

        /// <summary>
        /// Transforms a stream of byte arrays containing raw audio to an <see cref="AudioBuffer"/> stream.
        /// </summary>
        /// <param name="source">A stream of raw audio byte arrays.</param>
        /// <param name="audioFormat">The audio format of the raw audio contained within the byte arrays.</param>
        /// /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of audio buffers.</returns>
        public static IProducer<AudioBuffer> ToAudioBuffer(this IProducer<byte[]> source, WaveFormat audioFormat, DeliveryPolicy<byte[]> deliveryPolicy = null, string name = nameof(ToAudioBuffer))
            => source.Select(x => new AudioBuffer(x, audioFormat), deliveryPolicy, name);

        /// <summary>
        /// The frame shift operator.
        /// </summary>
        /// <param name="source">A stream containing the input data.</param>
        /// <param name="frameSizeInBytes">The frame size in bytes.</param>
        /// <param name="frameShiftInBytes">The number of bytes by which to shift the data.</param>
        /// <param name="bytesPerSec">The sampling frequency in bytes per second.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the frame-shifted data.</returns>
        public static IProducer<byte[]> FrameShift(this IProducer<byte[]> source, int frameSizeInBytes, int frameShiftInBytes, double bytesPerSec, DeliveryPolicy<byte[]> deliveryPolicy = null, string name = nameof(FrameShift))
            => source.PipeTo(new FrameShift(source.Out.Pipeline, frameSizeInBytes, frameShiftInBytes, bytesPerSec, name), deliveryPolicy);

        /// <summary>
        /// Converts a stream of audio data to a stream of floating point values.
        /// </summary>
        /// <param name="source">A stream containing the input audio data.</param>
        /// <param name="format">The audio format of the input audio.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of floating point audio sample values.</returns>
        public static IProducer<float[]> ToFloat(this IProducer<byte[]> source, WaveFormat format, DeliveryPolicy<byte[]> deliveryPolicy = null, string name = nameof(ToFloat))
            => source.PipeTo(new ToFloat(source.Out.Pipeline, format, name), deliveryPolicy);

        /// <summary>
        /// Applies dithering to input sample values.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="scaleFactor">The scale factor of the dither.</param>
        /// <param name="randomSeed">An initial random seed value.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of floating point sample values with dithering.</returns>
        public static IProducer<float[]> Dither(this IProducer<float[]> source, float scaleFactor, int randomSeed = 0, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(Dither))
        {
            float[] dithered = null;
            var random = new Random(randomSeed);
            return source.Select(
                values =>
                {
                    if (dithered == null || dithered.Length != values.Length)
                    {
                        dithered = new float[values.Length];
                    }

                    for (int i = 0; i < values.Length; ++i)
                    {
                        dithered[i] = values[i] + (((((float)random.Next() / (float)int.MaxValue) * 2.0f) - 1.0f) * scaleFactor);
                    }

                    return dithered;
                },
                deliveryPolicy,
                name);
        }

        /// <summary>
        /// Applies a Hanning window to input sample values.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="kernelLength">The Hanning window length.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of floating point sample values with Hanning window applied.</returns>
        public static IProducer<float[]> HanningWindow(this IProducer<float[]> source, int kernelLength, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(HanningWindow))
            => source.Select<float[], float[]>(new HanningWindow(kernelLength).Apply, deliveryPolicy, name);

        /// <summary>
        /// Performs a Fast Fourier Transform on input sample buffers.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="fftSize">The FFT size.</param>
        /// <param name="inputSize">The window size.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of FFTs of the input sample buffers.</returns>
        public static IProducer<float[]> FFT(this IProducer<float[]> source, int fftSize, int inputSize, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(FFT))
            => source.PipeTo(new FFT(source.Out.Pipeline, fftSize, inputSize, name), deliveryPolicy);

        /// <summary>
        /// Converts a stream of FFTs to FFT power spectra.
        /// </summary>
        /// <param name="source">A stream of FFTs.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of FFT power spectra.</returns>
        public static IProducer<float[]> FFTPower(this IProducer<float[]> source, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(FFTPower))
            => source.PipeTo(new FFTPower(source.Out.Pipeline, name), deliveryPolicy);

        /// <summary>
        /// Computes the log energy of a stream of input samples in the time domain.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of log energy values for the input samples.</returns>
        public static IProducer<float> LogEnergy(this IProducer<float[]> source, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(LogEnergy))
            => source.PipeTo(new LogEnergy(source.Out.Pipeline, name), deliveryPolicy);

        /// <summary>
        /// Computes the zero-crossing rate of input samples.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of zero-crossing rates for the input samples.</returns>
        public static IProducer<float> ZeroCrossingRate(this IProducer<float[]> source, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(ZeroCrossingRate))
            => source.PipeTo(new ZeroCrossingRate(source.Out.Pipeline, name), deliveryPolicy);

        /// <summary>
        /// Computes the frequency domain energy from the FFT power spectra.
        /// </summary>
        /// <param name="source">A stream of FFT power spectra.</param>
        /// <param name="start">The index of the starting frequency of the band.</param>
        /// <param name="end">The index of the ending frequency of the band.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of frequency domain energy values.</returns>
        public static IProducer<float> FrequencyDomainEnergy(this IProducer<float[]> source, int start, int end, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(FrequencyDomainEnergy))
            => source.PipeTo(new FrequencyDomainEnergy(source.Out.Pipeline, start, end, name), deliveryPolicy);

        /// <summary>
        /// Computes the spectral entropy within a frequency band.
        /// </summary>
        /// <param name="source">A stream of FFT power spectra.</param>
        /// <param name="start">The starting frequency of the band.</param>
        /// <param name="end">The ending frequency of the band.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of spectral entropy values.</returns>
        public static IProducer<float> SpectralEntropy(this IProducer<float[]> source, int start, int end, DeliveryPolicy<float[]> deliveryPolicy = null, string name = nameof(SpectralEntropy))
            => source.PipeTo(new SpectralEntropy(source.Out.Pipeline, start, end, name), deliveryPolicy);
    }
}
