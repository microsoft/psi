// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "stdafx.h"
#include "ImageBuffer.h"

namespace Microsoft
{
	namespace Psi
	{
		namespace Samples
		{
			namespace OpenCV
			{
				ImageBuffer::ImageBuffer(int width, int height, System::IntPtr data, int stride)
				{
					this->Width = width;
					this->Height = height;
					this->Data = data;
					this->Stride = stride;
				}
			}
		}
	}
}
