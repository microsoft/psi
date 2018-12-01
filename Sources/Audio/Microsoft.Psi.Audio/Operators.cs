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
        /// Transforms an <see cref="AudioBuffer"/> stream to a stream of byte arrays containing the raw audio.
        /// </summary>
        /// <param name="source">A stream of audio buffers.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of byte arrays containing the raw audio.</returns>
        public static IProducer<byte[]> ToByteArray(this IProducer<AudioBuffer> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(x => x.Data, deliveryPolicy);
        }

        /// <summary>
        /// Transforms a stream of byte arrays containing raw audio to an <see cref="AudioBuffer"/> stream.
        /// </summary>
        /// <param name="source">A stream of raw audio byte arrays.</param>
        /// <param name="audioFormat">The audio format of the raw audio contained within the byte arrays.</param>
        /// /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of audio buffers.</returns>
        public static IProducer<AudioBuffer> ToAudioBuffer(this IProducer<byte[]> source, WaveFormat audioFormat, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select(x => new AudioBuffer(x, audioFormat), deliveryPolicy);
        }

        /// <summary>
        /// The frame shift operator.
        /// </summary>
        /// <param name="source">A stream containing the input data.</param>
        /// <param name="frameSizeInBytes">The frame size in bytes.</param>
        /// <param name="frameShiftInBytes">The number of bytes by which to shift the data.</param>
        /// <param name="bytesPerSec">The sampling frequency in bytes per second.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream containing the frame-shifted data.</returns>
        public static IProducer<byte[]> FrameShift(this IProducer<byte[]> source, int frameSizeInBytes, int frameShiftInBytes, double bytesPerSec, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new FrameShift(source.Out.Pipeline, frameSizeInBytes, frameShiftInBytes, bytesPerSec), deliveryPolicy);
        }

        /// <summary>
        /// Converts a stream of audio data to a stream of floating point values.
        /// </summary>
        /// <param name="source">A stream containing the input audio data.</param>
        /// <param name="format">The audio format of the input audio.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of floating point audio sample values.</returns>
        public static IProducer<float[]> ToFloat(this IProducer<byte[]> source, WaveFormat format, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new ToFloat(source.Out.Pipeline, format), deliveryPolicy);
        }

        /// <summary>
        /// Applies dithering to input sample values.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="scaleFactor">The scale factor of the dither.</param>
        /// <param name="randomSeed">An initial random seed value.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of floating point sample values with dithering.</returns>
        public static IProducer<float[]> Dither(this IProducer<float[]> source, float scaleFactor, int randomSeed = 0, DeliveryPolicy deliveryPolicy = null)
        {
            float[] dithered = null;
            Random random = new Random(randomSeed);
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
                deliveryPolicy);
        }

        /// <summary>
        /// Applies a Hanning window to input sample values.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="kernelLength">The Hanning window length.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of floating point sample values with Hanning window applied.</returns>
        public static IProducer<float[]> HanningWindow(this IProducer<float[]> source, int kernelLength, DeliveryPolicy deliveryPolicy = null)
        {
            return source.Select<float[], float[]>(new HanningWindow(kernelLength).Apply, deliveryPolicy);
        }

        /// <summary>
        /// Performs a Fast Fourier Transform on input sample buffers.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="fftSize">The FFT size.</param>
        /// <param name="inputSize">The window size.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of FFTs of the input sample buffers.</returns>
        public static IProducer<float[]> FFT(this IProducer<float[]> source, int fftSize, int inputSize, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new FFT(source.Out.Pipeline, fftSize, inputSize), deliveryPolicy);
        }

        /// <summary>
        /// Converts a stream of FFTs to FFT power spectra.
        /// </summary>
        /// <param name="source">A stream of FFTs.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of FFT power spectra.</returns>
        public static IProducer<float[]> FFTPower(this IProducer<float[]> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new FFTPower(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Computes the log energy of a stream of input samples in the time domain.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of log energy values for the input samples.</returns>
        public static IProducer<float> LogEnergy(this IProducer<float[]> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new LogEnergy(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Computes the zero-crossing rate of input samples.
        /// </summary>
        /// <param name="source">A stream of floating point input sample values.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of zero-crossing rates for the input samples.</returns>
        public static IProducer<float> ZeroCrossingRate(this IProducer<float[]> source, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new ZeroCrossingRate(source.Out.Pipeline), deliveryPolicy);
        }

        /// <summary>
        /// Computes the frequency domain energy from the FFT power spectra.
        /// </summary>
        /// <param name="source">A stream of FFT power spectra.</param>
        /// <param name="start">The index of the starting frequency of the band.</param>
        /// <param name="end">The index of the ending frequency of the band.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of frequency domain energy values.</returns>
        public static IProducer<float> FrequencyDomainEnergy(this IProducer<float[]> source, int start, int end, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new FrequencyDomainEnergy(source.Out.Pipeline, start, end), deliveryPolicy);
        }

        /// <summary>
        /// Computes the spectral entropy within a frequency band.
        /// </summary>
        /// <param name="source">A stream of FFT power spectra.</param>
        /// <param name="start">The starting frequency of the band.</param>
        /// <param name="end">The ending frequency of the band.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of spectral entropy values.</returns>
        public static IProducer<float> SpectralEntropy(this IProducer<float[]> source, int start, int end, DeliveryPolicy deliveryPolicy = null)
        {
            return source.PipeTo(new SpectralEntropy(source.Out.Pipeline, start, end), deliveryPolicy);
        }
    }
}
