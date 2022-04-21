﻿# HoloLens Capture Server

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