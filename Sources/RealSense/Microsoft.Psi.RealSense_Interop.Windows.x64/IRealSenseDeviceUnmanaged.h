// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

//**********************************************************************
// Define the interface that our managed code (RealSenseDevice) uses to
// talk to the unmanaged side of the component.
//**********************************************************************
struct IRealSenseDeviceUnmanaged
{
	virtual unsigned int Initialize() = 0;
	virtual unsigned int ReadFrame(char *colorBuffer, unsigned int colorBufferLen, char *depthBuffer, unsigned int depthBufferLen) = 0;
	virtual unsigned int GetColorWidth() = 0;
	virtual unsigned int GetColorHeight() = 0;
	virtual unsigned int GetColorBpp() = 0;
	virtual unsigned int GetColorStride() = 0;
	virtual unsigned int GetDepthWidth() = 0;
	virtual unsigned int GetDepthHeight() = 0;
	virtual unsigned int GetDepthBpp() = 0;
	virtual unsigned int GetDepthStride() = 0;
	virtual unsigned int AddRef() = 0;
	virtual unsigned int Release() = 0;
};

//**********************************************************************
// CreateRealSenseDeviceUnmanaged creates the unmanaged side of our
// RealSenseDevice.
//**********************************************************************
unsigned int CreateRealSenseDeviceUnmanaged(IRealSenseDeviceUnmanaged **device);
