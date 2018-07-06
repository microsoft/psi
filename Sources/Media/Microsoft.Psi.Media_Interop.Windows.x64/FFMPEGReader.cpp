// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"
#ifdef USE_FFMPEG
#include <atlbase.h>
#include <mmreg.h>
#include "FFMPEGReader.h"

#pragma warning(push)
#pragma warning(disable:4996)
using namespace System::Runtime::InteropServices;

struct AVDictionary
{
    int count;
    AVDictionaryEntry *elems;
};
namespace Microsoft {
    namespace Psi {
        namespace Media {
            namespace Native {
                namespace Windows {
                    //**********************************************************************
                    // Opens a MP4 file for writing.
                    //**********************************************************************
                    void FFMPEGReader::Open(String ^fn, FFMPEGReaderConfiguration^ /*config*/)
                    {
                        IntPtr ptrToNativeString = Marshal::StringToHGlobalUni(fn);
						std::wstring wstr(static_cast<wchar_t*>(ptrToNativeString.ToPointer()));
						std::string str(wstr.begin(), wstr.end());
                        HRESULT hr = unmanagedData->Open((char*)str.c_str());
                        Marshal::FreeHGlobal(ptrToNativeString);
                        if (FAILED(hr))
                        {
                            char buffer[512];
                            sprintf(buffer, "Failed to read video frame. HRESULT=0x%x", hr);
                            throw gcnew Exception(gcnew System::String(buffer));
                        }
                    }

                    //**********************************************************************
                    // NextFrame() advances the playback engine to the next audio or video
                    // packet to be processed. This method will fill in 'info' with the type
                    // of packet we are about to process (FrameType), the presentation time
                    // stamp for the frame (Timestamp), and the size of the buffer required
                    // to hold the decompressed data (BufferSize). The actual data is then
                    // read by the client via a call to ReadFrameData().
                    // Returns true if a frame was read; false otherwise.
                    //**********************************************************************
                    bool FFMPEGReader::NextFrame(FFMPEGFrameInfo ^%info, [Out] bool %endOfStream)
                    {
                        int frameType;
                        int requiredBufferSize;
                        bool eos = false;
                        HRESULT hr = unmanagedData->NextFrame(&frameType, &requiredBufferSize, &eos);
                        if (hr == S_FALSE)
                        {
                            return false;
                        }
                        if (eos)
                        {
                            endOfStream = true;
                            return false;
                        }
                        info->FrameType = frameType;
                        info->BufferSize = requiredBufferSize;
                        if (FAILED(hr))
                        {
                            char buffer[512];
                            sprintf(buffer, "Failed to read video frame. HRESULT=0x%x", hr);
                            throw gcnew Exception(gcnew System::String(buffer));
                        }
                        return true;
                    }

                    //**********************************************************************
                    // ReadFrameData() reads the next video or audio frame from the stream.
                    // 'dataBuffer' will be filled with the decompressed data. The buffer
                    // is allocated and controlled by the calling client. The size of the required
                    // buffer was returned by the client's previous call to NextFrame().
                    // Return true if we successfully decoded a frame
                    //**********************************************************************
                    bool FFMPEGReader::ReadFrameData(IntPtr dataBuffer, int %bufferSize, double %timestampMillisecs)
                    {
                        double ts;
                        int bytesRead;
                        HRESULT hr = unmanagedData->ReadFrameData((byte*)dataBuffer.ToPointer(), &bytesRead, &ts);
                        if (FAILED(hr))
                        {
                            char buffer[512];
                            sprintf(buffer, "Failed to read video frame. HRESULT=0x%x", hr);
                            throw gcnew Exception(gcnew System::String(buffer));
                        }
                        if (hr != S_FALSE)
                        {
                            timestampMillisecs = ts;
                            bufferSize = bytesRead;
                            return true; // Successfully decoded frame
                        }
                        return false;
                    }

                    //**********************************************************************
                    void FFMPEGReader::Close()
                    {
                        HRESULT hr = S_OK;
                        if (unmanagedData != nullptr)
                        {
                            hr = unmanagedData->Close();
                            delete unmanagedData;
                            unmanagedData = nullptr;
                        }
                        if (FAILED(hr))
                        {
                            char buffer[512];
                            sprintf(buffer, "Failed to read video frame. HRESULT=0x%x", hr);
                            throw gcnew Exception(gcnew System::String(buffer));
                        }
                    }
                }
            }
        }
    }
}
#pragma warning(pop)
#endif // USE_FFMPEG
