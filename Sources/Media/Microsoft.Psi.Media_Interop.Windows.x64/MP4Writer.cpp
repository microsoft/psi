// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"
#include <atlbase.h>
#include <mmreg.h>
#include "MP4Writer.h"

using namespace System::Runtime::InteropServices;

namespace Microsoft {
    namespace Psi {
        namespace Media_Interop {

#define IFS(expr) do { if (SUCCEEDED(hr)) hr = expr; } while(0)

            //**********************************************************************  
            // Define ctor for object that contains the unmanaged data associated
            // with a MP4Writer object
            //**********************************************************************  
            MP4WriterUnmanagedData::MP4WriterUnmanagedData() :
                videoStreamIndex(0),
                numFramesWritten(0),
                outputWidth(0),
                outputHeight(0),
                frameRateNumerator(0),
                frameRateDenominator(0),
                targetBitrate(0),
                closed(true),
                hasAudio(false),
                audioStreamIndex(0),
                lastVideoTimestamp(0),
                lastAudioTimestamp(0),
                numAudioSamplesWritten(0)
            {
            }

            //**********************************************************************
            // Sets up our MF writer for handling audio. This code always generates
            // AAC for the audio output and assumes the audio input is always PCM
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::SetupAudio(UINT32 bitsPerSample, UINT32 samplesPerSecond, UINT32 numChannels)
            {
                CComPtr<IMFMediaType> audioMediaOutputType;

                // Add audio output
                // For complete description of these parameters see:
                //           http://msdn.microsoft.com/en-us/library/windows/desktop/dd742785(v=vs.85).aspx
                HRESULT hr = S_OK;
                IFS(MFCreateMediaType(&audioMediaOutputType));
                IFS(audioMediaOutputType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio));
                IFS(audioMediaOutputType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_AAC));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, 16));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, 48000));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, 24000));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, 2));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION, 0x29));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_AUDIO_BLOCK_ALIGNMENT, 1));
                IFS(audioMediaOutputType->SetUINT32(MF_MT_FIXED_SIZE_SAMPLES, TRUE));

                //**********************************************************************
                // MF's handling of MPEG is different. We have to include the following
                // block of data. This block represents the AudioSpecificConfig() portion of MP4.
                // See the following articles for more details:
                //  http://msdn.microsoft.com/en-us/library/windows/desktop/dd742784(v=vs.85).aspx
                //  http://www.wiki.multimedia.cx/index.php?title=MPEG-4_Audio#Sampling_Frequencies
                // The upshot is that the important bits are the last two bytes, which are defined
                // as (in bits):
                //    00010             : AAC LC (Low Complexity)
                //         0011         : Index 3, which is 48KHz
                //             0010     : 2 Channels
                //                  000 : Reserved stuff
                // ============================================
                //    0001 0001 1001 0000 = 0x11 0x90
                // The MSDN documentation shows this as 0x11, 0xb0, but this is for 48000Hz, with 6 channel audio
                //**********************************************************************
                BYTE buffer[] = { 0x00, 0x00, 0x29, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x90 };
                IFS(audioMediaOutputType->SetBlob(MF_MT_USER_DATA, buffer, sizeof(buffer)));
                IFS(writer->AddStream(audioMediaOutputType, &audioStreamIndex));

                CComPtr<IMFMediaType> audioMediaInputType;
                IFS(MFCreateMediaType(&audioMediaInputType));
                IFS(audioMediaInputType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio));
                IFS(audioMediaInputType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_PCM));
                IFS(audioMediaInputType->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, bitsPerSample));
                IFS(audioMediaInputType->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, samplesPerSecond));
                IFS(audioMediaInputType->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, numChannels));
                IFS(audioMediaInputType->SetUINT32(MF_MT_FIXED_SIZE_SAMPLES, TRUE));

                IFS(writer->SetInputMediaType(audioStreamIndex, audioMediaInputType, nullptr));
                return hr;
            }

            //**********************************************************************
            // Open() must be called before adding image or audio samples to
            // the final MP4 file.
            // Parameters:
            //   imageWidth - is the width in pixels of each image. The input images
            //           and the output video are assumed to be of the same dimensions.
            //   imageHeight - is the height in pixels of each image. The input images
            //           and the output video are assumed to be of the same dimensions.
            //   frameRateNum - is the numerator of the desired output framerate.
            //           Typically this is set to 30.
            //   frameRateDenom - is the denominator of the desired output framerate.
            //           Typically this is set to 1.
            //   bitrate - is the desired output bitrate (bits/sec). Typically this is set
            //           128000, 384000, 528560, 4000000, or 10000000
            //   pixelFormat - is the pixel format that each input image (passed to WriteVideoFrame)
            //           is assumed to be
            //   containsAudio - if true then the MP4 file will have an audio stream (filled
            //           by the client by calling WriteAudioSample)
            //   bitsPerSample - Bits per audio sample for the input audio. Typically this is 16.
            //   samplesPerSample - Bitrate of the input audio. Typically this is 48000.
            //   numChannels - Number of audio channels. This is assumed to be either 1 or 2.
            //   outputFilename - name of the output file for the generated .mp4 file
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::Open(UINT32 imageWidth, UINT32 imageHeight, UINT32 frameRateNum, UINT32 frameRateDenom, UINT32 bitrate, int pixelFormat,
                bool containsAudio, UINT32 bitsPerSample, UINT32 samplesPerSecond, UINT32 numChannels,
                wchar_t *outputFilename)
            {
                hasAudio = containsAudio;
                outputWidth = imageWidth;
                outputHeight = imageHeight;
                frameRateNumerator = frameRateNum;
                frameRateDenominator = frameRateDenom;
                targetBitrate = bitrate;
                numFramesWritten = 0;
                numAudioSamplesWritten = 0;
                audioBitsPerSample = bitsPerSample;
                audioSamplesPerSecond = samplesPerSecond;
                audioNumChannels = numChannels;
                firstTimestamp = 0;

                // H264 only supports even pixel dimensions and formats up to 2048. For HD video we would need
                // to switch to H265 or some other encoding method.
                if (outputWidth % 2 != 0 || outputHeight % 2 != 0 || outputWidth > 2048 || outputHeight > 2048)
                {
                    return E_INVALIDARG;
                }

                HRESULT hr = S_OK;
                IFS(MFCreateSinkWriterFromURL(outputFilename, nullptr, nullptr, &writer));

                // Define our output media type
                IFS(MFCreateMediaType(&outputMediaType));
                IFS(outputMediaType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video));
                IFS(outputMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_H264));
                IFS(MFSetAttributeRatio(outputMediaType, MF_MT_FRAME_RATE, frameRateNumerator, frameRateDenominator));
                IFS(MFSetAttributeSize(outputMediaType, MF_MT_FRAME_SIZE, outputWidth, outputHeight));
                IFS(MFSetAttributeRatio(outputMediaType, MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                IFS(outputMediaType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive));
                IFS(outputMediaType->SetUINT32(MF_MT_AVG_BITRATE, targetBitrate));
                IFS(writer->AddStream(outputMediaType, &videoStreamIndex));

                // Define the input media type
                IFS(MFCreateMediaType(&inputMediaType));
                IFS(inputMediaType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video));
                switch (pixelFormat)
                {
                case NativePixelFormat_Undefined:
                case NativePixelFormat_Gray_8bpp:
                case NativePixelFormat_Gray_16bpp:
                case NativePixelFormat_RGBA_64bpp:
                    hr = E_NOTIMPL;
                    break;
                case NativePixelFormat_BGRA_32bpp:
                case NativePixelFormat_BGRX_32bpp:
                    IFS(inputMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB32));
                    break;
                case NativePixelFormat_BGR_24bpp:
                    IFS(inputMediaType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_RGB24));
                    break;
                }
                IFS(MFSetAttributeRatio(inputMediaType, MF_MT_FRAME_RATE, frameRateNumerator, frameRateDenominator));
                IFS(MFSetAttributeSize(inputMediaType, MF_MT_FRAME_SIZE, outputWidth, outputHeight));
                IFS(inputMediaType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive));
                IFS(MFSetAttributeRatio(inputMediaType, MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                IFS(writer->SetInputMediaType(videoStreamIndex, inputMediaType, nullptr));

                if (containsAudio)
                {
                    IFS(SetupAudio(bitsPerSample, samplesPerSecond, numChannels));
                }

                IFS(writer->BeginWriting());
                closed = false;
                return hr;
            }

            //**********************************************************************
            // Copies the image data (from our managed Microsoft::Psi::Image object)
            // to the media buffer provided by MF.
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::CopyImageDataToMediaBuffer(IntPtr imageData, int pixelFormat, BYTE *outputBuffer)
            {
                BYTE *dstRow = outputBuffer;
                BYTE *srcRow = (BYTE*)imageData.ToPointer();
                BYTE *srcCol;
                BYTE *dstCol;
                for (UINT32 y = 0; y < outputHeight; y++)
                {
                    switch (pixelFormat)
                    {
                    case NativePixelFormat_BGR_24bpp:
                        srcCol = srcRow;
                        dstCol = dstRow;
                        for (UINT32 x = 0; x < outputWidth; x++)
                        {
                            dstCol[0] = srcCol[0];
                            dstCol[1] = srcCol[1];
                            dstCol[2] = srcCol[2];
                            srcCol += 3;
                            dstCol += 3;
                        }
                        srcRow += outputWidth * 3;
                        dstRow += outputWidth * 3;
                        break;

                    case NativePixelFormat_BGRA_32bpp:
                    case NativePixelFormat_BGRX_32bpp:
                        srcCol = srcRow;
                        dstCol = dstRow;
                        for (UINT32 x = 0; x < outputWidth; x++)
                        {
                            dstCol[3] = srcCol[0];
                            dstCol[2] = srcCol[1];
                            dstCol[1] = srcCol[2];
                            dstCol[0] = srcCol[3];
                            srcCol += 4;
                            dstCol += 4;
                        }
                        srcRow += outputWidth * 4;
                        dstRow += outputWidth * 4;
                        break;

                    default:
                        return E_UNEXPECTED;
                    }
                }
                return S_OK;
            }

            //**********************************************************************
            // Writes an image into our video stream
            // Parameters:
            //   timestamp - timestamp (in 100 nanoseconds) for this video frame
            //   imageData - buffer containing our image data
            //   imageWidth - width of image data in pixels
            //   imageHeight - height of image data in pixels
            //   pixelFormat - format of the pixels in imageData
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::WriteVideoFrame(LONGLONG timestamp, IntPtr imageData, UINT32 imageWidth, UINT32 imageHeight, int pixelFormat)
            {
                // Have we already closed the stream?
                if (closed)
                {
                    return E_UNEXPECTED;
                }

                // Make sure that frames are added in order
                if (numFramesWritten > 0 && timestamp <= lastVideoTimestamp)
                {
                    return E_INVALIDARG;
                }

                if (firstTimestamp == 0)
                {
                    firstTimestamp = timestamp;
                }

                HRESULT hr = S_OK;
                CComPtr<IMFSample> sample;
                IFS(MFCreateSample(&sample));

                CComPtr<IMFMediaBuffer> buffer;
                ULONGLONG frameDuration = 10000000L * frameRateDenominator / frameRateNumerator;
                IFS(MFCreateMediaBufferFromMediaType(inputMediaType, frameDuration, 0, 0, &buffer));

                BYTE *rawBuffer = nullptr;
                DWORD bufferLength = 0;
                IFS(buffer->Lock(&rawBuffer, nullptr, &bufferLength));
                if (SUCCEEDED(hr))
                {
                    switch (pixelFormat)
                    {
                    case NativePixelFormat_Undefined:
                    case NativePixelFormat_Gray_8bpp:
                    case NativePixelFormat_Gray_16bpp:
                    case NativePixelFormat_RGBA_64bpp:
                        hr = E_NOTIMPL;
                        break;
                    case NativePixelFormat_BGRA_32bpp:
                    case NativePixelFormat_BGRX_32bpp:
                        if (bufferLength != imageWidth * imageHeight * 4)
                        {
                            hr = E_UNEXPECTED;
                        }
                        break;
                    case NativePixelFormat_BGR_24bpp:
                        if (bufferLength != imageWidth * imageHeight * 3)
                        {
                            hr = E_UNEXPECTED;
                        }
                    }
                    IFS(CopyImageDataToMediaBuffer(imageData, pixelFormat, rawBuffer));
                    (void)buffer->Unlock();
                }

                IFS(buffer->SetCurrentLength(bufferLength));
                IFS(sample->AddBuffer(buffer));
                IFS(sample->SetSampleTime(timestamp - firstTimestamp));
                IFS(sample->SetSampleDuration(frameDuration));

                IFS(writer->WriteSample(videoStreamIndex, sample));
                if (SUCCEEDED(hr))
                {
                    lastVideoTimestamp = timestamp;
                    numFramesWritten++;
                }
                return hr;
            }

            //**********************************************************************
            // Writes an audio sample into our MP4 file
            // Parameters:
            //   pcmData - buffer containing the PCM audio data
            //   numDataBytes - number of bytes in pcmData buffer
            //   timestamp - timestamp (in 100 nanoseconds) for this sample
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::WriteAudioSample(LONGLONG timestamp, IntPtr pcmData, UINT32 numDataBytes, IntPtr waveFormat)
            {
                if (closed || !hasAudio)
                {
                    return E_UNEXPECTED;
                }

                // Make sure samples are in order
                if (numAudioSamplesWritten > 0 && timestamp <= lastAudioTimestamp)
                {
                    return E_INVALIDARG;
                }

                if (firstTimestamp == 0)
                {
                    firstTimestamp = timestamp;
                }

                // Make sure our audio data is in the correct format
                WAVEFORMATEX wavefmt = *(WAVEFORMATEX*)waveFormat.ToPointer();
                if (wavefmt.wFormatTag != WAVE_FORMAT_PCM ||
                    wavefmt.wBitsPerSample != audioBitsPerSample ||
                    wavefmt.nSamplesPerSec != audioSamplesPerSecond ||
                    wavefmt.nChannels != audioNumChannels)
                {
                    return E_UNEXPECTED;
                }

                CComPtr<IMFSample> sample;
                HRESULT hr = MFCreateSample(&sample);
                if (SUCCEEDED(hr))
                {
                    CComPtr<IMFMediaBuffer> mediaBuffer;
                    hr = MFCreateMemoryBuffer(numDataBytes, &mediaBuffer);
                    if (SUCCEEDED(hr))
                    {
                        BYTE *buffer;
                        DWORD bufferLength;
                        hr = mediaBuffer->Lock(&buffer, nullptr, &bufferLength);
                        if (SUCCEEDED(hr))
                        {
                            memcpy(buffer, (BYTE*)pcmData.ToPointer(), numDataBytes);
                            (void)mediaBuffer->Unlock();
                        }
                        IFS(mediaBuffer->SetCurrentLength(DWORD(numDataBytes)));
                        IFS(sample->AddBuffer(mediaBuffer));

                        DWORD oneSecWorthOfData = wavefmt.nChannels * (wavefmt.wBitsPerSample / 8) * wavefmt.nSamplesPerSec;
                        MFTIME sampleDurationIn100Ns = 10000 * ((1000 * numDataBytes) / oneSecWorthOfData);
                        IFS(sample->SetSampleDuration(sampleDurationIn100Ns));
                        IFS(sample->SetSampleTime(timestamp - firstTimestamp));
                        IFS(writer->WriteSample(audioStreamIndex, sample));
                        numAudioSamplesWritten++;
                        lastAudioTimestamp = timestamp;
                    }
                }
                return hr;
            }

            //**********************************************************************
            // Closes the current file. This must be called to ensure the MP4 file
            // is written properly.
            //**********************************************************************
            HRESULT MP4WriterUnmanagedData::Close()
            {
                if (writer)
                {
                    writer->Finalize();
                    writer = nullptr;
                }
                closed = true;
                return S_OK;
            }

            //**********************************************************************
            // Opens a MP4 file for writing.
            //**********************************************************************
            HRESULT MP4Writer::Open(String ^fn, MP4WriterConfiguration^ config)
            {
                unmanagedData = new MP4WriterUnmanagedData();
                IntPtr ptrToNativeString = Marshal::StringToHGlobalUni(fn);
                HRESULT hr = unmanagedData->Open(config->imageWidth, config->imageHeight, config->frameRateNumerator, config->frameRateDenominator, config->targetBitrate,
                    config->pixelFormat, config->containsAudio, config->bitsPerSample, config->samplesPerSecond, config->numChannels,
                    static_cast<wchar_t*>(ptrToNativeString.ToPointer()));
                Marshal::FreeHGlobal(ptrToNativeString);
                return hr;
            }

            //**********************************************************************
            HRESULT MP4Writer::WriteVideoFrame(LONGLONG timestamp, IntPtr imageData, UINT32 imgWidth, UINT32 imgHeight, int pixelFormat)
            {
                if (imageData.ToPointer() == nullptr)
                {
                    return E_POINTER;
                }
                if (imgWidth != (int)unmanagedData->outputWidth || imgHeight != (int)unmanagedData->outputHeight)
                {
                    return E_INVALIDARG;
                }
                return unmanagedData->WriteVideoFrame(timestamp, imageData, imgWidth, imgHeight, pixelFormat);
            }

            //**********************************************************************
            HRESULT MP4Writer::WriteAudioSample(LONGLONG timestamp, IntPtr pcmData, UINT32 numDataBytes, IntPtr waveFormat)
            {
                return unmanagedData->WriteAudioSample(timestamp, pcmData, numDataBytes, waveFormat);
            }

            //**********************************************************************
            HRESULT MP4Writer::Close()
            {
                HRESULT hr = S_OK;
                if (unmanagedData != nullptr)
                {
                    hr = unmanagedData->Close();
                    delete unmanagedData;
                    unmanagedData = nullptr;
                }
                return hr;
            }

            /// <summary>
            /// Called to initialize Media Foundation
            /// </summary>
            HRESULT MP4Writer::Startup()
            {
                return MFStartup(MF_VERSION, MFSTARTUP_LITE);
            }

            /// <summary>
            /// Called to shutdown Media Foundation
            /// </summary>
            HRESULT MP4Writer::Shutdown()
            {
                return MFShutdown();
            }

        }
    }
}
