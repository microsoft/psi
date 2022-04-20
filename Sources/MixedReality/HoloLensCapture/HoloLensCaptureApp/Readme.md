# HoloLens Capture App

This app runs on the HoloLens, capturing data streams and remoting to the companion [HoloLensCaptureServer](../HoloLensCaptureServer) running on another machine.

Note: The app uses the [Rendezvous System](https://github.com/microsoft/psi/wiki/Rendezvous-System) to connect to the capture server via TCP sockets, and all communication happens in the clear. These communication channels are not secure, and the user must ensure the security of the network as appropriate.

## Configuration

The IP address of the capture server must be known. A default (`captureServerAddress`) is given in code. This is overridden when a `CaptureServerIP.txt` file is present in `User Folders\Documents`. A new such file is also created upon first launch. This may be edited by hand and uploaded via the Device Portal.

## Packaging

The app may be deployed via Visual Studio or a previously built package may be loaded. The following instructions describe how to create an app package for sideloading onto the HoloLens via the _Device Portal_.

1) Create a self-signed certificate:

	```powershell
	New-SelfSignedCertificate -Type Custom -Subject "CN=Microsoft Corporation, O=Microsoft Corporation, C=US" -KeyUsage DigitalSignature -FriendlyName "HoloLensCaptureApp Certificate" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
	```

2) Add this certificate to the `Package.appxmanifest` by opening the manifest in Visual Studio, selecting the _Packaging_ tab and _Choose Certificate_, _Select From Store_. If the _SimgleCaptureApp Certificate_ is not listed, click _More Choices..._. Select the _HoloLensCaptureApp Certificate_ created above.

3) Create the package by right clicking the _HoloLensCaptureApp_ project, choose _Publish_ > _Create App Package..._ Select _Sideloading_ (the default) and uncheck _Enable Automatic Updates_. _Next_, _Next_, _Create_.

This will create the package in `\Internal\Applications\HoloLensCapture\HoloLensCaptureApp\AppPackages`.

In the _Device Portal_, navigate to _Apps Manager_ > _Deploy Apps_ section, select _Local Storage_ > 
_Select the application package_, _Choose File_ and browse to the app package and select _Install_.
