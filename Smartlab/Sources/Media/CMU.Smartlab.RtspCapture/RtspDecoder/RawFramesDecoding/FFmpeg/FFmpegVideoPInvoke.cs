// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// ......
    /// </summary>
    public enum FFmpegVideoCodecId
    {
        /// <summary>
        /// ....
        /// </summary>
        MJPEG = 7,

        /// <summary>
        /// ....
        /// </summary>
        H264 = 27,
    }

    /// <summary>
    /// ......
    /// </summary>
    [Flags]
    public enum FFmpegScalingQuality
    {
        /// <summary>
        /// ...
        /// </summary>
        FastBilinear = 1,

        /// <summary>
        /// ...
        /// </summary>
        Bilinear = 2,

        /// <summary>
        /// ....
        /// </summary>
        Bicubic = 4,

        /// <summary>
        /// ...
        /// </summary>
        Point = 0x10,

        /// <summary>
        /// ...
        /// </summary>
        Area = 0x20,
    }

    /// <summary>
    /// ......
    /// </summary>
    public enum FFmpegPixelFormat
    {
        /// <summary>
        /// ...
        /// </summary>
        None = -1,

        /// <summary>
        /// ...
        /// </summary>
        BGR24 = 3,

        /// <summary>
        /// ...
        /// </summary>
        GRAY8 = 8,

        /// <summary>
        /// ...
        /// </summary>
        BGRA = 28,
    }

    /// <summary>
    /// Component that....
    /// </summary>
    public static class FFmpegVideoPInvoke
    {
        private const string LibraryName = "libffmpeghelper.dll";

        /// <summary>
        /// Component that...
        /// </summary>
        /// <param name="videoCodecId">..</param>
        /// <param name="handle">....</param>
        /// <returns>.......</returns>
        [DllImport(LibraryName, EntryPoint = "create_video_decoder", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateVideoDecoder(FFmpegVideoCodecId videoCodecId, out IntPtr handle);

        /// <summary>
        /// Component that...
        /// </summary>
        /// <param name="handle">...</param>
        [DllImport(LibraryName, EntryPoint = "remove_video_decoder", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveVideoDecoder(IntPtr handle);

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="extradata">..</param>
        /// <param name="extradataLength">...</param>
        /// <returns>....</returns>
        [DllImport(LibraryName, EntryPoint = "set_video_decoder_extradata", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetVideoDecoderExtraData(IntPtr handle, IntPtr extradata, int extradataLength);

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="rawBuffer">..</param>
        /// <param name="rawBufferLength">...</param>
        /// <param name="frameWidth">....</param>
        /// <param name="frameHeight">.....</param>
        /// <param name="framePixelFormat">......</param>
        /// <returns>.......</returns>
        [DllImport(LibraryName, EntryPoint = "decode_video_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DecodeFrame(IntPtr handle, IntPtr rawBuffer, int rawBufferLength, out int frameWidth, out int frameHeight, out FFmpegPixelFormat framePixelFormat);

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="scalerHandle">...</param>
        /// <param name="scaledBuffer">..</param>
        /// <param name="scaledBufferStride">....</param>
        /// <returns>.....</returns>
        [DllImport(LibraryName, EntryPoint = "scale_decoded_video_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ScaleDecodedVideoFrame(IntPtr handle, IntPtr scalerHandle, IntPtr scaledBuffer, int scaledBufferStride);

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <param name="sourceLeft">.</param>
        /// <param name="sourceTop">..</param>
        /// <param name="sourceWidth">...</param>
        /// <param name="sourceHeight">....</param>
        /// <param name="sourcePixelFormat">.....</param>
        /// <param name="scaledWidth">.......</param>
        /// <param name="scaledHeight">..........</param>
        /// <param name="scaledPixelFormat">............</param>
        /// <param name="qualityFlags">........................</param>
        /// <param name="handle">.........................</param>
        /// <returns>................................</returns>
        [DllImport(LibraryName, EntryPoint = "create_video_scaler", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateVideoScaler(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, FFmpegPixelFormat sourcePixelFormat, int scaledWidth, int scaledHeight, FFmpegPixelFormat scaledPixelFormat, FFmpegScalingQuality qualityFlags, out IntPtr handle);

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <param name="handle">.</param>
        [DllImport(LibraryName, EntryPoint = "remove_video_scaler", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveVideoScaler(IntPtr handle);
    }
}