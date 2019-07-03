// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    /// <summary>
    /// MFT_MESSAGE_TYPE enum (defined in mftransform.h).
    /// </summary>
    internal enum MFTMessageType
    {
        /// <summary>
        /// MFT_MESSAGE_COMMAND_FLUSH = 0
        /// </summary>
        COMMAND_FLUSH = 0,

        /// <summary>
        /// MFT_MESSAGE_COMMAND_DRAIN = 0x1
        /// </summary>
        COMMAND_DRAIN = 0x1,

        /// <summary>
        /// MFT_MESSAGE_SET_D3D_MANAGER = 0x2
        /// </summary>
        SET_D3D_MANAGER = 0x2,

        /// <summary>
        /// MFT_MESSAGE_DROP_SAMPLES = 0x3
        /// </summary>
        DROP_SAMPLES = 0x3,

        /// <summary>
        /// MFT_MESSAGE_COMMAND_TICK = 0x4
        /// </summary>
        COMMAND_TICK = 0x4,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_BEGIN_STREAMING = 0x10000000
        /// </summary>
        NOTIFY_BEGIN_STREAMING = 0x10000000,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_END_STREAMING = 0x10000001
        /// </summary>
        NOTIFY_END_STREAMING = 0x10000001,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_END_OF_STREAM = 0x10000002
        /// </summary>
        NOTIFY_END_OF_STREAM = 0x10000002,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_START_OF_STREAM = 0x10000003
        /// </summary>
        NOTIFY_START_OF_STREAM = 0x10000003,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_RELEASE_RESOURCES = 0x10000004
        /// </summary>
        NOTIFY_RELEASE_RESOURCES = 0x10000004,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_REACQUIRE_RESOURCES = 0x10000005
        /// </summary>
        NOTIFY_REACQUIRE_RESOURCES = 0x10000005,

        /// <summary>
        /// MFT_MESSAGE_NOTIFY_EVENT = 0x10000006
        /// </summary>
        NOTIFY_EVENT = 0x10000006,

        /// <summary>
        /// MFT_MESSAGE_COMMAND_SET_OUTPUT_STREAM_STATE = 0x10000007
        /// </summary>
        COMMAND_SET_OUTPUT_STREAM_STATE = 0x10000007,

        /// <summary>
        /// MFT_MESSAGE_COMMAND_FLUSH_OUTPUT_STREAM = 0x10000008
        /// </summary>
        COMMAND_FLUSH_OUTPUT_STREAM = 0x10000008,

        /// <summary>
        /// MFT_MESSAGE_COMMAND_MARKER = 0x20000000
        /// </summary>
        COMMAND_MARKER = 0x20000000,
    }
}