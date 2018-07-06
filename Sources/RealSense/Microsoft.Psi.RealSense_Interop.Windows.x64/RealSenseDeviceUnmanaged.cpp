// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "RealSenseDeviceUnmanaged.h"
#include "librealsense2\h\rs_sensor.h"

#pragma managed(push, off)
RealSenseDeviceUnmanaged::RealSenseDeviceUnmanaged()
{
	refCount = 0;
}

RealSenseDeviceUnmanaged::~RealSenseDeviceUnmanaged()
{
	pipeline.stop();
}

#ifdef DUMP_DEVICE_INFO
void RealSenseDeviceUnmanaged::DumpDeviceInfo()
{
	rs2::context ctx;
	rs2::device_list devices = ctx.query_devices();
	for (rs2::device device : devices)
	{
		wchar_t buf[1024];
		swprintf(buf, L"Device: %S\n", device.get_info(RS2_CAMERA_INFO_NAME));
		OutputDebugString(buf);
		std::vector<rs2::sensor> sensors = device.query_sensors();
		for (rs2::sensor sensor : sensors)
		{
			swprintf(buf, L"Sensor: %S\n", sensor.get_info(RS2_CAMERA_INFO_NAME));
			OutputDebugString(buf);
			std::vector<rs2::stream_profile> strmProfiles = sensor.get_stream_profiles();
			for (int i = 0; i < strmProfiles.size(); i++)
			{
				rs2::stream_profile sprof = strmProfiles[i];
				int w, h;
				rs2_get_video_stream_resolution(sprof.get(), &w, &h, nullptr);
				swprintf(buf, L"Profile %d: StrmIndex:%d  StrmType:%S  Width:%d  Height:%d  Format:%S  FPS:%d\n",
					i,
					sprof.stream_index(),
					rs2_stream_to_string(sprof.stream_type()),
					w, h,
					rs2_format_to_string(sprof.format()),
					sprof.fps());
				OutputDebugString(buf);
			}

			for (int i = 0; i < RS2_OPTION_COUNT; i++)
			{
				rs2_option option_type = static_cast<rs2_option>(i);
				if (sensor.supports(option_type))
				{
					const char* description = sensor.get_option_description(option_type);
					swprintf(buf, L"    Option:%S\n", description);
					OutputDebugString(buf);
					float current_value = sensor.get_option(option_type);
					swprintf(buf, L"    Value:%f\n", current_value);
					OutputDebugString(buf);
				}
			}
		}
	}
}
#endif // DUMP_DEVICE_INFO

unsigned int RealSenseDeviceUnmanaged::Initialize()
{
	try
	{
		rs2::config config;
		config.enable_all_streams();
		rs2::pipeline_profile pipeprof = pipeline.start();

		// Read 30 frames so that things like autoexposure settle
		for (int i = 0; i < 30; i++)
		{
			try
			{
				pipeline.wait_for_frames();
			}
			catch (...)
			{
			}
		}


		rs2::frameset frame = pipeline.wait_for_frames();
		rs2::video_frame colorFrame = frame.get_color_frame();
		if (colorFrame)
		{
			colorWidth = colorFrame.get_width();
			colorHeight = colorFrame.get_height();
			colorBpp = colorFrame.get_bits_per_pixel();
			colorStride = colorFrame.get_stride_in_bytes();
		}
		rs2::depth_frame depthFrame = frame.get_depth_frame();
		if (depthFrame)
		{
			depthWidth = depthFrame.get_width();
			depthHeight = depthFrame.get_height();
			depthBpp = depthFrame.get_bits_per_pixel();
			depthStride = depthFrame.get_stride_in_bytes();
		}
	}
	catch (...)
	{
		return E_UNEXPECTED;
	}
	return 0;
}

unsigned int RealSenseDeviceUnmanaged::ReadFrame(char *colorBuffer, unsigned int colorBufferLen, char *depthBuffer, unsigned int depthBufferLen)
{
	try
	{
		rs2::frameset frame = pipeline.wait_for_frames();
		auto colorFrame = frame.get_color_frame();
		int w = colorFrame.get_width();
		int h = colorFrame.get_height();
		int stride = colorFrame.get_stride_in_bytes();
		int bpp = colorFrame.get_bytes_per_pixel();
		unsigned int colorFrameSize = h * stride;
		if (colorFrameSize > colorBufferLen)
		{
			return E_UNEXPECTED;
		}
		char *srcRow = (char*)colorFrame.get_data();
		char *dstRow = colorBuffer;
		for (int y = 0; y < h; y++)
		{
			char *srcCol = srcRow;
			char *dstCol = dstRow;
			for (int x = 0; x < w; x++)
			{
				dstCol[2] = srcCol[0];
				dstCol[1] = srcCol[1];
				dstCol[0] = srcCol[2];
				srcCol += bpp;
				dstCol += 3;
			}
			srcRow += stride;
			dstRow += w * 3;
		}

		auto depthFrame = frame.get_depth_frame();
		unsigned int depthFrameSize = depthFrame.get_height() * depthFrame.get_stride_in_bytes();
		if (depthFrameSize > depthBufferLen)
		{
			return E_UNEXPECTED;
		}
		memcpy(depthBuffer, depthFrame.get_data(), depthFrameSize);
	}
	catch (...)
	{
	}
	return S_OK;
}

unsigned int RealSenseDeviceUnmanaged::AddRef()
{
	return refCount++;
}

unsigned int RealSenseDeviceUnmanaged::Release()
{
	unsigned int refcnt = --refCount;
	if (refcnt == 0)
	{
		delete this;
	}
	return refcnt;
}

unsigned int CreateRealSenseDeviceUnmanaged(IRealSenseDeviceUnmanaged **device)
{
	RealSenseDeviceUnmanaged *dev = new RealSenseDeviceUnmanaged();
	if (dev == nullptr)
	{
		return E_OUTOFMEMORY;
	}
	dev->Initialize();
	dev->AddRef();
	*device = dev;
	return S_OK;
}

#pragma managed(pop)
