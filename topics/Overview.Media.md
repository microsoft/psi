---
layout: default
title:  Media Overview
---

# Media Overview

The following is a brief overview of support for streaming media in Platform for Situated Intelligence.

__NOTE__: Support for web cameras is currently limited to Windows only.

## Basic Components

There are three main components exposed by the \Psi Media Library. All of these components are part of the Microsoft.Psi.Media namespace.
<h4>MediaCapture</h4>
The <see cref="Microsoft.Psi.Media.MediaCapture">MediaCapture</see> component enables capturing of video from web camera.
<h4>MediaSource</h4>
The <see cref="Microsoft.Psi.Media.MediaSource">MediaSource</see> component enables play back of video from an external file/URL.
<br/>__NOTE__: The MediaSource component is only available on the Windows platform.
<h4>Mpeg4Writer</h4>
The <see cref="Microsoft.Psi.Media.Mpeg4Writer">Mpeg4Writer</see> component enables writing of \Psi Images to an external MPEG4 file.
<br/>__NOTE__: The Mpeg4Writer component is only available on the Windows platform.

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

### Writing images to an .mp4 file

This next snippet of code demonstrates how to instatiate a <see cref="Microsoft.Psi.Media.Mpeg4Writer">Mpeg4Writer</see> component to generate a .mp4 file from a \Psi pipeline. We read images from the webcam and write them out to output.mp4.

```csharp
using (var pipeline = Pipeline.Create())
{
    var webcam = new MediaCapture(pipeline, 1920, 1080, 30.0);

    var audioConfig = new Microsoft.Psi.Audio.AudioSourceConfiguration();
    audioConfig.OutputFormat = WaveFormat.Create16BitPcm(48000, 2);

    var audioSource = new Microsoft.Psi.Audio.AudioSource(pipeline, audioConfig);

    var writer = new Mpeg4Writer(pipeline, "output.mp4", 1920, 1080, Microsoft.Psi.Imaging.PixelFormat.BGR_24bpp);
    audioSource.Out.PipeTo(writer.AudioIn);
    webcam.Out.PipeTo(writer.ImageIn);
    pipeline.Run(System.TimeSpan.FromSeconds(30));
}
```
