---
layout: default
title:  Overview
---

# Platform for Situated Intelligence Overview

### What is Platform for Situated Intelligence?

<b>P</b>latform for <b>S</b>ituated <b>I</b>ntelligence (which we abbreviate as \\psi, pronounced like the greek letter) is __an open, extensible framework that enables the development, fielding and study of situated, integrative-AI systems__.

To clarify this statement, let us start with __situated, integrative-AI systems__. Here, "situated" refers to the fact that the framework is primarily targeted towards systems that sense and act in the real world. As such, \\psi systems generally operate over streaming data (typically originating in sensors), and need to act in the world with low-latency and under uncertainty. By low-latency, we mean mostly human-scale interactivity latencies, e.g., in the region of 50-200ms; by uncertainty, we refer largely to the uncertainty introduced by inaccurate sensing and inference. The term "integrative-AI" refers to the fact that \\psi targets systems that combine multiple, heterogeneous AI technologies and components.

The class of systems that \\psi therefore enables includes various cyber-physical systems, e.g. interactive robots, drones, virtual interactive agents, personal assistants, interactive instrumented meeting rooms, software systems that mesh human and machine intelligence, etc. Generally, any system that operates over streaming data and has low-latency constraints is a good candidate. \\psi might also benefit other kinds of applications, especially those that integrate multiple technologies and/or machine learning models.

Next, let us clarify __development, fielding and study__. \psi provides a framework that enables the development and fielding of integrative-AI systems. Given their complexity, the software development process for these systems is often expensive and challenging. \psi provides the infrastructure and a set of tools to mitigate some of these challenges and accelerate the development and fielding process. The framework allows for development, debugging, analysis, maintenance and continuous evolution of systems that couple multiple human-authored components with machine-learned models. It empowers the developer-in-the-loop, and enables rapid iteration, monitoring, and analysis. It provides a library of AI-primitives (e.g., supervised and online learning models, control models, etc.) that can be composed to enable broader AI scenarios. Finally, by providing a unifying underlying framework that couples computation over streaming data with machine learning and uncertainty, \psi will enable system-level reasoning and optimization capabilities (above the component level), and will enable research into the science of integrative systems.

Finally, Platform for Situated Intelligence is __open and extensible__. The codebase for \\psi is open-sourced under an [MIT License](https://github.com/Microsoft/psi/blob/master/LICENSE.txt).


### Why build Platform for Situated Intelligence?

In recent years, we have seen significant progress with machine learning techniques on various perceptual and control problems. At the same time, building end-to-end, multimodal, integrative-AI systems that leverage multiple technologies and act autonomously or interact with people in the open world remains a challenging, error-prone and time-consuming engineering task. Numerous challenges stem from the sheer complexity of these systems and are amplified by the lack of appropriate infrastructure and development tools.

The Platform for Situated Intelligence project aims to address these issues and provide a basis for developing, fielding and studying integrative-AI systems. By releasing the code under an open-source [MIT License](https://github.com/Microsoft/psi/blob/master/LICENSE.txt), we hope to enable the community to contribute, grow the \\psi ecosystem, and further lower the barrier to entry in developing complex, integrative-AI systems.

### What does Platform for Situated Intelligence contain?

The Platform for Situated Intelligence provides a **Runtime** that enables parallel coordinated computation, a set of **Tools** that provide development support and an ecosystem of **Components**. \\psi applications are authored by connecting together \\psi components. 

- **Runtime**. The \\psi runtime and core libraries provide a parallel, coordinated computation model centered on online processing of streaming data. Time is a first-order citizen in the framework. The runtime provides abstractions for computing over streaming data and for reasoning about time and synchronization, and is optimized for low-latency from the bottom up. In addition, the runtime provides fast persistence of generic streams, enabling data-driven development scenarios.

- **Tools**. \\psi provides a powerful set of tools that enable testing, visualization, data replay, data analytics and machine learning development for integrative-AI systems. The visualization subsystem allows for live and offline visualization of streaming data. A set of data processing APIs allow for re-running algorithms over collected data, data analytics and feature extraction for machine learning. Collectively, the \\psi tools and infrastructure enable rapid development of ML models and model integration in a \\psi application.

- **Components**. \\psi provides a wide array of AI technologies encapsulated into \\psi-components. \\psi applications can be easily developed by wiring together \\psi components. The initial set of components includes sensor components for cameras and microphones, audio and image processing components, as well as components that provide access to Microsoft's Cognitive Services. We hope to create through community contributions a broader ecosystem of components that will lower the barrier to entry for developing integrative-AI systems. 

More information about upcoming features in Platform for Situated Intelligence is available in the [Roadmap](/psi/Roadmap) document.

