// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeSensorDevice.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeSensorDevice : ResearchModeSensorDeviceT<ResearchModeSensorDevice>
    {
        ResearchModeSensorDevice();

        int32_t GetSensorCount();
        void DisableEyeSelection();
        void EnableEyeSelection();
        winrt::com_array<winrt::HoloLens2ResearchMode::ResearchModeSensorDescriptor> GetSensorDescriptors();
        winrt::HoloLens2ResearchMode::IResearchModeSensor GetSensor(winrt::HoloLens2ResearchMode::ResearchModeSensorType const& sensorType);
        winrt::guid GetRigNodeId();
        winrt::Windows::Foundation::IAsyncOperation<winrt::HoloLens2ResearchMode::ResearchModeSensorConsent> RequestCameraAccessAsync();
        winrt::Windows::Foundation::IAsyncOperation<winrt::HoloLens2ResearchMode::ResearchModeSensorConsent> RequestIMUAccessAsync();

        winrt::com_ptr<::IResearchModeSensorDevice> m_pSensorDevice;
        winrt::com_ptr<::IResearchModeSensorDeviceConsent> m_pSensorDeviceConsent;
    };
}

namespace winrt::HoloLens2ResearchMode::factory_implementation
{
    struct ResearchModeSensorDevice : ResearchModeSensorDeviceT<ResearchModeSensorDevice, implementation::ResearchModeSensorDevice>
    {
    };
}
