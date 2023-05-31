// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"
using namespace System::Runtime::InteropServices;

#include "SourceReaderCallback.h"
#include "ks.h"
#include "ksmedia.h"
#include <stdio.h>
#include <VersionHelpers.h>

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

/// <summary>
/// Creates an instance of the capture device which implements <c> IMFSourceReaderCallback </c>
/// </summary>
/// <param name="ppPlayer">The reference to the source reader to the created</param>
/// <returns> S_OK if succeeded. Error code if not.</returns>
HRESULT SourceReaderCallback::CreateInstance(SourceReaderCallback **ppCapture)
{
    if (ppCapture == NULL)
    {
        return E_POINTER;
    }

    SourceReaderCallback *pCapture = new SourceReaderCallback();

    if (pCapture == NULL)
    {
        return E_OUTOFMEMORY;
    }

    // The SourceReaderCallback constructor sets the ref count to 1.
    *ppCapture = pCapture;

    return S_OK;
}

/// <summary>
/// Constructor is private. Use static CreateInstance method to instantiate.
/// </summary>   
SourceReaderCallback::SourceReaderCallback() 
: m_pReader(NULL)
, m_lRefCount(1)
, m_readSampleHandler(NULL)
, m_pRgbBuffer(NULL)
, m_rgbBufSize(0)
{
}

/// <summary>
/// Destructor is private. Caller should call Release.
/// </summary>
SourceReaderCallback::~SourceReaderCallback()
{
    if (m_pRgbBuffer)
    {
        delete m_pRgbBuffer;
        m_pRgbBuffer = NULL;
    }
}

/// <remarks>IUnknown methods</remarks>

/// <summary>
/// Increments the reference count for an interface on an object
/// </summary>
/// <returns> The method returns the new reference count.</returns>
ULONG SourceReaderCallback::AddRef()
{
    return InterlockedIncrement(&m_lRefCount);
}


/// <summary>
/// Decrements the reference count for an interface on an object
/// </summary>
/// <returns> The method returns the new reference count.</returns>
ULONG SourceReaderCallback::Release()
{
    ULONG ulCount = InterlockedDecrement(&m_lRefCount);
    if (ulCount == 0)
    {
        delete this;
    }
    return ulCount;
}

/// <summary>
/// Retrieves pointers to the supported interfaces on an object
/// </summary>
/// <param name="ppv">The reference to interface to be retrieved</param>
/// <returns> S_OK if succeeded. Error code if not.</returns>
HRESULT SourceReaderCallback::QueryInterface(REFIID riid, void** ppv)
{
    if (riid == __uuidof(IMFSourceReaderCallback) || riid == __uuidof(IUnknown))
    {
        *ppv = this;
        AddRef();
        return S_OK;
    }

    return E_NOINTERFACE;
}

__forceinline BYTE SourceReaderCallback::Clip(int clr)
{
    return (BYTE)(clr < 0 ? 0 : ( clr > 255 ? 255 : clr ));
}

__forceinline RGBTRIPLE SourceReaderCallback::ConvertYCrCbToRGB(
    int y,
    int cr,
    int cb
    )
{
    RGBTRIPLE rgbt;

    int c = y - 16;
    int d = cb - 128;
    int e = cr - 128;

    rgbt.rgbtRed =   Clip(( 298 * c           + 409 * e + 128) >> 8);
    rgbt.rgbtGreen = Clip(( 298 * c - 100 * d - 208 * e + 128) >> 8);
    rgbt.rgbtBlue =  Clip(( 298 * c + 516 * d           + 128) >> 8);

    return rgbt;
}

void SourceReaderCallback::TransformImage_YUY2_to_RGB24(
    BYTE*       pDest,
    LONG        lDestStride,
    const BYTE* pSrc,
    LONG        lSrcStride,
    size_t      dwWidthInPixels,
    size_t      dwHeightInPixels
    )
{
    for (DWORD y = 0; y < dwHeightInPixels; y++)
    {
        RGBTRIPLE *pDestPel = (RGBTRIPLE*)pDest;
        WORD    *pSrcPel = (WORD*)pSrc;

        for (DWORD x = 0; x < dwWidthInPixels; x += 2)
        {
            // Byte order is U0 Y0 V0 Y1

            int y0 = (int)LOBYTE(pSrcPel[x]);
            int u0 = (int)HIBYTE(pSrcPel[x]);
            int y1 = (int)LOBYTE(pSrcPel[x + 1]);
            int v0 = (int)HIBYTE(pSrcPel[x + 1]);

            pDestPel[x] = ConvertYCrCbToRGB(y0, v0, u0);
            pDestPel[x + 1] = ConvertYCrCbToRGB(y1, v0, u0);
        }

        pSrc += lSrcStride;
        pDest += lDestStride;
    }
}

/// <summary>
/// Called when the IMFSourceReader::ReadSample method completes
/// </summary>
/// <param name="hrStatus">
/// The status code. 
/// If an error occurred while processing the next smple, this parameter contains the error code 
/// </param>
/// <param name="dwStreamIndex">The zero-based index of the stream that delivered the sample</param>
/// <param name="dwStreamFlags">A bitwise OR of zero or more flags from the MF_SOURCE_READER_FLAG enumeration</param>
/// <param name="llTimestamp">
/// The time stamp of the sample, or the time of the stream event indicated in dwStreamFlags. 
/// The time is given in 100-nanosecond units
/// </param>
/// <param name="pSample">A pointer to the IMFSample interface of a media sample. This parameter might be NULL.</param>
/// <returns> S_OK if succeeded. Error code if not.</returns>
HRESULT SourceReaderCallback::OnReadSample(
    _In_ HRESULT hrStatus,
    _In_ DWORD,  // dwStreamIndex
    _In_ DWORD,  // dwStreamFlags
    _In_ LONGLONG llTimeStamp,
    _In_opt_ IMFSample *pSample)      // Can be NULL
{
    UNREFERENCED_PARAMETER(llTimeStamp);

    HRESULT hr = S_FALSE;
    DWORD count = 0;
    DWORD cbLength = 0;
    IMFMediaBuffer *pBuffer = NULL;

	__try{

        if (FAILED(hrStatus))
        {
            hr = hrStatus;
            MF_CHKHR(hr);
        }

        if (pSample)
        {
            LONGLONG timestamp;
            pSample->GetSampleTime(&timestamp);

            // If we have a sample handler, setup the call
            if (m_readSampleHandler != NULL)
            {
                hr = pSample->GetBufferCount(&count);
                MF_CHKHR(hr);

                if (count > 1)
                {
                    hr = E_INVALIDARG;
                    MF_CHKHR(hr);
                }

                hr = pSample->GetBufferByIndex(0, &pBuffer);
                MF_CHKHR(hr);

                hr = pBuffer->GetCurrentLength(&cbLength);
                MF_CHKHR(hr);

                if (cbLength > 0)
                {
                    BYTE *pbData = NULL;

                    hr = pBuffer->Lock(&pbData, NULL, NULL);
                    MF_CHKHR(hr);
                    BYTE* pbYUY2 = NULL;
                    int cbYUY2Length = cbLength;
                    
                    pbYUY2 = new BYTE[cbYUY2Length]; 
                    memcpy_s(pbYUY2, cbYUY2Length, pbData, cbLength);

                    // initialize m_pRgbBuffer if necessary
                    if (!m_pRgbBuffer)
                    {
                        // conversion to RGB24 from YUY2 is 3:2
                        m_rgbBufSize = (DWORD)(cbYUY2Length * 1.5);
                        m_pRgbBuffer = new BYTE[m_rgbBufSize];
                    }

					if (IsWindows8OrGreater())
					{
						(*m_readSampleHandler)(pbYUY2, cbYUY2Length, timestamp);
					}
					else
					{
						TransformImage_YUY2_to_RGB24(
							m_pRgbBuffer,
							(DWORD)(m_width * 3),
							pbYUY2,
							(DWORD)(m_width * 2),
							m_width,
							m_height);
						(*m_readSampleHandler)(m_pRgbBuffer, m_rgbBufSize, timestamp);
					}

                    SAFE_DELETE_ARRAY(pbYUY2);

                    pBuffer->Unlock();
                }
            }
        }
    }
    __finally
    {        
        MF_RELEASE(pBuffer);
        //The first time you call ReadSample, we likely won't get a sample, so setup again       
        // Read another sample.
        m_pReader->ReadSample(
            (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
            0,
            NULL,   // actual
            NULL,   // flags
            NULL,   // timestamp
            NULL);  // sample
    }
    return hr;
}

/// <summary>
///  Sets up the capture pipeline if required and sets up async sample capture
/// </summary>        
/// <param name="handler">Handler to call after completing read sample</param>
void SourceReaderCallback::CaptureSample(ReadSampleHandlerForDevice handler)
{
    m_readSampleHandler = handler;

    // We have to tickle the reader
    m_pReader->ReadSample(
        (DWORD)MF_SOURCE_READER_FIRST_VIDEO_STREAM,
        0,
        NULL,
        NULL,
        NULL,
        NULL);
}
}}}
