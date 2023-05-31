//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

#pragma once

#include <windows.h>
#include <initguid.h>

#include <comdef.h>
#include <initguid.h>

#include <vector>
#include <cstring>
#include <string>

#include <DirectXMath.h>

interface IResearchModeCameraSensor;
interface IResearchModeSensor;
interface IResearchModeSensorFrame;

struct AccelDataStruct
{
    uint64_t VinylHupTicks;
    uint64_t SocTicks;
    float AccelValues[3];
    float temperature;
};

struct GyroDataStruct
{
    uint64_t VinylHupTicks;
    uint64_t SocTicks;
    float GyroValues[3];
    float temperature;
};

struct MagDataStruct
{
    uint64_t VinylHupTicks;
    uint64_t SocTicks;
    float MagValues[3];
};

enum ResearchModeSensorType
{
    LEFT_FRONT,
    LEFT_LEFT,
    RIGHT_FRONT,
    RIGHT_RIGHT,
    DEPTH_AHAT,
    DEPTH_LONG_THROW,
    IMU_ACCEL,
    IMU_GYRO,
    IMU_MAG
};

struct ResearchModeSensorDescriptor
{
    LUID sensorId;
    ResearchModeSensorType sensorType;
};

enum ResearchModeSensorTimestampSource
{
    SensorTimestampSource_USB_SOF = 0,
    SensorTimestampSource_Unknown = 1,
    SensorTimestampSource_CenterOfExposure = 2,
    SensorTimestampSource_Count = 3
};

struct ResearchModeSensorTimestamp {
    ResearchModeSensorTimestampSource Source;
    UINT64 SensorTicks;
    UINT64 SensorTicksPerSecond;
    UINT64 HostTicks;
    UINT64 HostTicksPerSecond;
};

struct ResearchModeSensorResolution {
    UINT32 Width;
    UINT32 Height;
    UINT32 Stride;
    UINT32 BitsPerPixel;
    UINT32 BytesPerPixel;
};

enum ResearchModeSensorConsent {
    DeniedBySystem = 0,
    NotDeclaredByApp = 1,
    DeniedByUser = 2,
    UserPromptRequired = 3,
    Allowed = 4
};

DECLARE_INTERFACE_IID_(IResearchModeSensorDevice, IUnknown, "65E8CC3C-3A03-4006-AE0D-34E1150058CC")
{
    STDMETHOD(DisableEyeSelection()) = 0;
    STDMETHOD(EnableEyeSelection()) = 0;

    STDMETHOD(GetSensorCount(
        _Out_ size_t *pOutCount)) = 0;
    STDMETHOD(GetSensorDescriptors(
        _Out_writes_(sensorCount) ResearchModeSensorDescriptor *pSensorDescriptorData,
        size_t sensorCount,
        _Out_ size_t *pOutCount)) = 0;
    STDMETHOD(GetSensor(
        ResearchModeSensorType sensorType,
        _Outptr_result_nullonfailure_ IResearchModeSensor **ppSensor)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeSensorDevicePerception, IUnknown, "C1678F4B-ECB4-47A8-B6FA-97DBF4417DB2")
{
    STDMETHOD(GetRigNodeId(
        _Outptr_ GUID *pRigNodeId)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeSensorDeviceConsent, IUnknown, "EAB9D672-9A88-4E43-8A69-9BA8f23A4C76")
{
    STDMETHOD_(HRESULT, RequestCamAccessAsync)(void (*camCallback)(ResearchModeSensorConsent))= 0;
    STDMETHOD_(HRESULT, RequestIMUAccessAsync)(void (*imuCallback)(ResearchModeSensorConsent)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeSensor, IUnknown, "4D4D1D4B-9FDD-4001-BA1E-F8FAB1DA14D0")
{
    STDMETHOD(OpenStream()) = 0;
    STDMETHOD(CloseStream()) = 0;
    STDMETHOD_(LPCWSTR, GetFriendlyName)() = 0;
    STDMETHOD_(ResearchModeSensorType, GetSensorType)() = 0;

    STDMETHOD(GetSampleBufferSize(
        _Out_ size_t *pSampleBufferSize)) = 0;
    STDMETHOD(GetNextBuffer(
        _Outptr_result_nullonfailure_ IResearchModeSensorFrame **ppSensorFrame)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeCameraSensor, IUnknown, "3BDB4977-960B-4F5D-8CA3-D21E68F26E76")
{
    STDMETHOD(MapImagePointToCameraUnitPlane(
        float (&uv) [2],
        float (&xy) [2])) = 0;
    STDMETHOD(MapCameraSpaceToImagePoint(
        float(&xy)[2],
        float(&uv)[2])) = 0;
    STDMETHOD(GetCameraExtrinsicsMatrix(DirectX::XMFLOAT4X4 *pCameraViewMatrix)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeAccelSensor, IUnknown, "627A7FAA-55EA-4951-B370-26186395AAB5")
{
    STDMETHOD(GetExtrinsicsMatrix(DirectX::XMFLOAT4X4 *pAccel)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeGyroSensor, IUnknown, "E6E8B36F-E6E7-494C-B4A8-7CFA2561BEE7")
{
    STDMETHOD(GetExtrinsicsMatrix(DirectX::XMFLOAT4X4 *pGyro)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeMagSensor, IUnknown, "CB082E34-1C69-445D-A91A-43CE96B3655E")
{
};

DECLARE_INTERFACE_IID_(IResearchModeDepthSensor, IUnknown, "CC317D10-C26E-45B2-B91B-0E4571486CEC")
{
};

DECLARE_INTERFACE_IID_(IResearchModeSensorFrame, IUnknown, "73479614-89C9-4FFD-9C16-615BC32C6A09")
{
    STDMETHOD(GetResolution(
        _Out_ ResearchModeSensorResolution *pResolution)) = 0;
    // For frames with batched samples this returns the time stamp for the first sample in the frame.
    STDMETHOD(GetTimeStamp(
        _Out_ ResearchModeSensorTimestamp *pTimeStamp)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeSensorVLCFrame, IUnknown, "5C693123-3851-4FDC-A2D9-51C68AF53976")
{
    STDMETHOD(GetBuffer(
        _Outptr_ const BYTE **ppBytes,
        _Out_ size_t *pBufferOutLength)) = 0;
    STDMETHOD(GetGain(
        _Out_ UINT32 *pGain)) = 0;
    STDMETHOD(GetExposure(
        _Out_ UINT64 *pExposure)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeSensorDepthFrame, IUnknown, "35167E38-E020-43D9-898E-6CB917AD86D3")
{
    STDMETHOD(GetBuffer(
        _Outptr_ const UINT16 **ppBytes,
        _Out_ size_t *pBufferOutLength)) = 0;
    STDMETHOD(GetAbDepthBuffer(
        _Outptr_ const UINT16 **ppBytes,
        _Out_ size_t *pBufferOutLength)) = 0;
    STDMETHOD(GetSigmaBuffer(
        _Outptr_ const BYTE **ppBytes,
        _Out_ size_t *pBufferOutLength)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeAccelFrame, IUnknown, "42AA75F8-E3FE-4C25-88C6-F2ECE1E8A2C5")
{
    STDMETHOD(GetCalibratedAccelaration(
        _Out_ DirectX::XMFLOAT3 *pAccel)) = 0;
    STDMETHOD(GetCalibratedAccelarationSamples(
        _Outptr_ const AccelDataStruct **ppAccelBuffer,
        _Out_ size_t *pBufferOutLength)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeGyroFrame, IUnknown, "4C0C5EE7-CBB8-4A15-A81F-943785F524A6")
{
    STDMETHOD(GetCalibratedGyro(
        _Out_ DirectX::XMFLOAT3 *pGyro)) = 0;
    STDMETHOD(GetCalibratedGyroSamples(
        _Outptr_ const GyroDataStruct **ppAccelBuffer,
        _Out_ size_t *pBufferOutLength)) = 0;
};

DECLARE_INTERFACE_IID_(IResearchModeMagFrame, IUnknown, "2376C9D2-7F3D-456E-A39E-3B7730DDA9E5")
{
    STDMETHOD(GetMagnetometer(
        _Out_ DirectX::XMFLOAT3 *pMag)) = 0;
    STDMETHOD(GetMagnetometerSamples(
        _Outptr_ const MagDataStruct **ppMagBuffer,
        _Out_ size_t *pBufferOutLength)) = 0;
};

HRESULT CreateResearchModeSensorDevice(
    _Outptr_result_nullonfailure_ IResearchModeSensorDevice **ppSensorDevice);

