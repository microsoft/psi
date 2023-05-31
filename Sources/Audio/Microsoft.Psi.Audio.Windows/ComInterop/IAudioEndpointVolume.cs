// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IAudioEndpointVolume COM interface (defined in Endpointvolume.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IAudioEndpointVolumeIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        /// <summary>
        /// Registers a client's notification callback interface.
        /// </summary>
        /// <param name="notify">The client's IAudioEndpointVolumeCallback interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int RegisterControlChangeNotify(IAudioEndpointVolumeCallback notify);

        /// <summary>
        /// Deletes the registration of a client's notification callback interface.
        /// </summary>
        /// <param name="notify">The client's IAudioEndpointVolumeCallback interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback notify);

        /// <summary>
        /// Gets a count of the channels in the audio stream.
        /// </summary>
        /// <param name="channelCount">The channel count.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetChannelCount(out int channelCount);

        /// <summary>
        /// Sets the master volume level of the audio stream, in decibels.
        /// </summary>
        /// <param name="levelDB">The new master volume level in decibels.</param>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int SetMasterVolumeLevel(float levelDB, [In] ref Guid pguidEventContext);

        /// <summary>
        /// Sets the master volume level, expressed as a normalized, audio-tapered value.
        /// </summary>
        /// <param name="level">The new master volume level.</param>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int SetMasterVolumeLevelScalar(float level, [In] ref Guid pguidEventContext);

        /// <summary>
        /// Gets the master volume level of the audio stream, in decibels.
        /// </summary>
        /// <returns>The master volume level in decibels.</returns>
        float GetMasterVolumeLevel();

        /// <summary>
        /// Gets the master volume level, expressed as a normalized, audio-tapered value.
        /// </summary>
        /// <returns>The master volume level.</returns>
        float GetMasterVolumeLevelScalar();

        /// <summary>
        /// Sets the volume level, in decibels, of the specified channel of the audio stream.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <param name="levelDB">The new volume level in decibels.</param>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int SetChannelVolumeLevel(uint channel, float levelDB, [In] ref Guid pguidEventContext);

        /// <summary>
        /// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <param name="level">The new volume level.</param>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int SetChannelVolumeLevelScalar(uint channel, float level, [In] ref Guid pguidEventContext);

        /// <summary>
        /// Gets the volume level, in decibels, of the specified channel in the audio stream.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <returns>The channel volume level in decibels.</returns>
        float GetChannelVolumeLevel(uint channel);

        /// <summary>
        /// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
        /// </summary>
        /// <param name="channel">The channel number.</param>
        /// <returns>The channel volume level.</returns>
        float GetChannelVolumeLevelScalar(uint channel);

        /// <summary>
        /// Sets the muting state of the audio stream.
        /// </summary>
        /// <param name="mute">The new muting state.</param>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, [In] ref Guid pguidEventContext);

        /// <summary>
        /// Gets the muting state of the audio stream.
        /// </summary>
        /// <returns>The muting state.</returns>
        bool GetMute();

        /// <summary>
        /// Gets information about the current step in the volume range.
        /// </summary>
        /// <param name="stepIndex">The current step index.</param>
        /// <param name="stepCount">The number of steps in the volume range.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetVolumeStepInfo(out uint stepIndex, out uint stepCount);

        /// <summary>
        /// Increases the volume level by one step.
        /// </summary>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int VolumeStepUp([In] ref Guid pguidEventContext);

        /// <summary>
        /// Decreases the volume level by one step.
        /// </summary>
        /// <param name="pguidEventContext">
        /// Context value for the IAudioEndpointVolumeCallback.OnNotify method.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int VolumeStepDown([In] ref Guid pguidEventContext);

        /// <summary>
        /// Queries the audio endpoint device for its hardware-supported functions.
        /// </summary>
        /// <returns>
        /// A hardware support mask that indicates the hardware capabilities of the audio endpoint device.
        /// </returns>
        int QueryHardwareSupport();

        /// <summary>
        /// Gets the volume range of the audio stream, in decibels.
        /// </summary>
        /// <param name="pflVolumeMindB">The minimum volume level.</param>
        /// <param name="pflVolumeMaxdB">The maximum volume level.</param>
        /// <param name="pflVolumeIncrementdB">The volume increment.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }
}
