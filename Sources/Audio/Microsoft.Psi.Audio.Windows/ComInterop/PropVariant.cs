// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// PROPVARIANT structure (defined in Propidl.h).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.StyleCop.CSharp.NamingRules",
        "SA1305:FieldNamesMustNotUseHungarianNotation",
        Justification = "Retain Win32 field names for consistency.")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariant
    {
        [FieldOffset(0)]
        private short vt;

        [FieldOffset(2)]
        private short wReserved1;

        [FieldOffset(4)]
        private short wReserved2;

        [FieldOffset(6)]
        private short wReserved3;

        [FieldOffset(8)]
        private sbyte cVal;

        [FieldOffset(8)]
        private byte bVal;

        [FieldOffset(8)]
        private short iVal;

        [FieldOffset(8)]
        private ushort uiVal;

        [FieldOffset(8)]
        private int lVal;

        [FieldOffset(8)]
        private uint ulVal;

        [FieldOffset(8)]
        private int intVal;

        [FieldOffset(8)]
        private uint uintVal;

        [FieldOffset(8)]
        private long hVal;

        [FieldOffset(8)]
        private ulong uhVal;

        [FieldOffset(8)]
        private float fltVal;

        [FieldOffset(8)]
        private double dblVal;

        [FieldOffset(8)]
        private bool boolVal;

        [FieldOffset(8)]
        private int scode;

        [FieldOffset(8)]
        private System.Runtime.InteropServices.ComTypes.FILETIME filetime;

        [FieldOffset(8)]
        private Blob blob;

        [FieldOffset(8)]
        private IntPtr pwszVal;

        /// <summary>
        /// Gets the property value.
        /// </summary>
        public object Value
        {
            get
            {
                VarEnum vt = (VarEnum)this.vt;
                switch (vt)
                {
                    case VarEnum.VT_I1:
                        return this.cVal;

                    case VarEnum.VT_I2:
                        return this.iVal;

                    case VarEnum.VT_I4:
                        return this.intVal;

                    case VarEnum.VT_UI4:
                        return this.uintVal;

                    case VarEnum.VT_I8:
                        return this.hVal;

                    case VarEnum.VT_LPWSTR:
                        return Marshal.PtrToStringUni(this.pwszVal);

                    case VarEnum.VT_BLOB:
                    {
                        byte[] blob = new byte[this.blob.Size];
                        Marshal.Copy(this.blob.Data, blob, 0, blob.Length);
                        return blob;
                    }
                }

                throw new NotImplementedException(vt.ToString());
            }
        }

        /// <summary>
        /// Calls PropVariantClear to free all elements that can be freed.
        /// </summary>
        public void Clear()
        {
            NativeMethods.PropVariantClear(ref this);
        }
    }

    /// <summary>
    /// Blob data structure.
    /// </summary>
    internal struct Blob
    {
        /// <summary>
        /// Blob data size.
        /// </summary>
        internal int Size;

        /// <summary>
        /// Blob data.
        /// </summary>
        internal IntPtr Data;
    }
}