// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeAccelFrame.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeAccelFrame : ResearchModeAccelFrameT<ResearchModeAccelFrame>
    {
        ResearchModeAccelFrame(::IResearchModeSensorFrame* pSensorFrame);

        com_array<AccelDataStruct> GetCalibratedAccelarationSamples();

        winrt::HoloLens2ResearchMode::ResearchModeSensorResolution GetResolution();
        winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp GetTimeStamp();

        winrt::com_ptr<::IResearchModeSensorFrame> m_pSensorFrame;
        winrt::com_ptr<::IResearchModeAccelFrame> m_pAccelFrame;
    };
}
