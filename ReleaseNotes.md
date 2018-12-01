---
layout: default
title:  Release Notes
---

# Release Notes

**2018/11/30**: Beta-release, version 0.6.48.2

BREAKING CHANGES:

* It is now a requirement that messages posted on an `Emitter` have strictly increasing originating times. Attempting to post multiple messages with the same originating time on the same stream will cause an exception to be thrown.
* The `Buffer`, `History` and `Window` operators have been unified as a single set of `Window` operators which take either an index-based or a relative time-based interval. The index-based variants emit the initial buffer only after the total count of messages within the specified index interval have been accumulated, whereas the time-based variants emit the initial buffer as soon as messages within the specified relative time interval are available.
* The following operators have been removed:
    * `SelectMany`.
    * `Mirror`.
    * `Repeat` - use `Pair` instead.
    * `Buffer` - use `Window` instead.
    * `History` - use `Window` instead.
    * `Previous`
* [Delivery Policies](/topics/InDepth.DeliveryPolicies.md) have been simplified and renamed:
    * The `Throttled` policy has been removed. It will be re-introduced in a later release once issues around throttling have been resolved.
    * The `Default`, `Immediate` and `ImmediateOrThrottle16` policies have been removed.
    * The `QueueSize` property has been renamed `InitialQueueSize`.
    * The `MaximumLag` property has been renamed `MaximumLatency`.
    * The `IsSynchronous` property has been renamed `AttemptSynchronous`.
* As a result of the above changes, many stream operators have been amended to take an optional `DeliveryPolicy` parameter.
* The constructors for the `Connector` component and associated `CreateConnector` extension methods no longer take an `owner` parameter.
* The serialization format for `SystemCalibration` has changed:
    * The `ImageWidth` and `ImageHeight` properties have been removed.
    * The `NumberOfFrames` property has been moved to the `CameraCalibration` class.
* The `ICameraIntrinsics` interface has changed:
    * The signature of the `DistortPoint` method has changed.


New Features in Platform for Situated Intelligence Studio:

* Updated the layout of the main Psi Studio screen. The Datasets tree view and the Visualizations tree view have been moved to the left hand side of the application. Furthermore they are no longer on separate tabs, they appear one above the other so that users no longer have to switch back and forth between tabs while laying out the visualizations. Datasets and Visualizations previously each had their own Properties pages, but now there is a single Properties page on the right hand side of the application that is able to display the properties of either type of object. Both the Datasets and Visualizations tree views and the Properties page can be resized or hidden completely to give more screen real estate to the main Visualization view.
* Added multi track event visualizer `TimeIntervalHistoryVisualizationObject` which is useful for visualizing multiple tracks of events having some finite timespan such as multiple speech-to-text streams. This visualizer will be loaded when visualizing streams containing messages of type `Dictionary<string, List<(TimeInterval, string)>>`. The `Dictionary` keys represent unique track IDs, each element in the `List` represents an event to display in the track and contains a tuple of the `TimeInterval` representing the start and end times of the event and a string representing the text that will be displayed inside the time interval. Note that since this is a history visualizer, each message should contain ALL events that have occurred up until the time of the message. This implies that the last message in the stream contains all of the data required to display the visualization.
* Users can now visualize streams by dragging them from the Datasets tree view directly into the main Visualization panel.
* Added 'Snap to Stream' functionality on certain visualizers to snap the cursor to the messages of the snapped stream.
* Added 'Visualize Messages in New Panel' and 'Visualize Latency in New Panel' commands to the stream context menu.
* Psi Studio now automatically attempts to repair corrupted stores when opening them.


New Features in Platform for Situated Intelligence Runtime/Core:

* Initial version of data interop with the introduction of `Microsoft.Psi.Interop`, with support for MessagePack, JSON and CSV data formats and ZeroMQ transport. See the [Interop topic](/topics/InDepth.Interop.md) for more details.
* New `dynamic` store reader allows reading any stream from any store to `dynamic` primitives or to `ExpandoObject` of `dynamic` without requiring a reference to the .NET type of the stream messages.
* New `PsiStoreTool` command-line tool which allows exploration of available streams in a store, conversion to other formats using interop, and saving to disk or sending over a message queue for consumption by other platforms and/or languages.
* Exposed `Scheduler` as a parameter to `Pipeline` and `Clock` as a parameter to `Scheduler`.
* Multiple handlers may now be registered on start, stop and final pipeline events.
* Improved `#TRACKLEAKS` debug information in `RecyclingPool`.


New Features in Imaging:

* Added `SetPixel` method to `Image`.
* Added `DrawText` extension method for `Image`.
* Added support for `CameraIntrinsics` and `CoordinateSystem` to `SystemCalibration`.
* The `IKinectCalibration` interface and `KinectCalibration` class have been extended to support conversion from depth coordinates to color space coordinates using the new `ToColorSpace` operator.

Bug Fixes:
 
* Fixed several issues where visualization objects were not being displayed in the correct color in Psi Studio.
* Fixed a bug which would sometimes cause Psi Studio to crash when visualizing image streams.
* Fixed a bug which caused the mouse to move the cursor position during playback in Psi Studio.
* Fixed an issue causing streams to disappear at the end of playback in Psi Studio.
* Fixed a crash in Psi Studio when opening a layout created from a store which has since been deleted.
* Fixed a bug which sometimes caused timeline plots to be truncated when loading a layout in Psi Studio.
* Fixed an exception when closing Psi Studio after a live visualization session.
* Fixed a performance issue reading from Psi stores which occurred at the transition between consecutive data files.
* Fixed a bug where the `ImageCompressor` was not properly disposing of an encoded image after decoding it.
* Fixed a bug where `ImagePool` would sometimes return a recycled image with incorrect dimensions.
* Fixed a bug where the pipeline replay interval would sometimes extend beyond the lifetime of a stream being read from a store.
* Fixed a bug which caused `KinectSample` to crash when it detected no faces.
* Fixed the `Scale` image extension method to throw an exception when attempting to call it on an `Image` with an unsupported format.
* Fixed a few intermittently failing unit tests.
* Fixed a bug which sometimes caused a loss of precision when computing the current pipeline time.


**2018/09/13**: Beta-release, version 0.5.48.2

<div style="color:red;font-weight:bold">IMPORTANT NOTE:</div>

For this release we have added the compiler switch /Qspectre to our C++ projects which helps mitigate against Spectre security vulnerabilities.  In order to successfully compile the Platform for Situated Intelligence solution you must upgrade your Visual Studio instance to version 15.7 or later and add the two following components to your Visual Studio installation:

* VC++ 2017 version *version_number* Libs for Spectre (x86 and x64)
* Visual C++ ATL (x86/x64) with Spectre Mitigations

For more information on the Spectre vulnerabilities and information about how to add the above components to Visual Studio, please see the following pages:

* [Visual C++ Team Blog - Spectre mitigations in MSVC](https://blogs.msdn.microsoft.com/vcblog/2018/01/15/spectre-mitigations-in-msvc/)
* [Microsoft Docs - /QSpectre](https://docs.microsoft.com/en-us/cpp/build/reference/qspectre?view=vs-2017)

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

The [Roadmap](/psi/Roadmap) document provides insights about future planned developments.
