// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// IPropertyStore COM interface (defined in Propsys.h).
    /// </summary>
    [ComImport]
    [Guid(Guids.IPropertyStoreIIDString)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        /// <summary>
        /// Gets the number of properties attached to the file.
        /// </summary>
        /// <returns>The property count.</returns>
        int GetCount();

        /// <summary>
        /// Gets a property key from an item's array of properties.
        /// </summary>
        /// <param name="property">The index of the property key in the array of PropertyKey structures.</param>
        /// <returns>The unique identifier for the property..</returns>
        PropertyKey GetAt(int property);

        /// <summary>
        /// Gets data for a specific property.
        /// </summary>
        /// <param name="key">A reference to the PropertyKey structure retrieved through IPropertyStore.GetAt.</param>
        /// <returns>A PropVariant structure that contains the property data.</returns>
        PropVariant GetValue(ref PropertyKey key);

        /// <summary>
        /// Sets a new property value, or replaces or removes an existing value.
        /// </summary>
        /// <param name="key">A reference to the PropertyKey structure retrieved through IPropertyStore.GetAt.</param>
        /// <param name="value">A PropVariant structure that contains the new property data.</param>
        /// <returns>An HRESULT return code.</returns>
        int SetValue(ref PropertyKey key, ref PropVariant value);

        /// <summary>
        /// Saves a property change.
        /// </summary>
        /// <returns>An HRESULT return code.</returns>
        int Commit();
    }
}
