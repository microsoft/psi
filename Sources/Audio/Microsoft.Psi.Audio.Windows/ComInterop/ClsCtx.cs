// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;

    /// <summary>
    /// CLSCTX enumeration (defined in WTypes.h).
    /// </summary>
    [Flags]
    internal enum ClsCtx : uint
    {
        /// <summary>
        /// CLSCTX_INPROC_SERVER
        /// </summary>
        INPROC_SERVER = 0x1,

        /// <summary>
        /// CLSCTX_INPROC_HANDLER
        /// </summary>
        INPROC_HANDLER = 0x2,

        /// <summary>
        /// CLSCTX_LOCAL_SERVER
        /// </summary>
        LOCAL_SERVER = 0x4,

        /// <summary>
        /// CLSCTX_INPROC_SERVER16
        /// </summary>
        INPROC_SERVER16 = 0x8,

        /// <summary>
        /// CLSCTX_REMOTE_SERVER
        /// </summary>
        REMOTE_SERVER = 0x10,

        /// <summary>
        /// CLSCTX_INPROC_HANDLER16
        /// </summary>
        INPROC_HANDLER16 = 0x20,

        /// <summary>
        /// CLSCTX_RESERVED1
        /// </summary>
        RESERVED1 = 0x40,

        /// <summary>
        /// CLSCTX_RESERVED2
        /// </summary>
        RESERVED2 = 0x80,

        /// <summary>
        /// CLSCTX_RESERVED3
        /// </summary>
        RESERVED3 = 0x100,

        /// <summary>
        /// CLSCTX_RESERVED4
        /// </summary>
        RESERVED4 = 0x200,

        /// <summary>
        /// CLSCTX_NO_CODE_DOWNLOAD
        /// </summary>
        NO_CODE_DOWNLOAD = 0x400,

        /// <summary>
        /// CLSCTX_RESERVED5
        /// </summary>
        RESERVED5 = 0x800,

        /// <summary>
        /// CLSCTX_NO_CUSTOM_MARSHAL
        /// </summary>
        NO_CUSTOM_MARSHAL = 0x1000,

        /// <summary>
        /// CLSCTX_ENABLE_CODE_DOWNLOAD
        /// </summary>
        ENABLE_CODE_DOWNLOAD = 0x2000,

        /// <summary>
        /// CLSCTX_NO_FAILURE_LOG
        /// </summary>
        NO_FAILURE_LOG = 0x4000,

        /// <summary>
        /// CLSCTX_DISABLE_AAA
        /// </summary>
        DISABLE_AAA = 0x8000,

        /// <summary>
        /// CLSCTX_ENABLE_AAA
        /// </summary>
        ENABLE_AAA = 0x10000,

        /// <summary>
        /// CLSCTX_FROM_DEFAULT_CONTEXT
        /// </summary>
        FROM_DEFAULT_CONTEXT = 0x20000,

        /// <summary>
        /// CLSCTX_ACTIVATE_32_BIT_SERVER
        /// </summary>
        ACTIVATE_32_BIT_SERVER = 0x40000,

        /// <summary>
        /// CLSCTX_ACTIVATE_64_BIT_SERVER
        /// </summary>
        ACTIVATE_64_BIT_SERVER = 0x80000,

        /// <summary>
        /// CLSCTX_ENABLE_CLOAKING
        /// </summary>
        ENABLE_CLOAKING = 0x100000,

        /// <summary>
        /// CLSCTX_APPCONTAINER
        /// </summary>
        APPCONTAINER = 0x400000,

        /// <summary>
        /// CLSCTX_ACTIVATE_AAA_AS_IU
        /// </summary>
        ACTIVATE_AAA_AS_IU = 0x800000,

        /// <summary>
        /// CLSCTX_PS_DLL
        /// </summary>
        PS_DLL = 0x80000000,

        /// <summary>
        /// CLSCTX_SERVER
        /// </summary>
        SERVER = INPROC_SERVER | LOCAL_SERVER | REMOTE_SERVER,

        /// <summary>
        /// CLSCTX_ALL
        /// </summary>
        ALL = INPROC_HANDLER | SERVER,
    }
}
