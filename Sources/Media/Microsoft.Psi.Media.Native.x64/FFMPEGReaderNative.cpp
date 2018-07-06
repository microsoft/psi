// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "stdafx.h"
#ifdef USE_FFMPEG
#include "FFMPEGReaderNative.h"
#include <locale>
#include <codecvt>
#include <stdio.h>

#pragma warning(push)
#pragma warning(disable:4996)
namespace Microsoft {
namespace Psi {
namespace Media {
namespace Native {
namespace Windows {

extern "C" {
    void *FFMPEGReaderNative_Alloc(int imageDepth)
    {
        FFMPEGReaderNative *pObj = new FFMPEGReaderNative();
        pObj->Initialize(imageDepth);
        return pObj;
    }
    
    void FFMPEGReaderNative_Dealloc(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        delete pObj;
    }
    
    int FFMPEGReaderNative_GetWidth(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->GetWidth();
    }
    
    int FFMPEGReaderNative_GetHeight(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->GetHeight();
    }
    
    int FFMPEGReaderNative_GetAudioBitsPerSample(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->GetAudioBitsPerSample();
    }
    
    int FFMPEGReaderNative_GetAudioSampleRate(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->GetAudioSampleRate();
    }
    
    int FFMPEGReaderNative_GetAudioNumChannels(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->GetAudioNumChannels();
    }
    
    int FFMPEGReaderNative_Open(void *obj, char *fn)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->Open(fn);
    }
    
    int FFMPEGReaderNative_NextFrame(void *obj, int *frameType, int *requiredBufferSize, bool *eos)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->NextFrame(frameType, requiredBufferSize, eos);
    }
    
    int FFMPEGReaderNative_ReadFrameData(void *obj, void *buffer, int *bytesRead, double *timestamp)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->ReadFrameData((uint8_t*)buffer, bytesRead, timestamp);
    }
    
    int FFMPEGReaderNative_Close(void *obj)
    {
        FFMPEGReaderNative *pObj = (FFMPEGReaderNative*)obj;
        return pObj->Close();
    }
}
  
  //**********************************************************************  
  // Define ctor for object that contains the unmanaged data associated
  // with a MP4Writer object
  //**********************************************************************  
  FFMPEGReaderNative::FFMPEGReaderNative() :
      formatCtx(nullptr),
      videoStreamIndex(-1),
      videoCodec(nullptr),
      videoCodecCtx(nullptr),
      videoFrame(nullptr),
      convertedVideoFrame(nullptr),
      convertedVideoBuffer(nullptr),
      audioStreamIndex(-1),
      audioCodec(nullptr),
      audioCodecCtx(nullptr),
      audioFrame(nullptr),
      audioBufferSize(0),
      outputFormat(AV_PIX_FMT_BGR32),
      bytesPerPixel(4)
  {
      audioBuffers[0] = nullptr;
      audioBuffers[1] = nullptr;
      audioClock = 0.0;
  }

  //**********************************************************************
  // Define dtor
  //**********************************************************************
  FFMPEGReaderNative::~FFMPEGReaderNative()
  {
      if (formatCtx != nullptr)
      {
          avformat_close_input(&formatCtx);
          formatCtx = nullptr; // NOTE: The formatCtx is freed by the call to avformat_close_input()
      }
      videoCodec = nullptr; // This appears to be a weak reference from FFMPEG
      videoCodecCtx = nullptr;
      audioCodec = nullptr; // This appears to be a weak reference from FFMPEG
      audioCodecCtx = nullptr;
      if (videoFrame != nullptr)
      {
          av_frame_free(&videoFrame);
          videoFrame = nullptr;
      }
      if (convertedVideoFrame != nullptr)
      {
          av_frame_free(&convertedVideoFrame);
          convertedVideoFrame = nullptr;
      }
      if (convertedVideoBuffer != nullptr)
      {
          av_free(convertedVideoBuffer);
          convertedVideoBuffer = nullptr;
      }
      if (audioFrame != nullptr)
      {
          av_frame_free(&audioFrame);
          audioFrame = nullptr;
      }
      if (audioBuffers[0] != nullptr)
      {
          av_freep(&audioBuffers[0]);
          audioBuffers[0] = nullptr;
      }
      if (audioBuffers[1] != nullptr)
      {
          av_freep(&audioBuffers[1]);
          audioBuffers[1] = nullptr;
      }
  }
  
  HRESULT FFMPEGReaderNative::ConvertFFMPEGError(int error)
  {
      HRESULT hr = E_FAIL;
      switch (error)
      {
      case AVERROR_BUFFER_TOO_SMALL: hr = PSIERR_BUFFER_TOO_SMALL; break;
      case AVERROR_BSF_NOT_FOUND: hr = PSIERR_BSF_NOT_FOUND; break;
      case AVERROR_BUG: hr = PSIERR_BUG; break;
      case AVERROR_DECODER_NOT_FOUND: hr = PSIERR_DECODER_NOT_FOUND; break;
      case AVERROR_DEMUXER_NOT_FOUND: hr = PSIERR_DEMUXER_NOT_FOUND; break;
      case AVERROR_ENCODER_NOT_FOUND: hr = PSIERR_ENCODER_NOT_FOUND; break;
      case AVERROR_EOF: hr = PSIERR_EOF; break;
      case AVERROR_EXIT: hr = PSIERR_EXIT; break;
      case AVERROR_EXTERNAL: hr = PSIERR_EXTERNAL; break;
      case AVERROR_FILTER_NOT_FOUND: hr = PSIERR_FILTER_NOT_FOUND; break;
      case AVERROR_INVALIDDATA: hr = PSIERR_INVALIDDATA; break;
      case AVERROR_MUXER_NOT_FOUND: hr = PSIERR_MUXER_NOT_FOUND; break;
      case AVERROR_OPTION_NOT_FOUND: hr = PSIERR_OPTION_NOT_FOUND; break;
      case AVERROR_PATCHWELCOME: hr = PSIERR_PATCHWELCOME; break;
      case AVERROR_PROTOCOL_NOT_FOUND: hr = PSIERR_PROTOCOL_NOT_FOUND; break;
      case AVERROR_STREAM_NOT_FOUND: hr = PSIERR_STREAM_NOT_FOUND; break;
      case AVERROR_BUG2: hr = PSIERR_BUG2; break;
      case AVERROR_UNKNOWN: hr = PSIERR_UNKNOWN; break;
      case AVERROR_EXPERIMENTAL: hr = PSIERR_EXPERIMENTAL; break;
      case AVERROR_INPUT_CHANGED: hr = PSIERR_INPUT_CHANGED; break;
      case AVERROR_OUTPUT_CHANGED: hr = PSIERR_OUTPUT_CHANGED; break;
      case AVERROR_HTTP_BAD_REQUEST: hr = PSIERR_HTTP_BAD_REQUEST; break;
      case AVERROR_HTTP_UNAUTHORIZED: hr = PSIERR_HTTP_UNAUTHORIZED; break;
      case AVERROR_HTTP_FORBIDDEN: hr = PSIERR_HTTP_FORBIDDEN; break;
      case AVERROR_HTTP_NOT_FOUND: hr = PSIERR_HTTP_NOT_FOUND; break;
      case AVERROR_HTTP_OTHER_4XX: hr = PSIERR_HTTP_OTHER_4XX; break;
      case AVERROR_HTTP_SERVER_ERROR: hr = PSIERR_HTTP_SERVER_ERROR; break;
      }
      return hr;
  }
  
  HRESULT FFMPEGReaderNative::Initialize(int imageDepth)
  {
      switch (imageDepth)
      {
      case 24:
		  outputFormat = AV_PIX_FMT_RGB24;
		  bytesPerPixel = 3;
          break;
      case 32:
          outputFormat = AV_PIX_FMT_RGB32;
          bytesPerPixel = 4;
          break;
      }
      avcodec_register_all();
      av_register_all();
      avformat_network_init();
      return S_OK;
  }

  //**********************************************************************
  // GetWidth() returns the width of each video frame in the currently opened video
  // Method will return 0 if video is not opened
  //**********************************************************************
  int FFMPEGReaderNative::GetWidth()
  {
      return (videoCodecCtx == nullptr) ? 0 : videoCodecCtx->width;
  }
  
  int FFMPEGReaderNative::GetAudioBitsPerSample()
  {
      return (audioCodecCtx == nullptr) ? 0 : audioCodecCtx->bits_per_coded_sample;
  }
  
  int FFMPEGReaderNative::GetAudioSampleRate()
  {
      return (audioCodecCtx == nullptr) ? 0 : audioCodecCtx->sample_rate;
  }
  
  int FFMPEGReaderNative::GetAudioNumChannels()
  {
      return (audioCodecCtx == nullptr) ? 0 : audioCodecCtx->channels;
  }
  
  //**********************************************************************
  // Getheight() returns the height of each video frame in the currently opened video
  // Method will return 0 if video is not opened
  //**********************************************************************
  int FFMPEGReaderNative::GetHeight()
  {
      return (videoCodecCtx == nullptr) ? 0 : videoCodecCtx->height;
  }
  
  HRESULT FFMPEGReaderNative::InitializeVideoStream()
  {
      if (videoStreamIndex == -1)
      {
          return S_OK;
      }

      // Next find the codec associated with each stream
      videoCodec = avcodec_find_decoder(formatCtx->streams[videoStreamIndex]->codec->codec_id);
      if (videoCodec == nullptr)
      {
          return PSIERR_DECODER_NOT_FOUND;
      }
      videoCodecCtx = formatCtx->streams[videoStreamIndex]->codec;
      
      /*if (videoCodec->capabilities & AV_CODEC_CAP_TRUNCATED)
        {
        videoCodecCtx->flags |= AV_CODEC_FLAG_TRUNCATED;
        }*/
      
      // Open the video codec
      int avResult = avcodec_open2(videoCodecCtx, videoCodec, nullptr);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      
      // Allocate our video frame
      videoFrame = av_frame_alloc();
      if (videoFrame == nullptr)
      {
          return E_OUTOFMEMORY;
      }
      convertedVideoFrame = av_frame_alloc();
      if (convertedVideoFrame == nullptr)
      {
          return E_OUTOFMEMORY;
      }
      avResult = avpicture_get_size(outputFormat, videoCodecCtx->width, videoCodecCtx->height);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      convertedVideoBuffer = (uint8_t*)av_malloc(avResult);
      avpicture_fill((AVPicture*)convertedVideoFrame, convertedVideoBuffer, outputFormat, videoCodecCtx->width, videoCodecCtx->height);
      return S_OK;
  }
  
  HRESULT FFMPEGReaderNative::InitializeAudioStream()
  {
      if (audioStreamIndex == -1)
      {
          return S_OK;
      }
      
      // Find the audio codec
      audioCodec = avcodec_find_decoder(formatCtx->streams[audioStreamIndex]->codec->codec_id);
      if (audioCodec == nullptr)
      {
          return PSIERR_DECODER_NOT_FOUND;
      }
      audioCodecCtx = formatCtx->streams[audioStreamIndex]->codec;

      int avResult = avcodec_open2(audioCodecCtx, audioCodec, nullptr);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      
      // Allocate our audio frame
      audioFrame = av_frame_alloc();
      if (audioFrame == nullptr)
      {
          return E_OUTOFMEMORY;
      }
      
      avResult = av_samples_alloc(audioBuffers, nullptr, audioCodecCtx->channels, audioCodecCtx->sample_rate, audioCodecCtx->sample_fmt, 0);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      audioBufferSize = avResult;
      audioFrame->linesize[0] = avResult;
      audioFrame->linesize[1] = avResult;
      audioFrame->data[0] = audioBuffers[0];
      audioFrame->data[1] = audioBuffers[1];
      
      return S_OK;
  }
  
  //**********************************************************************
  // Open() opens an MPEG file for playback via FFMPEG
  // Parameters:
  //   filename - Name of .mp4 file to playback
  //**********************************************************************
  HRESULT FFMPEGReaderNative::Open(char *filename)
  {
      std::string fn(filename);
      int avResult = avformat_open_input(&formatCtx, fn.c_str(), nullptr, nullptr);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      
      avResult = avformat_find_stream_info(formatCtx, nullptr);
      if (avResult < 0)
      {
          return ConvertFFMPEGError(avResult);
      }
      
      // Find which stream is our audio stream and which is our video stream
      videoStreamIndex = -1;
      audioStreamIndex = -1;
      for (int i = 0; i < (int)formatCtx->nb_streams; i++)
      {
          if (formatCtx->streams[i]->codec->codec_type == AVMEDIA_TYPE_VIDEO)
          {
              videoStreamIndex = i;
          }
          else if (formatCtx->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
          {
              audioStreamIndex = i;
          }
      }
      if (audioStreamIndex == -1 && videoStreamIndex == -1)
      {
          return E_UNEXPECTED;
      }
      
      InitializeVideoStream();
      InitializeAudioStream();
      
      av_init_packet(&packet);
      
      av_read_play(formatCtx);
      return S_OK;
  }
  
  HRESULT FFMPEGReaderNative::NextFrame(int *streamIndex, int *requiredBufferSize, bool *eos)
  {
      int avResult = av_read_frame(formatCtx, &packet);
      if (avResult < 0)
      {
          if (avResult == AVERROR_EOF)
          {
              *eos = true;
              return S_OK;
          }
          return ConvertFFMPEGError(avResult);
      }
      if (packet.stream_index == videoStreamIndex)
      {
          *streamIndex = 0;
          *requiredBufferSize = videoCodecCtx->width * videoCodecCtx->height * bytesPerPixel;
      }
      else if (packet.stream_index == audioStreamIndex)
      {
          *streamIndex = 1;
          *requiredBufferSize = audioBufferSize;
      }
      else
      {
          return S_FALSE;
      }
      
      return S_OK;
  }

  HRESULT FFMPEGReaderNative::ReadFrameData(uint8_t *dataBuffer, int *bytesRead, double *timestampMillisecs)
  {
      int decodedFrame;
      if (packet.stream_index == videoStreamIndex)
      {
#pragma warning(disable:4189)
          int dataRead = avcodec_decode_video2(videoCodecCtx, videoFrame, &decodedFrame, &packet);
          if (dataRead < 0)
          {
              return ConvertFFMPEGError(dataRead);
          }
          
          if (decodedFrame != 0)
          {
              double presentationTimestamp;
              if (packet.dts != AV_NOPTS_VALUE)
              {
                  presentationTimestamp = (double)av_frame_get_best_effort_timestamp(videoFrame);
              }
              else
              {
                  presentationTimestamp = 0.0;
              }
              presentationTimestamp *= av_q2d(videoCodecCtx->time_base);
              *timestampMillisecs = presentationTimestamp;
              
              // Convert the image from raw format to RGB
              struct SwsContext *convertorCtx = sws_getCachedContext(nullptr, videoCodecCtx->width, videoCodecCtx->height, videoCodecCtx->pix_fmt,
                     videoCodecCtx->width, videoCodecCtx->height, outputFormat, SWS_POINT, nullptr, nullptr, nullptr);
              uint8_t *const data[2] = {(uint8_t*)dataBuffer, nullptr};
              sws_scale(convertorCtx, ((AVPicture*)videoFrame)->data, ((AVPicture*)videoFrame)->linesize,
                        0, videoCodecCtx->height, data, ((AVPicture *)convertedVideoFrame)->linesize);
              sws_freeContext(convertorCtx);
              *bytesRead = videoCodecCtx->width * videoCodecCtx->height * bytesPerPixel;
          }
          else
          {
              return S_FALSE;
          }
      }
      else if (packet.stream_index == audioStreamIndex)
      {
          int samplesDecoded = avcodec_decode_audio4(audioCodecCtx, audioFrame, &decodedFrame, &packet);
          if (samplesDecoded < 0)
          {
              return ConvertFFMPEGError(samplesDecoded);
          }
          if (decodedFrame != 0)
          {
              auto ConvertSample = [&](float sample)
              {
                  if (sample < -1.0f)
                  {
                      sample = -1.0f;
                  }
                  else if (sample > +1.0f)
                  {
                      sample = +1.0f;
                  }
                  return (int16_t)(sample * 32767.0f);
              };
              int16_t *outputBuffer = (int16_t*)dataBuffer;
              if (audioFrame->channels == 1)
              {
                  float *inputChannel0 = (float*)audioFrame->extended_data[0];
                  for (int i = 0; i < audioFrame->nb_samples; i++)
                  {
                      outputBuffer[i] = ConvertSample(*inputChannel0++);
                  }
              }
              else
              {
                  float *inputChannel0 = (float*)audioFrame->extended_data[0];
                  float *inputChannel1 = (float*)audioFrame->extended_data[1];
                  for (int i = 0; i < audioFrame->nb_samples; i++)
                  {
                      outputBuffer[2 * i + 0] = ConvertSample(*inputChannel0++);
                      outputBuffer[2 * i + 1] = ConvertSample(*inputChannel1++);
                  }
              }
              *bytesRead = audioFrame->channels * 2 * audioFrame->nb_samples;
              double presentationTimestamp = audioClock;
              audioClock += 1000.0 * ((double)audioFrame->nb_samples / (double)audioCodecCtx->sample_rate);
              *timestampMillisecs = presentationTimestamp;
              return S_OK;
          }
          else
          {
              return S_FALSE;
          }
      }
      av_packet_unref(&packet);
      
      return S_OK;
  }

  //**********************************************************************
  // Closes the current file. This must be called to ensure the MP4 file
  // is written properly.
  //**********************************************************************
  HRESULT FFMPEGReaderNative::Close()
  {
      if (videoCodecCtx != nullptr)
      {
          avcodec_close(videoCodecCtx);
          av_free(videoCodecCtx);
          videoCodecCtx = nullptr;
      }
      if (videoFrame != nullptr)
      {
          av_frame_free(&videoFrame);
          videoFrame = nullptr;
      }
      return S_OK;
  }
}}}}}
#pragma warning(pop)
#else // USE_FFMPEG
#ifdef _WINDOWS
__declspec(dllexport)
#endif
int DummyFunctionSoLibGetsGenerated()
{
	return 0;
}
#endif // USE_FFMPEG
