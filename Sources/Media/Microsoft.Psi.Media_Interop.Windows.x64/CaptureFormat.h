// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "VideoFormats.h"
namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Struct used to represent a capture format supported by a capture device.
    /// </summary>
    public ref struct CaptureFormat
    {
    internal:
        static CaptureFormat^ FromMediaType(IMFMediaType *pMediaType);

    public:
        /// <summary>
        /// Gets or sets the pixel width of the format.
        /// </summary>
        property int nWidth;

        /// <summary>
        /// Gets or sets the pixel height of the format.
        /// </summary>
        property int nHeight;

        /// <summary>
        /// Gets or sets the frame rate numerator in frames.
        /// </summary>
        property int nFrameRateNumerator;

        /// <summary>
        /// Gets or sets the frame rate denominator in seconds.
        /// </summary>
        property int nFrameRateDenominator;

        /// <summary>
        /// Gets or sets video capture format.
        /// </summary>
        property VideoFormat^ subType;
    };
}}}
