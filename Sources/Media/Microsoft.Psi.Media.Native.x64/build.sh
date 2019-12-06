#!/usr/bin/env bash

if [[ -z "${FFMPEGDir}" ]]; then
    echo "FFMPEGDir Environment Variable Not Defined. Skipping Microsoft.Psi.Media.Native.x64"
    # Future implementation might consider finding the library's path instead of needing to be predefined.
    # If install using the package manager, the libs are located at /usr/lib/x86_64-linux-gnu/ and headers
    # are at /usr/include/x86_64-linux-gnu/
else
    make
fi