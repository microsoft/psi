// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Structs, enums and static methods for interacting with Advanced Linux Sound Architecture (ALSA) drivers.
    /// </summary>
    /// <remarks>
    /// This implementation is based on this spec: http://www.alsa-project.org/alsa-doc/alsa-lib
    /// The only dependency is on `asound`, which comes with the system.
    /// </remarks>
    internal static class LinuxAudioInterop
    {
        /// <summary>
        /// Audio device mode.
        /// </summary>
        internal enum Mode
        {
            /// <summary>
            /// Audio device playback mode
            /// </summary>
            Playback = 0, // SND_PCM_STREAM_PLAYBACK

            /// <summary>
            /// Audio device capture mode
            /// </summary>
            Capture = 1, // SND_PCM_STREAM_CAPTURE
        }

        /// <summary>
        /// Audio device access.
        /// </summary>
        internal enum Access
        {
            /// <summary>
            /// Audio device interleaved access
            /// </summary>
            Interleaved = 3, // SND_PCM_ACCESS_RW_INTERLEAVED

            /// <summary>
            /// Audio device noninterleaved access
            /// </summary>
            NonInterleaved = 4, // SND_PCM_ACCESS_RW_NONINTERLEAVED
        }

        /// <summary>
        /// Audio device format.
        /// </summary>
        internal enum Format
        {
            /// <summary>
            /// SND_PCM_FORMAT_UNKNOWN
            /// </summary>
            Unknown = -1,

            /// <summary>
            /// SND_PCM_FORMAT_S8
            /// </summary>
            S8 = 0,

            /// <summary>
            /// SND_PCM_FORMAT_U8
            /// </summary>
            U8 = 1,

            /// <summary>
            /// SND_PCM_FORMAT_S16_LE
            /// </summary>
            S16LE = 2,

            /// <summary>
            /// SND_PCM_FORMAT_S16_BE
            /// </summary>
            S16BE = 3,

            /// <summary>
            /// SND_PCM_FORMAT_U16_LE
            /// </summary>
            U16LE = 4,

            /// <summary>
            /// SND_PCM_FORMAT_U16_BE
            /// </summary>
            U16BE = 5,

            /// <summary>
            /// SND_PCM_FORMAT_S24_LE
            /// </summary>
            S24LE = 6,

            /// <summary>
            /// SND_PCM_FORMAT_S24_BE
            /// </summary>
            S24BE = 7,

            /// <summary>
            /// SND_PCM_FORMAT_U24_LE
            /// </summary>
            U24LE = 8,

            /// <summary>
            /// SND_PCM_FORMAT_U24_BE
            /// </summary>
            U24BE = 9,

            /// <summary>
            /// SND_PCM_FORMAT_S32_LE
            /// </summary>
            S32LE = 10,

            /// <summary>
            /// SND_PCM_FORMAT_S32_BE
            /// </summary>
            S32BE = 11,

            /// <summary>
            /// SND_PCM_FORMAT_U32_LE
            /// </summary>
            U32LE = 12,

            /// <summary>
            /// SND_PCM_FORMAT_U32_BE
            /// </summary>
            U32BE = 13,

            /// <summary>
            /// SND_PCM_FORMAT_FLOAT_LE
            /// </summary>
            F32LE = 14,

            /// <summary>
            /// SND_PCM_FORMAT_FLOAT_BE
            /// </summary>
            F32BE = 15,

            /// <summary>
            /// SND_PCM_FORMAT_FLOAT64_LE
            /// </summary>
            F64LE = 16,

            /// <summary>
            /// SND_PCM_FORMAT_FLOAT64_BE
            /// </summary>
            F64BE = 17,

            /// <summary>
            /// SND_PCM_FORMAT_IEC958_SUBFRAME_LE
            /// </summary>
            IEC958SubframeLE = 18,

            /// <summary>
            /// SND_PCM_FORMAT_IEC958_SUBFRAME_BE
            /// </summary>
            IEC958SubframeBE = 19,

            /// <summary>
            /// SND_PCM_FORMAT_MU_LAW
            /// </summary>
            MULaw = 20,

            /// <summary>
            /// SND_PCM_FORMAT_A_LAW
            /// </summary>
            ALaw = 21,

            /// <summary>
            /// SND_PCM_FORMAT_IMA_ADPCM
            /// </summary>
            IMA_ADPCM = 22,

            /// <summary>
            /// SND_PCM_FORMAT_MPEG
            /// </summary>
            MPEG = 23,

            /// <summary>
            /// SND_PCM_FORMAT_GSM
            /// </summary>
            GSM = 24,

            /// <summary>
            /// SND_PCM_FORMAT_SPECIAL = 31
            /// </summary>
            Special = 31,

            /// <summary>
            /// SND_PCM_FORMAT_S24_3LE = 32
            /// </summary>
            S24_3LE = 32,

            /// <summary>
            /// SND_PCM_FORMAT_S24_3BE
            /// </summary>
            S24_3BE = 33,

            /// <summary>
            /// SND_PCM_FORMAT_U24_3LE
            /// </summary>
            U24_3LE = 34,

            /// <summary>
            /// SND_PCM_FORMAT_U24_3BE
            /// </summary>
            U24_3BE = 35,

            /// <summary>
            /// SND_PCM_FORMAT_S20_3LE
            /// </summary>
            S20_3LE = 36,

            /// <summary>
            /// SND_PCM_FORMAT_S20_3BE
            /// </summary>
            S20_3BE = 37,

            /// <summary>
            /// SND_PCM_FORMAT_U20_3LE
            /// </summary>
            U20_3LE = 38,

            /// <summary>
            /// SND_PCM_FORMAT_U20_3BE
            /// </summary>
            U20_3BE = 39,

            /// <summary>
            /// SND_PCM_FORMAT_S18_3LE
            /// </summary>
            S18_3LE = 40,

            /// <summary>
            /// SND_PCM_FORMAT_S18_3BE
            /// </summary>
            S18_3BE = 41,

            /// <summary>
            /// SND_PCM_FORMAT_U18_3LE
            /// </summary>
            U18_3LE = 42,

            /// <summary>
            /// SND_PCM_FORMAT_U18_3BE
            /// </summary>
            U18_3BE = 43,

            /// <summary>
            /// SND_PCM_FORMAT_G723_24
            /// </summary>
            G723_24 = 44,

            /// <summary>
            /// SND_PCM_FORMAT_G723_24_1B
            /// </summary>
            G723_24_1B = 45,

            /// <summary>
            /// SND_PCM_FORMAT_G723_40
            /// </summary>
            G723_40 = 46,

            /// <summary>
            /// SND_PCM_FORMAT_G723_40_1B
            /// </summary>
            G723_40_1B = 47,

            /// <summary>
            /// SND_PCM_FORMAT_DSD_U8
            /// </summary>
            DSD_U8 = 48,

            /// <summary>
            /// SND_PCM_FORMAT_DSD_U16_LE
            /// </summary>
            DSD_U16_LE = 49,

            /// <summary>
            /// SND_PCM_FORMAT_DSD_U32_LE
            /// </summary>
            DSD_U32_LE = 50,

            /// <summary>
            /// SND_PCM_FORMAT_DSD_U16_BE
            /// </summary>
            DSD_U16_BE = 51,

            /// <summary>
            /// SND_PCM_FORMAT_DSD_U32_BE
            /// </summary>
            DSD_U32_BE = 52,
        }

        /// <summary>
        /// Convert supported `AudioCaptureConfiguration` and `AudioPlayerConfiguration` to interop `Format`.
        /// </summary>
        /// <remarks>
        /// Only 8-, 16- and 32-bit PCM and 32- or 64-bit float are supported.
        /// </remarks>
        /// <param name="configFormat">Audio format.</param>
        /// <returns>Converted interop `Format`.</returns>
        internal static Format ConvertFormat(WaveFormat configFormat)
        {
            switch (configFormat.FormatTag)
            {
                case WaveFormatTag.WAVE_FORMAT_PCM:
                    switch (configFormat.BitsPerSample)
                    {
                        case 8:
                            return LinuxAudioInterop.Format.S8;
                        case 16:
                            return LinuxAudioInterop.Format.S16LE;
                        case 32:
                            return LinuxAudioInterop.Format.S32LE;
                        default:
                            throw new ArgumentException("Only 8, 16 and 32 bits per sample are supported for PCM.");
                    }

                case WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT:
                    switch (configFormat.BitsPerSample)
                    {
                        case 32:
                            return LinuxAudioInterop.Format.F32LE;
                        case 64:
                            return LinuxAudioInterop.Format.F64LE;
                        default:
                            throw new ArgumentException("Only 16 and 32 bits per sample are supported for IEEE float.");
                    }

                default:
                    throw new ArgumentException("Only PCM and IEEE Float wave formats supported.");
            }
        }

        /// <summary>
        /// Open audio device.
        /// </summary>
        /// <param name="name">Device name (e.g. "plughw:0,0").</param>
        /// <param name="mode">Device mode.</param>
        /// <param name="rate">Data block rate (Hz).</param>
        /// <param name="channels">Number of channels.</param>
        /// <param name="format">Data format.</param>
        /// <param name="access">Device access.</param>
        /// <returns>Device handle.</returns>
        internal static unsafe AudioDevice Open(string name, Mode mode, int rate = 44100, int channels = 1, Format format = Format.S16LE, Access access = Access.Interleaved)
        {
            void* handle;
            CheckResult(NativeMethods.Open(&handle, name, (int)mode, 0), "Open failed");

            void* param;
            CheckResult(NativeMethods.HardwareParamsMalloc(&param), "Hardware params malloc failed");
            CheckResult(NativeMethods.HardwareParamsAny(handle, param), "Hardware params any failed");
            CheckResult(NativeMethods.HardwareParamsSetAccess(handle, param, (int)access), "Hardware params set access failed");
            CheckResult(NativeMethods.HardwareParamsSetFormat(handle, param, (int)format), "Hardware params set format failed");

            int* ratePtr = &rate;
            int dir = 0;
            int* dirPtr = &dir;
            CheckResult(NativeMethods.HardwareParamsSetRate(handle, param, ratePtr, dirPtr), "Hardware params set rate failed");
            CheckResult(NativeMethods.HardwareParamsSetChannels(handle, param, (uint)channels), "Hardware params set channels failed");
            CheckResult(NativeMethods.HardwareParams(handle, param), "Hardware set params failed");

            NativeMethods.HardwareParamsFree(param);

            CheckResult(NativeMethods.PrepareHandle(handle), "Prepare handle failed");

            return new AudioDevice(handle);
        }

        /// <summary>
        /// Read block from device.
        /// </summary>
        /// <param name="device">Device handle.</param>
        /// <param name="buffer">Buffer into which to read.</param>
        /// <param name="blockSize">Block size.</param>
        internal static unsafe void Read(AudioDevice device, byte[] buffer, int blockSize)
        {
            fixed (void* bufferPtr = buffer)
            {
                long err;
                if (Environment.Is64BitOperatingSystem)
                {
                    err = NativeMethods.Read64(device.Handle, bufferPtr, (ulong)blockSize);
                }
                else
                {
                    err = NativeMethods.Read32(device.Handle, bufferPtr, (uint)blockSize);
                }

                if (err < 0)
                {
                    CheckResult(NativeMethods.Recover(device.Handle, (int)err, 1), "Read recovery failed");
                }
                else if (err != blockSize)
                {
                    throw new ArgumentException($"Read failed (ALSA error code: {err}).");
                }
            }
        }

        /// <summary>
        /// Write block to device.
        /// </summary>
        /// <param name="device">Device handle.</param>
        /// <param name="buffer">Buffer to be written.</param>
        /// <param name="blockSize">Block size.</param>
        /// <param name="offset">Offset into buffer from which to start writing data.</param>
        /// <returns>Number of frames (1 frame=1 sample from each channel) written.</returns>
        internal static unsafe int Write(AudioDevice device, byte[] buffer, int blockSize, int offset = 0)
        {
            long err = 0;
            fixed (void* bufferPtr = buffer)
            {
                byte* pb = (byte*)bufferPtr + offset;
                if (Environment.Is64BitOperatingSystem)
                {
                    err = NativeMethods.Write64(device.Handle, pb, (ulong)blockSize);
                }
                else
                {
                    err = NativeMethods.Write32(device.Handle, pb, (uint)blockSize);
                }

                if (err < 0)
                {
                    CheckResult(NativeMethods.Recover(device.Handle, (int)err, 1), "Write recovery failed");
                }
                else if (err != blockSize)
                {
                }
            }

            return (int)err;
        }

        /// <summary>
        /// Close device.
        /// </summary>
        /// <param name="device">Device handle.</param>
        internal static unsafe void Close(AudioDevice device)
        {
            if (NativeMethods.CloseHandle(device.Handle) != 0)
            {
                throw new ArgumentException("Close failed.");
            }
        }

        /// <summary>
        /// Check result code and throw argument exception upon failure from ALSA APIs.
        /// </summary>
        /// <param name="result">Result code returned by ALSA API.</param>
        /// <param name="message">Error message in case of failure.</param>
        private static void CheckResult(long result, string message)
        {
            if (result != 0)
            {
                throw new ArgumentException($"{message} (ALSA error code: {result}).");
            }
        }

        /// <summary>
        /// Audio device handle.
        /// </summary>
        /// <remarks>Useful to keep users from having to be `unsafe`.</remarks>
        internal class AudioDevice
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AudioDevice"/> class.
            /// </summary>
            /// <param name="handle">Device handle pointer.</param>
            public unsafe AudioDevice(void* handle)
            {
                this.Handle = handle;
            }

            /// <summary>
            /// Gets device handle pointer.
            /// </summary>
            public unsafe void* Handle { get; private set; }
        }

        private static class NativeMethods
        {
            [DllImport("asound", EntryPoint = "snd_pcm_open", BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static unsafe extern int Open(void** handle, [MarshalAs(UnmanagedType.LPStr)]string name, int capture, int mode);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_malloc")]
            internal static unsafe extern int HardwareParamsMalloc(void** param);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_any")]
            internal static unsafe extern int HardwareParamsAny(void* handle, void* param);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_set_access")]
            internal static unsafe extern int HardwareParamsSetAccess(void* handle, void* param, int access);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_set_format")]
            internal static unsafe extern int HardwareParamsSetFormat(void* handle, void* param, int format);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_set_rate_near")]
            internal static unsafe extern int HardwareParamsSetRate(void* handle, void* param, int* rate, int* dir);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_set_channels")]
            internal static unsafe extern int HardwareParamsSetChannels(void* handle, void* param, uint channels);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params")]
            internal static unsafe extern int HardwareParams(void* handle, void* param);

            [DllImport("asound", EntryPoint = "snd_pcm_hw_params_free")]
            internal static unsafe extern void HardwareParamsFree(void* param);

            [DllImport("asound", EntryPoint = "snd_pcm_prepare")]
            internal static unsafe extern int PrepareHandle(void* handle);

            [DllImport("asound", EntryPoint = "snd_pcm_recover")]
            internal static unsafe extern int Recover(void* handle, int error, int silent);

            [DllImport("asound", EntryPoint = "snd_pcm_readi")]
            internal static unsafe extern int Read32(void* handle, void* buffer, uint blockSize);

            [DllImport("asound", EntryPoint = "snd_pcm_readi")]
            internal static unsafe extern long Read64(void* handle, void* buffer, ulong blockSize);

            [DllImport("asound", EntryPoint = "snd_pcm_writei")]
            internal static unsafe extern int Write32(void* handle, void* buffer, uint blockSize);

            [DllImport("asound", EntryPoint = "snd_pcm_writei")]
            internal static unsafe extern long Write64(void* handle, void* buffer, ulong blockSize);

            [DllImport("asound", EntryPoint = "snd_pcm_close")]
            internal static unsafe extern int CloseHandle(void* handle);
        }
    }
}
