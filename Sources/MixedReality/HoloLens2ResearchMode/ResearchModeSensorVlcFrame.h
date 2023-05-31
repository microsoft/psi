// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeSensorVlcFrame.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeSensorVlcFrame : ResearchModeSensorVlcFrameT<ResearchModeSensorVlcFrame>
    {
        ResearchModeSensorVlcFrame(::IResearchModeSensorFrame* pSensorFrame);

        com_array<uint8_t> GetBuffer();
        uint32_t GetGain();
        uint64_t GetExposure();
        winrt::HoloLens2ResearchMode::ResearchModeSensorResolution GetResolution();
        winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp GetTimeStamp();

        winrt::com_ptr<::IResearchModeSensorFrame> m_pSensorFrame;
        winrt::com_ptr<::IResearchModeSensorVLCFrame> m_pVlcFrame;
    };
}
