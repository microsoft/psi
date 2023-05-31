// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeGyroFrame.h"
#include "ResearchModeGyroFrame.g.cpp"

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeGyroFrame::ResearchModeGyroFrame(::IResearchModeSensorFrame* pSensorFrame)
    {
        m_pSensorFrame.attach(pSensorFrame);
        m_pGyroFrame = m_pSensorFrame.as<::IResearchModeGyroFrame>();
    }

	com_array<GyroDataStruct> ResearchModeGyroFrame::GetCalibratedGyroSamples()
    {
        const ::GyroDataStruct* pBuffer = nullptr; // note: this is the non-IDL-generated version from ResearchModeApi.h
        size_t bufferLength = 0;
        HRESULT hr = m_pGyroFrame->GetCalibratedGyroSamples(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        const GyroDataStruct* pBuffer2 = (GyroDataStruct*)(pBuffer); // cast to IDL-generated version
        return winrt::com_array(pBuffer2, pBuffer2 + bufferLength);
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorResolution ResearchModeGyroFrame::GetResolution()
    {
        ::ResearchModeSensorResolution resolution;
        winrt::check_hresult(m_pSensorFrame->GetResolution(&resolution));
        return *(reinterpret_cast<ResearchModeSensorResolution*>(&resolution));
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp ResearchModeGyroFrame::GetTimeStamp()
    {
        ::ResearchModeSensorTimestamp timestamp;
        winrt::check_hresult(m_pSensorFrame->GetTimeStamp(&timestamp));
        return *(reinterpret_cast<ResearchModeSensorTimestamp*>(&timestamp));
    }
}
