// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IAudioEndpointVolumeCallback COM interface (defined in Endpointvolume.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IAudioEndpointVolumeCallbackIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolumeCallback
    {
        /// <summary>
        /// Notifies the client that the volume level or muting state of the audio endpoint device has changed.
        /// </summary>
        /// <param name="notify">Pointer to the volume-notification data.</param>
        void OnNotify(IntPtr notify);
    }
}
