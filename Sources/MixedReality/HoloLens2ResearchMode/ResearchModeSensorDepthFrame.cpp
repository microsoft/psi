// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeSensorDepthFrame.h"
#include "ResearchModeSensorDepthFrame.g.cpp"

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeSensorDepthFrame::ResearchModeSensorDepthFrame(::IResearchModeSensorFrame* pSensorFrame)
    {
        m_pSensorFrame.attach(pSensorFrame);
        m_pDepthFrame = m_pSensorFrame.as<::IResearchModeSensorDepthFrame>();
    }

    com_array<uint16_t> ResearchModeSensorDepthFrame::GetBuffer()
    {
        const UINT16* pBuffer = nullptr;
        size_t bufferLength = 0;

        HRESULT hr = m_pDepthFrame->GetBuffer(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        return winrt::com_array(pBuffer, pBuffer + bufferLength);
    }

    com_array<uint16_t> ResearchModeSensorDepthFrame::GetAbDepthBuffer()
    {
        const UINT16* pBuffer = nullptr;
        size_t bufferLength = 0;

        HRESULT hr = m_pDepthFrame->GetAbDepthBuffer(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        return winrt::com_array(pBuffer, pBuffer + bufferLength);
    }

    com_array<uint8_t> ResearchModeSensorDepthFrame::GetSigmaBuffer()
    {
        const BYTE* pBuffer = nullptr;
        size_t bufferLength = 0;

        HRESULT hr = m_pDepthFrame->GetSigmaBuffer(&pBuffer, &bufferLength);
        winrt::check_hresult(hr);
        return winrt::com_array(pBuffer, pBuffer + bufferLength);
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorResolution ResearchModeSensorDepthFrame::GetResolution()
    {
        ::ResearchModeSensorResolution resolution;
        winrt::check_hresult(m_pSensorFrame->GetResolution(&resolution));
        return *(reinterpret_cast<ResearchModeSensorResolution*>(&resolution));
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp ResearchModeSensorDepthFrame::GetTimeStamp()
    {
        ::ResearchModeSensorTimestamp timestamp;
        winrt::check_hresult(m_pSensorFrame->GetTimeStamp(&timestamp));
        return *(reinterpret_cast<ResearchModeSensorTimestamp*>(&timestamp));
    }
}
