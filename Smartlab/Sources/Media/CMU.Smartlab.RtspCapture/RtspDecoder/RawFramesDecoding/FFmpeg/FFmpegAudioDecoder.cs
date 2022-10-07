// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Media;
    using RtspClientSharp.RawFrames.Audio;

    /// <summary>
    /// ..
    /// </summary>
    public class FFmpegAudioDecoder
    {
        private readonly IntPtr decoderHandle;
        private readonly FFmpegAudioCodecId audioCodecId;
        private IntPtr resamplerHandle;
        private AudioFrameFormat currentFrameFormat = new AudioFrameFormat(0, 0, 0);
        private DateTime currentRawFrameTimestamp;
        private byte[] extraData = new byte[0];
        private byte[] decodedFrameBuffer = new byte[0];
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegAudioDecoder"/> class.
        /// </summary>
        /// <param name="audioCodecId">.</param>
        /// <param name="bitsPerCodedSample">..</param>
        /// <param name="decoderHandle">...</param>
        private FFmpegAudioDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample, IntPtr decoderHandle)
        {
            this.audioCodecId = audioCodecId;
            this.BitsPerCodedSample = bitsPerCodedSample;
            this.decoderHandle = decoderHandle;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FFmpegAudioDecoder"/> class.
        /// </summary>
        ~FFmpegAudioDecoder()
        {
            this.Dispose();
        }

        /// <summary>
        /// Gets...
        /// </summary>
        public int BitsPerCodedSample { get; }

        /// <summary>
        /// ..
        /// </summary>
        /// <param name="audioCodecId">.</param>
        /// <param name="bitsPerCodedSample">....</param>
        /// <returns>...</returns>
        public static FFmpegAudioDecoder CreateDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample)
        {
            int resultCode = FFmpegAudioPInvoke.CreateAudioDecoder(audioCodecId, bitsPerCodedSample, out IntPtr decoderPtr);
            if (resultCode != 0)
            {
                throw new DecoderException($"An error occurred while creating audio decoder for {audioCodecId} codec, code: {resultCode}");
            }

            return new FFmpegAudioDecoder(audioCodecId, bitsPerCodedSample, decoderPtr);
        }

        /// <summary>
        /// <exception cref="DecoderException">.</exception>.
        /// </summary>
        /// <param name="rawAudioFrame">...</param>
        /// <returns>....</returns>
        public unsafe bool TryDecode(RawAudioFrame rawAudioFrame)
        {
            if (rawAudioFrame is RawAACFrame aacFrame)
            {
                Debug.Assert(aacFrame.ConfigSegment.Array != null, "aacFrame.ConfigSegment.Array != null");

                if (!this.extraData.SequenceEqual(aacFrame.ConfigSegment))
                {
                    if (this.extraData.Length == aacFrame.ConfigSegment.Count)
                    {
                        Buffer.BlockCopy(aacFrame.ConfigSegment.Array, aacFrame.ConfigSegment.Offset, this.extraData, 0, aacFrame.ConfigSegment.Count);
                    }
                    else
                    {
                        this.extraData = aacFrame.ConfigSegment.ToArray();
                    }

                    fixed (byte* extradataPtr = &this.extraData[0])
                    {
                        int resultCode = FFmpegAudioPInvoke.SetAudioDecoderExtraData(this.decoderHandle, (IntPtr)extradataPtr, aacFrame.ConfigSegment.Count);

                        if (resultCode != 0)
                        {
                            throw new DecoderException($"An error occurred while setting audio extra data, {this.audioCodecId} codec, code: {resultCode}");
                        }
                    }
                }
            }

            Debug.Assert(rawAudioFrame.FrameSegment.Array != null, "rawAudioFrame.FrameSegment.Array != null");

            fixed (byte* rawBufferPtr = &rawAudioFrame.FrameSegment.Array[rawAudioFrame.FrameSegment.Offset])
            {
                int resultCode = FFmpegAudioPInvoke.DecodeFrame(this.decoderHandle, (IntPtr)rawBufferPtr, rawAudioFrame.FrameSegment.Count, out int sampleRate, out int bitsPerSample, out int channels);

                this.currentRawFrameTimestamp = rawAudioFrame.Timestamp;

                if (resultCode != 0)
                {
                    return false;
                }

                if (rawAudioFrame is RawG711Frame g711Frame)
                {
                    sampleRate = g711Frame.SampleRate;
                    channels = g711Frame.Channels;
                }

                if (this.currentFrameFormat.SampleRate != sampleRate || this.currentFrameFormat.BitPerSample != bitsPerSample ||
                    this.currentFrameFormat.Channels != channels)
                {
                    this.currentFrameFormat = new AudioFrameFormat(sampleRate, bitsPerSample, channels);

                    if (this.resamplerHandle != IntPtr.Zero)
                    {
                        FFmpegAudioPInvoke.RemoveAudioResampler(this.resamplerHandle);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// <exception cref="DecoderException">..</exception>
        /// </summary>
        /// <param name="optionalAudioConversionParameters">....</param>
        /// <returns>.....</returns>
        public IDecodedAudioFrame GetDecodedFrame(AudioConversionParameters optionalAudioConversionParameters = null)
        {
            IntPtr outBufferPtr;
            int dataSize;

            AudioFrameFormat format;

            int resultCode;

            if (optionalAudioConversionParameters == null ||
                ((optionalAudioConversionParameters.OutSampleRate == 0 || optionalAudioConversionParameters.OutSampleRate == this.currentFrameFormat.SampleRate) &&
                (optionalAudioConversionParameters.OutBitsPerSample == 0 || optionalAudioConversionParameters.OutBitsPerSample == this.currentFrameFormat.BitPerSample) &&
                (optionalAudioConversionParameters.OutChannels == 0 || optionalAudioConversionParameters.OutChannels == this.currentFrameFormat.Channels)))
            {
                resultCode = FFmpegAudioPInvoke.GetDecodedFrame(this.decoderHandle, out outBufferPtr, out dataSize);

                if (resultCode != 0)
                {
                    throw new DecoderException($"An error occurred while getting decoded audio frame, {this.audioCodecId} codec, code: {resultCode}");
                }

                format = this.currentFrameFormat;
            }
            else
            {
                if (this.resamplerHandle == IntPtr.Zero)
                {
                    resultCode = FFmpegAudioPInvoke.CreateAudioResampler(this.decoderHandle, optionalAudioConversionParameters.OutSampleRate, optionalAudioConversionParameters.OutBitsPerSample, optionalAudioConversionParameters.OutChannels, out this.resamplerHandle);

                    if (resultCode != 0)
                    {
                        throw new DecoderException($"An error occurred while creating audio resampler, code: {resultCode}");
                    }
                }

                resultCode = FFmpegAudioPInvoke.ResampleDecodedFrame(this.decoderHandle, this.resamplerHandle, out outBufferPtr, out dataSize);

                if (resultCode != 0)
                {
                    throw new DecoderException($"An error occurred while converting audio frame, code: {resultCode}");
                }

                format = new AudioFrameFormat(
                    optionalAudioConversionParameters.OutSampleRate != 0 ? optionalAudioConversionParameters.OutSampleRate : this.currentFrameFormat.SampleRate,
                    optionalAudioConversionParameters.OutBitsPerSample != 0 ? optionalAudioConversionParameters.OutBitsPerSample : this.currentFrameFormat.BitPerSample,
                    optionalAudioConversionParameters.OutChannels != 0 ? optionalAudioConversionParameters.OutChannels : this.currentFrameFormat.Channels);
            }

            if (this.decodedFrameBuffer.Length < dataSize)
            {
                this.decodedFrameBuffer = new byte[dataSize];
            }

            Marshal.Copy(outBufferPtr, this.decodedFrameBuffer, 0, dataSize);
            return new DecodedAudioFrame(this.currentRawFrameTimestamp, new ArraySegment<byte>(this.decodedFrameBuffer, 0, dataSize), format);
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            FFmpegAudioPInvoke.RemoveAudioDecoder(this.decoderHandle);

            if (this.resamplerHandle != IntPtr.Zero)
            {
                FFmpegAudioPInvoke.RemoveAudioResampler(this.resamplerHandle);
            }

            GC.SuppressFinalize(this);
        }
    }
}
