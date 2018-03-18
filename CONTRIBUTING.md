# Contributing to Platform for Situated Intelligence

We welcome contributions from the community in a variety of forms: from simply using it and filing issues and bugs, to writing and releasing your own new components, to creating pull requests for bug fixes or new features, etc. This document describes some of the things you need to know if you are going to contribute to the Platform for Situated Intelligence ecosystem. Please read it carefully before making source code changes.

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information on this code of conduct, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Filing Issues

We encourage you to use GitHub issues to flag problems and bugs, or issue requests for new features.

We have already defined the following issue labels:

* [`bug`](https://github.com/Microsoft/psi/labels/bug): these issues describe code defects.

* [`documentation`](https://github.com/Microsoft/psi/labels/documentation): these issues are requests for additional or improved documentation.

* [`feature request`](https://github.com/Microsoft/psi/labels/feature%20request): these issues are requests for additional or improved documentation.

* [`help wanted`](https://github.com/Microsoft/psi/labels/help%20wanted): these issues are specifically well suited for outside contributors.

* [`good first issue`](https://github.com/Microsoft/psi/labels/good%20first%20issue): these issues are small and appropriate for people who wish to familiarize themselves with GitHub pull requests and/or \\psi's contributor guidelines, build process, and running tests. We're here to help you get started in open source.

## Contributing New Components

One of the stated goals for the Platform for Situated Intelligence project is to create an open eco-system of pluggable components that can lower the barrier to entry for developing multimodal integrative-AI systems. If you have a new component you have written for \\psi that you think might be useful to others, we encourage you to release it to the community if possible. Here are a few recommendations and guidelines that we believe would help enable a future thriving eco-system:

* __NuGet__. Release if possible as a [NuGet](https://www.nuget.org) package: NuGet packages are easy to consume and work on both Windows and Linux. 
  * __Naming__. Use the following naming conventions:
    * Name the package `[YourInstitution].Psi.[Foo]`
    * Append `.Windows` or `.Linux` if the package only runs on one of those operating systems
    * Append `.x64` or `.x86` if the package only runs on those platforms (e.g. if it is not `AnyCPU`)
  * __Description__. In the package description, use a phrasing like: _Provides Platform for Situated Intelligence APIs and components for ..._
  * __Tags__. In the package tags, add `Psi`.

* __Target__. Where possible, target .NET Standard: this will allow your component library to work cross-platform.

* __Let us know__. If possible we'd love to hear from you when you develop a new package. You can do so by opening an [issue](https://github.com/Microsoft/psi/issues), tag it with the [announcement](https://github.com/Microsoft/psi/labels/announcement) tag, and including a pointer to your component.

## Contributing to the Existing Code-base via Pull Requests

Apart from contributing by releasing your own Platform for Situated Intelligence components, you could also contribute by fixing bugs, improving documentation, adding new features to the existing codebase.

### Legal

You will need to complete a Contributor License Agreement (CLA) before your pull request can be accepted. This agreement testifies that you are granting us permission to use the source code you are submitting, and that this work is being submitted under appropriate license that we can use it.

You can complete the CLA by going through the steps at [https://cla.microsoft.com](https://cla.microsoft.com). Once we have received the signed CLA, we'll review the request. You will only need to do this once.

### Code Organization

Below is a description of the directory structure for the Platform for Situated Intelligence source tree. Every time you modify the structure by adding a new project, please update the table below.

| Folder    | Subfolder     | Description |
| :-------- | :------------ | :---------- |
| Build     |               | Contains \psi build tools. |
| Samples   |               | Contains \psi sample applications. |
| Sources   |               | Contains \psi source code. |
| Sources   | Audio         | Contains class libraries for audio components. |
| Sources   | Calibration   | Contains class libraries for calibrating cameras. |
| Sources   | Common        | Contains class libraries for common test support. |
| Sources   | Extensions    | Contains class libraries that extend the \psi runtime class libraries. |
| Sources   | Imaging       | Contains class libraries for \psi imaging, e.g. images, video capture, etc. |
| Sources   | Integrations  | Contains integrations - libraries that provide shims around 3rd party libraries. |
| Sources   | Kinect        | Contains class libraries for Kinect sensor components. |
| Sources   | Language      | Contains class libraries for natural language processing components. |
| Sources   | Media         | Contains class libraries for media components. |
| Sources   | Runtime       | Contains class libraries for \psi runtime. |
| Sources   | Speech        | Contains class libraries for speech components. |
| Sources   | Toolkits      | Contains toolkits - e.g. Finite State Machine toolkit, etc. |
| Sources   | Tools         | Contains tools - e.g. PsiStudio, etc. |
| Sources   | Visualization | Contains class libraries for visualization. |

### Coding Style

Platform for Situated Intelligence is an organically grown codebase. The consistency of style reflects this.
For the most part, the team follows these [coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) along with these [design guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/). Pull requests that reformat the code will not be accepted.

In case you would like to add a new project to the `Psi.sln` we require that the project is setup in a similar ways to the other projects to ensure a certain coding standard.

### Build and Test

To fully validate your changes, do a complete rebuild and test for both Debug and Release Configurations.

### Pull Requests

We accept __bug fix pull requests__. Please make sure there is a corresponding tracking issue for the bug. When you submit a PR for a bug, please link to the issue.

We also accept __new feature pull requests__. We are available to discuss new features. We recommend you open an issue if you plan to develop new features.

Pull requests should:

* Include a description of what your change intends to do
* Be a child commit of a reasonably recent commit in the master branch
* Pass all unit tests
* Have a clear commit message
* Ideally, include adequate tests
