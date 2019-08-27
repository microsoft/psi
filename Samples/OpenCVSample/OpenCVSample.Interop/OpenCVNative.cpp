// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "stdafx.h"
#pragma warning(push)
#pragma warning(disable : 4793)
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/core/cvdef.h>
#pragma warning(pop)
#include <msclr/marshal_cppstd.h>
#include "ImageBuffer.h"

namespace Microsoft
{
	namespace Psi
	{
		namespace Samples
		{
			namespace OpenCV
			{
				public ref class OpenCVMethods
				{
					// helper function
					static cv::Mat WrapInMat(ImageBuffer ^img)
					{
						cv::Mat ret = cv::Mat(img->Height, img->Width, CV_MAKETYPE(CV_8U, img->Stride / img->Width), (void *)img->Data, cv::Mat::AUTO_STEP);
						return ret;
					}

				public:
					static ImageBuffer^ ToGray(ImageBuffer ^colorImage, ImageBuffer ^grayImage)
					{
						cv::Mat greyMat = WrapInMat(grayImage);
						cv::Mat colorMat = WrapInMat(colorImage);
						cv::cvtColor(colorMat, greyMat, cv::COLOR_BGR2GRAY);
						return grayImage;
					}

					static void SaveImage(ImageBuffer ^img, System::String ^filename)
					{
                        std::string fn = msclr::interop::marshal_as<std::string>(filename);
                        cv::Mat matImg = WrapInMat(img);
                        cv::imwrite(fn, matImg);
					}
				};
			}
		}
	}
}
