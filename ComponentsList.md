---
layout: default
title:  Components
---

# Components

This document contains a [**list of components**](/psi/ComponentsList#ListOfComponents) available in the Platform for Situated Intelligence repository, as well as [**pointers to other third-party repositories**](/psi/ComponentsList#ThirdParty) containing other Platform for Situated Intelligence components.

<a name="ListOfComponents"></a>

## 1. Components in the \psi Repository

The table below contains the list of \psi components that are available in the current release, together with the namespace in which the component can be found (in general, the NuGet packages in which you can find the component follow the same naming convention, potentially with additional platform suffixes)

| | Name | Description | Windows | Linux | Namespace / NuGet Package |
| :--- | :---- | :------------------------ | :----: |:----: |:--------- |
| __Sensors__ | | | | | | 
| | `AudioCapture` | Component that captures and streams audio from an input device such as a microphone |	AnyCPU | Yes | Microsoft.Psi.Audio |
| | `MediaCapture` | Component that captures and streams video and audio from a video camera (audio is currently supported only on the Windows version) | X64 | Yes | Microsoft.Psi.Media |
| | `RealSenseSensor` |	Component that captures and streams video and depth from an Intel RealSense camera | X64 | No | Microsoft.Psi.RealSense |
| | `KinectSensor` | Component that captures and streams information (video, depth, audio, tracked bodies, etc.) from a Kinect One (v2) sensor | AnyCPU | No | Microsoft.Psi.Kinect |
| __File sources__ | | | | | |
| | `FFMPEGMediaSource` | Component that streams video and audio from an MPEG file | No | Yes | Microsoft.Psi.Media |
| | `MediaSource` | Component that streams video and audio from a media file | X64 | No | Microsoft.Psi.Media |
| | `WaveFileAudioSource` | Component that streams audio from a WAVE file | AnyCPU | Yes | Microsoft.Psi.Audio |
| __File writers__ | | | | | |
| | `WaveFileWriter` | Component that writes an audio stream into a WAVE file | AnyCPU | Yes | Microsoft.Psi.Audio |
| | `MPEG4Writer` | Component that writes video and audio streams into an MPEG-4 file | X64 | No | Microsoft.Psi.Media |
| __Imaging__ | | | | | |
| | `ImageEncoder` | Component that encodes an image using a specified encoder (e.g. JPEG, PNG) | AnyCPU | Yes | Microsoft.Psi.Imaging |
| | `ImageDecoder` | Component that decodes an image using a specified decoder (e.g. JPEG, PNG) | AnyCPU | Yes | Microsoft.Psi.Imaging |
| | `ImageTransformer` | Component that transforms an image given a specified transformer | AnyCPU | Yes | Microsoft.Psi.Imaging |
| __Vision__ | | | | | |
| | `ImageAnalyzer` | Component that performs image analysis via [Microsoft Cognitive Services Vision API](https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/). | AnyCPU | No | Microsoft.Psi.CognitiveServices.Vision |
| | `FaceRecognizer` | Component that performs face recognition via [Microsoft Cognitive Services Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/). | AnyCPU | No | Microsoft.Psi.CognitiveServices.Face |
| __Audio processing__ | | | | | |
| | `AudioResampler` | Component that resamples an audio stream into a different format | AnyCPU | No | Microsoft.Psi.Audio |
| | `AcousticFeaturesExtractor` | Component that extracts acoustic features (e.g. LogEnergy, ZeroCrossing, FFT) from an audio stream | AnyCPU | Yes | Microsoft.Psi.Audio |
| __Speech processing__ | | | | | |
| | `SystemVoiceActivityDetector` | Component that performs voice activity detection by using the desktop speech recognition engine from `System.Speech` | AnyCPU | No | Microsoft.Psi.Speech |
| | `SimpleVoiceActivityDetector` | Component that performs voice activity detection via a simple heuristic using the energy in the audio stream | AnyCPU | Yes | Microsoft.Psi.Speech |
| | `SystemSpeechRecognizer` | Component that performs speech recognition using the desktop speech recognition engine from `System.Speech`. | AnyCPU | No | Microsoft.Psi.Speech |
| | `SystemSpeechIntentDetector` | Component that performs grammar-based intent detection using the desktop speech recognition engine from `System.Speech`. | AnyCPU | No | Microsoft.Psi.Speech |
| | `MicrosoftSpeechRecognizer` | Component that performs speech recognition using the Microsoft Speech Platform SDK. | AnyCPU | No | Microsoft.Psi.MicrosoftSpeech |
| | `MicrosoftSpeechIntentDetector` | Component that performs grammar-based intent detection using the speech recognition engine from the Microsoft Speech Platform SDK. | AnyCPU | No | Microsoft.Psi.MicrosoftSpeech |
| | `AzureSpeechRecognizer` | Component that performs speech recognition using the [Microsoft Cognitive Services Speech to Text Service](https://azure.microsoft.com/en-us/services/cognitive-services/speech/). | AnyCPU | Yes | Microsoft.Psi.CognitiveServices.Speech |
| | `LUISIntentDetector` | Component that performs intent detection and entity extraction using the [Microsoft Cognitive Services LUIS API](https://www.luis.ai/). | AnyCPU | Yes | Microsoft.Psi.CognitiveServices.Language | 
| | <div style="color:red;font-weight:bold">[DEPRECATED]</div> `BingSpeechRecognizer` | Component that performs speech recognition using the [Microsoft Cognitive Services Bing Speech API](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech). | AnyCPU | Yes | Microsoft.Psi.CognitiveServices.Speech |
| __Output__ | | | | | |
| | `AudioPlayer` | Component that plays back an audio stream to an output device such as the speakers. | AnyCPU | Yes | Microsoft.Psi.Audio |
| | `SystemSpeechSynthesizer` | Component that performs speech synthesis via the desktop speech synthesis engine from `System.Speech`. | AnyCPU | No | Microsoft.Psi.Speech


<a name="ThirdParty"></a>

## 2. Repositories with Components by Third Parties

You might also be interested in exploring the repositories below containing components for the Platform for Situated Intelligence ecosystem written by third parties, not affiliated with Microsoft. Microsoft makes NO WARRANTIES about these components, including about their usability or reliability.

| Repo | Description |
| :-- | :-- |
| https://github.com/bsu-slim/psi-components | Components developed by the [SLIM research group](https://coen.boisestate.edu/slim/) at Boise State University. |