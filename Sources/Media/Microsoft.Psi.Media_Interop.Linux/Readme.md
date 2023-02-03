This folder contains the media interop library for Linux. To build this library, simply run the bash script:
```
./build.sh
```

Note that this project currently only contains optional limited support for FFmpeg, so building it is skipped if the `FFMPEGDir` environment variable is not defined. For more details on enabling FFmpeg support, see the [Readme](../Microsoft.Psi.Media.Linux/Readme.md) in the Microsoft.Psi.Media.Linux project.