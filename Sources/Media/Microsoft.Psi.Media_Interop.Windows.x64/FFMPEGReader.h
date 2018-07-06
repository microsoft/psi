// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#ifdef USE_FFMPEG
#include "Managed.h"

#pragma warning(push)
#pragma warning(disable:4634 4635 4244 4996)
extern "C" {
#include <libavcodec\avcodec.h>
#include <libavformat\avformat.h>
#include <libswscale\swscale.h>
#include <libswresample\swresample.h>
#include <libavutil\dict.h>
#include <libavutil\opt.h>
}
#include <string>
#pragma warning(pop)
#include "FFMPEGReaderNative.h"

namespace Microsoft {
    namespace Psi {
        namespace Media {
            namespace Native {
                namespace Windows {
                    /// <summary>
                    /// Class for configuring our MPEG4 Reader
                    /// </summary>
                    public ref class FFMPEGReaderConfiguration
                    {
                    public:
                    };

                    /// <summary>
                    /// Class used for returning what time of data is about to
                    /// read by ReadFrameData().
                    /// </summary>
                    public ref class FFMPEGFrameInfo
                    {
                    public:
                        static int FrameTypeVideo = 0;
                        static int FrameTypeAudio = 1;
                        property int FrameType; // Type of data to be returned next by call to ReadFrameData
                        property int BufferSize; // The size of the buffer required to hold the decompressed data
                    };

                    /// <summary>
                    /// Class for playing back MPEG files via FFMPEG
                    /// </summary>
                    public ref class FFMPEGReader
                    {
                    private:
                        FFMPEGReaderNative * unmanagedData;
                    public:
                        FFMPEGReader(int imageDepth) :
                            unmanagedData(nullptr)
                        {
                            unmanagedData = new FFMPEGReaderNative();
                            unmanagedData->Initialize(imageDepth);
                        }

                        ~FFMPEGReader()
                        {
                            if (unmanagedData != nullptr)
                            {
                                delete unmanagedData;
                                unmanagedData = nullptr;
                            }
                        }

                        property int Width
                        {
                            int get() { return (unmanagedData == nullptr) ? 0 : unmanagedData->GetWidth(); }
                        }

                        property int Height
                        {
                            int get() { return (unmanagedData == nullptr) ? 0 : unmanagedData->GetHeight(); }
                        }

                        property int AudioSampleRate
                        {
                            int get() { return (unmanagedData == nullptr) ? 0 : unmanagedData->GetAudioSampleRate(); }
                        }

                        property int AudioBitsPerSample
                        {
                            int get() { return (unmanagedData == nullptr) ? 0 : unmanagedData->GetAudioBitsPerSample(); }
                        }

                        property int AudioNumChannels
                        {
                            int get() { return (unmanagedData == nullptr) ? 0 : unmanagedData->GetAudioNumChannels(); }
                        }

                        void Open(String ^fn, FFMPEGReaderConfiguration^ config);
                        void Close();
                        bool NextFrame(FFMPEGFrameInfo ^%info, [Out] bool %eos);
                        bool ReadFrameData(IntPtr dataBuffer, int %bufferSize, double %timestamp);
                    };

                }
            }
        }
    }
}
#endif // USE_FFMPEG
