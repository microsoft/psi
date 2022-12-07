# Capturing HoloLens Sensor Streams

Several tools are provided for capturing sensor data from the HoloLens to \psi stores and for exporting these stores to other formats.

* [HoloLensCaptureApp](https://github.com/microsoft/psi/tree/master/Sources/MixedReality/HoloLensCapture/HoloLensCaptureApp) runs on the HoloLens, capturing and remoting data streams.
* [HoloLensCaptureServer](https://github.com/microsoft/psi/tree/master/Sources/MixedReality/HoloLensCapture/HoloLensCaptureServer) runs on a separate machine, receiving remoted data streams and writing to a \psi store.
* [HoloLensCaptureExporter](https://github.com/microsoft/psi/tree/master/Sources/MixedReality/HoloLensCapture/HoloLensCaptureExporter) is a tool to convert data within \psi stores to other formats.

Data can be collected by running the capture app on the HoloLens and remoting the streams of sensor data to the server app which is running on a different machine. Communication may be over WiFi or via USB tethering. Data stores written by the server may then be examined and analyzed in PsiStudio or may be processed by other \psi applications. While \psi stores are optimized for performance and work well with PsiStudio and other \psi applications, you can also use the exporter tool to export to other standard formats.

## Visualization

The server app persists all streams to a local \\psi store. This store can be opened in [PsiStudio](https://github.com/microsoft/psi/wiki/Psi-Studio) for visualization.

Note that the visualizer for hand tracking data is defined in `Microsoft.Psi.MixedReality.Visualization.Windows`. The visualizers for 3D depth and image camera views are defined in `Microsoft.Psi.Spatial.Euclidean.Visualization.Windows`. Follow the instructions for [3rd Party Visualizers](https://github.com/microsoft/psi/wiki/3rd-Party-Visualizers) to add those projects' assemblies to `PsiStudioSettings.xml` in order to visualize 3D hands and camera views in PsiStudio. 

You may need to double-click a stream (or right-click and select "Add Member Derived Streams") in order to drill down into sub-streams that can be visualized. For example, hands may be persisted in a stream of tuples, which can be expanded to reveal derived sub-streams for the left and right hand (each of which can be visualized separately). Any of the `CameraView` streams can be similarly expanded into `CameraIntrinsics`, `CameraPose`, and `ViewedObject` members. Right-click on `ViewedObject` to reveal options for visualizing as a 2D image.
