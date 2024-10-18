// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The method used to streamline audio buffers.
    /// </summary>
    public enum AudioStreamlineMethod
    {
        /// <summary>
        /// Concatenates audio buffers.
        /// </summary>
        Concatenate,

        /// <summary>
        /// Pleats the audio buffers.
        /// </summary>
        /// <remarks>
        /// If two consecutive audio buffers have overlapping data, the overlapping portion of the first one is retained.
        /// If two consecutive audio buffers have a gap between them, the gap is filled with silence (zeros).
        /// </remarks>
        Pleat,

        /// <summary>
        /// Unpleats the audio buffers.
        /// </summary>
        /// <remarks>
        /// If consecutive audio buffers have overlapping data or gaps, they are concatenated up until the offset between
        /// the originating times of the buffers and the corresponding time of the concatenated audio frames is less than
        /// a specified threshold. Once the offset exceeds the threshold, a corresponding pleat or silence is introduced.
        /// </remarks>
        Unpleat,
    }

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
        /// Extracts the specified channel from the input audio stream.
        /// </summary>
        /// <param name="source">The input audio stream.</param>
        /// <param name="channel">The channel to select.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream containing the selected audio channel.</returns>
        public static IProducer<AudioBuffer> SelectChannel(this IProducer<AudioBuffer> source, int channel, DeliveryPolicy<AudioBuffer> deliveryPolicy = null, string name = nameof(SelectChannel))
            => source.Select(x => x.SelectChannel(channel), deliveryPolicy, name);

        /// <summary>
        /// Selects the specified channel from the input audio buffer.
        /// </summary>
        /// <param name="source">The input audio.</param>
        /// <param name="channel">The channel to select.</param>
        /// <returns>An audio buffer containing the selected channel.</returns>
        public static AudioBuffer SelectChannel(this AudioBuffer source, int channel)
        {
            if (source.HasValidData && source.Format.Channels > 1)
            {
                var format = source.Format;
                int bytesPerSample = format.BitsPerSample / 8;
                int numSamples = source.Length / format.BlockAlign;
                int byteOffset = 0 + ((channel % format.Channels) * bytesPerSample);
                int outOffset = 0;

                // Copy the selected channel to a new buffer
                byte[] outData = new byte[numSamples * bytesPerSample];
                for (int offset = byteOffset; outOffset < outData.Length; offset += format.BlockAlign)
                {
                    Array.Copy(source.Data, offset, outData, outOffset, bytesPerSample);
                    outOffset += bytesPerSample;
                }

                // Return a new AudioBuffer with the format adjusted to reflect only one channel
                return new AudioBuffer(
                    outData,
                    WaveFormat.Create(
                        format.FormatTag,
                        (int)format.SamplesPerSec,
                        format.BitsPerSample,
                        1,
                        bytesPerSample,
                        bytesPerSample * (int)format.SamplesPerSec));
            }
            else
            {
                return source;
            }
        }

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

        /// <summary>
        /// Streamlines an audio stream with a specified method.
        /// </summary>
        /// <param name="source">The source audio stream.</param>
        /// <param name="audioStreamlineMethod">The method used to streamline the audio stream.</param>
        /// <param name="maxOffsetBeforeForcedRealignmentMs">Max offset before force realignment is done when using the <see cref="AudioStreamlineMethod.Unpleat"/> method.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">The name of the streamline operator.</param>
        /// <returns>A streamlined audio stream.</returns>
        public static IProducer<AudioBuffer> Streamline(
            this IProducer<AudioBuffer> source,
            AudioStreamlineMethod audioStreamlineMethod,
            double maxOffsetBeforeForcedRealignmentMs = 0,
            DeliveryPolicy<AudioBuffer> deliveryPolicy = null,
            string name = nameof(Streamline))
        {
            if (audioStreamlineMethod == AudioStreamlineMethod.Concatenate)
            {
                var unpleatedAudioTime = DateTime.MinValue;
                return source
                    .Window(new IntInterval(-1, 0), x => x, deliveryPolicy)
                    .Process<IEnumerable<Message<AudioBuffer>>, AudioBuffer>(
                        (messages, envelope, emitter) =>
                        {
                            var last = messages.LastOrDefault();
                            var first = messages.FirstOrDefault();

                            var audioBuffer = default(AudioBuffer);
                            if (unpleatedAudioTime == DateTime.MinValue)
                            {
                                unpleatedAudioTime = first.OriginatingTime + last.Data.Duration;
                                audioBuffer = new AudioBuffer(first.Data.Duration + last.Data.Duration, last.Data.Format);
                                Array.Copy(first.Data.Data, 0, audioBuffer.Data, 0, first.Data.Length);
                                Array.Copy(last.Data.Data, 0, audioBuffer.Data, first.Data.Length, last.Data.Length);
                            }
                            else
                            {
                                unpleatedAudioTime += last.Data.Duration;
                                audioBuffer = last.Data;
                            }

                            emitter.Post(audioBuffer, unpleatedAudioTime);
                        },
                        deliveryPolicy: DeliveryPolicy.SynchronousOrThrottle,
                        name: name);
            }
            else if (audioStreamlineMethod == AudioStreamlineMethod.Pleat)
            {
                var firstPleat = true;
                return source.Window(
                    -1,
                    0,
                    messages =>
                    {
                        var last = messages.LastOrDefault();
                        var first = messages.FirstOrDefault();
                        var delta = (last.OriginatingTime - last.Data.Duration - first.OriginatingTime).TotalSeconds;
                        if (firstPleat)
                        {
                            firstPleat = false;
                            var newBuffer = new AudioBuffer(last.OriginatingTime - first.OriginatingTime + first.Data.Duration, last.Data.Format);
                            Array.Copy(first.Data.Data, 0, newBuffer.Data, 0, first.Data.Length);
                            if (delta > 0)
                            {
                                Array.Copy(last.Data.Data, 0, newBuffer.Data, newBuffer.Data.Length - last.Data.Length, last.Data.Length);
                            }
                            else
                            {
                                Array.Copy(last.Data.Data, first.Data.Length + last.Data.Length - newBuffer.Data.Length, newBuffer.Data, first.Data.Length, newBuffer.Data.Length - first.Data.Length);
                            }

                            return newBuffer;
                        }
                        else
                        {
                            if (delta > 0)
                            {
                                var newBuffer = new AudioBuffer(last.OriginatingTime - first.OriginatingTime, last.Data.Format);
                                Array.Copy(last.Data.Data, 0, newBuffer.Data, newBuffer.Data.Length - last.Data.Length, last.Data.Length);
                                return newBuffer;
                            }
                            else if (delta < 0)
                            {
                                var newBuffer = new AudioBuffer(last.OriginatingTime - first.OriginatingTime, last.Data.Format);
                                Array.Copy(last.Data.Data, last.Data.Data.Length - newBuffer.Data.Length, newBuffer.Data, 0, newBuffer.Data.Length);
                                return newBuffer;
                            }
                            else
                            {
                                return last.Data;
                            }
                        }
                    },
                    deliveryPolicy,
                    name);
            }
            else if (audioStreamlineMethod == AudioStreamlineMethod.Unpleat)
            {
                var unpleatedAudioTime = DateTime.MinValue;
                return source
                    .Window(new IntInterval(-1, 0), x => x, deliveryPolicy)
                    .Process<IEnumerable<Message<AudioBuffer>>, AudioBuffer>(
                        (messages, envelope, emitter) =>
                        {
                            var last = messages.LastOrDefault();
                            var first = messages.FirstOrDefault();

                            if (unpleatedAudioTime == DateTime.MinValue)
                            {
                                unpleatedAudioTime = first.OriginatingTime + last.Data.Duration;
                            }
                            else
                            {
                                unpleatedAudioTime += last.Data.Duration;
                            }

                            var offset = last.OriginatingTime - unpleatedAudioTime;

                            var audioBuffer = default(AudioBuffer);
                            if (offset > TimeSpan.FromMilliseconds(maxOffsetBeforeForcedRealignmentMs))
                            {
                                audioBuffer = new AudioBuffer(offset + last.Data.Duration, last.Data.Format);
                                Array.Copy(last.Data.Data, 0, audioBuffer.Data, audioBuffer.Data.Length - last.Data.Length, last.Data.Length);
                                unpleatedAudioTime = unpleatedAudioTime - last.Data.Duration + audioBuffer.Duration;
                            }
                            else if (offset < -TimeSpan.FromMilliseconds(maxOffsetBeforeForcedRealignmentMs))
                            {
                                if (offset + last.Data.Duration > TimeSpan.Zero)
                                {
                                    audioBuffer = new AudioBuffer(offset + last.Data.Duration, last.Data.Format);
                                    Array.Copy(last.Data.Data, 0, audioBuffer.Data, 0, audioBuffer.Data.Length);
                                    unpleatedAudioTime = unpleatedAudioTime - last.Data.Duration + audioBuffer.Duration;
                                }
                            }
                            else
                            {
                                audioBuffer = last.Data;
                            }

                            if (audioBuffer.Length > 0)
                            {
                                emitter.Post(audioBuffer, unpleatedAudioTime);
                            }
                        },
                        deliveryPolicy: DeliveryPolicy.SynchronousOrThrottle,
                        name: name);
            }
            else
            {
                throw new ArgumentException("Invalid audio streamline method.");
            }
        }
    }
}
