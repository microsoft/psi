# HoloLens Capture Server

This app is the companion to the [HoloLensCaptureApp](..\HoloLensCaptureApp) which runs on the HoloLens, capturing data streams and remoting to this app running on another machine.

Streams are written to a \psi store. Upon stopping in the client (or upon errors) the store is closed. Additionally, pressing a key at the console will exit and properly close any open store.

Note: The capture server uses the [Rendezvous System](https://github.com/microsoft/psi/wiki/Rendezvous-System) to connect to the HoloLens app via TCP sockets, and all communication happens in the clear. These communication channels are not secure, and the user must ensure the security of the network as appropriate.

## Configuration

The store name and path may be configured in `HoloLensCaptureServer.config`:

```xml
<add key="storeName" value="HoloLensCapture" />
<add key="storePath" value="C:\data\Temp" />
```

## Statistics

A text file (`CaptureLog.txt`) is saved alongside the \psi store, containing statistics about the video and depth streams, including the frame count, the time extents and average frames per second. This is updated every 10 seconds while capturing. The file ends with the line "In progress..." while capturing and says "Complete!" followed by information about how/why it completed (e.g. stopped at the client/server or errors). Example:

```text
Capture Statistics

Video: FPS 8.3439778778468 (frames=390 time=00:00:46.7402965)
Depth: FPS 1.02304885288577 (frames=43 time=00:00:42.0312284)

In progress... Complete!

(Client stopped recording)
```

## Deployment

To deploy a build of the HoloLensCaptureServer, simply build in Visual Studio and copy the entire `Internal\Applications\HoloLensCapture\HoloLensCaptureServer\bin\<BUILD>\net472` folder. This contains the executable `HoloLensCaptureServer.exe` itself and all of its dependencies.

## Visualization

The server app persists all streams to a local \\psi store. This store can be opened in [PsiStudio](https://github.com/microsoft/psi/wiki/Psi-Studio) for visualization.

Note that the visualizer for hand tracking data is defined in `Microsoft.Psi.MixedReality.Visualization.Windows`. The visualizers for 3D depth and image camera views are defined in `Microsoft.Psi.Spatial.Euclidean.Visualization.Windows`. Follow the instructions for [3rd Party Visualizers](https://github.com/microsoft/psi/wiki/3rd-Party-Visualizers) to add those projects' assemblies to `PsiStudioSettings.xml` in order to visualize 3D hands and camera views in PsiStudio. 

You may need to double-click a stream (or right-click and select "Add Member Derived Streams") in order to drill down into sub-streams that can be visualized. For example, hands may be persisted in a stream of tuples, which can be expanded to reveal derived sub-streams for the left and right hand (each of which can be visualized separately). Any of the `CameraView` streams can be similarly expanded into `CameraIntrinsics`, `CameraPose`, and `ViewedObject` members. Right-click on `ViewedObject` to reveal options for visualizing as a 2D image.
