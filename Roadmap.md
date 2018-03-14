---
layout: default
title:  Roadmap
---

# Roadmap

This is an initial, beta version of Platform for Situated Intelligence. The current version includes the Platform for Situated Intelligence runtime, visualization tools, and an initial set of components (mostly geared towards audio and visual processing).

The codebase is currently in beta and various aspects of the platform are at different levels of completion and robustness. The are probably still bugs and we likely be making breaking API changes. 

Moving forward, we plan to prioritize addressing the bugs and issues that are discovered. The roadmap below describes some of the areas we plan to focus our efforts on moving forward (note that this roadmap is tentative and plans may change without notice as we re-assess priorities, etc.):

### Runtime

Apart from fixing bugs and issues, in the near future we expect to focus our efforts on: further developing synchronization primitives, e.g., `Join` and interpolators; adding support for reasoning about stream closings; adding support for sub-pipelines that can be instantiated and closed at runtime; improving the debugging experience.


### Tools

__Visualization__. Platform for Situated Intelligence Studio was developed as a WPF application and as such is only available on Windows. Moving forward we plan to develop a cross-platform version of this tool. As we move to the cross-platform solution, we expect to deprecate and no longer support the current WPF version of Platform for Situated Intelligence Studio. 

__Data processing__. Currently the Dataset APIs enable some data processing scenarios over multiple stores. These APIs are still in a nascent state. We plan to further evolve these capabilies. 

__Learning__. We plan to develop infrastructure for supporting machine-learning scenarios. The infrastructure will be agnostic to and allow coupling a variety of learning engines, but will support the end-to-end ML development loop, from data collection, to feature engineering, model construction and evaluation, all the way to running the models into an existing pipeline. 

### Components

__Improve existing components__. We plan to work on bug fixes and improving the existing set of components based on community feedback. 

__Interaction Toolkit__. We plan to develop representation and a set of components that are geared towards reasoning about physically situated language interaction.