---
layout: default
title:  Kinect Overview
---

# Kinect Overview

Platform for Situated Intelligence supports reading from a Microsoft Kinect V2 Depth Camera. This includes capture of video, audio, body tracking, and face tracking streams from the Kinect.

**Please note**: <br/>Support for Kinect is currently limited to Windows only.<br/>Support for Face tracking via Kinect is limited to Windows 64 bit applications.

## Basic Components

Basic Kinect capabilities are provided by instantiating a <see cref="Microsoft.Psi.Kinect.KinectSensor">KinectSensor</see> component which is part of the <see cref="Microsoft.Psi.Kinect">Microsoft.Psi.Kinect</see> namespace.
<br/>Support for Kinect Face tracking is provided by instantiating a <see cref="Microsoft.Psi.Kinect.KinectFaceDetector">KinectFaceDetector</see> component which is part of the <see cref="Microsoft.Psi.Kinect.Face">Microsoft.Psi.Kinect.Face</see> namespace.

## Common Patterns of Usage

The following are some examples of how to use the Kinect sensor in \\psi.

### Capturing video from the Kinect device.

The following example shows how to create a <see cref="Microsoft.Psi.Kinect.KinectSensor">KinectSensor</see> and to receive images from the Kinect's color camera.

```csharp
using (var pipeline = Pipeline.Create())
{
    var kinectSensorConfig = new Microsoft.Psi.Kinect.KinectSensorConfiguration();
    kinectSensorConfig.OutputColor = true;
    var kinectSensor = new KinectSensor(pipeline, kinectSensorConfig);
    kinectSensor.ColorImage.Do((img, e) =>
    {
        // Do something with the image
    });
    pipeline.Run();
}
```

### Capturing audio from the Kinect device.

The next example shows how to receive audio from the Kinect and convert the audio stream into 16KHz-16b PCM format.

```csharp
using (var pipeline = Pipeline.Create())
{
    var kinectSensorConfig = new Microsoft.Psi.Kinect.KinectSensorConfiguration();
    kinectSensorConfig.OutputAudio = true;
    var kinectSensor = new KinectSensor(pipeline, kinectSensorConfig);
    var convertedAudio = kinectSensor.Audio.Resample(WaveFormat.Create16kHz1Channel16BitPcm());
    convertedAudio.Do((audio, e) =>
	{
        // Do something with the audio block
	});
    pipeline.Run();
}
```

### Performing face tracking with the Kinect device.

This final example demonstrates how to use the Kinect to perform face tracking. This simple example will print out for each face detected whether the person's mouth is open or closed. Note: That face tracking on the Kinect relies on enabling the body tracking, and thus we need to enable <see cref="Microsoft.Psi.Kinect.KinectSensorConfiguration.OutputBodies">OutputBodies</see> in the <see cref="Microsoft.Psi.Kinect.KinectSensorConfiguration">KinectSensorConfiguration</see>.

```csharp
using Microsoft.Psi.Kinect;
using (var pipeline = Pipeline.Create())
{
    var kinectSensorConfig = new KinectSensorConfiguration();
    kinectSensorConfig.OutputFaces = true;
    kinectSensorConfig.OutputBodies = true;
    var kinectSensor = new KinectSensor(pipeline, kinectSensorConfig);
    var faceTracker = new Face.KinectFaceDetector(pipeline, kinectSensor, Face.KinectFaceDetectorConfiguration.Default);
    faceTracker.Faces.Select((List<Face.KinectFace> list) => 
    {
       for (int i = 0; i < list.Count; i++)
       {
          if (list[i] != null)
          {
              string mouthIsOpen = "closed";
              if (list[i].FaceProperties[Microsoft.Kinect.Face.FaceProperty.MouthOpen] == Microsoft.Kinect.DetectionResult.Yes)
                  mouthIsOpen = "open";
              Console.WriteLine($"Person={i} mouth is {mouthIsOpen}");
          }
       }
    });
    pipeline.Run();
}
```

