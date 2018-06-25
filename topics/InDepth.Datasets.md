---
layout: default
title:  Datasets
---

# Datasets

The Platform for Situated Intelligence framework provides a set of APIs that allow you to organize data collected by an application
into datasets and process it accordingly.

This document provides an introduction to the dataset APIs. It is structured in the following sections:


1. [Stores and Datasets](/psi/topics/InDepth.Datasets#StoresAndDatasets): introduces the concepts of stores and datasets.
2. [The Structure of a Dataset: Sessions and Partitions](/psi/topics/InDepth.Datasets#Structure): describes the internal structure of a dataset.
3. [Organizing Data With the Dataset APIs](/psi/topics/InDepth.Datasets#OrganizingData): covers some of the basic APIs for organizing and managing datasets.
4. [Derived Partitions](/psi/topics/InDepth.Datasets#DerivedPartitions): describes the concept of derived partitions and how to create them.

<a name="StoresAndDatasets"></a>

### 1. Stores and Datasets

First, we should clarify some terminology - specifically the notions of a <b><i>\\psi store</i></b> and a <b><i>\\psi dataset</i></b>. A
store is the unit of storage that is created when an application persists data to disk. As described in the [Brief Introduction](/psi/tutorials),
\\psi applications can write streams to disk using the `Store.Create` function and the `Write` stream
operator. For example:

```csharp
// Create a store to write data to (change this path as you wish - the data will be stored there)
var store = Store.Create(p, "demo", "c:\\recordings");
    
// Write a stream to the store
myStream.Write("MyStream", store);
```

As a result of the `Store.Create` call, a \\psi store is created on disk at the specified location. The store 
contains the serialized data from all the streams that are written to it, and consists of several files, 
including the catalog, data, and index files. When the application terminates, the store is closed. 

A <b><i>\\psi dataset</i></b> allows you to group together multiple stores under a common umbrella. This can be very useful
if you have an application that runs multiple times, and generates a new store each time it runs (such as an interactive robot).
Datasets also allow you to group multiple <b><i>sessions</i></b> together, and reason over them in a unified way. 

<a name="Structure"></a>

### 2. The Structure of a Dataset: Sessions and Partitions

Internally, a dataset is composed of a set of <b><i>sessions</i></b>. Each session
in turn contains one or more <b><i>partitions</i></b>. Each partition corresponds to a single store. The following diagram
illustrates the typical structure of a \\psi dataset.

![Dataset structure](/psi/topics/Dataset.png)

In the image above, the horizontal dimension normally corresponds to time. The multiple sessions generally correspond to multiple runs of an 
application. Sessions are therefore generally sequentially laid out in time. However, this is not a hard requirement. For instance, 
we could have a robot application that is deployed and run simultaneously on three different robots. In this case the 
sessions collected by the individual robots may overlap in time but can nonetheless be grouped into one dataset. Each session in 
a dataset is given a unique name.

The multiple partitions (or stores) inside a session generally correspond to the same time period. For instance, an application
might simultaneously write streams to multiple stores when it runs. This group of stores collectively represents a "session". 

The need to simultaneously write to multiple stores might arise for instance in a distributed application where different parts of the 
\\psi pipeline run on different machines (and therefore persist data on different disks). Data replay and experimentation provides another 
use case for multiple partitions. Consider the case where we have multiple runs of an application where each run persists raw collected data
to a store. We can assemble a dataset that contains multiple sessions, with each session containing a single partition with 
raw data from each run as illustrated in the following diagram:

![Dataset with raw partitions](/psi/topics/Dataset.Raw.png)

We can then, for example, run some data processing functions over these streams to compute derived streams, which we will save in parallel stores (or 
partitions), resulting in a dataset with the following structure:

![Dataset with raw and derived partitions](/psi/topics/Dataset.Derived.png)

Another use case for partitions is one where we create manual annotations that correspond to the data in each session, and store these
annotations in an additional partition on the session.

The \\psi platform provides the `Dataset` class to allow users to organize data stores in `Sessions` and `Partitions`
to support these kinds of scenarios. The Platform for Situated Intelligence Studio visualization tool is aware of the structure of 
datasets, sessions and partitions and has facilities for visualizing groups of streams from the same session, across multiple partitions. 
Note that there is no hard requirement that imposes a temporal structure. Sessions and partitions simply provide a means to 
organize data. That said, the patterns we have described above are the most commonly encountered.

<a name="OrganizingData"></a>

### 3. Organizing Data With the Dataset APIs

Datasets may be created and modified either programmatically via a set of APIs, or directly from the Platform for Situated Interaction Studio UI. This section will provide a brief overview of the dataset APIs. 

At the root of the dataset hierarchy is the `Dataset` class, which as previously described, serves as a container for a group of sessions. A new dataset may be created simply by calling the class constructor:

```csharp
// Create a new untitled Dataset
var dataset = new Dataset();
```

In general, datasets (and sessions and partitions) should be named by providing a name during construction or by setting the `Name` property:

```csharp
// Create a new Dataset
var dataset = new Dataset("MyDataset");

// Rename a Dataset
dataset.Name = "MyNewDataset";
```

Upon creation, a dataset is initially empty. New empty sessions may be created and added to the dataset as follows:

```csharp
// Create a new Session in the Dataset
var session = dataset.CreateSession("MySession");
```

The `Partition` class is the abstract base class from which all partition types derive. \psi currently supports <i>store partitions</i> implemented by the `StorePartition` class. A store partition represents all the data from a \psi store (either on disk or in-memory). Other partition types may be added in the future to represent different kinds of stores, such as an MPEG-4 file containing streams of audio and video, or a comma-separated values (CSV) file.

Partitions may be added to a session in a variety of ways: by creating a new empty partition (and corresponding store on disk), by adding from a pre-existing store, or by computing a derived partition from the data contained in other partitions. The latter case will be covered in more detail in [a separate section](/psi/topics/InDepth.Datasets#DerivedPartitions).

The `Session` class provides the `CreateStorePartition` method to create a new empty store and add the corresponding partition to a session:

```csharp
// Create a new empty StorePartition
var partition = session.CreateStorePartition("NewStore", "c:\\stores", "NewPartition");
```

This creates a new store named "NewStore" under the c:\\stores folder, and associates it with a new partition in the session named "NewPartition". Note that the last argument (the partition name) is optional, and if omitted, the store name will be used for the partition name. Note that the partition name serves to distinguish partitions from one another within a session, whereas the store name relates to the name of the store on disk.

A partition may also be created from an existing store and added to the session using the `AddStorePartition` method:

```csharp
// Add a partition from an existing store
var partition = session.AddStorePartition("MyStore", "c:\\recordings", "MyPartition");
```

This creates a new partition named "MyPartition" representing the store "MyStore" located in the c:\\recordings folder, and adds it to the session.

For convenience, the `Dataset` class also provides a number of methods to add existing store partitions directly to the dataset or to create an entirely new dataset from an existing store on disk. In all cases, each resulting partition will be contained within its own session in the dataset.

For instance, we can create a new dataset from previously recorded store on disk using the static `CreateFromExistingStore` method:

```csharp
// Create a new Dataset from an existing store
var dataset = Dataset.CreateFromExistingStore("MyStore", "c:\\recordings", "MyPartition");
```

If we simply wanted to add a store partition in its own separate session to an existing dataset, we would use the `AddSessionFromExistingStore` method:

```csharp
// Create and add a new Session from an existing store
var session = dataset.AddSessionFromExistingStore("MySession", "MyStore", "c:\\recordings", "MyPartition");
```

Datasets and the organizational structure they define may be saved to a dataset file. This allows the structure of the sessions and partitions to be retained and reused at a later time (for instance to visualize the same set of data). Note that saving the dataset only preserves the layout structure of the data. The original stores containing the actual data are not saved and are assumed to exist on disk at the same path locations relative to the dataset file.

```csharp
// Save the Dataset to a file
dataset.Save("c:\\datasets\MyDataset.pds");

// Load a Dataset from a file
var dataset = Dataset.Load("c:\\datasets\MyDataset.pds");
```

<a id="DerivedPartitions"></a>

### 4. Derived Partitions

As previously described, datasets and sessions provide a means for organizing and reasoning over data stored in multiple partitions. While simply grouping together pre-existing stores of data can be useful from the point of view of organizing the data for consumption (e.g. visualization), another powerful feature of datasets and sessions is the ability they provide to perform computations over large amounts of data and generate derivative streams of data, which may be stored in a separate partition alongside the original data partitions. We can accomplish this using the `CreateDerivedPartitionAsync` methods on the `Dataset` class.

The simplest overload of this API takes an `Action` delegate that performs the actual computation and creation of the derived streams:

```csharp
public async Task CreateDerivedPartitionAsync(
    Action<Pipeline, SessionImporter, Exporter> computeDerived,
    string outputPartitionName,
    bool overwrite = false,
    string outputStoreName = null,
    string outputStorePath = null,
    ReplayDescriptor replayDescriptor = null,
    CancellationToken cancellationToken = default(CancellationToken))
```

The `CreateDerivedPartitionAsync` APIs are asynchronous to accommodate awaiting and cancelling long-running computations over large datasets.

As an example, given a raw dataset from containing a stream of audio captured from a run of an application, we may want to compute a set of acoustic feature streams over the audio for further analysis, or simply to visualize them. Assuming that the dataset contains a stream named "RawAudioData", we can define an action that will import the data from this stream using the `SessionImporter`, compute the desired acoustic features using the `AcousticFeatures` \\psi component, and write the computed streams back to the dataset in a derived partion using the `Exporter`:

```csharp
await dataset.CreateDerivedPartitionAsync(
    (pipeline, importer, exporter) =>
    {
        // create a component to compute the acoustic features
        var acousticFeatures = new AcousticFeatures(pipeline);

        // import the raw audio stream and pipe it to the acoustic features component
        var rawAudio = importer.OpenStream<AudioBuffer>("RawAudioData");
        rawAudio.PipeTo(acousticFeatures);

        // save the derived acoustic feature streams
        acousticFeatures.LogEnergy.Write("LogEnergy", exporter);
        acousticFeatures.LowFrequencyEnergy.Write("LowFrequencyEnergy", exporter);
        acousticFeatures.SpectralEntropy.Write("SpectralEntropy", exporter);
    },
    "AcousticFeatures",
    true,
    "AcousticFeatures",
    "c:\\derived");
```

When the above method executes, it creates a \\psi pipeline as the context in which the `computeDerived` action runs. All the code that is necessary to setup the pipeline to import the raw stream, compute the derived features and export them to the derived partition is defined within the action.

The computed streams are persisted to a store named "AcousticFeatures" at the specified path on disk (c:\\derived), and a new partition (also named "AcousticFeatures") is added to each session in the dataset. 

Additional overloads are provided that allow for progress reporting and computed output paths. Taking the previous example again, we may want to provide feedback to the UI on the status and percentage completion of the asynchronous operation as the derived streams are computed. We may do this by supplying as an argument an object which implements the `IProgress<(string, double)>` interface. In this case, the type argument (a `ValueTuple` of `(string, double)`) represents a status message reported by the method and a value between 0 and 1.0 indicating the fractional completion of the operation. .NET provides a built-in [`Progress<T>`](https://docs.microsoft.com/en-us/dotnet/api/system.progress-1?view=netstandard-2.0) class which may be used for this purpose. In the following example, the progress is just written to the console by the delegate used to contruct the `Progress<T>` object.

```csharp
// create an object for reporting progress
var progress = new Progress<(string, double)>(p => Console.WriteLine($"[{p.Item2:P1}] {p.Item1}"));

await dataset.CreateDerivedPartitionAsync(
    (pipeline, importer, exporter) =>
    {
        // create a component to compute the acoustic features
        var acousticFeatures = new AcousticFeatures(pipeline);

        // import the raw audio stream and pipe it to the acoustic features component
        var rawAudio = importer.OpenStream<AudioBuffer>("RawAudioData");
        rawAudio.PipeTo(acousticFeatures);

        // save the derived acoustic feature streams
        acousticFeatures.LogEnergy.Write("LogEnergy", exporter);
        acousticFeatures.LowFrequencyEnergy.Write("LowFrequencyEnergy", exporter);
        acousticFeatures.SpectralEntropy.Write("SpectralEntropy", exporter);
    },
    "AcousticFeatures",
    true,
    "AcousticFeatures",
    (session) => $"c:\\derived\\{session.Name}",
    null,
    progress);
```

Also of note in the above example is the use of a function delegate for the output path argument (as opposed to a fixed path such as c:\\derived in the previous example). This allows derived partitions from different sessions to be written to stores under different subfolders. For example, data in a dataset may be organized in multiple sessions, each representing the data collected from a separate run of an application. It would make sense therefore to maintain a similar organizational structure for the derived stores on disk, with the derived stores for each session saved under a subfolder respresenting the session (in the example, we simply use subfolders with the session name).

Once a derived partition has been created, the derived streams may be visualized alongside the raw data in the modified dataset:

![Visualizing dataset with raw and derived partitions](/psi/topics/Dataset.Visualize.png)

This is just one example of how the `Dataset` class facilitates the computation and organization of derived data. Another common use case is the extraction of features and labels for machine learning problems.
