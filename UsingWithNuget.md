---
layout: default
title:  Using with NuGet Packages
---

# Using Platform for Situated Intelligence via NuGet packages

To build \\psi applications, we recommend using [Visual Studio 2017](https://www.visualstudio.com/vs/ "Visual Studio 2017") on Windows (the free, Community editions is sufficient). Under Linux, we recommend using [Visual Studio Code](https://code.visualstudio.com/).

To build a \\psi application using [NuGet packages](http://www.nuget.org), simply reference the relevant `Microsoft.Psi.*` packages available on www.nuget.org in your application. The [Brief Introduction](/psi/tutorials) tutorial also contains a quick description of how you build a very simple, initial \\psi application.

__Choice of .NET Framework__. Platform for Situated Intelligence is build on .NET, and can be used from .NET applications.

* If you are developing a Linux or a cross-platform application, make sure you use .NET Core 2.0 or above.
* If you are building a Windows-only application that needs to leverage features specific only to .NET Framework, make sure you configure your application to use .NET Framework 4.7 or above. 

__Platform target__. Certain \\psi nuget packages work only on 64 bit configurations (see [\\psi NuGet packages list](/psi/NuGetPackagesList)). If you plan to use one of these packages, you will need to configure the platform target for your application accordingly. You can change the platform target by going to right-clicking on the project, then _Properties_ -> _Build_ -> _Platform Target_ set from `AnyCPU` to `x64`.