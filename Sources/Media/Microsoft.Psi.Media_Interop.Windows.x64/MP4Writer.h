// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "VideoFormats.h"
#include "CaptureFormat.h"
#include "VideoProperty.h"
#include "ManagedCameraControlProperty.h"
#include "SourceReaderCallback.h"
#include "MediaFoundationUtility.h"

namespace Microsoft {
    namespace Psi {
        namespace Media_Interop {

            //**********************************************************************
            // Defines list of native pixel formats. NOTE: This list must match
            // the list in Microsoft.Psi.Imaging.PixelFormats. The reason for
            // the duplication is to avoid taking a dependency in Media.Native.Windows
            // on the Imaging layer.
            //**********************************************************************
            const int NativePixelFormat_Undefined = 0;
            const int NativePixelFormat_Gray_8bpp = 1;
            const int NativePixelFormat_Gray_16bpp = 2;
            const int NativePixelFormat_BGR_24bpp = 3;
            const int NativePixelFormat_BGRX_32bpp = 4;
            const int NativePixelFormat_BGRA_32bpp = 5;
            const int NativePixelFormat_RGBA_64bpp = 6;

            //**********************************************************************
            // Define our unmanaged data associated with the MP4Writer object
            //**********************************************************************
            class MP4WriterUnmanagedData
            {
            public:
                CComPtr<IMFSinkWriter> writer;         /* MP4 sink to write frames to */
                DWORD videoStreamIndex;                /* stream index containing video stream */
                UINT32 numFramesWritten;               /* Number of image frames added thus far */
                CComPtr<IMFMediaType> outputMediaType; /* Output media type */
                CComPtr<IMFMediaType> inputMediaType;  /* Input media type of each image frame */
                UINT32 outputWidth;                    /* Width of output image frames */
                UINT32 outputHeight;                   /* Height of output image frames */
                UINT32 frameRateNumerator;             /* Numerator of framerate (typically 30) */
                UINT32 frameRateDenominator;           /* Denominator of framerate (typically 1) */
                UINT32 targetBitrate;                  /* Target bitrate (typically 128000,384000,528560,4000000,or 10000000) */
                bool closed;                           /* Has Close() been called? */
                bool hasAudio;                         /* Does the output file contain an audio stream? */
                DWORD audioStreamIndex;                /* Stream index containing the audio stream */
                LONGLONG lastVideoTimestamp;           /* video timestamp on last image we added */
                LONGLONG lastAudioTimestamp;           /* audio timestamp on last image we added */
                UINT32 numAudioSamplesWritten;         /* Total number of audio samples written thus far */
                UINT32 audioBitsPerSample;             /* Number of bits per audio sample (typically 16) */
                UINT32 audioSamplesPerSecond;          /* Audio's sample rate (typically 48000) */
                UINT32 audioNumChannels;               /* Number of audio channels (typically 1 or 2) */
                LONGLONG firstTimestamp;               /* Initial timestamp received by component. Subtracted from all times written to the file */

                HRESULT CopyImageDataToMediaBuffer(IntPtr imageData, int format, BYTE *outputBuffer);
                HRESULT SetupAudio(UINT32 bitsPerSample, UINT32 samplesPerSecond, UINT32 numChannels);
            public:
                MP4WriterUnmanagedData();
                HRESULT Open(UINT32 imageWidth, UINT32 imageHeight, UINT32 frameRateNum, UINT32 frameRateDenom, UINT32 bitrate, int pixelFormat,
                    bool containsAudio, UINT32 bitsPerSample, UINT32 samplesPerSecond, UINT32 numChannels,
                    wchar_t *outputFilename);
                HRESULT WriteVideoFrame(LONGLONG timestamp, IntPtr imageData, UINT32 imageWidth, UINT32 imageHeight, int pixelFormat);
                HRESULT WriteAudioSample(LONGLONG timestamp, IntPtr pcmData, UINT32 numDataBytes, IntPtr waveFormat);
                HRESULT Close();
            };

            /// <summary>
            /// Class for configuring our MPEG4 writer
            /// </summary>
            public ref class MP4WriterConfiguration
            {
            public:
                UINT32 imageWidth;           /* Width of image frames */
                UINT32 imageHeight;          /* Height of image frames */
                UINT32 frameRateNumerator;   /* Numerator of framerate (typically 30) */
                UINT32 frameRateDenominator; /* Denominator of framerate (typically 1) */
                UINT32 targetBitrate;        /* Target bitrate (typically 128000,384000,528560,4000000,or 10000000) */
                int pixelFormat;             /* Input image's native pixel format (see NativePixelFormat_*) */
                bool containsAudio;          /* Does the output .mp4 contain audio stream */
                UINT32 bitsPerSample;        /* Number of bits per audio sample (typically 16) */
                UINT32 samplesPerSecond;     /* Audio's sample rate (typically 48000) */
                UINT32 numChannels;          /* Number of audio channels (typically 1 or 2) */
            };

            /// <summary>
            /// Class for recording video to an MPEG file via Media Foundation
            /// </summary>
            public ref class MP4Writer
            {
            private:
                MP4WriterUnmanagedData * unmanagedData;
            public:
                MP4Writer() :
                    unmanagedData(nullptr)
                {
                }

                ~MP4Writer()
                {
                    if (unmanagedData != nullptr)
                    {
                        delete unmanagedData;
                        unmanagedData = nullptr;
                    }
                }

                HRESULT Open(String ^fn, MP4WriterConfiguration^ config);
                HRESULT WriteVideoFrame(LONGLONG timestamp, IntPtr imageData, UINT32 imgWidth, UINT32 imgHeight, int pixelFormat);
                HRESULT WriteAudioSample(LONGLONG timestamp, IntPtr pcmData, UINT32 numDataBytes, IntPtr waveFormat);
                HRESULT Close();

                static HRESULT Startup();
                static HRESULT Shutdown();
            };

        }
    }
}
