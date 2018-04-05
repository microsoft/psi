# Platform for Situated Intelligence

**Platform for Situated Intelligence** is an open, extensible framework that enables the enables the development, fielding and study of situated, integrative-AI systems.

In recent years, we have seen significant progress with machine learning techniques on various perceptual and control problems. At the same time, building end-to-end, multimodal, integrative-AI systems that leverage multiple technologies and act autonomously or interact with people in the open world remains a challenging, error-prone and time-consuming engineering task. Numerous challenges stem from the sheer complexity of these systems and are amplified by the lack of appropriate infrastructure and development tools.

The Platform for Situated Intelligence project aims to address these issues and provide a basis for developing, fielding and studying integrative-AI systems. The platform consists of three layers. The **Runtime** layer provides a parallel programming model centered around temporal streams of data, and enables easy development of components and applications using .NET, while retaining the performance properties of natively written, carefully tuned systems. A set of **Tools** enable multimodal data visualization, annotations, analytics, tuning and machine learning scenarios. Finally, an open ecosystem of **Components** encapsulate various AI technologies and allow for quick compositing of integrative-AI applications. For more information about the goals of the project, the types of systems that you can build using it, and the various layers see [Platform for Situated Intelligence Overview](https://microsoft.github.io/psi/PlatformOverview).

# Using and Building

Platform for Situated Intelligence is built on the .NET Framework. Large parts of it are built on .NET Standard and therefore run both on Windows and Linux, whereas some components are specific and available only to one operating system (for instance the Kinect sensor component is available only for Windows.)

You can build applications based on Platform for Situated Intelligence either by leveraging nuget packages, or by cloning and building the code. Below are instructions:

* [Using \\psi via Nuget packages](https://microsoft.github.io/psi/UsingWithNuget)
* [Building the \\psi codebase](https://microsoft.github.io/psi/BuildingPsi)

# Getting Started

__Brief Introduction__. To get started with using Platform for Situated Intelligence, the [Brief Introduction](https://microsoft.github.io/psi/tutorials) page provides a guided walk-through for some of the main concepts in \\psi. It shows how to create a simple program, describes the core concept of a stream, and explains how to transform, synchronize, visualize, persist to and replay data from disk. We recommend that you work through the examples in this tutorial to familiarize yourself with these core concepts.

__Samples__. After going through this first brief tutorial, it may be helpful to look through the set of [Samples](https://microsoft.github.io/psi/samples) provided. While some of the samples address specialized topics such as how to leverage speech recognition components or how to bridge to ROS, reading them will give you more insight into programming with \\psi.

__In-depth Topics__. Finally, additional information is provided in a set of [In-Depth Topics](https://microsoft.github.io/psi/topics) that dive into ore detail on various aspects of the framework including synchronization, persistence, remoting, visualization etc. 

Like the rest of the codebase, the documentation is still under construction and in various phases of completion. If you need further explanation in any of these areas, please open an issue, label it `documentation`, as this will help us target our documentation development efforts to the highest priority needs.

# Disclaimer

The codebase is currently in beta and various aspects of the platform are at different levels of completion and robustness. There are probably still bugs in the code and we will likely be making breaking API changes. We plan to continuously improve the framework and we encourage the community to contribute.

For additional information, we recommend you read the [Known Issues](https://microsoft.github.io/psi/ReleaseNotes#KnownIssues) section from the [Release Notes](https://microsoft.github.io/psi/ReleaseNotes) document, which provides more information about important issues that are known and which we plan to address in the near future. Also, the [Roadmap](https://microsoft.github.io/psi/Roadmap) document provides more information about our future plans. 

# Getting Help

If you find a reproducible bug or if you would like to request a new feature or additional documentation, please file an [issue on the github repo](https://github.com/microsoft/psi/issues). If you do so, please make sure a corresponding issue has not already been filed. Use the [`bug`](https://github.com/microsoft/psi/labels/bug) label when filing issues that represent code defects, and provide enough information to reproduce. Use the [`feature request`](https://github.com/microsoft/psi/labels/feature%20request) label to request new features, and use the [`documentation`](https://github.com/microsoft/psi/labels/documentation) label to request additional documentation. 

# Contributing

We hope the community can help improve and evolve Platform for Situated Intelligence. If you plan to contribute to the codebase, please read the [Contributing Guidelines](CONTRIBUTING.md) document. It describes how the source code is organized and things you need to know before making any source code changes.

# Who is Using

Platform for Situated Intelligence is currently being used in a number of industry and academic research labs, including (but not limited to):
* in the [Situated Interaction](https://www.microsoft.com/en-us/research/project/situated-interaction/) project, as well as other research projects at Microsoft Research.
* in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at Carnegie Mellon University.
* in the [Speech Language and Interactive Machines](https://coen.boisestate.edu/slim/) research group at Boise State University.
* in the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/), Northwestern University. 

If you would like to be added to this list, just add a [GitHub issue](https://github.com/Microsoft/psi/issues) and label it with the [`whoisusing`](https://github.com/Microsoft/psi/labels/whoisusing) label. Add a url for your research lab, website or project that you would like us to link to. 

## License

Platform for Situated Intelligence is available under an [MIT License](LICENSE.txt). See also [Third Party Notices](ThirdPartyNotices.txt).
# Acknowledgments

We would like to thank our internal and external early adopters for the feedback provided during the alpha testing period, including (but not limited to): [Daniel McDuff](http://alumni.media.mit.edu/~djmcduff/) and [Kael Rowan](https://www.microsoft.com/en-us/research/people/kaelr/) at MSR, Chirag Raman and Louis-Phillipe Morency in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at CMU, as well as adopters in the [SLIM research group](https://coen.boisestate.edu/slim/) at Boise State and in the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/) at Northwestern University.
