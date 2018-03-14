---
layout: default
title:  ROS Integration
---

# \\psi / ROS Integration {#PsiRosIntegration}

The [ROS Bridge](https://github.com/Microsoft/psi/blob/master/Sources/Integrations/ROS/Microsoft.ROS/Readme.md) may be used to integrate ROS components into a \\psi system.
Generally a long-running ROS system is integrated into a \\psi application by constructing components utilizing the bridge to become proper publishers and subscribers over ROS protocols; exposing as emitters and receivers.

## Samples

After reading about the ROS bridge, the sample applications are a great way to understand how to integrate with \\psi.

* [Turtle Sample](https://github.com/Microsoft/psi/blob/master/Samples/PsiRosTurtleSample/Readme.md) - builds a \\psi application controlling the `turtlesim` (no hardware required)
* [Arm Control Sample](https://github.com/Microsoft/psi/blob/master/Samples/ArmControlROSSample/Readme.md) - builds a \\psi application controlling the [uArm Metal](http://ufactory.cc/#/en/uarm1) (hardware required)

## Comparisons

Like \\psi, ROS is an actor framework allowing loosely coupled modules to cooperate through message passing.
Unlike \\psi, ROS Nodes are generally separate, and possibly distributed, processes (except less common "Nodelets") and communication is over TCP sockets.
By consequence, ROS systems are generally very much more course grained by comparison to \\psi.

Also unlike \\psi, there is a ROS Master (`roscore`) that coordinates Nodes. In \\psi the application constructs the graph, rather than more or less "autonomous" nodes discovering one another.

For the most part, ROS message passing is much like \\psi streams - unidirectional, async.
However, ROS does have the concept of Services which are bidirectional, synchronous request/response messages.