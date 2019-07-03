// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IMFCollection COM interface definition (defined in mfobjects.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMFCollectionIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFCollection
    {
        /// <summary>
        /// Retrieves the number of objects in the collection.
        /// </summary>
        /// <param name="elementCount">The object count.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetElementCount(out int elementCount);

        /// <summary>
        /// Retrieves an object in the collection.
        /// </summary>
        /// <param name="elementIndex">The object index.</param>
        /// <param name="unknownElement">The IUnknown interface of the object.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetElement(int elementIndex, [MarshalAs(UnmanagedType.IUnknown)] out object unknownElement);

        /// <summary>
        /// Adds an object to the collection.
        /// </summary>
        /// <param name="unknownElement">The IUnknown interface of the object.</param>
        /// <returns>An HRESULT return code.</returns>
        int AddElement([MarshalAs(UnmanagedType.IUnknown), In] object unknownElement);

        /// <summary>
        /// Removes an object from the collection.
        /// </summary>
        /// <param name="elementIndex">The object index.</param>
        /// <param name="unknownElement">The IUnknown interface of the object.</param>
        /// <returns>An HRESULT return code.</returns>
        int RemoveElement(int elementIndex, [MarshalAs(UnmanagedType.IUnknown)] out object unknownElement);

        /// <summary>
        /// Adds an object at the specified index in the collection.
        /// </summary>
        /// <param name="index">The index where the object will be added to the collection.</param>
        /// <param name="unknownElement">The IUnknown interface of the object.</param>
        /// <returns>An HRESULT return code.</returns>
        int InsertElementAt(int index, [MarshalAs(UnmanagedType.IUnknown), In] object unknownElement);

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int RemoveAllElements();
    }
}
