// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"

#include "RGBCameraEnumerator.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

/// <summary>
///  ctor that initializes the enumerator by getting the list of source devices
///  that are registered as video capture devices
/// </summary>
RGBCameraEnumerator::RGBCameraEnumerator()
{
    HRESULT hr = S_OK;

    IMFAttributes *pAttributes = NULL;
    IMFActivate **ppDevices = NULL;
    UINT32 count = NULL;

    _ppDevices = NULL;
    m_nCount = 0;
    m_nIndex = -1;

    try
    {
        hr = MFCreateAttributes(&pAttributes, 1);
        MF_THROWHR(hr);
    
        hr = pAttributes->SetGUID(
            MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
            MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
        MF_THROWHR(hr);
    
        hr = MFEnumDeviceSources(pAttributes, &ppDevices, &count);
        MF_THROWHR(hr);

        m_nCount = count;
        _ppDevices = ppDevices;
    }
    finally
    {
        MF_RELEASE(pAttributes);
    }
}

/// <summary>
///  dtor that releases and frees the device activate instances and frees the
///  storage used.
/// </summary>
RGBCameraEnumerator::~RGBCameraEnumerator()
{
    if (_ppDevices != NULL)
    {
        for (int index = 0; index < m_nCount; index++)
        {
            MF_RELEASE(_ppDevices[index]);
        }
        CoTaskMemFree(_ppDevices);
        _ppDevices = NULL;
    }
}

/// <summary>
///  Advances the cursor through the collection.
/// </summary>
/// <returns> True if the cursor is still within the collection. </returns>
bool RGBCameraEnumerator::MoveNext()
{
    m_nIndex++;

    return m_nIndex >= 0 && m_nIndex < m_nCount;
}

/// <summary>
///  Resets the cursor to before the first element.
/// </summary>
void RGBCameraEnumerator::Reset()
{
    m_nIndex = -1;
}


/// <summary>
///  Instantiates a capture device for the currently selected device
/// </summary>
///<returns>An instance of the RGBCamera class.</returns>
///<exceptions> InvalidOperationException - thrown if the cursor is out of bounds </exceptions>
MediaCaptureDevice^ RGBCameraEnumerator::Current::get()
{
    if (m_nIndex < 0 || m_nIndex >= m_nCount)
    {
        throw gcnew InvalidOperationException();
    }

    return gcnew MediaCaptureDevice(_ppDevices[m_nIndex]);
}

}}}
