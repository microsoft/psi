// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;

    /// <summary>
    /// Class that contains IID and CLSID strings and GUIDs.
    /// </summary>
    internal static class Guids
    {
        /// <summary>
        /// IMMDevice IID string.
        /// </summary>
        public const string IMMDeviceIIDString = "D666063F-1587-4E43-81F1-B948E807363F";

        /// <summary>
        /// IMMDeviceEnumerator IID string.
        /// </summary>
        internal const string IMMDeviceEnumeratorIIDString = "A95664D2-9614-4F35-A746-DE8DB63617E6";

        /// <summary>
        /// IMMDeviceCollection IID string.
        /// </summary>
        internal const string IMMDeviceCollectionIIDString = "0BD7A1BE-7A1A-44DB-8397-CC5392387B5E";

        /// <summary>
        /// IMMNotificationClient IID string.
        /// </summary>
        internal const string IMMNotificationClientIIDString = "7991EEC9-7E89-4D85-8390-6C703CEC60C0";

        /// <summary>
        /// IAudioClient IID string.
        /// </summary>
        internal const string IAudioClientIIDString = "1CB9AD4C-DBFA-4C32-B178-C2F568A703B2";

        /// <summary>
        /// IAudioClient2 IID string.
        /// </summary>
        internal const string IAudioClient2IIDString = "726778CD-F60A-4EDA-82DE-E47610CD78AA";

        /// <summary>
        /// IAudioCaptureClient IID string.
        /// </summary>
        internal const string IAudioCaptureClientIIDString = "C8ADBD64-E71E-48A0-A4DE-185C395CD317";

        /// <summary>
        /// IAudioRenderClient IID string.
        /// </summary>
        internal const string IAudioRenderClientIIDString = "F294ACFC-3146-4483-A7BF-ADDCA7C260E2";

        /// <summary>
        /// IAudioEndpointVolume IID string.
        /// </summary>
        internal const string IAudioEndpointVolumeIIDString = "5CDF2C82-841E-4546-9722-0CF74078229A";

        /// <summary>
        /// IAudioEndpointVolumeCallback IID string.
        /// </summary>
        internal const string IAudioEndpointVolumeCallbackIIDString = "657804FA-D6AD-4496-8A60-352752AF4F89";

        /// <summary>
        /// IPropertyStore IID string.
        /// </summary>
        internal const string IPropertyStoreIIDString = "886d8eeb-8cf2-4446-8d02-cdba1dbdcf99";

        /// <summary>
        /// IMFTransform IID string.
        /// </summary>
        internal const string IMFTransformIIDString = "bf94c121-5b05-4e6f-8000-ba598961414d";

        /// <summary>
        /// IMFAttributes IID string.
        /// </summary>
        internal const string IMFAttributesIIDString = "2cd2d921-c447-44a7-a13c-4adabfc247e3";

        /// <summary>
        /// IMFMediaType IID string.
        /// </summary>
        internal const string IMFMediaTypeIIDString = "44ae0fa8-ea31-4109-8d2e-4cae4997c555";

        /// <summary>
        /// IMFMediaEvent IID string.
        /// </summary>
        internal const string IMFMediaEventIIDString = "DF598932-F10C-4E39-BBA2-C308F101DAA3";

        /// <summary>
        /// IMFSample IID string.
        /// </summary>
        internal const string IMFSampleIIDString = "c40a00f2-b93a-4d80-ae8c-5a1c634f58e4";

        /// <summary>
        /// IMFMediaBuffer IID string.
        /// </summary>
        internal const string IMFMediaBufferIIDString = "045FA593-8799-42b8-BC8D-8968C6453507";

        /// <summary>
        /// IMFCollection IID string.
        /// </summary>
        internal const string IMFCollectionIIDString = "5BC8A76B-869A-46a3-9B03-FA218A66AEBE";

        /// <summary>
        /// MMDeviceEnumerator CLSID string.
        /// </summary>
        internal const string MMDeviceEnumeratorCLSIDString = "BCDE0395-E52F-467C-8E3D-C4579291692E";

        /// <summary>
        /// CResamplerMediaObject CLSID string.
        /// </summary>
        internal const string CResamplerMediaObjectCLSIDString = "f447b69e-1884-4a7e-8055-346f74d6edb3";

        /// <summary>
        /// MFMediaType_Audio (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMediaTypeAudio = new Guid(0x73647561, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

        /// <summary>
        /// MF_MT_MAJOR_TYPE {48eba18e-f8c9-4687-bf11-0a74c9f96a8f} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTMajorType = new Guid(0x48eba18e, 0xf8c9, 0x4687, 0xbf, 0x11, 0x0a, 0x74, 0xc9, 0xf9, 0x6a, 0x8f);

        /// <summary>
        /// MF_MT_SUBTYPE {f7e34c9a-42e8-4714-b74b-cb29d72c35e5} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTSubType = new Guid(0xf7e34c9a, 0x42e8, 0x4714, 0xb7, 0x4b, 0xcb, 0x29, 0xd7, 0x2c, 0x35, 0xe5);

        /// <summary>
        /// MF_MT_AUDIO_NUM_CHANNELS {37e48bf5-645e-4c5b-89de-ada9e29b696a} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAudioNumChannels = new Guid(0x37e48bf5, 0x645e, 0x4c5b, 0x89, 0xde, 0xad, 0xa9, 0xe2, 0x9b, 0x69, 0x6a);

        /// <summary>
        /// MF_MT_AUDIO_SAMPLES_PER_SECOND {5faeeae7-0290-4c31-9e8a-c534f68d9dba} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAudioSamplesPerSecond = new Guid(0x5faeeae7, 0x0290, 0x4c31, 0x9e, 0x8a, 0xc5, 0x34, 0xf6, 0x8d, 0x9d, 0xba);

        /// <summary>
        /// MF_MT_AUDIO_BLOCK_ALIGNMENT {322de230-9eeb-43bd-ab7a-ff412251541d} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAudioBlockAlignment = new Guid(0x322de230, 0x9eeb, 0x43bd, 0xab, 0x7a, 0xff, 0x41, 0x22, 0x51, 0x54, 0x1d);

        /// <summary>
        /// MF_MT_AUDIO_AVG_BYTES_PER_SECOND {1aab75c8-cfef-451c-ab95-ac034b8e1731} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAudioAvgBytesPerSecond = new Guid(0x1aab75c8, 0xcfef, 0x451c, 0xab, 0x95, 0xac, 0x03, 0x4b, 0x8e, 0x17, 0x31);

        /// <summary>
        /// MF_MT_AUDIO_BITS_PER_SAMPLE {f2deb57f-40fa-4764-aa33-ed4f2d1ff669} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAudioBitsPerSample = new Guid(0xf2deb57f, 0x40fa, 0x4764, 0xaa, 0x33, 0xed, 0x4f, 0x2d, 0x1f, 0xf6, 0x69);

        /// <summary>
        /// MF_MT_ALL_SAMPLES_INDEPENDENT {c9173739-5e56-461c-b713-46fb995cb95f} (defined in mfapi.h).
        /// </summary>
        internal static readonly Guid MFMTAllSamplesIndependent = new Guid(0xc9173739, 0x5e56, 0x461c, 0xb7, 0x13, 0x46, 0xfb, 0x99, 0x5c, 0xb9, 0x5f);

        /// <summary>
        /// KSDATAFORMAT_SUBTYPE_PCM {"00000001-0000-0010-8000-00aa00389b71"} (defined in mmreg.h).
        /// </summary>
        internal static readonly Guid KSDataFormatSubTypePCM = new Guid("00000001-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// KSDATAFORMAT_SUBTYPE_IEEE_FLOAT {"00000003-0000-0010-8000-00aa00389b71"} (defined in mmreg.h).
        /// </summary>
        internal static readonly Guid KSDataFormatSubTypeIeeeFloat = new Guid("00000003-0000-0010-8000-00aa00389b71");
    }
}
