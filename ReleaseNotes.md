---
layout: default
title:  Release Notes
---

# Release Notes

**2018/03/17**: Beta-release, version 0.2.123.1

Initial, beta version of the Platform for Situated Intelligence. Includes the Platform for Situated Intelligence runtime, visualization tools, and an initial set of components (mostly geared towards audio and visual processing). Relevant documents:

* [Documentation](/psi/) - top-level documentation page for Platform for Situated Intelligence.
* [Platform for Situated Intelligence Overview](/psi/PlatformOverview) - high-level overview of the platform.
* [NuGet Packages List](/psi/NuGetPackagesList) - list of NuGet packages available in this release.
* [Building the Code](/psi/BuildingPsi) - information about how to build the code.
* [Brief Introduction](/psi/tutorials) - brief introduction on how to write a simple application.
* [Samples](/psi/samples) - list of samples available. 

Below you can find a list of known issues, which we hope to address soon. Additionally, the [Roadmap](/psi/Roadmap) document provides insights about future planned developments.

<a name="KnownIssues" />

## Known Issues

Below we highlight a few known issues. You can see the full, current list of issues [here](https://github.com/Microsoft/psi/issues).

- - -

**Throttling:** Message throttling doesn't work when using the pre-defined delivery policies `Throttled` and `Immediate`.

Workaround: Create a new `DeliveryPolicy` instance and set `Throttling=true` and `MaxQueueSize=1`.

- - -

**[Identical Timestamps](https://github.com/Microsoft/psi/issues/1):**
Consecutive messages posted on the same stream with identical originating time timestamps are ambiguous when the stream is joined with another stream. In this case the result of the join operation is timing-dependent and not reproducible.

Workaround:
Ensure the originating time on consecutive messages is incremented by at least 1 tick.

- - -