// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// Native methods.
    /// </summary>
    internal class NativeMethods
    {
        /// <summary>
        /// Frees all elements that can be freed in a given PropVariant structure.
        /// </summary>
        /// <param name="pvar">
        /// A reference to an initialized PropVariant structure for which any deallocatable elements
        /// are to be freed. On return, all zeroes are written to the PROPVARIANT structure.
        /// </param>
        /// <returns>
        /// S_OK (0) if the VT types are recognized and all items that can be freed have been freed.
        /// STG_E_INVALIDPARAMETER (0x80030057) if the variant has an unknown VT type.
        /// </returns>
        [DllImport("ole32.dll")]
        internal static extern int PropVariantClear(ref PropVariant pvar);

        /// <summary>
        /// Creates an empty media sample.
        /// </summary>
        /// <returns>The IMFSample interface of the media sample.</returns>
        [DllImport("Mfplat.dll", PreserveSig = false)]
        internal static extern IMFSample MFCreateSample();

        /// <summary>
        /// Creates an empty media type.
        /// </summary>
        /// <returns>The IMFMediaType interface.</returns>
        [DllImport("Mfplat.dll", PreserveSig = false)]
        internal static extern IMFMediaType MFCreateMediaType();

        /// <summary>
        /// Allocates system memory and creates a media buffer to manage it.
        /// </summary>
        /// <param name="maxLength">Size of the buffer, in bytes.</param>
        /// <returns>The IMFMediaBuffer interface of the media buffer.</returns>
        [DllImport("Mfplat.dll", PreserveSig = false)]
        internal static extern IMFMediaBuffer MFCreateMemoryBuffer(int maxLength);

        /// <summary>
        /// Associates the calling thread with the specified task.
        /// </summary>
        /// <param name="taskName">
        /// The name of the task to be performed. This name must match the name of one of the subkeys of the following
        /// key HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks.
        /// </param>
        /// <param name="taskIndex">
        /// The unique task identifier. The first time this function is called, this value must be 0 on input.
        /// The index value is returned on output and can be used as input in subsequent calls.
        /// </param>
        /// <returns>
        /// If the function succeeds, it returns a handle to the task. If the function fails, it returns 0.
        /// To retrieve extended error information, call GetLastError.
        /// </returns>
        [DllImport("Avrt.dll", CharSet = CharSet.Unicode)]
        internal static extern int AvSetMmThreadCharacteristics(string taskName, [In, Out] ref int taskIndex);

        /// <summary>
        /// Indicates that a thread is no longer performing work associated with the specified task.
        /// </summary>
        /// <param name="avrtHandle">
        /// A handle to the task returned by the AvSetMmThreadCharacteristics.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value
        /// is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("Avrt.dll")]
        internal static extern bool AvRevertMmThreadCharacteristics(int avrtHandle);
    }
}