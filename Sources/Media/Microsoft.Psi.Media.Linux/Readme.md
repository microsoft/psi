# Additional requirements for FFmpeg support

The `Microsoft.Psi.Media.Linux` library includes optional support for a basic FFmpeg-based MPEG-4 file reader and media source component. By default, this functionality is not included when building the project. In order to enable it, see the following sections.

## FFmpeg Version
FFmpeg support currently depends on Version 3.x of FFmpeg (tested against FFmpeg 3.4.11 on Ubuntu 18.04 LTS). Building the interop library requires both the FFmpeg runtime and development files. These may be obtained either by building FFmpeg [from source](http://ffmpeg.org/releases/), or by installing the appropriate development packages, e.g.:
  - Debian: [libavdevice-dev](https://packages.debian.org/stretch/libavdevice-dev) (installs all required dependencies)
  - Ubuntu: [libavdevice-dev](https://packages.ubuntu.com/bionic/libavdevice-dev) (installs all required dependencies)
 

## Environment Variable
An environment variable `FFMPEGDir` needs to be defined in your build environment to point to the location of the FFmpeg libraries. This should be the path to the lib directory in which the various FFmpeg `lib*.so` files are located. You can set this in your bash shell before building, e.g.:
```
export $FFMPEGDir=/usr/lib/x86_64-linux-gnu
./build.sh
```

## Native Interop Library
In addition, you will need to build the native interop project located in the `../Microsoft.Psi.Media_Interop.Linux/` directory. Make sure that you have followed the previous step to set the `FFMPEGDir` variable to point to the location of the FFmpeg libraries before building the interop library. This should produce a shared library `../Microsoft.Psi.Media_Interop.Linux/bin/Microsoft.Psi.Media_Interop.so` which will then need to be copied to your runtime directory (i.e. the same location as your application executable).
