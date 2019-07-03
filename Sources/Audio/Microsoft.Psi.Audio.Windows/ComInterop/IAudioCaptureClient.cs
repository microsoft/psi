// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IAudioCaptureClient COM interface (defined in Audioclient.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IAudioCaptureClientIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioCaptureClient
    {
        /// <summary>
        /// Retrieves a pointer to the next available packet of data in the capture endpoint buffer.
        /// </summary>
        /// <param name="dataBuffer">
        /// Pointer variable into which the method writes the starting address of the next data packet that is available for the client to read.
        /// </param>
        /// <param name="numFramesToRead">
        /// A variable into which the method writes the frame count (the number of audio frames available in the data packet). The client should either read the entire data packet or none of it.
        /// </param>
        /// <param name="bufferFlags">
        /// A variable into which the method writes the buffer-status flags.
        /// </param>
        /// <param name="devicePosition">
        /// A variable into which the method writes the device position of the first audio frame in the data packet.
        /// </param>
        /// <param name="qpcPosition">
        /// A variable into which the method writes the value of the performance counter at the time that the audio endpoint device recorded the device position of the first audio frame in the data packet.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        [PreserveSig]
        int GetBuffer(out IntPtr dataBuffer, out int numFramesToRead, out int bufferFlags, out long devicePosition, out long qpcPosition);

        /// <summary>
        /// Releases the buffer.
        /// </summary>
        /// <param name="numFramesRead">The number of audio frames that the client read from the capture buffer.</param>
        /// <returns>An HRESULT return code.</returns>
        int ReleaseBuffer(int numFramesRead);

        /// <summary>
        /// Retrieves the number of frames in the next data packet in the capture endpoint buffer.
        /// </summary>
        /// <param name="numFramesInNextPacket">
        /// A variable into which the method writes the frame count (the number of audio frames in the next capture packet).
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int GetNextPacketSize(out int numFramesInNextPacket);
    }
}
