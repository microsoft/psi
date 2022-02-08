// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include "ResearchModeCameraSensor.h"
#include "ResearchModeCameraSensor.g.cpp"
#include "ResearchModeSensorDepthFrame.h"
#include "ResearchModeSensorVlcFrame.h"

using namespace winrt::Windows::Foundation;
using namespace winrt::Windows::Foundation::Numerics;

namespace winrt::HoloLens2ResearchMode::implementation
{
    ResearchModeCameraSensor::ResearchModeCameraSensor(::IResearchModeSensor* pSensor)
    {
        m_pSensor.attach(pSensor);
        m_pCameraSensor = m_pSensor.as<::IResearchModeCameraSensor>();
        m_sensorType = static_cast<ResearchModeSensorType>(m_pSensor->GetSensorType());
    }

    int32_t ResearchModeCameraSensor::MapImagePointToCameraUnitPlane(winrt::Windows::Foundation::Point const& uv, winrt::Windows::Foundation::Point& xy) noexcept
    {
        float xyVal[2];
        HRESULT hr = m_pCameraSensor->MapImagePointToCameraUnitPlane(*reinterpret_cast<float(*)[2]>(&const_cast<Point&>(uv)), xyVal);
        xy = Point(xyVal[0], xyVal[1]);
        return hr;
    }

    int32_t ResearchModeCameraSensor::MapCameraSpaceToImagePoint(winrt::Windows::Foundation::Point const& xy, winrt::Windows::Foundation::Point& uv) noexcept
    {
        float uvVal[2];
        HRESULT hr = m_pCameraSensor->MapCameraSpaceToImagePoint(*reinterpret_cast<float(*)[2]>(&const_cast<Point&>(xy)), uvVal);
        uv = Point(uvVal[0], uvVal[1]);
        return hr;
    }

    winrt::Windows::Foundation::Numerics::float4x4 ResearchModeCameraSensor::GetCameraExtrinsicsMatrix()
    {
        float4x4 cameraViewMatrix;
        HRESULT hr = m_pCameraSensor->GetCameraExtrinsicsMatrix(reinterpret_cast<DirectX::XMFLOAT4X4*>(&cameraViewMatrix));
        winrt::check_hresult(hr);
        return cameraViewMatrix;
    }

    void ResearchModeCameraSensor::OpenStream()
    {
        winrt::check_hresult(m_pSensor->OpenStream());
    }

    void ResearchModeCameraSensor::CloseStream()
    {
        winrt::check_hresult(m_pSensor->CloseStream());
    }

    hstring ResearchModeCameraSensor::GetFriendlyName()
    {
        return m_pSensor->GetFriendlyName();
    }

    winrt::HoloLens2ResearchMode::ResearchModeSensorType ResearchModeCameraSensor::GetSensorType()
    {
        return m_sensorType;
    }

    winrt::HoloLens2ResearchMode::IResearchModeSensorFrame ResearchModeCameraSensor::GetNextBuffer()
    {
        ::IResearchModeSensorFrame* pSensorFrame = nullptr;
        HRESULT hr = m_pSensor->GetNextBuffer(&pSensorFrame);
        winrt::check_hresult(hr);

        switch (m_sensorType)
        {
            case ResearchModeSensorType::DepthAhat:
            case ResearchModeSensorType::DepthLongThrow:
                return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeSensorDepthFrame>(pSensorFrame);

            case ResearchModeSensorType::LeftFront:
            case ResearchModeSensorType::LeftLeft:
            case ResearchModeSensorType::RightFront:
            case ResearchModeSensorType::RightRight:
                return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeSensorVlcFrame>(pSensorFrame);

            default:
                return nullptr;
        }
    }
}
