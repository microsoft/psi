// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeAccelFrame.h"
#include "ResearchModeAccelFrame.g.cpp"

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeAccelFrame::ResearchModeAccelFrame(::IResearchModeSensorFrame* pSensorFrame)
    {
        m_pSensorFrame.attach(pSensorFrame);
        m_pAccelFrame = m_pSensorFrame.as<::IResearchModeAccelFrame>();
    }

	com_array<AccelDataStruct> ResearchModeAccelFrame::GetCalibratedAccelarationSamples()
    {
        const ::AccelDataStruct* pBuffer = nullptr; // note: this is the non-IDL-generated version from ResearchModeApi.h
        size_t bufferLength = 0;
        HRESULT hr = m_pAccelFrame->GetCalibratedAccelarationSamples(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        const AccelDataStruct* pBuffer2 = (AccelDataStruct*)(pBuffer); // cast to IDL-generated version
        return winrt::com_array(pBuffer2, pBuffer2 + bufferLength);
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorResolution ResearchModeAccelFrame::GetResolution()
    {
        ::ResearchModeSensorResolution resolution;
        winrt::check_hresult(m_pSensorFrame->GetResolution(&resolution));
        return *(reinterpret_cast<ResearchModeSensorResolution*>(&resolution));
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp ResearchModeAccelFrame::GetTimeStamp()
    {
        ::ResearchModeSensorTimestamp timestamp;
        winrt::check_hresult(m_pSensorFrame->GetTimeStamp(&timestamp));
        return *(reinterpret_cast<ResearchModeSensorTimestamp*>(&timestamp));
    }
}
