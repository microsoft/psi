// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "pch.h"
#include <winrt/Windows.ApplicationModel.h>
#include <winrt/Windows.UI.Core.h>
#include "ResearchModeSensorDevice.h"
#include "ResearchModeSensorDevice.g.cpp"
#include "ResearchModeCameraSensor.h"
#include "ResearchModeImuSensor.h"
#include "ResearchModeApi.h"

namespace winrt::HoloLens2ResearchMode::implementation
{
    extern "C"
        HMODULE LoadLibraryA(
            LPCSTR lpLibFileName
        );

    // The following statics are to support the callback from IResearchModeSensorDeviceConsent::RequestCam/IMUAccessAsync()
    static ResearchModeSensorConsent camAccessCheck = ResearchModeSensorConsent::DeniedBySystem;
    static ResearchModeSensorConsent imuAccessCheck = ResearchModeSensorConsent::DeniedBySystem;
    static HANDLE camConsentGiven = CreateEvent(nullptr, true, false, nullptr);
    static HANDLE imuConsentGiven = CreateEvent(nullptr, true, false, nullptr);

    ResearchModeSensorDevice::ResearchModeSensorDevice()
    {
        // Load Research Mode library
        HMODULE hrResearchMode = LoadLibraryA("ResearchModeAPI");
        winrt::check_pointer(hrResearchMode);

        typedef HRESULT(__cdecl* PFN_CREATEPROVIDER) (::IResearchModeSensorDevice** ppSensorDevice);

        PFN_CREATEPROVIDER pfnCreate = reinterpret_cast<PFN_CREATEPROVIDER>(GetProcAddress(hrResearchMode, "CreateResearchModeSensorDevice"));
        winrt::check_pointer(pfnCreate);

        HRESULT hr = pfnCreate(m_pSensorDevice.put());
        winrt::check_hresult(hr);

        m_pSensorDeviceConsent = m_pSensorDevice.as<::IResearchModeSensorDeviceConsent>();
        
        winrt::check_pointer(camConsentGiven);
    }

    int32_t ResearchModeSensorDevice::GetSensorCount()
    {
        size_t sensorCount = 0;
        HRESULT hr = m_pSensorDevice->GetSensorCount(&sensorCount);
        winrt::check_hresult(hr);
        return static_cast<int32_t>(sensorCount);
    }

    void ResearchModeSensorDevice::DisableEyeSelection()
    {
        winrt::check_hresult(m_pSensorDevice->DisableEyeSelection());
    }

    void ResearchModeSensorDevice::EnableEyeSelection()
    {
        winrt::check_hresult(m_pSensorDevice->EnableEyeSelection());
    }

    winrt::com_array<winrt::HoloLens2ResearchMode::ResearchModeSensorDescriptor> ResearchModeSensorDevice::GetSensorDescriptors()
    {
        size_t sensorCount = 0;

        HRESULT hr = m_pSensorDevice->GetSensorCount(&sensorCount);
        winrt::check_hresult(hr);

        std::vector<ResearchModeSensorDescriptor> sensorDescriptors;
        sensorDescriptors.resize(sensorCount);

        hr = m_pSensorDevice->GetSensorDescriptors(reinterpret_cast<::ResearchModeSensorDescriptor*>(sensorDescriptors.data()), sensorDescriptors.size(), &sensorCount);
        winrt::check_hresult(hr);

        return winrt::com_array(sensorDescriptors);
    }

    winrt::HoloLens2ResearchMode::IResearchModeSensor ResearchModeSensorDevice::GetSensor(winrt::HoloLens2ResearchMode::ResearchModeSensorType const& sensorType)
    {
        ::IResearchModeSensor* pSensor = nullptr;

        HRESULT hr = m_pSensorDevice->GetSensor((::ResearchModeSensorType)sensorType, &pSensor);
        winrt::check_hresult(hr);

        switch (sensorType)
        {
        case ResearchModeSensorType::LeftFront:
        case ResearchModeSensorType::LeftLeft:
        case ResearchModeSensorType::RightFront:
        case ResearchModeSensorType::RightRight:
        case ResearchModeSensorType::DepthAhat:
        case ResearchModeSensorType::DepthLongThrow:
            return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeCameraSensor>(pSensor);
            break;

        case ResearchModeSensorType::ImuAccel:
        case ResearchModeSensorType::ImuGyro:
        case ResearchModeSensorType::ImuMag:
            return winrt::make<winrt::HoloLens2ResearchMode::implementation::ResearchModeImuSensor>(pSensor);

        default:
            throw winrt::hresult_invalid_argument();
        }
    }

    winrt::guid ResearchModeSensorDevice::GetRigNodeId()
    {
        GUID rigNodeGuid;
        auto pSensorDevicePerception = m_pSensorDevice.as<::IResearchModeSensorDevicePerception>();
        HRESULT hr = pSensorDevicePerception->GetRigNodeId(&rigNodeGuid);
        winrt::check_hresult(hr);
        return rigNodeGuid;
    }

    winrt::Windows::Foundation::IAsyncOperation<winrt::HoloLens2ResearchMode::ResearchModeSensorConsent> ResearchModeSensorDevice::RequestCameraAccessAsync()
    {
        // Check if consent already obtained
        if (WaitForSingleObject(camConsentGiven, 0) == WAIT_OBJECT_0)
        {
            co_return camAccessCheck;
        }

        winrt::check_hresult(m_pSensorDeviceConsent->RequestCamAccessAsync(
            [](::ResearchModeSensorConsent consent)
            {
                camAccessCheck = static_cast<ResearchModeSensorConsent>(consent);
                SetEvent(camConsentGiven);
            }));
        
        // Return control to the caller and wait for the result
        co_await winrt::resume_background();

        if (WaitForSingleObject(camConsentGiven, INFINITE) != WAIT_OBJECT_0)
        {
            winrt::throw_last_error();
        }

        co_return camAccessCheck;
    }

    winrt::Windows::Foundation::IAsyncOperation<winrt::HoloLens2ResearchMode::ResearchModeSensorConsent> ResearchModeSensorDevice::RequestIMUAccessAsync()
    {
        // Check if consent already obtained
        if (WaitForSingleObject(imuConsentGiven, 0) == WAIT_OBJECT_0)
        {
            co_return imuAccessCheck;
        }

        winrt::check_hresult(m_pSensorDeviceConsent->RequestIMUAccessAsync(
            [](::ResearchModeSensorConsent consent)
            {
                imuAccessCheck = static_cast<ResearchModeSensorConsent>(consent);
                SetEvent(imuConsentGiven);
            }));
        
        // Return control to the caller and wait for the result
        co_await winrt::resume_background();

        if (WaitForSingleObject(imuConsentGiven, INFINITE) != WAIT_OBJECT_0)
        {
            winrt::throw_last_error();
        }

        co_return imuAccessCheck;
    }
}
