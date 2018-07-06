// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#pragma managed(push, off)
#include <librealsense2/rs.hpp>
#include <windows.h>
#include "IRealSenseDeviceUnmanaged.h"

class RealSenseDeviceUnmanaged : public IRealSenseDeviceUnmanaged
{
private:
	rs2::pipeline pipeline;
	unsigned int colorWidth;
	unsigned int colorHeight;
	unsigned int colorBpp;
	unsigned int colorStride;
	unsigned int depthWidth;
	unsigned int depthHeight;
	unsigned int depthBpp;
	unsigned int depthStride;
	unsigned int refCount;

	void DumpDeviceInfo();
public:
	RealSenseDeviceUnmanaged();
	~RealSenseDeviceUnmanaged();
	virtual unsigned int Initialize();
	virtual unsigned int ReadFrame(char *colorBuffer, unsigned int colorBufferLen, char *depthBuffer, unsigned int depthBufferLen);
	virtual unsigned int GetColorWidth() { return colorWidth; }
	virtual unsigned int GetColorHeight() { return colorHeight; }
	virtual unsigned int GetColorBpp() { return colorBpp; }
	virtual unsigned int GetColorStride() { return colorStride; }
	virtual unsigned int GetDepthWidth() { return depthWidth; }
	virtual unsigned int GetDepthHeight() { return depthHeight; }
	virtual unsigned int GetDepthBpp() { return depthBpp; }
	virtual unsigned int GetDepthStride() { return depthStride; }
	virtual unsigned int AddRef();
	virtual unsigned int Release();
};
#pragma managed(pop)
