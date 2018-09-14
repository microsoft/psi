---
layout: default
title:  Release Notes
---

# Release Notes

**2018/09/13**: Beta-release, version 0.5.48.2

NOTE: For this release we have added the compiler switch /Qspectre to our C++ projects which helps mitigate against Spectre security vulnerabilities.  You should upgrade your Visual Studio instance to version 15.8 or later to take advantage of these Spectre mitigations.  In addition, you should install the new library component **VC++ 2017 version X.X.XX Libs for Spectre (x86 and x64)** to your Visual Studio instance.  For more details see https://docs.microsoft.com/en-us/cpp/build/reference/qspectre?view=vs-2017

New Features in Platform for Situated Intelligence Studio:

* Added new PlotStyles to Timeline Visualizer.  Plots can be rendered with the following styles:
    * Direct (default): A straight line is drawn from each message to the next message to create a standard line plot.
    * Step: Messages are joined by a horizontal line followed by a vertical line for visualizing quantized data.
    * None: No lines are drawn between messages.  If you select this plot style then you should also update the MarkerStyle from its default value of None or nothing will be drawn in the plot.
* Visualization Panels in Psi Studio can now be resized by dragging their bottom edge vertically.
* Visualization Panels can now also be re-ordered with the mouse via drag & drop.
* Users can now drag the visible portion of a Timeline Plot to the left or right using the mouse.
* Added modal window while loading a dataset to inform the user of the progress of the data load operation.
* Added new timing information toolbar buttons.  These buttons can be used to display absolute message times, message times relative to the start of the session, and message times relative to the start of the current selection.
* Performance improvements when plotting messages.

New Features in Runtime:

* Join operator now matches against a final secondary message upon stream closing once it can be proven that no better match will exist.
* Added support for Parallel operators that take an Action rather than a Func.
* Adding components once a pipeline is running is no longer supported and now throws an exception. The recommended approach is to add a Subpipeline.
* Consolidated Windows SDK versions.  Previously different parts of the toolset required different WinSDK versions, consolidated so that the only Windows SDK version that Psi now requires is 10.0.17134.0
* Improved how the pipeline shuts down to ensure that all existing messages are drained from it before stopping.

Bug Fixes:
 
* Fixed bug where switching Psi Studio from "Realtime" mode to "Playback" mode would result in the user being unable to move the timeline cursor or reposition the timeline.
* Fixed bug where we were trying to calculate the relative path of a partition to a dataset when the dataset was stored on the local disk but the partitions within the dataset were stored on a network share.
* Fixed bug where loading very large datasets would sometimes crash PsiStudio.

BREAKING CHANGES in this release:

* Removed several versions of the Parallel operator
* Renamed several components for consistency:
    *	AudioSource -> AudioCapture
    *	AudioSourceConfiguration -> AudioCaptureConfiguration
    *	AudioConfiguration (on linux) was eliminated and replaced by two corresponding classes AudioCaptureConfiguration and AudioPlayerConfiguration
    *	TransformImageComponent -> ImageTransformer
    *	AcousticFeatures -> AcousticFeaturesExtractor
    *	AcousticFeaturesConfiguration -> AcousticFeaturesExtractorConfiguration
* Renamed Parallel component to ParallelFixedLength to match naming convention with the other components.
* Partial speech recognition results for the SystemSpeechRecognizer, MicrosoftSpeechRecognizer and BingSpeechRecognizer components are now posted on a new stream named PartialRecognitionResults. The default Out stream now contains only final recognition results.
* Psi Studio will no longer load third party visualizers, for the time being it will only display its built-in visualizers.


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
