---
layout: default
title:  Roadmap
---

# Roadmap

This is an initial, beta version of Platform for Situated Intelligence. The current version includes the Platform for Situated Intelligence runtime, visualization tools, and an initial set of components (mostly geared towards audio and visual processing).

The codebase is currently in beta and various aspects of the platform are at different levels of completion and robustness. The are probably still bugs and we will likely be making breaking API changes. 

Moving forward, we plan to prioritize addressing the bugs and issues that are discovered. The roadmap below describes some of the areas we plan to focus our efforts on moving forward (note that this roadmap is tentative and plans may change without notice as we re-assess priorities, etc.):

## Runtime

Apart from fixing bugs and issues, in the near future we expect to focus our efforts on: expanding the available set of [delivery policies](/psi/topics/InDepth.DeliveryPolicies) to allow for throttling and synchronous delivery; updating the pipeline shutdown procedure to guarantee no messages can be received after the Final handler is called; better documenting the mechanisms for cooperative buffering, how to control pipeline execution and replay, and serialization; further developing [synchronization](/psi/topics/Synchronization) primitives, e.g., `Join` and interpolators; improving the debugging experience.

## Tools

__Visualization__. In the near future we plan to enable Platform for Situated Intelligence Studio to perform live visualization by directly opening stores that are in the process of being written by an application (we plan to deprecate the programmatic APIs for live visualization); we also plan to enable data annotation scenarios and we will continue to improve performance, polish existing visualizers and potentially add new ones, and add features that enable more rapid exploration of the data. While the current version of Platform for Situated Intelligence Studio is a WPF application and as such is only available on Windows, in the long run we would like to develop a cross-platform version of this tool. 

__Data processing__. Currently the Dataset APIs enable some data processing scenarios over multiple stores. These APIs are still in a nascent state. We plan to further evolve these capabilies. 

__Learning__. We plan to develop infrastructure for supporting machine-learning scenarios. The infrastructure will be agnostic to and allow coupling a variety of learning engines, but will focus on supporting the end-to-end ML development loop, from data collection, to feature engineering, model construction and evaluation, all the way to running the models into an existing \\psi pipeline. 

## Components

__Improve existing components__. We plan to work on bug fixes and improving the existing set of components based on community feedback. 

__Interaction Toolkit__. We plan to develop representations and a set of components that are geared towards reasoning about physically situated language interaction.