// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    /// <summary>
    /// Color space.
    /// </summary>
    public enum ColorSpace : uint // v4l2_colorspace enum
    {
        /// <summary>
        /// V4L2_COLORSPACE_DEFAULT
        /// </summary>
        Default = 0,

        /// <summary>
        /// V4L2_COLORSPACE_SMPTE170M
        /// </summary>
        SMPTE170M = 1,

        /// <summary>
        /// V4L2_COLORSPACE_SMPTE240M
        /// </summary>
        SMPTE240M = 2,

        /// <summary>
        /// V4L2_COLORSPACE_REC709
        /// </summary>
        REC709 = 3,

        /// <summary>
        /// V4L2_COLORSPACE_BT878
        /// </summary>
        BT878 = 4,

        /// <summary>
        /// V4L2_COLORSPACE_470_SYSTEM_M
        /// </summary>
        SystemM470 = 5,

        /// <summary>
        /// V4L2_COLORSPACE_470_SYSTEM_BG
        /// </summary>
        SystemBG470 = 6,

        /// <summary>
        /// V4L2_COLORSPACE_JPEG
        /// </summary>
        JPEG = 7,

        /// <summary>
        /// V4L2_COLORSPACE_SRGB
        /// </summary>
        SRGB = 8,

        /// <summary>
        /// V4L2_COLORSPACE_ADOBERGB
        /// </summary>
        AdobeRGB = 9,

        /// <summary>
        /// V4L2_COLORSPACE_BT2020
        /// </summary>
        BT2020 = 10,

        /// <summary>
        /// V4L2_COLORSPACE_RAW
        /// </summary>
        Raw = 11,

        /// <summary>
        /// V4L2_COLORSPACE_DCI_P3
        /// </summary>
        DCIP3 = 12,
    }
}