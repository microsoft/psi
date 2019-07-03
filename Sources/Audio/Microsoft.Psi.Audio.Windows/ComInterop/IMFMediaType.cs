// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// IMFMediaType COM interface (defined in Mfobjects.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IMFMediaTypeIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMFMediaType
    {
        #region IMFAttributes methods

        /// <summary>
        /// Retrieves the value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">A PROPVARIANT that receives the value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetItem([In] ref Guid guidKey, [In, Out] ref PropVariant value);

        /// <summary>
        /// Retrieves the data type of the value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="type">The attribute type, as an MF_ATTRIBUTE_TYPE enumeration value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetItemType([In] ref Guid guidKey, out short type);

        /// <summary>
        /// Queries whether a stored attribute value equals a specified PROPVARIANT.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to query.</param>
        /// <param name="value">PROPVARIANT that contains the value to compare.</param>
        /// <param name="result">A boolean value indicating whether the attribute matches the value.</param>
        /// <returns>An HRESULT return code.</returns>
        int CompareItem([In] ref Guid guidKey, [In] ref PropVariant value, out bool result);

        /// <summary>
        /// Compares the attributes on this object with the attributes on another object.
        /// </summary>
        /// <param name="theirs">An IMFAttributes interface to compare with this object.</param>
        /// <param name="matchType">
        /// Member of the MFAttributesMatchType enumeration, specifying the type of comparison to make.
        /// </param>
        /// <param name="result">A boolean value representing the match result.</param>
        /// <returns>An HRESULT return code.</returns>
        int Compare(IMFAttributes theirs, MFAttributesMatchType matchType, out bool result);

        /// <summary>
        /// Retrieves a UINT32 value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">The UINT32 value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetUINT32([In] ref Guid guidKey, out uint value);

        /// <summary>
        /// Retrieves a UINT64 value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">The UINT64 value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetUINT64([In] ref Guid guidKey, out ulong value);

        /// <summary>
        /// Retrieves a double value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">The double value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetDouble([In] ref Guid guidKey, out double value);

        /// <summary>
        /// Retrieves a GUID value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="guidValue">The GUID value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetGUID([In] ref Guid guidKey, out Guid guidValue);

        /// <summary>
        /// Retrieves the length of a string value associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="length">The length of the string value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetStringLength([In] ref Guid guidKey, out int length);

        /// <summary>
        /// Retrieves a wide-character string associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">A StringBuilder to which the string value will be written.</param>
        /// <param name="size">The maximum length of the string value to return.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder value, int size, out int length);

        /// <summary>
        /// Retrieves a wide-character string associated with a key. This method allocates the memory for the string.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="value">The string value.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetAllocatedString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string value, out int length);

        /// <summary>
        /// Retrieves the length of a byte array associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="blobSize">The size of the array, in bytes.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetBlobSize([In] ref Guid guidKey, out int blobSize);

        /// <summary>
        /// Retrieves a byte array associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="buf">A buffer allocated by the caller.</param>
        /// <param name="bufSize">The size of the buffer, in bytes.</param>
        /// <param name="blobSize">Receives the size of the byte array.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetBlob([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buf, int bufSize, out int blobSize);

        /// <summary>
        /// Retrieves a byte array associated with a key. This method allocates the memory for the array.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="buf">A copy of the array.</param>
        /// <param name="size">The size of the array, in bytes.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetAllocatedBlob([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray)] out byte[] buf, out int size);

        /// <summary>
        /// Retrieves an interface pointer associated with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies which value to retrieve.</param>
        /// <param name="iid">Interface identifier (IID) of the interface to retrieve.</param>
        /// <param name="interfacePtr">The requested interface.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetUnknown([In] ref Guid guidKey, [In] ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePtr);

        /// <summary>
        /// Associates an attribute value with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="value">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetItem([In] ref Guid guidKey, [In] ref PropVariant value);

        /// <summary>
        /// Removes a key/value pair from the object's attribute list.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to delete.</param>
        /// <returns>An HRESULT return code.</returns>
        int DeleteItem([In] ref Guid guidKey);

        /// <summary>
        /// Removes all key/value pairs from the object's attribute list.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int DeleteAllItems();

        /// <summary>
        /// Associates a UINT32 value with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="value">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetUINT32([In] ref Guid guidKey, uint value);

        /// <summary>
        /// Associates a UINT64 value with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="value">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetUINT64([In] ref Guid guidKey, ulong value);

        /// <summary>
        /// Associates a double value with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="value">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetDouble([In] ref Guid guidKey, double value);

        /// <summary>
        /// Associates a GUID value with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="guidValue">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetGUID([In] ref Guid guidKey, [In] ref Guid guidValue);

        /// <summary>
        /// Associates a wide-character string with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="value">The string to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr), In] string value);

        /// <summary>
        /// Associates a byte array with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="buf">The byte array to associate with this key.</param>
        /// <param name="size">Size of the array, in bytes.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetBlob([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), In] byte[] buf, int size);

        /// <summary>
        /// Associates an IUnknown pointer with a key.
        /// </summary>
        /// <param name="guidKey">A GUID that identifies the value to set.</param>
        /// <param name="interfacePtr">The value to associate with this key.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetUnknown([In] ref Guid guidKey, [MarshalAs(UnmanagedType.IUnknown), In] object interfacePtr);

        /// <summary>
        /// Locks the attribute store so that no other thread can access it.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int LockStore();

        /// <summary>
        /// Unlocks the attribute store.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int UnlockStore();

        /// <summary>
        /// Retrieves the number of attributes that are set on this object.
        /// </summary>
        /// <param name="items">The number of attributes.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetCount(out int items);

        /// <summary>
        /// Retrieves an attribute at the specified index.
        /// </summary>
        /// <param name="index">Index of the attribute to retrieve.</param>
        /// <param name="guidKey">The GUID that identifies this attribute.</param>
        /// <param name="value">A PROPVARIANT that receives the value.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetItemByIndex(int index, out Guid guidKey, [In, Out] ref PropVariant value);

        /// <summary>
        /// Copies all of the attributes from this object into another attribute store.
        /// </summary>
        /// <param name="dest">The IMFAttributes interface of the attribute store that receives the copy.</param>
        /// <returns>An HRESULT return code.</returns>
        int CopyAllItems(IMFAttributes dest);

        #endregion

        /// <summary>
        /// Retrieves the major type of the format.
        /// </summary>
        /// <param name="guidMajorType">The major type GUID.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetMajorType(out Guid guidMajorType);

        /// <summary>
        /// Queries whether the media type is a compressed format.
        /// </summary>
        /// <param name="compressed">A boolean value indicating if the format uses temporal compression.</param>
        /// <returns>An HRESULT return code.</returns>
        int IsCompressedFormat(out bool compressed);

        /// <summary>
        /// Compares two media types and determines whether they are identical.
        /// </summary>
        /// <param name="mediaType">The IMFMediaType interface of the media type to compare.</param>
        /// <param name="flags">
        /// A bitwise OR of zero or more flags, indicating the degree of similarity between the two media types.
        /// </param>
        /// <returns>An HRESULT return code.</returns>
        int IsEqual(IMFMediaType mediaType, out int flags);

        /// <summary>
        /// Retrieves an alternative representation of the media type.
        /// </summary>
        /// <param name="guidRepresentation">GUID that specifies the representation to retrieve.</param>
        /// <param name="ptrRepresentation">A pointer to a structure that contains the representation.</param>
        /// <returns>An HRESULT return code.</returns>
        int GetRepresentation(Guid guidRepresentation, out IntPtr ptrRepresentation);

        /// <summary>
        /// Frees memory that was allocated by the GetRepresentation method.
        /// </summary>
        /// <param name="guidRepresentation">GUID that was passed to the GetRepresentation method.</param>
        /// <param name="ptrRepresentation">Pointer to the buffer that was returned by the GetRepresentation method.</param>
        /// <returns>An HRESULT return code.</returns>
        int FreeRepresentation(Guid guidRepresentation, IntPtr ptrRepresentation);
    }
}
