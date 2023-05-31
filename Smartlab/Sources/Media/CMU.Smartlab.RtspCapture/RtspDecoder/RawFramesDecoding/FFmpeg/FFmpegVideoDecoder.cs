// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Psi.Media;
    using RtspClientSharp.RawFrames.Video;

    /// <summary>
    /// this component..
    /// </summary>
    public class FFmpegVideoDecoder
    {
        private readonly Dictionary<TransformParameters, FFmpegDecodedVideoScaler> scalersMap =
    new Dictionary<TransformParameters, FFmpegDecodedVideoScaler>();

        private readonly IntPtr decoderHandle;
        private readonly FFmpegVideoCodecId videoCodecId;

        private DecodedVideoFrameParameters currentFrameParameters =
            new DecodedVideoFrameParameters(0, 0, FFmpegPixelFormat.None);

        private byte[] extraData = new byte[0];
        private bool disposed;

        private FFmpegVideoDecoder(FFmpegVideoCodecId videoCodecId, IntPtr decoderHandle)
        {
            this.videoCodecId = videoCodecId;
            this.decoderHandle = decoderHandle;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FFmpegVideoDecoder"/> class.
        /// </summary>
        ~FFmpegVideoDecoder()
        {
            this.Dispose();
        }

        /// <summary>
        /// this component..
        /// </summary>
        /// <param name="videoCodecId">.</param>
        /// <returns>..</returns>
        public static FFmpegVideoDecoder CreateDecoder(FFmpegVideoCodecId videoCodecId)
        {
            int resultCode = FFmpegVideoPInvoke.CreateVideoDecoder(videoCodecId, out IntPtr decoderPtr);

            if (resultCode != 0)
            {
                throw new DecoderException($"An error occurred while creating video decoder for {videoCodecId} codec, code: {resultCode}");
            }

            return new FFmpegVideoDecoder(videoCodecId, decoderPtr);
        }

        /// <summary>
        /// this compnent..
        /// </summary>
        /// <param name="rawVideoFrame">..</param>
        /// <param name="parameters">Raw parameters which stores the width, height and pixel format.</param>
        /// <returns>...</returns>
        public unsafe IDecodedVideoFrame TryDecode(RawVideoFrame rawVideoFrame, out DecodedVideoFrameParameters parameters)
        {
            fixed (byte* rawBufferPtr = &rawVideoFrame.FrameSegment.Array[rawVideoFrame.FrameSegment.Offset])
            {
                int resultCode;

                if (rawVideoFrame is RawH264IFrame rawH264IFrame)
                {
                    if (rawH264IFrame.SpsPpsSegment.Array != null &&
                        !this.extraData.SequenceEqual(rawH264IFrame.SpsPpsSegment))
                    {
                        if (this.extraData.Length != rawH264IFrame.SpsPpsSegment.Count)
                        {
                            this.extraData = new byte[rawH264IFrame.SpsPpsSegment.Count];
                        }

                        Buffer.BlockCopy(rawH264IFrame.SpsPpsSegment.Array, rawH264IFrame.SpsPpsSegment.Offset, this.extraData, 0, rawH264IFrame.SpsPpsSegment.Count);

                        fixed (byte* initDataPtr = &this.extraData[0])
                        {
                            resultCode = FFmpegVideoPInvoke.SetVideoDecoderExtraData(
                                this.decoderHandle, (IntPtr)initDataPtr, this.extraData.Length);

                            if (resultCode != 0)
                            {
                                throw new DecoderException(
                                    $"An error occurred while setting video extra data, {this.videoCodecId} codec, code: {resultCode}");
                            }
                        }
                    }
                }

                resultCode = FFmpegVideoPInvoke.DecodeFrame(this.decoderHandle, (IntPtr)rawBufferPtr, rawVideoFrame.FrameSegment.Count, out int width, out int height, out FFmpegPixelFormat pixelFormat);

                if (resultCode != 0)
                {
                    parameters = null;
                    return null;
                }

                if (this.currentFrameParameters.Width != width || this.currentFrameParameters.Height != height ||
                    this.currentFrameParameters.PixelFormat != pixelFormat)
                {
                    this.currentFrameParameters = new DecodedVideoFrameParameters(width, height, pixelFormat);
                    this.DropAllVideoScalers();
                }

                parameters = this.currentFrameParameters;
                return new DecodedVideoFrame(this.TransformTo);
            }
        }

        /// <summary>
        /// this component.
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            FFmpegVideoPInvoke.RemoveVideoDecoder(this.decoderHandle);
            this.DropAllVideoScalers();
            GC.SuppressFinalize(this);
        }

        private void DropAllVideoScalers()
        {
            foreach (var scaler in this.scalersMap.Values)
            {
                scaler.Dispose();
            }

            this.scalersMap.Clear();
        }

        private void TransformTo(IntPtr buffer, int bufferStride, TransformParameters parameters)
        {
            if (!this.scalersMap.TryGetValue(parameters, out FFmpegDecodedVideoScaler videoScaler))
            {
                videoScaler = FFmpegDecodedVideoScaler.Create(this.currentFrameParameters, parameters);
                this.scalersMap.Add(parameters, videoScaler);
            }

            int resultCode = FFmpegVideoPInvoke.ScaleDecodedVideoFrame(this.decoderHandle, videoScaler.Handle, buffer, bufferStride);

            if (resultCode != 0)
            {
                throw new DecoderException($"An error occurred while converting decoding video frame, {this.videoCodecId} codec, code: {resultCode}");
            }
        }
    }
}