# Platform for Situated Intelligence

![Build status](https://dev.azure.com/msresearch/psi/_apis/build/status/psi-github-ci?branchName=master)
[![Join the chat at https://gitter.im/Microsoft/psi](https://badges.gitter.im/Microsoft/psi.svg)](https://gitter.im/Microsoft/psi?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

**Platform for Situated Intelligence** is an open, extensible framework that enables the development, fielding and study of situated, integrative-AI systems.

In recent years, we have seen significant progress with machine learning techniques on various perceptual and control problems. At the same time, building end-to-end, multimodal, integrative-AI systems that leverage multiple technologies and act autonomously or interact with people in the open world remains a challenging, error-prone and time-consuming engineering task. Numerous challenges stem from the sheer complexity of these systems and are amplified by the lack of appropriate infrastructure and development tools.

The Platform for Situated Intelligence project aims to address these issues and provide a basis for developing, fielding and studying integrative-AI systems. The platform consists of three layers. The **Runtime** layer provides a parallel programming model centered around temporal streams of data, and enables easy development of components and applications using .NET, while retaining the performance properties of natively written, carefully tuned systems. A set of **Tools** enable multimodal data visualization, annotations, analytics, tuning and machine learning scenarios. Finally, an open ecosystem of **Components** encapsulate various AI technologies and allow for quick compositing of integrative-AI applications. For more information about the goals of the project, the types of systems that you can build using it, and the various layers see [Platform for Situated Intelligence Overview](https://github.com/microsoft/psi/wiki/Platform-Overview).

# Using and Building

Platform for Situated Intelligence is built on the .NET Framework. Large parts of it are built on .NET Standard and therefore run both on Windows and Linux, whereas some components are specific and available only to one operating system (for instance the Kinect sensor component is available only for Windows.)

You can build applications based on Platform for Situated Intelligence either by leveraging nuget packages, or by cloning and building the code. Below are instructions:

* [Using \\psi via Nuget packages](https://github.com/microsoft/psi/wiki/Using-via-NuGet-Packages)
* [Building the \\psi codebase](https://github.com/microsoft/psi/wiki/Building-the-Codebase)

# Getting Started

__Brief Introduction__. A number of [tutorials](https://github.com/microsoft/psi/wiki/Basic-Tutorials) are available to get you started with using Platform for Situated Intelligence. We recommend starting with the [Brief Introduction](https://github.com/microsoft/psi/wiki/Brief-Introduction), which provides a guided walk-through for some of the main concepts in \psi. It shows how to create a simple program, describes the core concept of a stream, and explains how to transform, synchronize, visualize, persist to and replay streams from disk. We recommend that you first work through the examples in this tutorial to familiarize yourself with these core concepts. The [Writing Components](https://github.com/microsoft/psi/wiki/Writing-Components) tutorial explains how to write new \psi components, and the [Delivery Policies](https://github.com/microsoft/psi/wiki/Delivery-Policies) tutorial describes how to control throughput on streams in your application.

__Advanced Topics__. A number of documents on more [advanced topics](https://github.com/microsoft/psi/wiki/More-Advanced-Topics) describe in more detail various aspects of the framework, including [stream operators](https://github.com/microsoft/psi/wiki/Stream-Operators), [synchronization](https://github.com/microsoft/psi/wiki/Synchronization), [remoting](https://github.com/microsoft/psi/wiki/Remoting), [interop](https://github.com/microsoft/psi/wiki/Interop), [shared objects and memory management](https://github.com/microsoft/psi/wiki/Shared-Objects), etc.


__Samples__. Besides the tutorials and topics, it may be helpful to look through the set of [Samples](https://github.com/microsoft/psi/wiki/Samples) provided. While some of the samples address specialized topics such as how to leverage speech recognition components or how to bridge to ROS, reading them will give you more insight into programming with \psi.

__Components__. Additional useful information regarding available packages and components can be found in the [NuGet packages list](https://github.com/microsoft/psi/wiki/List-of-NuGet-Packages) and in the [component list](https://github.com/microsoft/psi/wiki/List-of-Components) pages. The latter page also has pointers to other repositories by third parties containing other \psi components.

__Documentation__. Like the rest of the codebase, the documentation available in the [wiki](https://github.com/microsoft/psi/wiki) is still under construction and in various phases of completion. If you need further explanation in any of these areas, please open an issue, label it `documentation`, as this will help us target our documentation development efforts to the highest priority needs.

# Disclaimer

The codebase is currently in beta and various aspects of the platform are at different levels of completion and robustness. There are probably still bugs in the code and we will likely be making breaking API changes. We plan to continuously improve the framework and we encourage the community to contribute.

The [Roadmap](https://github.com/microsoft/psi/wiki/Roadmap) document provides more information about our future plans. 

# Getting Help

If you find a reproducible bug or if you would like to request a new feature or additional documentation, please file an [issue on the github repo](https://github.com/microsoft/psi/issues). If you do so, please make sure a corresponding issue has not already been filed. Use the [`bug`](https://github.com/microsoft/psi/labels/bug) label when filing issues that represent code defects, and provide enough information to reproduce. Use the [`feature request`](https://github.com/microsoft/psi/labels/feature%20request) label to request new features, and use the [`documentation`](https://github.com/microsoft/psi/labels/documentation) label to request additional documentation. 

# Contributing

We hope the community can help improve and evolve Platform for Situated Intelligence. If you plan to contribute to the codebase, please read the [Contributing Guidelines](https://github.com/microsoft/psi/wiki/Contributing) page. It describes how the source code is organized and things you need to know before making any source code changes.

# Who is Using

Platform for Situated Intelligence is currently being used in a number of industry and academic research labs, including (but not limited to):
* in the [Situated Interaction](https://www.microsoft.com/en-us/research/project/situated-interaction/) project, as well as other research projects at Microsoft Research.
* in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at Carnegie Mellon University.
* in the [Speech Language and Interactive Machines](https://coen.boisestate.edu/slim/) research group at Boise State University.
* in the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/), Northwestern University. 

If you would like to be added to this list, just add a [GitHub issue](https://github.com/Microsoft/psi/issues) and label it with the [`whoisusing`](https://github.com/Microsoft/psi/labels/whoisusing) label. Add a url for your research lab, website or project that you would like us to link to. 

# License

Platform for Situated Intelligence is available under an [MIT License](LICENSE.txt). See also [Third Party Notices](ThirdPartyNotices.txt).

# Acknowledgments

We would like to thank our internal collaborators and external early adopters, including (but not limited to): [Daniel McDuff](http://alumni.media.mit.edu/~djmcduff/), [Kael Rowan](https://www.microsoft.com/en-us/research/people/kaelr/), [Lev Nachmanson](https://www.microsoft.com/en-us/research/people/levnach/) and [Mike Barnett](https://www.microsoft.com/en-us/research/people/mbarnett) at MSR, Chirag Raman and Louis-Phillipe Morency in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at CMU, as well as adopters in the [SLIM research group](https://coen.boisestate.edu/slim/) at Boise State and in the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/) at Northwestern University.