# OpenCV Sample

This sample demostrates how to integrate OpenCV with Platform for Situated Intelligence. There are two projects in this sample:
  1) OpenCVSample.csproj is the main sample. It creates a webcam source and sends the images from the camera to OpenCV to convert them to gray scale.
  2) OpenCVSampleInterop is a C++ project that is used to interop between C# and OpenCV.

The OpenCVSample.Interop project is the interop layer between the sample (project OpenCVSample) and OpenCV. In order to build this project you will need OpenCV installed on your machine. OpenCV can be obtained [here](http://opencv.org/releases.html). The sample relies on version 4.1.1. You will need to set an environment variable named "OpenCVDir_V4" that points to your OpenCV installation. The path should be the root of OpenCV which contains the "sources" directory (along with the license). For instance, "D:\OpenCV-4.1.1\opencv".
