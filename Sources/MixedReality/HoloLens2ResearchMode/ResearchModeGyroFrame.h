// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeGyroFrame.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeGyroFrame : ResearchModeGyroFrameT<ResearchModeGyroFrame>
    {
        ResearchModeGyroFrame(::IResearchModeSensorFrame* pSensorFrame);

        com_array<GyroDataStruct> GetCalibratedGyroSamples();

        winrt::HoloLens2ResearchMode::ResearchModeSensorResolution GetResolution();
        winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp GetTimeStamp();

        winrt::com_ptr<::IResearchModeSensorFrame> m_pSensorFrame;
        winrt::com_ptr<::IResearchModeGyroFrame> m_pGyroFrame;
    };
}
