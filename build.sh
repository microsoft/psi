#!/usr/bin/env bash


(cd ./Sources/Media/Microsoft.Psi.Media.Native.x64/                         && . ./build.sh)
(cd ./Sources/Audio/Microsoft.Psi.Audio/                                    && . ./build.sh)
(cd ./Sources/Audio/Microsoft.Psi.Audio.Linux/                              && . ./build.sh)
(cd ./Sources/Common/Test.Psi.Common/                                       && . ./build.sh)
(cd ./Sources/Imaging/Microsoft.Psi.Imaging                                 && . ./build.sh)
(cd ./Sources/Integrations/ROS/Microsoft.ROS/                               && . ./build.sh)
(cd ./Sources/Media/Microsoft.Psi.Media.Linux/                              && . ./build.sh)
(cd ./Sources/Runtime/Microsoft.Psi/                                        && . ./build.sh)
(cd ./Sources/Runtime/Microsoft.Psi.Interop/                                && . ./build.sh)
(cd ./Sources/Runtime/Test.Psi/                                             && . ./build.sh)
(cd ./Sources/Toolkits/FiniteStateMachine/Microsoft.Psi.FiniteStateMachine/ && . ./build.sh)
(cd ./Samples/PsiRosTurtleSample/                                           && . ./build.sh)
