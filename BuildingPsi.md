---
layout: default
title:  Building Psi
---

# Building Platform for Situated Intelligence

To build, first you will need to clone the Platform for Situated Intelligence [github repo](https://github.com/microsoft/psi "\psi"). Then, depending on which operating system you are, follow the steps below.

## On Windows

__Setup Visual Studio__:

We recommend you use [Visual Studio 2017](https://www.visualstudio.com/vs/) (the Community Edition of Visual Studio is sufficient). Make sure the following features are installed (you can check these features by running the Visual Studio Installer again and looking at both the Workloads and Individual Components tabs):

* Make sure you have installed the __.NET desktop development__ workload, as well as the __.NET Framework 4.7 targeting pack__ feature.
* Make sure you have installed the __Desktop development with C++__ workload, as well as the __Windows 10 SDK (10.0.17134.0) for Desktop C++ [x86 and x64]__ feature.

__Optional prerequisites__:

A couple of the projects in the Platform for Situated Intelligence codebase have install prerequisites. If you want to build these projects as part of the solution, you will need to install the prerequisites below. If the prerequisites are not found, these projects will not be build (the rest of the solution will build correctly.)

* __Open CV Sample__: the __OpenCVSample__ and __OpenCVSample.Interop__ sample projects (from the `Samples` folder) require an installation of OpenCV. OpenCV can be obtained [here](http://opencv.org/releases.html). The sample relies on version 3.3.0. For these projects to build correctly, will need to set an environment variable named `OpenCVDir` that points to your OpenCV installation. The path should be the root of OpenCV which contains the _sources_ directory (along with the license). For instance, `D:\cv3.3\opencv`.

* __RealSense Support__: the __Microsoft.Psi.RealSense.Windows.x64__ and __Microsoft.Psi.RealSense_Interop.Windows.x64__ projects (from the `Sources\RealSense` folder) provide support for the Intel RealSense depth camera. This requires the [Intel RealSense SDK v2.0](https://realsense.intel.com/sdk-2/) to be installed. Additionally, you will need to set an environment variable named `RealSenseSDKDir` that points to the location in which you installed the SDK (typically _C:\Program Files (x86)\Intel RealSense SDK 2.0_).

* __FFMPEG Support__: the __Microsoft.Psi.Media.Windows.x64__ and __Microsoft.Psi.Media_Interop.Windows.x64__ projects (from the `Sources\Media` folder) provide support for playback of media files via [FFMPEG](https://ffmpeg.org/). Use of FFMPEG requires the [FFMPEG SDK](https://ffmpeg.org/download.html#build-windows) to be installed. Additionally, you will need to set an environment variable named `FFMPEGDir` that points to the location in which you installed the SDK. The path should be the root of the FFMPEG folder which contains the _LICENSE.txt_ file. For instance, `D:\FFMPEG\ffmpeg-20180227-fa0c9d6-win64-dev`.

* __Microsoft.Speech Recognizer__: the __Microsoft.Psi.MicrosoftSpeech.Windows__ project (from the `Sources\Integrations` folder), which includes the \\psi components for the Microsoft.Speech recognizer requires the [Microsoft Speech Platform SDK v11.0](http://go.microsoft.com/fwlink/?LinkID=223570). Note that only the 64-bit version of the SDK is currently supported. Additionally, you will need to set an environment variable named `MsSpeechSdkDir` that points to the location in which you installed the SDK. The path should be the root of the SDK folder which contains the _Assembly_ directory. By default, this is `C:\Program Files\Microsoft SDKs\Speech\v11.0`. In order to run applications developed using this component, you will also need to install the [Microsoft Speech Platform Runtime v11.0](http://go.microsoft.com/fwlink/?LinkID=223568) as well as the applicable [Language Pack](http://go.microsoft.com/fwlink/?LinkID=223569) for the speech recognition language you wish to use (e.g. en-US).

__Build__:

* Launch Visual Studio 2017.
* Open the `Psi.sln` solution from the root of your cloned repo.
* From the *Build* menu choose *Rebuild Solution*.
  * This will build the currently selected configuration (Release or Debug).
  * It is a good idea to build both configurations - select the other configuration and rebuild.

## On Linux

__Prerequisites__:

You will need .NET Core on Linux. You can find the [installation instructions here](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x).

Although \psi is built on .NET Standard, IL assembly still depends on _Mono's_ `ilasm` tool. [Install at least `mono-devel`](https://www.mono-project.com/download/stable/#download-lin).

__Build__:

To build, launch the `./build.sh` script. This will build all individual projects that support Linux by calling individual `build.sh` scripts that are associated with each of these projects. 
