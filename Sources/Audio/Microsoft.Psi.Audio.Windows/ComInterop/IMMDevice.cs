// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMMDevice COM interface (defined in Mmdeviceapi.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMMDeviceIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        /// <summary>
        /// Creates a COM object with the specified interface.
        /// </summary>
        /// <param name="iid">The interface identifier.</param>
        /// <param name="clsCtx">The execution context in which the code that manages the newly created object will run.</param>
        /// <param name="activationParams">
        /// Set to NULL to activate an IAudioClient, IAudioEndpointVolume, IAudioMeterInformation,
        /// IAudioSessionManager, or IDeviceTopology interface on an audio endpoint device.
        /// </param>
        /// <returns>The interface specified by parameter iid.</returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object Activate(ref Guid iid, ClsCtx clsCtx, IntPtr activationParams);

        /// <summary>
        /// Gets an interface to the device's property store.
        /// </summary>
        /// <param name="stgmAccess">The storage-access mode.</param>
        /// <returns>The IPropertyStore interface of the device's property store.</returns>
        IPropertyStore OpenPropertyStore(int stgmAccess);

        /// <summary>
        /// Gets a string that identifies the device.
        /// </summary>
        /// <returns>The endpoint device ID string.</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetId();

        /// <summary>
        /// Gets the current state of the device.
        /// </summary>
        /// <returns>The device-state value, as one of the DEVICE_STATE_XXX constants.</returns>
        int GetState();
    }
}
