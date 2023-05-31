// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Read sample delegate which is called back for each image.
    /// </summary>
    /// <param name="data"> Image data as RGB24 byte array </param>
    /// <param name="cbLength"> Length of Image data in bytes </param>
    /// <param name="timestamp"> Sample timestamp </param>
    public delegate void ReadSampleDelegate(IntPtr data, int cbLength, LONGLONG timestamp);

    /// <summary>
    /// CABI for read sample handler.
    /// </summary>
    /// <param name="data"> Image data as RGB24 byte array </param>
    /// <param name="cbLength"> Length of Image data in bytes </param>
    /// <param name="timestamp"> Sample timestamp </param>
    typedef void (__stdcall *ReadSampleHandlerForDevice)(BYTE* data, int cbLength, LONGLONG timestamp);

    /// <summary>
    /// Class used to represent a capture device which can receive asynchronous notifications.
    /// </summary>
    class SourceReaderCallback : public IMFSourceReaderCallback
    {
    public:
        static HRESULT CreateInstance(SourceReaderCallback **ppPlayer);

        // IUnknown methods
        STDMETHODIMP QueryInterface(REFIID iid, void** ppv);
        STDMETHODIMP_(ULONG) AddRef();
        STDMETHODIMP_(ULONG) Release();

        // IMFSourceReaderCallback methods

        STDMETHODIMP OnReadSample(
            _In_ HRESULT hrStatus,
            _In_ DWORD dwStreamIndex,
            _In_ DWORD dwStreamFlags,
            _In_ LONGLONG llTimestamp,
            _In_opt_ IMFSample *pSample);

        /// <summary>
        /// Called when the source reader receives certain events from the media source
        /// </summary>
        /// <param name="dwStreamIndex">
        /// For stream events, the value is the zero-based index of the stream that sent the event. 
        /// For source events, the value is MF_SOURCE_READER_MEDIASOURCE
        /// </param>
        /// <param name="pEvent">A pointer to the IMFMediaEvent interface of the event </param>
        /// <returns> S_OK if succeeded. Error code if not.</returns>
        STDMETHODIMP OnEvent(_In_ DWORD dwStreamIndex, _In_ IMFMediaEvent *pEvent)
        {
            UNREFERENCED_PARAMETER(dwStreamIndex);
            UNREFERENCED_PARAMETER(pEvent);
            return S_OK;
        }

        /// <summary>
        /// Called when the IMFSourceReader::Flush method completes
        /// </summary>
        /// <param name="dwStreamIndex">
        /// For stream events, the value is the zero-based index of the stream that sent the event. 
        /// For source events, the value is MF_SOURCE_READER_MEDIASOURCE
        /// </param>
        /// <returns> S_OK if succeeded. Error code if not.</returns>
        STDMETHODIMP OnFlush(DWORD dwStreamIndex)
        {
            UNREFERENCED_PARAMETER(dwStreamIndex);
            return S_OK;
        }

        /// <summary>
        /// Sets the video format.
        /// </summary>        
        /// <param name="width">Width in pixels </param>
        /// <param name="height">Height in pixels </param>
        void SetFormat(size_t width, size_t height)
        {
            m_width = width;
            m_height = height;
        }

        void CaptureSample(ReadSampleHandlerForDevice handler);

        /// <summary>
        /// Sets the source reader
        /// </summary>        
        /// <param name="pReader">The MF source reader being wrapped by this class</param>
        void SetSourceReader(IMFSourceReader *pReader)
        {
            m_pReader = pReader;
            m_pReader->AddRef();
        }

    protected:
        SourceReaderCallback();
        virtual ~SourceReaderCallback();        

        /// <summary>
        /// Reference count.
        /// </summary> 
        long                    m_lRefCount;

        /// <summary>
        /// MF Source reader which is wrapped by this class
        /// </summary> 
        IMFSourceReader         *m_pReader;

        /// <summary>
        /// Width in pixels
        /// </summary> 
        size_t                   m_width;

        /// <summary>
        /// Height in pixels
        /// </summary> 
        size_t                   m_height;

        /// <summary>
        /// Callback to call to handle read sample completion.
        /// </summary> 
        ReadSampleHandlerForDevice m_readSampleHandler;

        /// <summary>
        /// Buffer for YUY2 -> RGB24 conversion
        /// </summary>
        BYTE                    *m_pRgbBuffer;
        DWORD                    m_rgbBufSize;

        void TransformImage_YUY2_to_RGB24(
            BYTE*       pDest,
            LONG        lDestStride,
            const BYTE* pSrc,
            LONG        lSrcStride,
            size_t      dwWidthInPixels,
            size_t      dwHeightInPixels
            );

        RGBTRIPLE ConvertYCrCbToRGB(
            int y,
            int cr,
            int cb
            );

         BYTE Clip(int clr);
    };
}}}
