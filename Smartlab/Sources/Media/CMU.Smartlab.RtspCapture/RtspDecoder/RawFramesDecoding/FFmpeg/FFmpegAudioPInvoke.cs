// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// this component..
    /// </summary>
    public class FFmpegAudioPInvoke
    {
        private const string LibraryName = "libffmpeghelper.dll";

        /// <summary>
        /// ..
        /// </summary>
        /// <param name="audioCodecId">.</param>
        /// <param name="bitsPerCodedSample">...</param>
        /// <param name="handle">....</param>
        /// <returns>.....</returns>
        [DllImport(LibraryName, EntryPoint = "create_audio_decoder", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateAudioDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample, out IntPtr handle);

        /// <summary>
        /// ...................
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="extradata">..</param>
        /// <param name="extradataLength">....</param>
        /// <returns>.........</returns>
        [DllImport(LibraryName, EntryPoint = "set_audio_decoder_extradata", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetAudioDecoderExtraData(IntPtr handle, IntPtr extradata, int extradataLength);

        /// <summary>
        /// .......................
        /// </summary>
        /// <param name="handle">...</param>
        [DllImport(LibraryName, EntryPoint = "remove_audio_decoder", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveAudioDecoder(IntPtr handle);

        /// <summary>
        /// ..................
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="rawBuffer">..</param>
        /// <param name="rawBufferLength">...</param>
        /// <param name="sampleRate">....</param>
        /// <param name="bitsPerSample">.....</param>
        /// <param name="channels">........</param>
        /// <returns>............</returns>
        [DllImport(LibraryName, EntryPoint = "decode_audio_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DecodeFrame(IntPtr handle, IntPtr rawBuffer, int rawBufferLength, out int sampleRate, out int bitsPerSample, out int channels);

        /// <summary>
        /// .................
        /// </summary>
        /// <param name="handle">.</param>
        /// <param name="outBuffer">..</param>
        /// <param name="outDataSize">...</param>
        /// <returns>.....</returns>
        [DllImport(LibraryName, EntryPoint = "get_decoded_audio_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetDecodedFrame(IntPtr handle, out IntPtr outBuffer, out int outDataSize);

        /// <summary>
        /// ...................
        /// </summary>
        /// <param name="decoderHandle">.</param>
        /// <param name="outSampleRate">..</param>
        /// <param name="outBitsPerSample">...</param>
        /// <param name="outChannels">.....</param>
        /// <param name="handle">........</param>
        /// <returns>.........</returns>
        [DllImport(LibraryName, EntryPoint = "create_audio_resampler", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateAudioResampler(IntPtr decoderHandle, int outSampleRate, int outBitsPerSample, int outChannels, out IntPtr handle);

        /// <summary>
        /// .................
        /// </summary>
        /// <param name="decoderHandle">.</param>
        /// <param name="resamplerHandle">..</param>
        /// <param name="outBuffer">...</param>
        /// <param name="outDataSize">....</param>
        /// <returns>.....</returns>
        [DllImport(LibraryName, EntryPoint = "resample_decoded_audio_frame", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResampleDecodedFrame(IntPtr decoderHandle, IntPtr resamplerHandle, out IntPtr outBuffer, out int outDataSize);

        /// <summary>
        /// ..
        /// </summary>
        /// <param name="handle">.</param>
        [DllImport(LibraryName, EntryPoint = "remove_audio_resampler", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveAudioResampler(IntPtr handle);
    }
}
