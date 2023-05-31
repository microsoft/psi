// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeSensorVlcFrame.h"
#include "ResearchModeSensorVlcFrame.g.cpp"

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeSensorVlcFrame::ResearchModeSensorVlcFrame(::IResearchModeSensorFrame* pSensorFrame)
    {
        m_pSensorFrame.attach(pSensorFrame);
        m_pVlcFrame = m_pSensorFrame.as<::IResearchModeSensorVLCFrame>();
    }

    com_array<uint8_t> ResearchModeSensorVlcFrame::GetBuffer()
    {
        const BYTE* pBuffer = nullptr;
        size_t bufferLength = 0;

        HRESULT hr = m_pVlcFrame->GetBuffer(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        return winrt::com_array(pBuffer, pBuffer + bufferLength);
    }

    uint32_t ResearchModeSensorVlcFrame::GetGain()
    {
        UINT32 gain = 0;

        HRESULT hr = m_pVlcFrame->GetGain(&gain);
        winrt::check_hresult(hr);
        return gain;
    }

    uint64_t ResearchModeSensorVlcFrame::GetExposure()
    {
        UINT64 exposure = 0;

        HRESULT hr = m_pVlcFrame->GetExposure(&exposure);
        winrt::check_hresult(hr);
        return exposure;
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorResolution ResearchModeSensorVlcFrame::GetResolution()
    {
        ::ResearchModeSensorResolution resolution;
        winrt::check_hresult(m_pSensorFrame->GetResolution(&resolution));
        return *(reinterpret_cast<ResearchModeSensorResolution*>(&resolution));
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp ResearchModeSensorVlcFrame::GetTimeStamp()
    {
        ::ResearchModeSensorTimestamp timestamp;
        winrt::check_hresult(m_pSensorFrame->GetTimeStamp(&timestamp));
        return *(reinterpret_cast<ResearchModeSensorTimestamp*>(&timestamp));
    }
}
