# Capturing HoloLens Sensor Streams

Several tools are provided for capturing sensor data from the HoloLens to \psi stores and for exporting these stores to other formats.

* [HoloLensCaptureApp](.\HoloLensCaptureApp) runs on the HoloLens, capturing and remoting data streams.
* [HoloLensCaptureServer](.\HoloLensCaptureServer) runs on a separate machine, receiving remoted data streams and writing to a \psi store.
* [HoloLensCaptureExporter](.\HoloLensCaptureExporter) is a tool to convert data within \psi stores to other formats.

Data can be collected by running the capture app on the HoloLens and remoting the streams of sensor data to the server app which is running on a different machine. Communication may be over WiFi or via USB tethering. Data stores written by the server may then be examined and analyzed in PsiStudio or may be processed by other \psi applications. While \psi stores are optimized for performance and work well with PsiStudio and other \psi applications, you can also use the exporter tool to export to other standard formats.
