---
layout: default
title:  Media Overview
---

# Media Overview

The following is a brief overview of support for streaming media (e.g. streaming video from a Web Camera) in Platform for Situated Intelligence.

__NOTE__: Support for web cameras is currently limited to Windows only.

## Basic Components

Basic Media capture capabilities are provided by instantiating a <see cref="Microsoft.Psi.Media.MediaCapture">MediaCapture</see> component which is part of the <see cref="Microsoft.Psi.Media">Microsoft.Psi.Media</see> namespace. This component is capable of streaming video from a webcam into a \\psi application.

There is also a <see cref="Microsoft.Psi.Media.MediaSource">MediaSource</see> component which may be used to stream video and audio from a file (such as an .mp4 file).

## Common Patterns of Usage

### Capturing video from a web camera.

The following code snippet demonstrates how to capture audio and video from a web camera. The video is then converted into a stream of JPG images. 

__NOTE__: Currently, the `MediaCapture` component requires you to specify a image resolution and frame rate that the hardware supports. If you specify an unsupported resolution or frame rate \\psi will throw an ArgumentException error.

```csharp
using (var pipeline = Pipeline.Create())
{
    var webcam = new Microsoft.Psi.Media.MediaCapture(pipeline, 1920, 1080, 30);
    var encodedImages = webcam.Out.EncodeJpeg(90, Microsoft.Psi.DeliveryPolicy.LatestMessage);
    encodedImages.Out.Do(
	(img, e) =>
	{
		// Do something with the JPG image
	});
    var audioConfig = new Microsoft.Psi.Audio.AudioSourceConfiguration()
	{
            OutputFormat = Microsoft.Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm()
	});
    var audioInput = new Microsoft.Psi.Audio.AudioSource(pipeline, audioConfig);
    audioInput.Out.Do(
	(audioBuffer, e) =>
	{
		// Do something with the audio buffer
	});
    pipeline.Run();
}
```

### Playing video from a .mp4 file

This next snippet of code demonstrates how to instatiate a <see cref="Microsoft.Psi.Media.MediaSource">MediaSource</see> component to use for playing back an MPEG file.

```csharp
using (var pipeline = Pipeline.Create())
{
    var player = new Microsoft.Psi.Media.MediaSource(pipeline, "test.mp4");
    player.Image.Do(
	(image, e) =>
	{
            // Do something with the video frame
	});
    var convertedAudio = player.Audio.Resample(WaveFormat.Create16kHz1Channel16BitPcm());
    convertedAudio.Do(
	(audio, e) =>
	{
            // Do something with the audio block
	});
    pipeline.Run();
}
```
