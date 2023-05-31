// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeImuSensor.h"
#include "ResearchModeImuSensor.g.cpp"
#include "ResearchModeAccelFrame.h"
#include "ResearchModeGyroFrame.h"
#include "ResearchModeMagFrame.h"

using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Numerics;

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeImuSensor::ResearchModeImuSensor(::IResearchModeSensor* pSensor)
    {
        m_pSensor.attach(pSensor);
        m_sensorType = static_cast<ResearchModeSensorType>(m_pSensor->GetSensorType());
    }

    void ResearchModeImuSensor::OpenStream()
    {
        winrt::check_hresult(m_pSensor->OpenStream());
    }

    void ResearchModeImuSensor::CloseStream()
    {
        winrt::check_hresult(m_pSensor->CloseStream());
    }

    hstring ResearchModeImuSensor::GetFriendlyName()
    {
        return m_pSensor->GetFriendlyName();
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorType ResearchModeImuSensor::GetSensorType()
    {
        return m_sensorType;
    }

    winrt::HoloLens2ResearchMode::IResearchModeSensorFrame ResearchModeImuSensor::GetNextBuffer()
    {
        ::IResearchModeSensorFrame* pSensorFrame = nullptr;
        HRESULT hr = m_pSensor->GetNextBuffer(&pSensorFrame);
        winrt::check_hresult(hr);

        switch (m_sensorType)
        {
            case ResearchModeSensorType::ImuAccel:
                return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeAccelFrame>(pSensorFrame);
            case ResearchModeSensorType::ImuGyro:
                return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeGyroFrame>(pSensorFrame);
            case ResearchModeSensorType::ImuMag:
                return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeMagFrame>(pSensorFrame);
            default:
                return nullptr;
        }
    }
}
