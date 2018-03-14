---
layout: default
title:  Datasets
---

# Datasets

The Platform for Situated Intelligence framework provides a set of APIs that allow you to organize data collected by an application
into datasets and process it accordingly. 

### 1. Stores and Datasets

First, let's clarify some terminology, specifically the notion of a <b><i>\\psi store</i></b> and a <b><i>\\psi dataset</i></b>. A
store is the unit of storage constructed when an application persists data to disk. As described in the [Brief Introduction](/psi/tutorials)
\\psi applications can write streams to disk using the `Store.Create` function and the `Write` stream
operator, for instance like below:

```csharp
// Create a store to write data to (change this path as you wish - the data will be stored there)
var store = Store.Create(p, "demo", "c:\\recordings");
    
// Write a stream to the store
myStream.Write("MyStream", store);
```

As as result of the `Store.Create` call, a \\psi store is created on disk at the specified location. The store 
contains the serialized data for all the streams that are written to it, and on disk is composed of several files, 
including catalog, data, and index files. When the application stops, the store is closed. 

A <b><i>\\psi dataset</i></b> allows you to group together multiple stores under a common umbrella. This can be very useful
if you have an application that runs multiple times, and generates a new store each time it runs (such as an interactive robot).
Dataset allow you to group multiple sessions together, and treat them in a unified way. 

### 2. The structure of a dataset: sessions and partitions

Datasets have a particular internal structure. Specifically, a dataset is composed of a set of <b><i>sessions</i></b>. Each session
in turn contains one or more <b><i>partitions</i></b>. Each partition is a store. The diagram below
shows the typical structure of a \\psi dataset.

![Dataset structure](/psi/topics/Dataset.png)

In the image above, the horizontal axis corresponds to time. The multiple sessions generally correspond to multiple runs of an 
application. Sessions are therefore generally sequentially laid out in time. This is however not a hard requirement: for instance, 
we could have a robot application that is deployed and can be run simultaneously on three different robots; in this case the 
sessions collected by the individual robot may overlap in time but can nonetheless be grouped into a dataset. Each session into 
a dataset is given a unique name.

The multiple partitions (or stores) inside a session generally correspond to the same time period. For instance, an application
might simultaneously write streams to multiple stores when it runs. All these stores taken represent a "session". 

The need to write at the same time to multiple stores might arise for instance in a distributed application where different parts of the 
\\psi pipeline run on different machines (and therefore persist on different disks). Data replay and experimentation provides another 
use case for multiple partitions. Consider the case where we have multiple runs of an application that persists raw collected data
to a store - we can assemble a dataset that contains multiple sessions, and each session contains a single partition with the 
raw data collected by the system at runtime, like below.

![Dataset with raw partitions](/psi/topics/Dataset.Raw.png)

We can then run some processing over these streams to compute some derived streams, which we can store in a parallel store, or 
partition, resulting in a structure like this:

![Dataset with raw and derived partitions](/psi/topics/Dataset.Derived.png)

Another use-case for partitions is one where we create manual annotations that correspond to the data in each session, and store these
annotations in an additional partition on the session.

The \\psi platform provides the <b><i>dataset</i></b> construct and allows users to organize the stores in sessions and partitions 
to support this type of scenarios. The Platform for Situated Intelligence Studio visualization tool is aware of the structure of 
datasets, with sessions and partitions and facilities visualizing together streams from the same session, across multiple partitions. 
Note however that there is no hard requirement imposed on the temporal structure. Sessions and partitions are just a means to 
organize data, and often the pattern we have described above is convenient. 
