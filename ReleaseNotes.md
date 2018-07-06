---
layout: default
title:  Release Notes
---

# Release Notes

**2018/07/02**: Beta-release, version 0.4.216.2

Interim release with support for new devices, runtime enhancements and several API changes, as well as minor bug fixes:

* Added support for RealSense depth camera
* Added the `FFMPEGMediaSource` component for Linux
* Added a [`Subpipeline`] class, enabling nested pipelines(psi/topics/InDepth.WritingComponents#SubPipelines)
* [`Parallel`](/psi/topics/InDepth.BasicStreamOperators#Parallel)
 now uses subpipelines
* [`Sequence`, `Repeat` and `Range` generators](/psi/topics/InDepth.BasicStreamOperators#Producing) now allow time-aligned messages
* Additional minor bug fixes.

Several API changes have been made:

* `Generators.Timer(...)` is now `Timers.Timer(...)`
* `IStartable` has been [replaced by `ISourceControl`/`IFiniteSourceControl`](/psi/topics/InDepth.WritingComponents#SourceComponents) and the [way that components get notified about the pipeline starting and stopping](/psi/topics/InDepth.WritingComponents#PipelineStartStop) has changed

**2018/04/04**: Beta-release, version 0.3.16.5

Interim release with a few changes to the samples and some minor bug fixes:

* ArmControlROSSample is now RosArmControlSample.
* PsiRosTurtleSample is now RosTurtleSample.
* Added LinuxSpeechSample.
* KinectFaceDetector component now outputs an empty list if no face is detected.
* NuGet packages are now marked beta.
* Additional minor bug fixes.

**2018/03/17**: Beta-release, version 0.2.123.1

Initial, beta version of the Platform for Situated Intelligence. Includes the Platform for Situated Intelligence runtime, visualization tools, and an initial set of components (mostly geared towards audio and visual processing). Relevant documents:

* [Documentation](/psi/) - top-level documentation page for Platform for Situated Intelligence.
* [Platform for Situated Intelligence Overview](/psi/PlatformOverview) - high-level overview of the platform.
* [NuGet Packages List](/psi/NuGetPackagesList) - list of NuGet packages available in this release.
* [Building the Code](/psi/BuildingPsi) - information about how to build the code.
* [Brief Introduction](/psi/tutorials) - brief introduction on how to write a simple application.
* [Samples](/psi/samples) - list of samples available. 

Below you can find a list of known issues, which we hope to address soon. Additionally, the [Roadmap](/psi/Roadmap) document provides insights about future planned developments.

<a name="KnownIssues" />

## Known Issues

Below we highlight a few known issues. You can see the full, current list of issues [here](https://github.com/Microsoft/psi/issues).

- - -

**Throttling:** Message throttling doesn't work when using the pre-defined delivery policies `Throttled` and `Immediate`.

Workaround: Create a new `DeliveryPolicy` instance and set `Throttling=true` and `MaxQueueSize=1`.

- - -

**[Identical Timestamps](https://github.com/Microsoft/psi/issues/1):**
Consecutive messages posted on the same stream with identical originating time timestamps are ambiguous when the stream is joined with another stream. In this case the result of the join operation is timing-dependent and not reproducible.

Workaround:
Ensure the originating time on consecutive messages is incremented by at least 1 tick.

**[Speech Recognition](https://github.com/Microsoft/psi/issues/12):**
Originating times of speech recognition messages from the SystemSpeechRecognizer and MicrosoftSpeechRecognizer components may not reflect the exact times of the corresponding utterances in the audio stream.

Workaround:
Do not rely on the originating times of messages from the SystemSpeechRecognizer and MicrosoftSpeechRecognizer components to be precise with respect to the input audio stream. If such precision is required, align the bytes in the `StreamingSpeechRecognitionResult.Audio` property of the output message with the raw input audio to locate the corresponding utterance within the input audio stream.

- - -
