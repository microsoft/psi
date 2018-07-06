// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#ifdef USE_FFMPEG

#pragma warning(push)
#pragma warning(disable:4634 4635 4244 4996)
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
#include <libavutil/dict.h>
#include <libavutil/opt.h>
}
#include <string>
#pragma warning(pop)

#ifdef LINUX
#define __declspec(x)
#define HRESULT int
#define S_OK 0
#define S_FALSE 1
#define E_FAIL -100
#define E_OUTOFMEMORY -101
#define E_UNEXPECTED -102
#define MAKE_HRESULT(X,Y,N) -(N)
#endif

namespace Microsoft {
namespace Psi {
namespace Media {
namespace Native {
namespace Windows {
#define PSIERR_BUFFER_TOO_SMALL     MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 1)
#define PSIERR_BSF_NOT_FOUND        MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 2)
#define PSIERR_BUG                  MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 3)
#define PSIERR_DECODER_NOT_FOUND    MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 4)
#define PSIERR_DEMUXER_NOT_FOUND    MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 5)
#define PSIERR_ENCODER_NOT_FOUND    MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 6)
#define PSIERR_EOF                  MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 7)
#define PSIERR_EXIT                 MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 8)
#define PSIERR_EXTERNAL             MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 9)
#define PSIERR_FILTER_NOT_FOUND     MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 10)
#define PSIERR_INVALIDDATA          MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 11)
#define PSIERR_MUXER_NOT_FOUND      MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 12)
#define PSIERR_OPTION_NOT_FOUND     MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 13)
#define PSIERR_PATCHWELCOME         MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 14)
#define PSIERR_PROTOCOL_NOT_FOUND   MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 15)
#define PSIERR_STREAM_NOT_FOUND     MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 16)
#define PSIERR_BUG2                 MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 17)
#define PSIERR_UNKNOWN              MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 18)
#define PSIERR_EXPERIMENTAL         MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 19)
#define PSIERR_INPUT_CHANGED        MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 20)
#define PSIERR_OUTPUT_CHANGED       MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 21)
#define PSIERR_HTTP_BAD_REQUEST     MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 22)
#define PSIERR_HTTP_UNAUTHORIZED    MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 23)
#define PSIERR_HTTP_FORBIDDEN       MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 24)
#define PSIERR_HTTP_NOT_FOUND       MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 25)
#define PSIERR_HTTP_OTHER_4XX       MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 26)
#define PSIERR_HTTP_SERVER_ERROR    MAKE_HRESULT(SEVERITY_ERROR, FACILITY_ITF, 27)

  //**********************************************************************
  // Define our unmanaged data associated with the MP4Writer object
  //**********************************************************************
  class __declspec(dllexport) FFMPEGReaderNative
  {
      AVFormatContext *formatCtx;
      int videoStreamIndex;
      int audioStreamIndex;
      AVCodec *audioCodec;
      AVCodec *videoCodec;
      AVPacket packet;
      AVCodecContext *videoCodecCtx;
      AVCodecContext *audioCodecCtx;
      AVFrame *videoFrame;
      AVFrame *convertedVideoFrame;
      uint8_t *convertedVideoBuffer;
      AVFrame *audioFrame;
      uint8_t *audioBuffers[2];
      int audioBufferSize;
      AVPixelFormat outputFormat; /* Pixel format for our output image */
      int bytesPerPixel;
      double audioClock;
      
      HRESULT ConvertFFMPEGError(int error);
      HRESULT InitializeVideoStream();
      HRESULT InitializeAudioStream();
  public:
      FFMPEGReaderNative();
      ~FFMPEGReaderNative();
      HRESULT Initialize(int outputDepth);
      HRESULT Open(char *filename);
      HRESULT NextFrame(int *streamIndex, int *requiredBufferSize, bool *eos);
      HRESULT ReadFrameData(uint8_t *imageData, int *bytesRead, double *timestampMillisecs);
      HRESULT Close();
      int GetWidth();
      int GetHeight();
      int GetAudioSampleRate();
      int GetAudioBitsPerSample();
      int GetAudioNumChannels();
  };
}}}}}
#endif // USE_FFMPEG
