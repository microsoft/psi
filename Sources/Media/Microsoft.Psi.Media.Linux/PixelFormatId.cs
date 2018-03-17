// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    /// <summary>
    /// Pixel format ID (v4l2_pix_format_id).
    /// </summary>
    /// <remarks>
    /// These are four character codes (see: https://www.fourcc.org/).
    /// </remarks>
    public enum PixelFormatId : uint
    {
        /// <summary>
        /// RGB332 fourcc value.
        /// </summary>
        RGB332 = 826427218,

        /// <summary>
        /// RGB444 fourcc value.
        /// </summary>
        RGB444 = 875836498,

        /// <summary>
        /// RGB555 fourcc value.
        /// </summary>
        RGB555 = 1329743698,

        /// <summary>
        /// RGB565 fourcc value.
        /// </summary>
        RGB565 = 1346520914,

        /// <summary>
        /// RGB555X fourcc value.
        /// </summary>
        RGB555X = 1363298130,

        /// <summary>
        /// RGB565X fourcc value.
        /// </summary>
        RGB565X = 1380075346,

        /// <summary>
        /// BGR24 fourcc value.
        /// </summary>
        BGR24 = 861030210,

        /// <summary>
        /// RGB24 fourcc value.
        /// </summary>
        RGB24 = 859981650,

        /// <summary>
        /// BGR32 fourcc value.
        /// </summary>
        BGR32 = 877807426,

        /// <summary>
        /// RGB32 fourcc value.
        /// </summary>
        RGB32 = 876758866,

        /// <summary>
        /// GREY fourcc value.
        /// </summary>
        GREY = 1497715271,

        /// <summary>
        /// YUYV fourcc value.
        /// </summary>
        YUYV = 1448695129,

        /// <summary>
        /// UYVY fourcc value.
        /// </summary>
        UYVY = 1498831189,

        /// <summary>
        /// Y41P fourcc value.
        /// </summary>
        Y41P = 1345401945,

        /// <summary>
        /// YVU420 fourcc value.
        /// </summary>
        YVU420 = 842094169,

        /// <summary>
        /// YUV420 fourcc value.
        /// </summary>
        YUV420 = 842093913,

        /// <summary>
        /// YVU410 fourcc value.
        /// </summary>
        YVU410 = 961893977,

        /// <summary>
        /// YUV410 fourcc value.
        /// </summary>
        YUV410 = 961959257,

        /// <summary>
        /// YUV422P fourcc value.
        /// </summary>
        YUV422P = 1345466932,

        /// <summary>
        /// NV12 fourcc value.
        /// </summary>
        NV12 = 842094158,

        /// <summary>
        /// NV21 fourcc value.
        /// </summary>
        NV21 = 825382478,

        /// <summary>
        /// DV fourcc value.
        /// </summary>
        DV = 1685288548,

        /// <summary>
        /// ET61X251 fourcc value.
        /// </summary>
        ET61X251 = 892483141,

        /// <summary>
        /// HI240 fourcc value.
        /// </summary>
        HI240 = 875710792,

        /// <summary>
        /// HM12 fourcc value.
        /// </summary>
        HM12 = 842091848,

        /// <summary>
        /// MJPEG fourcc value.
        /// </summary>
        MJPEG = 1196444237,

        /// <summary>
        /// PWC1 fourcc value.
        /// </summary>
        PWC1 = 826496848,

        /// <summary>
        /// PWC2 fourcc value.
        /// </summary>
        PWC2 = 843274064,

        /// <summary>
        /// SN9C10X fourcc value.
        /// </summary>
        SN9C10X = 808532307,

        /// <summary>
        /// WNVA fourcc value.
        /// </summary>
        WNVA = 1096175191,

        /// <summary>
        /// YYUV fourcc value.
        /// </summary>
        YYUV = 1448434009,
    }
}