// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"
using namespace System::Runtime::InteropServices;

#include "CaptureFormat.h"
#include "RGBCameraEnumerator.h"
#include "SourceReaderCallback.h"
#include "MediaCaptureDevice.h"

#include "ks.h"
#include "ksmedia.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

/// <summary>
/// Create the Capture Format from the given media type.
/// </summary>
///<param name="pMediaType"> The media type for the capture format</param>
CaptureFormat^ CaptureFormat::FromMediaType(IMFMediaType *pMediaType)
{
    HRESULT hr = S_OK;
    CaptureFormat^ format = gcnew CaptureFormat();
    GUID mediaSubType = {0};
    UINT32 rateNum = 0;
    UINT32 rateDenom = 0;
    UINT32 width = 0;
    UINT32 height = 0;

    MF_THROWPTR(pMediaType);

    hr = pMediaType->GetGUID(MF_MT_SUBTYPE, &mediaSubType);
    MF_THROWHR(hr);

    format->subType = VideoFormat::FromGuid(FromGUID(mediaSubType));

    hr = MFGetAttributeRatio(pMediaType, MF_MT_FRAME_RATE, &rateNum, &rateDenom);
    MF_THROWHR(hr);
    
    format->nFrameRateNumerator = rateNum;
    format->nFrameRateDenominator = rateDenom;
    
    hr = MFGetAttributeSize(pMediaType, MF_MT_FRAME_SIZE, &width, &height);
    MF_THROWHR(hr);

    format->nWidth = width;
    format->nHeight = height;

    return format;
}
}}}