// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;

    /// <summary>
    /// Enumeration of flags that control the behavior of cloning.
    /// </summary>
    [Flags]
    public enum CloningFlags
    {
        /// <summary>
        /// No flags set.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Allow cloning of IntPtr fields.
        /// </summary>
        CloneIntPtrFields = 0x01,

        /// <summary>
        /// Allow cloning of unmanaged pointer fields.
        /// </summary>
        ClonePointerFields = 0x02,

        /// <summary>
        /// Skip cloning of NonSerialized fields.
        /// </summary>
        SkipNonSerializedFields = 0x04,
    }
}
