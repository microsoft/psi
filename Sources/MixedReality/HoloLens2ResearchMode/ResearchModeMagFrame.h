// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeMagFrame.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeMagFrame : ResearchModeMagFrameT<ResearchModeMagFrame>
    {
        ResearchModeMagFrame(::IResearchModeSensorFrame* pSensorFrame);

        com_array<MagDataStruct> GetMagnetometerSamples();

        winrt::HoloLens2ResearchMode::ResearchModeSensorResolution GetResolution();
        winrt::HoloLens2ResearchMode::ResearchModeSensorTimestamp GetTimeStamp();

        winrt::com_ptr<::IResearchModeSensorFrame> m_pSensorFrame;
        winrt::com_ptr<::IResearchModeMagFrame> m_pMagFrame;
    };
}
