// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "RealSenseDevice.h"

namespace Microsoft {
	namespace Psi {
		namespace RealSense {
			namespace Windows {
				RealSenseDevice::RealSenseDevice()
				{
					IRealSenseDeviceUnmanaged *pDevice;
					CreateRealSenseDeviceUnmanaged(&pDevice);
					m_device = pDevice;
				}

				RealSenseDevice::~RealSenseDevice()
				{
					if (m_device != nullptr)
					{
						m_device->Release();
						m_device = nullptr;
					}
				}

				unsigned int RealSenseDevice::ReadFrame(System::IntPtr colorBuffer, unsigned int colorBufferLen, System::IntPtr depthBuffer, unsigned int depthBufferLen)
				{
					pin_ptr<char> colorBuf = (char*)colorBuffer.ToPointer();
					pin_ptr<char> depthBuf = (char*)depthBuffer.ToPointer();
					return m_device->ReadFrame(colorBuf, colorBufferLen, depthBuf, depthBufferLen);
				}

				unsigned int RealSenseDevice::GetColorWidth()
				{
					return m_device->GetColorWidth();
				}

				unsigned int RealSenseDevice::GetColorHeight()
				{
					return m_device->GetColorHeight();
				}

				unsigned int RealSenseDevice::GetColorBpp()
				{
					return m_device->GetColorBpp();
				}

				unsigned int RealSenseDevice::GetColorStride()
				{
					return m_device->GetColorStride();
				}

				unsigned int RealSenseDevice::GetDepthWidth()
				{
					return m_device->GetDepthWidth();
				}

				unsigned int RealSenseDevice::GetDepthHeight()
				{
					return m_device->GetDepthHeight();
				}

				unsigned int RealSenseDevice::GetDepthBpp()
				{
					return m_device->GetDepthBpp();
				}

				unsigned int RealSenseDevice::GetDepthStride()
				{
					return m_device->GetDepthStride();
				}
			}
		}
	}
}
