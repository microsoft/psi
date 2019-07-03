// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IAudioRenderClient COM interface (defined in Audioclient.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IAudioRenderClientIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioRenderClient
    {
        /// <summary>
        /// Retrieves a pointer to the next available space in the rendering endpoint buffer.
        /// </summary>
        /// <param name="numFramesRequested">
        /// The number of audio frames in the data packet that the caller plans to write to the requested space in the buffer.
        /// </param>
        /// <returns>
        /// The starting address of the buffer area into which the caller will write the data packet.
        /// </returns>
        IntPtr GetBuffer(int numFramesRequested);

        /// <summary>
        /// Releases the buffer space acquired in the previous call to the IAudioRenderClient.GetBuffer method.
        /// </summary>
        /// <param name="numFramesWritten">
        /// The number of audio frames written by the client to the data packet.
        /// </param>
        /// <param name="flags">The buffer-configuration flags.</param>
        /// <returns>An HRESULT return code.</returns>
        int ReleaseBuffer(int numFramesWritten, int flags);
    }
}
