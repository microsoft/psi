// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeImuSensor.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeImuSensor : ResearchModeImuSensorT<ResearchModeImuSensor>
    {
        // Implementation-only constructor
        ResearchModeImuSensor(::IResearchModeSensor* pSensor);

        void OpenStream();
        void CloseStream();
        hstring GetFriendlyName();
        winrt::HoloLens2ResearchMode::ResearchModeSensorType GetSensorType();
        winrt::HoloLens2ResearchMode::IResearchModeSensorFrame GetNextBuffer();

        winrt::com_ptr<::IResearchModeSensor> m_pSensor;
        ResearchModeSensorType m_sensorType;
    };
}
