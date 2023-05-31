// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeMagFrame.h"
#include "ResearchModeMagFrame.g.cpp"

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeMagFrame::ResearchModeMagFrame(::IResearchModeSensorFrame* pSensorFrame)
    {
        m_pSensorFrame.attach(pSensorFrame);
        m_pMagFrame = m_pSensorFrame.as<::IResearchModeMagFrame>();
    }

	com_array<MagDataStruct> ResearchModeMagFrame::GetMagnetometerSamples()
    {
        const ::MagDataStruct* pBuffer = nullptr; // note: this is the non-IDL-generated version from ResearchModeApi.h
        size_t bufferLength = 0;
        HRESULT hr = m_pMagFrame->GetMagnetometerSamples(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        const MagDataStruct* pBuffer2 = (MagDataStruct*)(pBuffer); // cast to IDL-generated version
        return winrt::com_array(pBuffer2, pBuffer2 + bufferLength);
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorResolution ResearchModeMagFrame::GetResolution()
    {
        ::ResearchModeSensorResolution resolution;
        winrt::check_hresult(m_pSensorFrame->GetResolution(&resolution));
        return *(reinterpret_cast<ResearchModeSensorResolution*>(&resolution));
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp ResearchModeMagFrame::GetTimeStamp()
    {
        ::ResearchModeSensorTimestamp timestamp;
        winrt::check_hresult(m_pSensorFrame->GetTimeStamp(&timestamp));
        return *(reinterpret_cast<ResearchModeSensorTimestamp*>(&timestamp));
    }
}
