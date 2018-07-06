// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "IRealSenseDeviceUnmanaged.h"

namespace Microsoft {
    namespace Psi {
        namespace RealSense {
            namespace Windows {
                //**********************************************************************
                // RealSenseDevice defines a managed wrapper around the unmanaged side
                // of our RealSense device.
                //**********************************************************************
                public ref class RealSenseDevice sealed
                {
                private:
                    IRealSenseDeviceUnmanaged *m_device; // The actual unmanaged device code
                public:
                    RealSenseDevice();
                    ~RealSenseDevice();

                    unsigned int ReadFrame(System::IntPtr colorBuffer, unsigned int colorBufferLen, System::IntPtr depthBuffer, unsigned int depthBufferLen);
                    unsigned int GetColorWidth();
                    unsigned int GetColorHeight();
                    unsigned int GetColorBpp();
                    unsigned int GetColorStride();
                    unsigned int GetDepthWidth();
                    unsigned int GetDepthHeight();
                    unsigned int GetDepthBpp();
                    unsigned int GetDepthStride();
                };
            }
        }
    }
}
