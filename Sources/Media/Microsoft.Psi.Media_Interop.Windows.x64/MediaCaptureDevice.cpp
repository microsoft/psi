// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"
using namespace System::Runtime::InteropServices;

#include "MediaCaptureDevice.h"
#include "RGBCameraEnumerator.h"
#include "RGBCameraCollection.h"
#include <VersionHelpers.h>

#include "ks.h"
#include "ksmedia.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

/// <summary>
///  ctor that initializes using an IMFActivate for the capture device.
///  This is called when the enumerator creates devices.
/// </summary>
/// <param name="pActivate">The IMFActivate to create a device from</param>
MediaCaptureDevice::MediaCaptureDevice(IMFActivate *pActivate)
{
    MF_THROWPTR(pActivate);
    
    m_pSourceReader = NULL;
    _pMediaSource = NULL;
    
    m_readSampleHandlerHandle = nullptr;

    InitializeFromActivate(pActivate, nullptr);

    InitilaizePerformanceCounterFrequency();
}

/// <summary>
///  ctor that initializes using the symbolic link for the device. This allows
///  construction without going through the enumerator.
/// </summary>
/// <param name="name">The name to use for the device</param>
/// <param name="symbolicLink">The device path of the capture device</param>
MediaCaptureDevice::MediaCaptureDevice(String^ name, String^ symbolicLink, bool useInSharedMode)
{
    m_pSourceReader = NULL;
    _pMediaSource = NULL;

    IMFActivate *pActivate = NULL;

    try
    {
        pActivate = GetActivate(symbolicLink, useInSharedMode);

        InitializeFromActivate(pActivate, name);

        InitilaizePerformanceCounterFrequency();
    }
    finally
    {
        MF_RELEASE(pActivate);
    }
}

/// <summary>
/// dtor. Always shut down the capture device.
/// </summary>
MediaCaptureDevice::~MediaCaptureDevice()
{
    Shutdown();
}

/// <summary>
/// Initializes the performance counter frequency
/// </summary>
void MediaCaptureDevice::InitilaizePerformanceCounterFrequency()
{
    __int64 freq;
    QueryPerformanceFrequency((LARGE_INTEGER*)&freq);
    m_performanceCounterFrequency = freq;
}

/// <summary>
///     Initializes the friendly name and symbolic link of the device from the activate object
/// </summary>
/// <param name="pActivate">  The activate object representing the device </param>
/// <param name="name">  Friendly name </param>
void MediaCaptureDevice::InitializeFromActivate(IMFActivate *pActivate, String^ name)
{
    if (String::IsNullOrEmpty(name))
    {
        m_name = MediaFoundationUtility::GetStringProperty(pActivate, MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME);
    }
    else
    {
        m_name = name;
    }

    m_symbolicLink = MediaFoundationUtility::GetStringProperty(pActivate, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK);
}

// This is a temporary work around until MF_DEVICESTREAM_FRAMESERVER_SHARED is made public in RS4 (it's currently internal in RS3)
// TODO: Remove this attribute after it becomes public (in the RS4 release of Windows)
EXTERN_GUID(MF_DEVSOURCE_ATTRIBUTE_FRAMESERVER_SHARE_MODE, 0x44d1a9bc, 0x2999, 0x4238, 0xae, 0x43, 0x7, 0x30, 0xce, 0xb2, 0xab, 0x1b);

/// <summary>
/// Uses the symbolic link or device path for a capture device to create an  activate object.
/// </summary>
/// <param name="symbolicLink"> Symbolic Link </param>
IMFActivate *MediaCaptureDevice::GetActivate(String^ symbolicLink, bool useInSharedMode)
{
    HRESULT hr = S_OK;
    IMFAttributes *pAttributes = NULL;
    IMFActivate *pActivate = NULL;
    IntPtr link = IntPtr::Zero;

    try
    {
        hr = MFCreateAttributes(&pAttributes, 2);
        MF_THROWHR(hr);

        hr = pAttributes->SetGUID(
            MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
            MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
        MF_THROWHR(hr);
		
		if (useInSharedMode)
		{
			hr = pAttributes->SetUINT32(
				MF_DEVSOURCE_ATTRIBUTE_FRAMESERVER_SHARE_MODE /* Replace with MF_DEVICESTREAM_FRAMESERVER_SHARED once it is public in RS4 release of Windows */,
				1);
			MF_THROWHR(hr);
		}

        link = Marshal::StringToCoTaskMemUni(symbolicLink);

        hr = pAttributes->SetString(
            MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
            (LPCWSTR)link.ToPointer());
        MF_THROWHR(hr);

        hr = MFCreateDeviceSourceActivate(pAttributes, &pActivate);
        MF_THROWHR(hr);
    }
    finally
    {
        Marshal::FreeCoTaskMem(link);
        MF_RELEASE(pAttributes);
    }

    return pActivate;
}

/// <summary>
///  Attaches to the underlying device.
/// </summary>
/// <returns>True if succesfully attached, false otherwise</returns>
bool MediaCaptureDevice::Attach(bool useInSharedMode)
{
    if (m_pSourceReader != NULL)
    {
        // already attached
        return TRUE;
    }

    HRESULT hr = S_OK;
    IMFActivate *pActivate = NULL;
    IMFMediaSource *pMediaSource = NULL;
    IMFSourceReader *pSourceReader = NULL;
    IMFAttributes *pAttributes = NULL;

    try
    {
        pin_ptr<SourceReaderCallback*> p = &m_callback;
        hr = SourceReaderCallback::CreateInstance(p);
        MF_THROWHR(hr);

        m_readSampleDelegateInternal = gcnew ReadSampleDelegate(this, &MediaCaptureDevice::ReadSampleThunk);

        // pin this callback in memory
        m_readSampleHandlerHandle = GCHandle::Alloc(m_readSampleDelegateInternal);

        pActivate = GetActivate(m_symbolicLink, useInSharedMode);

        hr = MFCreateAttributes(&pAttributes, 2);
        MF_THROWHR(hr);

		// If we are running on Windows 8.0+ then we can use the MFTs to do color conversion
		if (IsWindows8OrGreater())
		{
			hr = pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING, TRUE);
			MF_THROWHR(hr);
			hr = pAttributes->SetUINT32(MF_READWRITE_DISABLE_CONVERTERS, FALSE);
			MF_THROWHR(hr);
		}
		else
		{
			hr = pAttributes->SetUINT32(MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, TRUE);
			MF_THROWHR(hr);
		}


		hr = pAttributes->SetUnknown(MF_SOURCE_READER_ASYNC_CALLBACK, m_callback);
        MF_THROWHR(hr);

        hr = pActivate->ActivateObject(IID_IMFMediaSource, (void **)&pMediaSource);
        MF_THROWHR(hr);

        hr = MFCreateSourceReaderFromMediaSource(pMediaSource, pAttributes, &pSourceReader);
        MF_THROWHR(hr);

        hr = pSourceReader->SetStreamSelection((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, TRUE);
        MF_THROWHR(hr);

        m_callback->SetSourceReader(pSourceReader);

        m_pSourceReader = pSourceReader;
        m_pSourceReader->AddRef();

        _pMediaSource = pMediaSource;
        _pMediaSource->AddRef();

        m_pActivate = pActivate;
        m_pActivate->AddRef();

        m_lastFrameTime = 0;
    }
    catch (Object^)
    {
        Shutdown();
        return false;
    }
    finally
    {
        MF_RELEASE(pAttributes);
        MF_RELEASE(pActivate);
        MF_RELEASE(pMediaSource);
        MF_RELEASE(pSourceReader);
    }
    return true;
}

/// <summary>
/// Thunks from Managed to unmanaged code
/// </summary>
/// <param name="data"> Image data as RGB24 byte array </param>
/// <param name="cbLength"> Length of Image data in bytes </param>
/// <param name="timestamp"> Timestamp of sample </param>
void MediaCaptureDevice::ReadSampleThunk(IntPtr pbData, int cbLength, LONGLONG timestamp)
{
    if (m_readSampleCallback != nullptr)
    {
        m_readSampleCallback(pbData, cbLength, timestamp);
    }
}


/// <summary>
///  Detaches from the underlying source reader device.
/// </summary>  
void MediaCaptureDevice::Shutdown()
{
    if (_pMediaSource)
    {
        _pMediaSource->Shutdown();
    }

    MF_RELEASE(m_pSourceReader);
    MF_RELEASE(_pMediaSource);
    MF_RELEASE(m_pActivate);
    MF_RELEASE(m_callback);

    if (m_readSampleHandlerHandle != nullptr)
    {
        m_readSampleHandlerHandle->Free();
        m_readSampleHandlerHandle = nullptr;
    }
}


/// <summary>
///  Gets the list of capture formats supported
/// </summary> 
/// <returns>List of supported capture formats</returns>
IEnumerable<CaptureFormat^>^ MediaCaptureDevice::Formats::get()
{
    HRESULT hr = S_OK;
    IMFMediaType *pMediaType = NULL;
    List<CaptureFormat^>^ list = gcnew List<CaptureFormat^>();
    CaptureFormat^ curr = nullptr;

    if (!fAttached)
    {
        return list;
    }

    try
    {
        for (DWORD dwType = 0; ; dwType++)
        {
            MF_RELEASE(pMediaType);

            hr = m_pSourceReader->GetNativeMediaType(
                (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                dwType,
                &pMediaType);
            if (hr == MF_E_NO_MORE_TYPES)
            {
                hr = S_OK;
                break;
            }
            MF_THROWHR(hr);

            curr = CaptureFormat::FromMediaType(pMediaType);

            bool fExists = false;
            for each (CaptureFormat^ existing in list)
            {
                if (existing->nWidth == curr->nWidth &&
                    existing->nHeight == curr->nHeight &&
                    existing->nFrameRateNumerator == curr->nFrameRateNumerator &&
                    existing->nFrameRateDenominator == curr->nFrameRateDenominator &&
                    existing->subType->Guid == curr->subType->Guid)
                {
                    fExists = true;
                    break;
                }
            }
            if (!fExists)
            {
                list->Add(curr);
            }
        }
    }
    finally
    {
        MF_RELEASE(pMediaType);
    }

    return list;
}

/// <summary>
///  Gets the list of video property values supported
/// </summary>        
/// <returns>List of supported video property values</returns>
IEnumerable<VideoPropertyValue^>^ MediaCaptureDevice::VideoProperties::get()
{
    HRESULT hr = S_OK;
    List<VideoPropertyValue^>^ list = gcnew List<VideoPropertyValue^>();
    IAMVideoProcAmp *pProcAmp = NULL;

    if (!fAttached)
    {
        return list;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pProcAmp));
        if (SUCCEEDED(hr))
        {
            for (int i = VideoProcAmp_Brightness; i <= VideoProcAmp_Gain; i++)
            {
                long lValue = 0;
                long lFlags = 0;

                hr = pProcAmp->Get(i, &lValue, &lFlags);
                if (FAILED(hr))
                {
                    continue;
                }

                long lMin = 0;
                long lMax = 0;
                long lSteppingDelta = 0;
                long lDefaultValue = 0;

                hr = pProcAmp->GetRange(i, &lMin, &lMax, &lSteppingDelta, &lDefaultValue, &lFlags);
                if (FAILED(hr))
                {
                    continue;
                }

                VideoPropertyValue^ prop = gcnew VideoPropertyValue();

                prop->m_Property = (VideoProperty)i;
                prop->nValue = lValue;
                prop->nMinimum = lMin;
                prop->nMaximum = lMax;
                prop->nSteppingDelta = lSteppingDelta;
                prop->nDefault = lDefaultValue;
                prop->m_Flags = (VideoPropertyFlags)lFlags;

                list->Add(prop);
            }
        }
    }
    finally
    {
        MF_RELEASE(pProcAmp);
    }

    return list;
}

/// <summary>
///  Gets the list of camera control property values supported
/// </summary>        
/// <returns>List of supported camera control property values</returns>
IEnumerable<ManagedCameraControlPropertyValue^>^ MediaCaptureDevice::ManagedCameraControlProperties::get()
{
    HRESULT hr = S_OK;
    List<ManagedCameraControlPropertyValue^>^ list = gcnew List<ManagedCameraControlPropertyValue^>();
    IAMCameraControl *pCameraControl  = NULL;

    if (!fAttached)
    {
        return list;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pCameraControl));
        if (SUCCEEDED(hr))
        {
            for (int i = KSPROPERTY_CAMERACONTROL_PAN; i <= KSPROPERTY_CAMERACONTROL_AUTO_EXPOSURE_PRIORITY; i++)
            {
                long lValue = 0;
                long lFlags = 0;

                hr = pCameraControl->Get(i, &lValue, &lFlags);
                if (FAILED(hr))
                {
                    continue;
                }

                long lMin = 0;
                long lMax = 0;
                long lSteppingDelta = 0;
                long lDefaultValue = 0;

                hr = pCameraControl->GetRange(i, &lMin, &lMax, &lSteppingDelta, &lDefaultValue, &lFlags);
                if (FAILED(hr))
                {
                    continue;
                }

                ManagedCameraControlPropertyValue^ prop = gcnew ManagedCameraControlPropertyValue();

                prop->m_Property = (ManagedCameraControlProperty)i;
                prop->nValue = lValue;
                prop->nMinimum = lMin;
                prop->nMaximum = lMax;
                prop->nSteppingDelta = lSteppingDelta;
                prop->nDefault = lDefaultValue;
                prop->m_Flags = (ManagedCameraControlPropertyFlags)lFlags;

                list->Add(prop);
            }
        }
    }
    finally
    {
        MF_RELEASE(pCameraControl);
    }

    return list;
}


/// <summary>
///  Sets the value for the specified property with the given flags
/// </summary>        
/// <param name="prop">Property to be set</param>
/// <param name="nValue">Value of the property</param>
/// <param name="flags">Flags corresponding to the property</param>
/// <returns>True if the property was set, false otherwise</returns>
bool MediaCaptureDevice::SetProperty(VideoProperty prop, int nValue, VideoPropertyFlags flags)
{
    HRESULT hr = S_OK;
    IAMVideoProcAmp *pProcAmp = NULL;

    if (!fAttached)
    {
        return false;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pProcAmp));
        if (SUCCEEDED(hr))
        {
            hr = pProcAmp->Set((long)prop, nValue, (long)flags);
        }
    }
    finally
    {
        MF_RELEASE(pProcAmp);
    }

    return SUCCEEDED(hr);
}

bool MediaCaptureDevice::GetRange(VideoProperty prop, long %min, long %max, long %stepSize, long %defaultValue, int %flag)
{
	HRESULT hr = S_OK;
	IAMVideoProcAmp *pProcAmp = NULL;

	if (!fAttached)
	{
		return false;
	}

	try
	{
		hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pProcAmp));
		if (SUCCEEDED(hr))
		{
			long minv;
			long maxv;
			long stepv;
			long defv;
			long f;

			hr = pProcAmp->GetRange((long)prop, &minv, &maxv, &stepv, &defv, &f);
			if (SUCCEEDED(hr))
			{
				min = minv;
				max = maxv;
				stepSize = stepv;
				defaultValue = defv;
				flag = f;
			}
		}
	}
	finally
	{
		MF_RELEASE(pProcAmp);
	}

	return SUCCEEDED(hr);
}

bool MediaCaptureDevice::GetRange(ManagedCameraControlProperty prop, long %min, long %max, long %stepSize, long %defaultValue, int %flag)
{
	HRESULT hr = S_OK;
	IAMCameraControl *pCameraControl = NULL;

	if (!fAttached)
	{
		return false;
	}

	try
	{
		hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pCameraControl));
		if (SUCCEEDED(hr))
		{
			long minv;
			long maxv;
			long stepv;
			long defv;
			long f;

			hr = pCameraControl->GetRange((long)prop, &minv, &maxv, &stepv, &defv, &f);
			if (SUCCEEDED(hr))
			{
				min = minv;
				max = maxv;
				stepSize = stepv;
				defaultValue = defv;
				flag = f;
			}
		}
	}
	finally
	{
		MF_RELEASE(pCameraControl);
	}

	return SUCCEEDED(hr);
}

/// <summary>
///  Gets the value for the specified property with the given flags
/// </summary>        
/// <param name="prop">Property to be set</param>
/// <param name="nValue">Value of the property</param>
/// <param name="flags">Flags corresponding to the property</param>
/// <returns>True if the property was set, false otherwise</returns>
bool MediaCaptureDevice::GetProperty(VideoProperty prop, int% nValue, int% flag)
{
    HRESULT hr = S_OK;
    IAMVideoProcAmp *pProcAmp = NULL;

    if (!fAttached)
    {
        return false;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pProcAmp));
        if (SUCCEEDED(hr))
        {
            long v;
            long f;

            hr = pProcAmp->Get((long)prop, &v, &f);
            if (SUCCEEDED(hr))
            {
                nValue = v;
                flag = f;
            }
        }
    }
    finally
    {
        MF_RELEASE(pProcAmp);
    }

    return SUCCEEDED(hr);
}

/// <summary>
///  Sets the value for the specified property with the given flags
/// </summary>        
/// <param name="prop">Property to be set</param>
/// <param name="nValue">Value of the property</param>
/// <param name="flags">Flags corresponding to the property</param>
/// <returns>True if the property was set, false otherwise</returns>
bool MediaCaptureDevice::SetProperty(ManagedCameraControlProperty prop, int nValue, ManagedCameraControlPropertyFlags flags)
{
    HRESULT hr = S_OK;
    IAMCameraControl *pCameraControl = NULL;

    if (!fAttached)
    {
        return false;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pCameraControl));
        if (SUCCEEDED(hr))
        {
            hr = pCameraControl->Set((long)prop, nValue, (long)flags);
        }
    }
    finally
    {
        MF_RELEASE(pCameraControl);
    }

    return SUCCEEDED(hr);
}

/// <summary>
///  Sets the value for the specified property with the given flags
/// </summary>        
/// <param name="prop">Property to be set</param>
/// <param name="nValue">Value of the property</param>
/// <param name="flags">Flags corresponding to the property</param>
/// <returns>True if the property was set, false otherwise</returns>
bool MediaCaptureDevice::GetProperty(ManagedCameraControlProperty prop, int% nValue, int% flag)
{
    HRESULT hr = S_OK;
    IAMCameraControl *pCameraControl = NULL;

    if (!fAttached)
    {
        return false;
    }

    try
    {
        hr = _pMediaSource->QueryInterface(IID_PPV_ARGS(&pCameraControl));
        if (SUCCEEDED(hr))
        {
            long v;
            long f;

            hr = pCameraControl->Get((long)prop, &v, &f);
            if (SUCCEEDED(hr))
            {
                nValue = v;
                flag = f;
            }
        }
    }
    finally
    {
        MF_RELEASE(pCameraControl);
    }

    return SUCCEEDED(hr);
}

/// <summary>
///  Gets the capture format currently used by the device.
///  This is not necessarily one of the formats in the supported formats list.
/// </summary>
/// <returns>Current capture format</returns>
CaptureFormat^ MediaCaptureDevice::CurrentFormat::get()
{
    if (!fAttached)
    {
        return nullptr;
    }

    HRESULT hr = S_OK;
    IMFMediaType *pMediaType = NULL;

    try
    {
        hr = m_pSourceReader->GetCurrentMediaType(
            (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
            &pMediaType);
        MF_THROWHR(hr);

        // Return the user requested frame rate since this class will downsample
        hr = MFSetAttributeRatio(pMediaType, MF_MT_FRAME_RATE, m_desiredRateNumerator, m_desiredRateDenominator);
        MF_THROWHR(hr);
        
        return CaptureFormat::FromMediaType(pMediaType);
    }
    finally
    {
        MF_RELEASE(pMediaType);
    }
}

/// <summary>
///  Sets the capture format currently used by the device.
/// </summary>
/// <param name="value">Desired capture format</param>
void MediaCaptureDevice::CurrentFormat::set(CaptureFormat ^value)
{
    if (!fAttached)
    {
        throw gcnew InvalidOperationException();
    }

    if (value->nFrameRateDenominator == 0)
    {
        throw gcnew ArgumentOutOfRangeException("nFrameRateDenominator cannot not be 0"); // Check for invalid denominator
    }

    m_desiredRateNumerator = value->nFrameRateNumerator;

    if (value->subType->Guid == Guid::Empty)
    {
        throw gcnew ArgumentOutOfRangeException("subtype->Guid cannot be Guid::Empty"); // Require subtype
    }

    m_desiredRateDenominator = value->nFrameRateDenominator;

    array<Byte>^ rawGuid = value->subType->Guid.ToByteArray();
    pin_ptr<BYTE> pbData = &(rawGuid[0]);
    GUID subtype = *(_GUID *)pbData;


    HRESULT hr = S_OK;
    IMFMediaType *pMediaType = NULL;
    bool found = false;

    for (int nTypeIndex = 0; !found ;nTypeIndex++)
    {
        GUID nativeSubType;
        UINT32 resWidth = 0;
        UINT32 resHeight = 0;
        UINT32 numeratorRate = 0;
        UINT32 denominatorRate = 0;
        double desiredRate = ((double)value->nFrameRateNumerator) / value->nFrameRateDenominator;
        try
        {

            // Fetch the native media type and fill in set info
            hr = m_pSourceReader->GetNativeMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, nTypeIndex, &pMediaType);
            MF_THROWHR(hr);

            hr = pMediaType->GetGUID(MF_MT_SUBTYPE, &nativeSubType);
            MF_THROWHR(hr);

            UINT64 res;
            hr = pMediaType->GetUINT64(MF_MT_FRAME_SIZE, &res);
            MF_THROWHR(hr);
            Unpack2UINT32AsUINT64(res, &resWidth, &resHeight);

            UINT64 rate;
            hr = pMediaType->GetUINT64(MF_MT_FRAME_RATE, &rate);
            MF_THROWHR(hr);
            Unpack2UINT32AsUINT64(rate, &numeratorRate, &denominatorRate);

            // Avoid divide by zero exception. We are being extra cautious here since MF shouldn't return a denominatorRate of 0.
            if (denominatorRate == 0)
            {
                continue;
            }

            double frameRate = ((double)numeratorRate) / denominatorRate;
            
            // Find a media match
            if (resWidth == (UINT32)value->nWidth &&
                resHeight == (UINT32)value->nHeight &&
				frameRate == desiredRate &&
				nativeSubType == subtype)
            {   
                hr = pMediaType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
                MF_THROWHR(hr);

                hr = MFSetAttributeSize(pMediaType, MF_MT_FRAME_SIZE, value->nWidth, value->nHeight);
                MF_THROWHR(hr);

                // Do not change the native frame rate of the webcam, since that causes letterboxing. RGB Camera will do the integer frame rate conversion.
                hr = MFSetAttributeRatio(pMediaType, MF_MT_FRAME_RATE, numeratorRate, denominatorRate);
                MF_THROWHR(hr);

				if (IsWindows8OrGreater())
				{
					hr = pMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB24);
					MF_THROWHR(hr);
				}
				else
				{
					// We only support MJPG, YUY2  formats
					if (subtype == MFVideoFormat_MJPG || subtype == MFVideoFormat_YUY2)
					{
						// The WMV reader does not support mjpg as an input format. However, MF
						// will transcode if we set all of the other parameters up, but make the video type
						// YUY2.
				
						// Set the image frame format to YUY2
						hr = pMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_YUY2);
						MF_THROWHR(hr);
					}
					else
					{
						hr = MF_E_UNSUPPORTED_FORMAT;
						MF_THROWHR(hr);
					}
				}


                // Saves the callback from having to read this from the media format which can be expensive.
                m_callback->SetFormat(resWidth, resHeight);

                hr = m_pSourceReader->SetCurrentMediaType((DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM, NULL, pMediaType);
                MF_THROWHR(hr);

                found = true;
            }
            
            MF_RELEASE(pMediaType);
            if (found)
            {
                break;
            }
        }
        finally
        {
            MF_RELEASE(pMediaType);
        }
    }

    if (!found)
    {
        hr = MF_E_UNSUPPORTED_FORMAT;
        MF_THROWHR(hr);
    }
}

/// <summary>
///  Sets up the capture pipeline if required and sets up async sample capture
/// </summary>        
/// <param name="handler">Handler to call after completing read sample</param>
void MediaCaptureDevice::CaptureSample(ReadSampleDelegate^ handler)
{
    if (!fAttached)
    {
        throw gcnew InvalidOperationException();
    }

    m_readSampleCallback = handler;

    IntPtr ip = Marshal::GetFunctionPointerForDelegate(m_readSampleDelegateInternal);
    m_callback->CaptureSample((ReadSampleHandlerForDevice)ip.ToPointer());
}

/// <summary>
///  Gets a list of all available capture devices
/// </summary>        
IEnumerable<MediaCaptureDevice^>^ MediaCaptureDevice::AllDevices::get()
{
   return gcnew RGBCameraCollection();
}

}}}
