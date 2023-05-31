// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMFMediaBuffer COM interface (defined in Mfobjects.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMFMediaBufferIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaBuffer
    {
        /// <summary>
        /// Gives the caller access to the memory in the buffer.
        /// </summary>
        /// <param name="bufferPtr">A pointer to the start of the buffer.</param>
        /// <param name="maxLength">The maximum amount of data that can be written to the buffer.</param>
        /// <param name="currentLength">The length of the valid data in the buffer, in bytes.</param>
        /// <returns>An HRESULT return code.</returns>
        int Lock(out IntPtr bufferPtr, out int maxLength, out int currentLength);

        /// <summary>
        /// Unlocks a buffer that was previously locked.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int Unlock();

        /// <summary>
        /// Retrieves the length of the valid data in the buffer.
        /// </summary>
        /// <returns>The length of the valid data, in bytes.</returns>
        int GetCurrentLength();

        /// <summary>
        /// Sets the length of the valid data in the buffer.
        /// </summary>
        /// <param name="currentLength">Length of the valid data, in bytes.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetCurrentLength(int currentLength);

        /// <summary>
        /// Retrieves the allocated size of the buffer.
        /// </summary>
        /// <returns>The allocated size of the buffer, in bytes.</returns>
        int GetMaxLength();
    }
}
