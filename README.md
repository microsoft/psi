# Platform for Situated Intelligence

![Build status](https://dev.azure.com/msresearch/psi/_apis/build/status/psi-github-ci?branchName=master)
[![Join the chat at https://gitter.im/Microsoft/psi](https://badges.gitter.im/Microsoft/psi.svg)](https://gitter.im/Microsoft/psi?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

**Platform for Situated Intelligence** (or in short, \\psi, pronounced like the greek letter) is an open, extensible framework for development and research of multimodal, integrative-AI systems. These are systems that process various types of streaming sensor data (such as audio, video, depth, etc.) and that need to leverage and coordinate a variety of component technologies. Examples range from social robots or embodied agents that interact with people, to smart spaces such as instrumented meeting rooms, all the way to applications based on small devices that process streaming sensor data.

The framework accelerates the development of multimodal, integrative-AI systems by providing: 
-	a modern, performant **infrastructure** for working with multimodal, temporally streaming data
-	a set of **tools** for multimodal data visualization, annotation, and processing
-	an ecosystem of **components** for various sensors, processing technologies, and effectors

<br>

![Psi Overview](https://www.microsoft.com/en-us/research/uploads/prod/2018/01/Psi-Gif2-960-Corrected.gif)

# Getting Started

The core \\psi infrastructure is built on .NET Standard and therefore runs both on Windows and Linux. Some [components](https://github.com/microsoft/psi/wiki/List-of-Components) and tools are more specific and are available only on one or the other operating system. You can build \\psi applications either by [leveraging \\psi NuGet packages](https://github.com/microsoft/psi/wiki/Using-via-NuGet-Packages), or by [cloning and building the source code](https://github.com/microsoft/psi/wiki/Building-the-Codebase). 

__A Quick Introduction.__ To learn more about \\psi and how to build applications with it, we recommend you start with the [Brief Introduction](https://github.com/microsoft/psi/wiki/Brief-Introduction) tutorial, which will walk you through for some of the main concepts. It shows how to create a simple program, describes the core concept of a stream, and explains how to transform, synchronize, visualize, persist and replay streams from disk.

__Samples.__ A number of small [sample applications](https://github.com/microsoft/psi/wiki/Samples), several with walkthroughs, are also available. A good starting point is a sample ([Windows version](https://github.com/Microsoft/psi-samples/tree/main/Samples/WebcamWithAudioSample) or [Linux version](https://github.com/Microsoft/psi-samples/tree/main/Samples/LinuxWebcamWithAudioSample)) that illustrates how to capture and process synchronized data from a webcam and microphone.

__Documentation.__ The documentation for \\psi is available in the [github project wiki](https://github.com/microsoft/psi/wiki). It contains many additional resources, including [tutorials]( https://github.com/microsoft/psi/wiki/Tutorials), other [specialized topics]( https://github.com/microsoft/psi/wiki/Other-Topics), and a full [API reference](https://microsoft.github.io/psi/api/Microsoft.Psi.html) that can help you learn more about the framework.

# Getting Help
If you find a bug or if you would like to request a new feature or additional documentation, please file an [issue in github](https://github.com/microsoft/psi/issues). Use the [`bug`](https://github.com/microsoft/psi/labels/bug) label when filing issues that represent code defects, and provide enough information to reproduce the bug. Use the [`feature request`](https://github.com/microsoft/psi/labels/feature%20request) label to request new features, and use the [`documentation`](https://github.com/microsoft/psi/labels/documentation) label to request additional documentation. 

# Contributing

We are looking forward to engaging with the community to improve and evolve Platform for Situated Intelligence! We welcome contributions in many forms: from simply using it and filing issues and bugs, to writing and releasing your own new components, to creating pull requests for bug fixes or new features. The [Contributing Guidelines](https://github.com/microsoft/psi/wiki/Contributing) page in the wiki describes many ways in which you can get involved, and some useful things to know before contributing to the code base.

To find more information about our future plans, please see the [Roadmap](https://github.com/microsoft/psi/wiki/Roadmap) document.

# Who is Using

Platform for Situated Intelligence is currently being used in several industry and academic research labs, including (but not limited to):
* the [Situated Interaction](https://www.microsoft.com/en-us/research/project/situated-interaction/) project, as well as other research projects at Microsoft Research.
* the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at Carnegie Mellon University.
* the [Speech Language and Interactive Machines](https://coen.boisestate.edu/slim/) research group at Boise State University.
* the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/), Northwestern University. 
* the [Intelligent Human Perception Lab](https://www.ihp-lab.org), at USC Institute for Creative Technologies.
* the [Teledia research group](https://www.cs.cmu.edu/~cprose), at Carnegie Mellon University.
* the [F&M Computational, Affective, Robotic, and Ethical Sciences (F&M CARES) lab](https://fandm-cares.github.io/), at Franklin and Marshall College.

If you would like to be added to this list, just file a [GitHub issue](https://github.com/Microsoft/psi/issues) and label it with the [`whoisusing`](https://github.com/Microsoft/psi/labels/whoisusing) label. Add a url for your research lab, website or project that you would like us to link to. 

# Disclaimer

The codebase is currently in beta and various aspects of the framework are under active development. There are probably still bugs in the code and we may make breaking API changes. 

# License

Platform for Situated Intelligence is available under an [MIT License](LICENSE.txt). See also [Third Party Notices](ThirdPartyNotices.txt).

# Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

# Acknowledgments

We would like to thank our internal collaborators and external early adopters, including (but not limited to): [Daniel McDuff](http://alumni.media.mit.edu/~djmcduff/), [Kael Rowan](https://www.microsoft.com/en-us/research/people/kaelr/), [Lev Nachmanson](https://www.microsoft.com/en-us/research/people/levnach/) and [Mike Barnett](https://www.microsoft.com/en-us/research/people/mbarnett) at MSR, Chirag Raman and Louis-Phillipe Morency in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at CMU, as well as researchers in the [SLIM research group](https://coen.boisestate.edu/slim/) at Boise State and the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/) at Northwestern University.

