# Platform for Situated Intelligence

![Build status](https://dev.azure.com/msresearch/psi/_apis/build/status/psi-github-ci?branchName=master)
[![Join the chat at https://gitter.im/Microsoft/psi](https://badges.gitter.im/Microsoft/psi.svg)](https://gitter.im/Microsoft/psi?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

**Platform for Situated Intelligence** (or in short, \\psi, pronounced like the greek letter) is an open, extensible framework for development and research of multimodal, integrative-AI systems. Examples include multimodal interactive systems such as social robots and embodied conversational agents, mixed-reality systems, applications for ambient intelligence or smart spaces, etc. In essence, any application that processes streaming, sensor data (such as audio, video, depth, etc.), combines multiple (AI) technologies, and operates under latency constraints can benefit from the affordances the provided by the framework.

The framework provides: 
-	**a modern, performant infrastructure** for working with multimodal, temporally streaming data
-	**a set of tools** for multimodal data visualization, annotation, and processing
-	**an ecosystem of components** for various sensors, processing technologies, and effectors

<br>

![Psi Overview](https://www.microsoft.com/en-us/research/uploads/prod/2018/01/Psi-Gif2-960-Corrected.gif)

A high-level overview of the framework is available in [this blog post](https://www.microsoft.com/en-us/research/blog/platform-for-situated-intelligence-an-open-source-framework-for-multimodal-integrative-ai/). A webinar containing a brief introduction and tutorial on how to code with \psi is now available [as an online video](https://youtu.be/A6nr8MWwM3o).

# What’s New

__12/08/2022__: This week we have released [beta version 0.18](https://github.com/microsoft/psi/wiki/Release-Notes#20221206-beta-release-version-018721), continuing to refine support for building mixed reality applications with \psi, and further evolving debugging and visualization capabilities in PsiStudio.

__04/21/2022__: We've recently released [beta version 0.17](https://github.com/microsoft/psi/wiki/Release-Notes#20220420-beta-release-version-017521), which includes important updates to mixed reality support in \psi, including [a set of tools for streaming data from a HoloLens 2 to a separate PC for data collection and export](https://github.com/microsoft/psi/tree/master/Sources/MixedReality/HoloLensCapture). The release also includes several updates to visualization and PsiStudio, the addition of a wrapper for running MaskRCNN models, updates to Azure Kinect Components, as well as some runtime updates and various other bug fixes.

__07/29/2021__: Check out this [new sample application](https://github.com/microsoftgraph/microsoft-graph-comms-samples/tree/master/Samples/PublicSamples/PsiBot) which shows how you can integrate \\psi with the Teams bot architecture to develop bots that can participate in live meetings! (Please note that although it is hosted in the Microsoft Graph repository, you should post any issues or questions about this sample here).

__05/02/2021__: We've opened the [Discussions](https://github.com/microsoft/psi/discussions) tab on the repo and plan to use it as a place to connect with other members of our community. Please use these forums to
[ask questions](https://github.com/microsoft/psi/discussions/categories/q-a), [share ideas and feature requests](https://github.com/microsoft/psi/discussions/categories/ideas), [show off the cool components or projects you're building with \psi](https://github.com/microsoft/psi/discussions/categories/show-and-tell), and generally engage with other community members.

__04/29/2021__: Thanks to all who joined us for the [Platform for Situated Intelligence Workshop](https://aka.ms/psi-w1)! In this workshop, we discussed the basics on how to use the framework to accelerate your own work in the space of multimodal, integrative AI; presented some in-depth tutorials, demos, and previews of new features; and had a fun panel on how to build and nurture the open source community. All sessions were recorded, and you can find the videos on the [event website](https://aka.ms/psi-w1) now.

__03/31/2021__: We published a [technical report](https://arxiv.org/abs/2103.15975) containing a more in-depth description of the various aspects of the framework.

# Getting Started

The core \\psi infrastructure is built on .NET Standard and therefore runs both on Windows and Linux. Some [components](https://github.com/microsoft/psi/wiki/List-of-Components) and tools are more specific and are available only on one or the other operating system. You can build \\psi applications either by [leveraging \\psi NuGet packages](https://github.com/microsoft/psi/wiki/Using-via-NuGet-Packages), or by [cloning and building the source code](https://github.com/microsoft/psi/wiki/Building-the-Codebase). 

__A Brief Introduction.__ To learn more about \\psi and how to build applications with it, we recommend you start with the [Brief Introduction](https://github.com/microsoft/psi/wiki/Brief-Introduction) tutorial, which will walk you through for some of the main concepts. It shows how to create a simple program, describes the core concept of a stream, and explains how to transform, synchronize, visualize, persist and replay streams from disk.

__A Video Webinar.__ If you prefer getting started by watching a presentation about the framework, this [video webinar](https://youtu.be/A6nr8MWwM3o) gives a 30 minute high-level overview of the framework, followed by a 30 minute hands-on coding session illustrating how to write a first, simple application. Alternatively, for a shorter (~13 min) high-level overview, see [this presentation](https://innovation.microsoft.com/en-us/tech-minutes-platform-for-situated-intelligence) we did as part of the Tech Minutes series.

__Samples.__ If you would like to directly start from sample code, a number of small [sample applications](https://github.com/microsoft/psi/wiki/Samples) are also available, and several of them have walkthroughs that explain how the sample was constructed and point to additional documentation. We recommend you start with the samples below, listed in increasing order of complexity:
| Name | Description | Cross-plat | Requirements |
| :----------- | :---------- | :--: | :--: |
| [HelloWorld](https://github.com/Microsoft/psi-samples/tree/main/Samples/HelloWorld) <br> ![HelloWorldPreview](https://github.com/microsoft/psi-samples/blob/main/Samples/HelloWorld/Images/HelloWorldPreview.png) | This sample provides the simplest starting point for creating a \\psi application: it illustrates how to create and run a simple \psi pipeline containing a single stream. | Yes | None |
| [SimpleVoiceActivityDetector](https://github.com/Microsoft/psi-samples/tree/main/Samples/SimpleVoiceActivityDetector) <br> ![SimpleVADPreview](https://github.com/microsoft/psi-samples/blob/main/Samples/SimpleVoiceActivityDetector/Images/SimpleVADPreview.png) | This sample captures audio from a microphone and performs voice activity detection, i.e., it computes a boolean signal indicating whether or not the audio contains voiced speech. | Yes | Microphone |
| [WebcamWithAudio for Windows](https://github.com/Microsoft/psi-samples/tree/main/Samples/WebcamWithAudioSample) or [Linux](https://github.com/Microsoft/psi-samples/tree/main/Samples/LinuxWebcamWithAudioSample) <br> ![WebcamPreview](https://github.com/microsoft/psi-samples/blob/main/Samples/WebcamWithAudioSample/Images/WebcamPreview.gif) | This sample shows how to display images from a camera and the audio energy level from a microphone and illustrates the basics of stream synchronization. | Yes | Webcam and Microphone | 
| [WhatIsThat](https://github.com/microsoft/psi-samples/tree/main/Samples/WhatIsThat) <br> ![WhatIsThatPreview](https://github.com/microsoft/psi-samples/blob/main/Samples/WhatIsThat/Images/WhatIsThatPreview.gif)| This sample implements a simple application that uses an Azure Kinect sensor to detect the objects a person is pointing to. | Windows-only | Azure Kinect + Cognitive Services |
| [HoloLensSample](https://github.com/microsoft/psi-samples/tree/main/Samples/HoloLensSample) <br> ![Preview gif of visualized output streams from one of the HoloLens Sample demos](https://github.com/microsoft/psi-samples/blob/main/Samples/HoloLensSample/Images/MovableMarker.gif) | This sample demonstrates how to develop Mixed Reality \\psi applications for HoloLens 2. | UWP | HoloLens 2 |

__Documentation.__ The documentation for \\psi is available in the [github project wiki](https://github.com/microsoft/psi/wiki). It contains many additional resources, including [tutorials]( https://github.com/microsoft/psi/wiki/Tutorials), other [specialized topics]( https://github.com/microsoft/psi/wiki/Other-Topics), and a full [API reference](https://microsoft.github.io/psi/api/Microsoft.Psi.html) that can help you learn more about the framework.

# Getting Help
If you find a bug or if you would like to request a new feature or additional documentation, please file an [issue in github](https://github.com/microsoft/psi/issues). Use the [`bug`](https://github.com/microsoft/psi/labels/bug) label when filing issues that represent code defects, and provide enough information to reproduce the bug. Use the [`feature request`](https://github.com/microsoft/psi/labels/feature%20request) label to request new features, and use the [`documentation`](https://github.com/microsoft/psi/labels/documentation) label to request additional documentation.

Please also make use of the [Discussions](https://github.com/microsoft/psi/discussions) for asking general questions, sharing ideas about new features or applications you might be interested in, showing off the wonderful things you're building with \psi, and engaging with other community members.

# Contributing

We are looking forward to engaging with the community to improve and evolve Platform for Situated Intelligence! We welcome contributions in many forms: from simply using it and filing issues and bugs, to writing and releasing your own new components, to creating pull requests for bug fixes or new features. The [Contributing Guidelines](https://github.com/microsoft/psi/wiki/Contributing) page in the wiki describes many ways in which you can get involved, and some useful things to know before contributing to the code base.

To find more information about our future plans, please see the [Roadmap](https://github.com/microsoft/psi/wiki/Roadmap) document.

# Who is Using

Platform for Situated Intelligence has been and is currently used in several industry and academic research labs, including (but not limited to):
* the [Situated Interaction](https://www.microsoft.com/en-us/research/project/situated-interaction/) project, as well as other research projects at Microsoft Research.
* the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at Carnegie Mellon University.
* the [Speech Language and Interactive Machines](https://coen.boisestate.edu/slim/) research group at Boise State University.
* the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/), Northwestern University. 
* the [Intelligent Human Perception Lab](https://www.ihp-lab.org), at USC Institute for Creative Technologies.
* the [Teledia research group](https://www.cs.cmu.edu/~cprose), at Carnegie Mellon University.
* the [F&M Computational, Affective, Robotic, and Ethical Sciences (F&M CARES) lab](https://fandm-cares.github.io/), at Franklin and Marshall College.
* the [Transportation, Bots, & Disability Lab](https://tbd.ri.cmu.edu) at the Carnegie Mellon University.

If you would like to be added to this list, just file a [GitHub issue](https://github.com/Microsoft/psi/issues) and label it with the [`whoisusing`](https://github.com/Microsoft/psi/labels/whoisusing) label. Add a url for your research lab, website or project that you would like us to link to. 

# Technical Report

A more in-depth description of the framework is available in [this technical report](https://arxiv.org/abs/2103.15975). Please cite as: 

```text
@misc{bohus2021platform,
      title={Platform for Situated Intelligence}, 
      author={Dan Bohus and Sean Andrist and Ashley Feniello and Nick Saw and Mihai Jalobeanu and Patrick Sweeney and Anne Loomis Thompson and Eric Horvitz},
      year={2021},
      eprint={2103.15975},
      archivePrefix={arXiv},
      primaryClass={cs.AI}
}
```

# Disclaimer

The codebase is currently in beta and various aspects of the framework are under active development. There are probably still bugs in the code and we may make breaking API changes. 

While the Platform for Situated Intelligence source code and the `Microsoft.Psi.*` NuGet packages are available under an MIT license, our code and NuGet packages have dependencies on other NuGet packages. If you build an application using Platform for Situated Intelligence, please check the licensing requirements for all referenced NuGet packages in your solution.

# License

Platform for Situated Intelligence is available under an [MIT License](LICENSE.txt). See also [Third Party Notices](ThirdPartyNotices.txt).

# Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

# Acknowledgments

We would like to thank our internal collaborators and external early adopters, including (but not limited to): [Daniel McDuff](http://alumni.media.mit.edu/~djmcduff/), [Kael Rowan](https://www.microsoft.com/en-us/research/people/kaelr/), [Lev Nachmanson](https://www.microsoft.com/en-us/research/people/levnach/) and [Mike Barnett](https://www.microsoft.com/en-us/research/people/mbarnett) at MSR, Chirag Raman and Louis-Phillipe Morency in the [MultiComp Lab](http://multicomp.cs.cmu.edu/) at CMU, as well as researchers in the [SLIM research group](https://coen.boisestate.edu/slim/) at Boise State and the [Qualitative Reasoning Group](http://www.qrg.northwestern.edu/) at Northwestern University.

