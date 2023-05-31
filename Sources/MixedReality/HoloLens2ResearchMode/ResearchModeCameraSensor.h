// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "ResearchModeCameraSensor.g.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    struct ResearchModeCameraSensor : ResearchModeCameraSensorT<ResearchModeCameraSensor>
    {
        // Implementation-only constructor
        ResearchModeCameraSensor(::IResearchModeSensor* pSensor);

        int32_t MapImagePointToCameraUnitPlane(winrt::Windows::Foundation::Point const& uv, winrt::Windows::Foundation::Point& xy) noexcept;
        int32_t MapCameraSpaceToImagePoint(winrt::Windows::Foundation::Point const& xy, winrt::Windows::Foundation::Point& uv) noexcept;
        winrt::Windows::Foundation::Numerics::float4x4 GetCameraExtrinsicsMatrix();
        void OpenStream();
        void CloseStream();
        hstring GetFriendlyName();
        winrt::HoloLens2ResearchMode::ResearchModeSensorType GetSensorType();
        winrt::HoloLens2ResearchMode::IResearchModeSensorFrame GetNextBuffer();

        winrt::com_ptr<::IResearchModeSensor> m_pSensor;
        winrt::com_ptr<::IResearchModeCameraSensor> m_pCameraSensor;
        ResearchModeSensorType m_sensorType;
    };
}
