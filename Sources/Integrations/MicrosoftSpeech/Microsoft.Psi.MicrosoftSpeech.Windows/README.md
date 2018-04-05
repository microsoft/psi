# Microsoft Speech Component

This project builds the integration component for speech recognition based on the Microsoft Speech Platform.

In order to build this project, the [Microsoft Speech Platform SDK v11.0](http://go.microsoft.com/fwlink/?LinkID=223570) must be installed on your machine. Note that only the 64-bit version of the SDK is currently supported. Additionally, you will need to set an environment variable named `MsSpeechSdkDir` that points to the location in which you installed the SDK. The path should be the root of the SDK folder which contains the Assembly directory. By default, this is `C:\Program Files\Microsoft SDKs\Speech\v11.0`

In order to run applications using this component, you will also need to install the [Microsoft Speech Platform Runtime v11.0](http://go.microsoft.com/fwlink/?LinkID=223568) as well as the applicable [Language Pack](http://go.microsoft.com/fwlink/?LinkID=223569) for the speech recognition language you wish to use (e.g. en-US).
