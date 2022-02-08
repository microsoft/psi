// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeSensorDepthFrame.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeSensorDepthFrame : ResearchModeSensorDepthFrameT<ResearchModeSensorDepthFrame>
    {
        ResearchModeSensorDepthFrame(::IResearchModeSensorFrame* pSensorFrame);

        com_array<uint16_t> GetBuffer();
        com_array<uint16_t> GetAbDepthBuffer();
        com_array<uint8_t> GetSigmaBuffer();
        winrt::HoloLens2ResearchMode::ResearchModeSensorResolution GetResolution();
        winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp GetTimeStamp();

        winrt::com_ptr<::IResearchModeSensorFrame> m_pSensorFrame;
        winrt::com_ptr<::IResearchModeSensorDepthFrame> m_pDepthFrame;
    };
}
