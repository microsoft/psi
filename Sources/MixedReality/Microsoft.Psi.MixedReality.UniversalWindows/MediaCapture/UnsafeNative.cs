// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.MediaCapture
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides unsafe native APIs.
    /// </summary>
    public static class UnsafeNative
    {
        /// <summary>
        /// Provides access to an IMemoryBuffer as an array of bytes.
        /// </summary>
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public unsafe interface IMemoryBufferByteAccess
        {
            /// <summary>
            /// Gets an IMemoryBuffer as an array of bytes.
            /// </summary>
            /// <param name="buffer">A pointer to a byte array containing the buffer data.</param>
            /// <param name="capacity">The number of bytes in the returned array.</param>
            void GetBuffer(out byte* buffer, out uint capacity);
        }
    }
}
