// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMMDeviceEnumerator COM interface (defined in Mmdeviceapi.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMMDeviceEnumeratorIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        /// <summary>
        /// Generates a collection of audio endpoint devices that meet the specified criteria.
        /// </summary>
        /// <param name="dataFlow">The data-flow direction for the endpoint devices in the collection.</param>
        /// <param name="stateMask">The state or states of the endpoints that are to be included in the collection.</param>
        /// <returns>The IMMDeviceCollection interface of the device-collection object.</returns>
        IMMDeviceCollection EnumAudioEndpoints(EDataFlow dataFlow, int stateMask);

        /// <summary>
        /// Retrieves the default audio endpoint for the specified data-flow direction and role.
        /// </summary>
        /// <param name="dataFlow">The data-flow direction for the endpoint device.</param>
        /// <param name="role">The role of the endpoint device.</param>
        /// <returns>The IMMDevice interface of the endpoint object for the default audio endpoint device.</returns>
        IMMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role);

        /// <summary>
        /// Retrieves an endpoint device that is specified by an endpoint device-identification string.
        /// </summary>
        /// <param name="id">A string containing the endpoint ID.</param>
        /// <returns>The IMMDevice interface for the specified device.</returns>
        IMMDevice GetDevice(string id);

        /// <summary>
        /// Registers a client's notification callback interface.
        /// </summary>
        /// <param name="client">
        /// The IMMNotificationClient interface that the client is registering for notification callbacks.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int RegisterEndpointNotificationCallback(IMMNotificationClient client);

        /// <summary>
        /// Deletes the registration of a notification interface that the client registered in a previous call to
        /// the IMMDeviceEnumerator.RegisterEndpointNotificationCallback method.
        /// </summary>
        /// <param name="client">The client's IMMNotificationClient interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }
}
