// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// Provides a collection of audio device utility methods.
    /// </summary>
    internal static class DeviceUtil
    {
        private const int DeviceStateActive = 0x00000001;
        private const int StgmRead = 0x00000000;

        private static readonly PropertyKey PKeyDeviceFriendlyName = new PropertyKey()
        {
            FormatId = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
            PropertyId = 14,
        };

        /// <summary>
        /// Gets the friendly name of the specified device.
        /// </summary>
        /// <param name="device">The <see cref="IMMDevice"/> interface of the device.</param>
        /// <returns>The friendly name of the device.</returns>
        internal static string GetDeviceFriendlyName(IMMDevice device)
        {
            string deviceId = device.GetId();

            IPropertyStore propertyStore = device.OpenPropertyStore(StgmRead);

            PropVariant friendlyNameProperty = propertyStore.GetValue(PKeyDeviceFriendlyName);
            Marshal.ReleaseComObject(propertyStore);

            string friendlyName = (string)friendlyNameProperty.Value;
            friendlyNameProperty.Clear();

            return friendlyName;
        }

        /// <summary>
        /// Gets the default device for a given role and data flow.
        /// </summary>
        /// <param name="dataFlow">The data flow.</param>
        /// <param name="role">The device role.</param>
        /// <returns>The default device matching the data flow and role.</returns>
        internal static IMMDevice GetDefaultDevice(EDataFlow dataFlow, ERole role)
        {
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice device = deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, role);
            Marshal.ReleaseComObject(deviceEnumerator);

            return device;
        }

        /// <summary>
        /// Gets a specific device by its friendly name.
        /// </summary>
        /// <param name="dataFlow">The data flow.</param>
        /// <param name="deviceDescription">The device friendly name.</param>
        /// <returns>The default device matching the supplied name.</returns>
        internal static IMMDevice GetDeviceByName(EDataFlow dataFlow, string deviceDescription)
        {
            IMMDevice device = null;
            IMMDeviceCollection deviceCollection = GetAvailableDevices(dataFlow);
            int deviceCount = deviceCollection.GetCount();

            for (int i = 0; i < deviceCount; ++i)
            {
                IMMDevice dev = deviceCollection.Item(i);
                if (GetDeviceFriendlyName(dev) == deviceDescription)
                {
                    // found the named device so stop looking
                    device = dev;
                    break;
                }

                Marshal.ReleaseComObject(dev);
            }

            Marshal.ReleaseComObject(deviceCollection);

            return device;
        }

        /// <summary>
        /// Gets a collection of available devices.
        /// </summary>
        /// <param name="dataFlow">The data flow.</param>
        /// <returns>A collection of available devices matching the supplied role.</returns>
        internal static IMMDeviceCollection GetAvailableDevices(EDataFlow dataFlow)
        {
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDeviceCollection deviceCollection = deviceEnumerator.EnumAudioEndpoints(dataFlow, DeviceStateActive);
            Marshal.ReleaseComObject(deviceEnumerator);

            return deviceCollection;
        }

        /// <summary>
        /// Create Media Foundation transform that resamples audio in specified input format
        /// into specified output format.
        /// </summary>
        /// <param name="inputFormat">
        /// Wave format input to resampling operation.
        /// </param>
        /// <param name="outputFormat">
        /// Wave format output from resampling operation.
        /// </param>
        /// <returns>
        /// Media transform object that will resample audio.
        /// </returns>
        internal static IMFTransform CreateResampler(WaveFormat inputFormat, WaveFormat outputFormat)
        {
            IMFTransform resampler = null;
            IMFMediaType inputType = null;
            IMFMediaType outputType = null;

            try
            {
                resampler = (IMFTransform)new CResamplerMediaObject();
                inputType = CreateMediaType(inputFormat);
                resampler.SetInputType(0, inputType, 0);
                outputType = CreateMediaType(outputFormat);
                resampler.SetOutputType(0, outputType, 0);
            }
            finally
            {
                Marshal.ReleaseComObject(inputType);
                Marshal.ReleaseComObject(outputType);
            }

            return resampler;
        }

        /// <summary>
        /// Create a media buffer to be used as input or output for resampler.
        /// </summary>
        /// <param name="bufferSize">Size of buffer to create.</param>
        /// <param name="sample">Media Foundation sample created.</param>
        /// <param name="buffer">Media buffer created.</param>
        internal static void CreateResamplerBuffer(int bufferSize, out IMFSample sample, out IMFMediaBuffer buffer)
        {
            sample = NativeMethods.MFCreateSample();
            buffer = NativeMethods.MFCreateMemoryBuffer(bufferSize);
            sample.AddBuffer(buffer);
        }

        /// <summary>
        /// Gets the appropriate Media Foundation audio media subtype from the specified wave format.
        /// </summary>
        /// <param name="format">
        /// Input wave format to convert.
        /// </param>
        /// <returns>
        /// Media Foundation audio subtype resulting from conversion.
        /// </returns>
        internal static Guid GetMediaSubtype(WaveFormat format)
        {
            switch (format.FormatTag)
            {
                case WaveFormatTag.WAVE_FORMAT_PCM:
                case WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT:
                case WaveFormatTag.WAVE_FORMAT_DTS:
                case WaveFormatTag.WAVE_FORMAT_DOLBY_AC3_SPDIF:
                case WaveFormatTag.WAVE_FORMAT_DRM:
                case WaveFormatTag.WAVE_FORMAT_WMAUDIO2:
                case WaveFormatTag.WAVE_FORMAT_WMAUDIO3:
                case WaveFormatTag.WAVE_FORMAT_WMAUDIO_LOSSLESS:
                case WaveFormatTag.WAVE_FORMAT_WMASPDIF:
                case WaveFormatTag.WAVE_FORMAT_WMAVOICE9:
                case WaveFormatTag.WAVE_FORMAT_MPEGLAYER3:
                case WaveFormatTag.WAVE_FORMAT_MPEG:
                case WaveFormatTag.WAVE_FORMAT_MPEG_HEAAC:
                case WaveFormatTag.WAVE_FORMAT_MPEG_ADTS_AAC:
                    {
                        // These format tags map 1-to-1 to Media Foundation formats.
                        // The MSDN topic http://msdn.microsoft.com/en-us/library/aa372553(VS.85).aspx indicates that
                        // to create an audio subtype GUID one can:
                        // 1. Start with the value MFAudioFormat_Base
                        // 2. Replace the first DWORD of this GUID with the format tag
                        return new Guid((uint)format.FormatTag, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
                    }

                case WaveFormatTag.WAVE_FORMAT_EXTENSIBLE:
                    {
                        WaveFormatEx formatEx = (WaveFormatEx)format;

                        // We only support PCM and IEEE float subtypes for extensible wave formats
                        if (formatEx.SubFormat == Guids.KSDataFormatSubTypePCM)
                        {
                            return new Guid((uint)WaveFormatTag.WAVE_FORMAT_PCM, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
                        }
                        else if (formatEx.SubFormat == Guids.KSDataFormatSubTypeIeeeFloat)
                        {
                            return new Guid((uint)WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
                        }

                        break;
                    }
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Converts the specified wave format into the appropriate Media Foundation audio media type.
        /// </summary>
        /// <param name="format">Input wave format to convert.</param>
        /// <returns>Media Foundation type resulting from conversion.</returns>
        internal static IMFMediaType CreateMediaType(WaveFormat format)
        {
            IMFMediaType mediaType = null;

            Guid guidSubType = GetMediaSubtype(format);
            if (guidSubType != Guid.Empty)
            {
                // Create the empty media type.
                mediaType = NativeMethods.MFCreateMediaType();

                // Calculate derived values.
                uint blockAlign = (uint)(format.Channels * (format.BitsPerSample / 8));
                uint bytesPerSecond = (uint)(blockAlign * format.SamplesPerSec);

                // Set attributes on the type.
                mediaType.SetGUID(Guids.MFMTMajorType, Guids.MFMediaTypeAudio);
                mediaType.SetGUID(Guids.MFMTSubType, guidSubType);
                mediaType.SetUINT32(Guids.MFMTAudioNumChannels, format.Channels);
                mediaType.SetUINT32(Guids.MFMTAudioSamplesPerSecond, format.SamplesPerSec);
                mediaType.SetUINT32(Guids.MFMTAudioBlockAlignment, blockAlign);
                mediaType.SetUINT32(Guids.MFMTAudioAvgBytesPerSecond, bytesPerSecond);
                mediaType.SetUINT32(Guids.MFMTAudioBitsPerSample, format.BitsPerSample);
                mediaType.SetUINT32(Guids.MFMTAllSamplesIndependent, 1);
            }

            return mediaType;
        }
    }
}