// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "VideoFormats.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Video Properties enumeration
    /// </summary>
    public enum class VideoProperty
    {
        /// <summary>
        /// Brightness
        /// </summary>
        Brightness = VideoProcAmp_Brightness,

        /// <summary>
        /// Contrast
        /// </summary>
        Contrast = VideoProcAmp_Contrast,

        /// <summary>
        /// Hue
        /// </summary>
        Hue = VideoProcAmp_Hue,

        /// <summary>
        /// Saturation
        /// </summary>
        Saturation = VideoProcAmp_Saturation,

        /// <summary>
        /// Sharpness
        /// </summary>
        Sharpness = VideoProcAmp_Sharpness,

        /// <summary>
        /// Gamma
        /// </summary>
        Gamma = VideoProcAmp_Gamma,

        /// <summary>
        /// Color Enable
        /// </summary>
        ColorEnable = VideoProcAmp_ColorEnable,

        /// <summary>
        /// While Balance
        /// </summary>
        WhiteBalance = VideoProcAmp_WhiteBalance,

        /// <summary>
        /// Backlight compensation
        /// </summary>
        BacklightCompensation = VideoProcAmp_BacklightCompensation,

        /// <summary>
        /// Gain
        /// </summary>
        Gain = VideoProcAmp_Gain,
    };

    /// <summary>
    /// Video Property flags enumeration
    /// </summary>
    public enum class VideoPropertyFlags
    {
        /// <summary>
        /// Auto settings
        /// </summary>
        Auto = VideoProcAmp_Flags_Auto,

        /// <summary>
        /// Manual settings
        /// </summary>
        Manual = VideoProcAmp_Flags_Manual
    };

    /// <summary>
    /// Video Property Value Structure.
    /// </summary>
    public ref struct VideoPropertyValue
    {
    public:
        /// <summary>
        /// Video Property.
        /// </summary>
        property VideoProperty m_Property;

        /// <summary>
        /// Value of the video property
        /// </summary>
        property int nValue;

        /// <summary>
        /// Minimum of the video property
        /// </summary>
        property int nMinimum;

        /// <summary>
        /// Maximum of the video property
        /// </summary>
        property int nMaximum;

        /// <summary>
        /// Stepping delta of the video property
        /// </summary>
        property int nSteppingDelta;

        /// <summary>
        /// Default value of the video property
        /// </summary>
        property int nDefault;

        /// <summary>
        /// Video property flags
        /// </summary>
        property VideoPropertyFlags m_Flags;
    };
}}}
