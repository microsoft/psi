// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IAudioClient2 COM interface (defined in Audioclient.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IAudioClient2IIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient2
    {
        #region IAudioClient methods

        /// <summary>
        /// Initializes the audio stream.
        /// </summary>
        /// <param name="shareMode">The sharing mode for the connection.</param>
        /// <param name="streamFlags">Flags to control creation of the stream.</param>
        /// <param name="hnsBufferDuration">The buffer capacity as a time value.</param>
        /// <param name="hnsPeriodicity">The device period.</param>
        /// <param name="format">Pointer to a format descriptor.</param>
        /// <param name="audioSessionGuid">A session GUID.</param>
        /// <returns>An HRESULT return code.</returns>
        int Initialize(AudioClientShareMode shareMode, AudioClientStreamFlags streamFlags, long hnsBufferDuration, long hnsPeriodicity, [In] IntPtr format, [In] ref Guid audioSessionGuid);

        /// <summary>
        /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint.
        /// </summary>
        /// <returns>The buffer size.</returns>
        int GetBufferSize();

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has
        /// been initialized.
        /// </summary>
        /// <returns>A time value representing the latency.</returns>
        long GetStreamLatency();

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer.
        /// </summary>
        /// <returns>The current padding.</returns>
        int GetCurrentPadding();

        /// <summary>
        /// Indicates whether the audio endpoint device supports a particular stream format.
        /// </summary>
        /// <param name="shareMode">The sharing mode for the stream format.</param>
        /// <param name="format">Pointer to the specified stream format.</param>
        /// <param name="closestMatchFormat">
        /// A pointer variable into which the method writes the address of a WAVEFORMATEX or WAVEFORMATEXTENSIBLE
        /// structure. This structure specifies the supported format that is closest to the format that the client
        /// specified through the pFormat parameter.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        [PreserveSig]
        int IsFormatSupported(AudioClientShareMode shareMode, [In] IntPtr format, out IntPtr closestMatchFormat);

        /// <summary>
        /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// </summary>
        /// <returns>The address of the mix format structure.</returns>
        IntPtr GetMixFormat();

        /// <summary>
        /// Retrieves the length of the periodic interval separating successive processing passes by the audio engine
        /// on the data in the endpoint buffer.
        /// </summary>
        /// <param name="defaultDevicePeriod">
        /// A variable into which the method writes a time value specifying the default interval between periodic
        /// processing passes by the audio engine.
        /// </param>
        /// <param name="minimumDevicePeriod">
        /// A variable into which the method writes a time value specifying the minimum interval between periodic
        /// processing passes by the audio endpoint device.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        /// <summary>
        /// Starts the audio stream.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int Start();

        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int Stop();

        /// <summary>
        /// Resets the audio stream.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int Reset();

        /// <summary>
        /// Sets the event handle that the audio engine will signal each time a buffer becomes ready to be processed
        /// by the client.
        /// </summary>
        /// <param name="eventHandle">The event handle.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetEventHandle(IntPtr eventHandle);

        /// <summary>
        /// Accesses additional services from the audio client object.
        /// </summary>
        /// <param name="interfaceId">The interface ID for the requested service.</param>
        /// <returns>An instance of the requested interface.</returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetService(ref Guid interfaceId);

        #endregion

        /// <summary>
        /// Retrieves information about whether or not the endpoint on which a stream is created is capable of
        /// supporting an offloaded stream.
        /// </summary>
        /// <param name="category">An enumeration that specifies the category of an audio stream.</param>
        /// <returns>A boolean value indicating whether the endpoint is offload-capable.</returns>
        bool IsOffloadCapable(AudioStreamCategory category);

        /// <summary>
        /// Sets the properties of the audio stream by populating an AudioClientProperties structure.
        /// </summary>
        /// <param name="properties">An AudioClientProperties structure.</param>
        /// <returns>An HRESULT return code.</returns>
        [PreserveSig]
        int SetClientProperties(ref AudioClientProperties properties);

        /// <summary>
        /// Retrieves the buffer size limits of the hardware audio engine in 100-nanosecond units.
        /// </summary>
        /// <param name="format">
        /// A pointer to the target format that is being queried for the buffer size limit.
        /// </param>
        /// <param name="eventDriven">
        /// Boolean value to indicate whether or not the stream can be event-driven.
        /// </param>
        /// <param name="minBufferDuration">
        /// The minimum buffer size (in 100-nanosecond units) that is required for the underlying hardware audio
        /// engine to operate at the format specified in the format parameter, without frequent audio glitching.
        /// </param>
        /// <param name="maxBufferDuration">
        /// the maximum buffer size (in 100-nanosecond units) that the underlying hardware audio engine can support
        /// for the format specified in the format parameter.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int GetBufferSizeLimits(IntPtr format, bool eventDriven, out long minBufferDuration, out long maxBufferDuration);
    }
}
