// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMMNotificationClient COM interface (defined in Mmdeviceapi.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMMNotificationClientIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMNotificationClient
    {
        /// <summary>
        /// Indicates that the state of an audio endpoint device has changed.
        /// </summary>
        /// <param name="deviceId">The endpoint ID string that identifies the audio endpoint device.</param>
        /// <param name="newState">Specifies the new state of the endpoint device.</param>
        void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int newState);

        /// <summary>
        /// Indicates that a new audio endpoint device has been added.
        /// </summary>
        /// <param name="pwstrDeviceId">The endpoint ID string that identifies the audio endpoint device.</param>
        void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);

        /// <summary>
        /// Indicates that an audio endpoint device has been removed.
        /// </summary>
        /// <param name="deviceId">The endpoint ID string that identifies the audio endpoint device.</param>
        void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        /// <summary>
        /// Notifies the client that the default audio endpoint device for a particular role has changed.
        /// </summary>
        /// <param name="flow">The data-flow direction of the endpoint device.</param>
        /// <param name="role">The device role of the audio endpoint device.</param>
        /// <param name="defaultDeviceId">The endpoint ID string that identifies audio endpoint device.</param>
        void OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string defaultDeviceId);

        /// <summary>
        /// Indicates that the value of a property belonging to an audio endpoint device has changed.
        /// </summary>
        /// <param name="pwstrDeviceId">The endpoint ID string that identifies the audio endpoint device.</param>
        /// <param name="key">A PROPERTYKEY structure that specifies the property.</param>
        void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PropertyKey key);
    }
}
