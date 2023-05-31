# Platform for Situated Intelligence Store Tool

This command-line tool allows exploration of available streams in a store, conversion [to other formats (MessagePack, JSON, CSV)](../../Runtime/Microsoft.Psi.Interop/Format/Readme.md)
and [persisting to disk or sending over a message queue](../../Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages.
It works with *any* types found within; converting to `dynamic`/`ExpandoObject` streams.
It also allows concatenation of stores and execution of tasks defined in external assemblies; acting over stores and streams within.

## Verbs and Options

The following verbs are supported:

| Verb       | Description                                          |
| ---------- | ---------------------------------------------------- |
| `list`     | List streams within a Psi data store.                |
| `info`     | Display stream information (metadata).               |
| `messages` | Display messages in stream.                          |
| `save`     | Save messages to file system.                        |
| `send`     | Send messages to message queue (ZeroMQ/NetMQ).       |
| `concat`   | Concatenate a set of stores, generating a new store. |
| `crop`     | Crops a store between specified interval.            |
| `tasks`    | List available tasks in assemblies given.            |
| `exec`     | Execute task defined in assembly given.              |
| `help`     | Display more information on a specific command.      |
| `version`  | Display version information.                         |

The following options are available:

| Option | Abbr         | Description                                                                  |
| ------ | ------------ | ---------------------------------------------------------------------------- |
| `p`    | `path`       | File path to Psi data store (default=working directory).                     |
| `s`    | `stream`     | Name Psi stream within data store.                                           |
| `m`    | `format`     | Format specifier (msg, json, csv).                                           |
| `n`    | `number`     | Include first n messages (optional).                                         |
| `f`    | `file`       | File to which to persist data.                                               |
| `t`    | `topic`      | Topic name to which to send messages (default='').                           |
| `a`    | `address`    | Connection address to which to send messages (e.g. 'tcp://localhost:12345'). |
| `o`    | `output`     | Optional name of concatenated output Psi data store (default=Concatenated).  |
| `t`    | `task`       | Task name to execute.                                                        |
| `m`    | `assemblies` | Optional assemblies containing task (semicolon-separated).                   |
| `a`    | `arguments`  | Task arguments provided at the command-line (semicolon-separated).           |
| `s`    | `start`      | Start of cropping interval.                                                  |
| `l`    | `length`     | Length of cropping interval.                                                 |

## Exploring

To list the streams within a store:

```bash
> dotnet PsiStoreTool.dll list -p E:\Data -d MyStore

Available Streams (store=MyStore, path=E:\Data)
MyStream (System.Double)
AnotherStream (MyNamespace.MyType)
...
Count: 3394
```

This displays the name and .NET type of each stream. Adding '-s true' option enables listing the size of each stream (the information displayed includes both the average message size and the total size of all messages in the store).

To get info about a particular stream:

```bash
> dotnet PsiStoreTool.dll info -p E:\Data -d MyStore -s MyStream

Stream Metadata (stream=MyStream, store=MyStore, path=E:\Data)

ID: 6
Name: Vision.VideoSources.FrontCamera
TypeName: Microsoft.Psi.Shared`1[[Microsoft.Psi.Imaging.EncodedImage, Microsoft.Psi.Imaging.Windows, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]], Microsoft.Psi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
MessageCount: 2768
AverageFrequency: 226.907379833755
AverageLatency: -1631182609
AverageMessageSize: 88004
FirstMessageOriginatingTime: 1/24/2017 8:30:28 AM
LastMessageOriginatingTime: 1/24/2017 8:32:03 AM
IsClosed: True
IsIndexed: False
IsPersisted: True
IsPolymorphic: False
```

To get an idea of the structure of the messages, they may be displayed:

```bash
> dotnet PsiStoreTool.dll messages -p E:\Data -d MyStore -s MyStream -n 10

Stream Messages (stream=MyStream, store=MyStore, path=E:\Data, number=10)

Originating Time: 1/24/2017 8:30:28 AM
Message:
  DetectedFacesCount: 1
  RanFaceDetection: True
  RanFaceDetectionOnVideoFrameId: 12
  VideoFrameTimeStamp: 1/24/2017 8:30:28 AM
  VideoFrameID: 187982
  FaceConfidenceModelOutput: 0.107624922736431

...
```

This will display the messages and their structure; having been converted from their .NET types to `dynamic` primitives and/or `ExpandoObject`.

The number of messages displayed may optionally be limited to the first `n` messages.

## Saving

Streams may be persisted to disk in any of [several supported formats (MessagePack, JSON, CSV)](../../Runtime/Microsoft.Psi.Interop/Format/Readme.md) for consumption by other platforms and languages. For example:

```bash
> dotnet PsiStoreTool.dll save -p E:\Data -d MyStore -s MyStream -f MyData.json -m json

Saving Stream Messages (stream=MyStream, store=MyStore, path=E:\Data, file=MyData.json, format=json)
........................................
```

## Sending

Streams may be [sent over a message queue (ZeroMQ/NetMQ)](../../Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages. For example:

```bash
> dotnet PsiStoreTool.dll send -p E:\Data -d MyStore -s MyStream -a tcp://localhost:12345 -t MyTopic -m json

Saving Stream Messages (stream=MyStream, store=MyStore, path=E:\Data, file=MyData.json, format=json)
........................................
```

Where it may then be consumed in Python, for example:

```python
import zmq, json

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://localhost:12345")
socket.setsockopt(zmq.SUBSCRIBE, '') # '' means all topics, otherwise 'sin-topic'/'cos-topic'

while True:
    [topic, message] = socket.recv_multipart()
    j = json.loads(message)
    print "Message: ", repr(j['message'])
    print "Time: ", repr(j['time'])
```

Similarly, `ZmqSocket.js` can be used from JavaScript.

## Concatenating

Stores collected at different times may be concatenated into a single store.
Streams with matching names are combined; requiring matching types and non-overlapping originating lifetimes.
For example:

`dotnet PsiStoreTool.dll concat -p E:\Data -d MyFirstStore;MySecondStore;MyThirdStore -o MyConcatenatedStore`

## Cropping

Stores may be cropped to a specified interval.
For example:

`dotnet PsiStoreTool.dll crop -p E:\Data -d MyStore -o MyCroppedStore -s 00:05:00 -l 00:01:00`

Start is a timespan relative to the start of the store (e.g. 00:05:00 starts five minutes in). If not specified then the beginning of the store is assumed.

Length is also a timespan, but relative to the start (e.g. 00:01:00 continues one minute from the starting point [five minutes in]). If not specified then to the end of the store is assumed.

## Tasks

Tasks may be defined in external assemblies to be applied to stores or streams within.
Such tasks may be listed with:

`dotnet PsiStoreTool.dll tasks`

Discovery of the task assumes that `PsiStoreTool.exe.config` has the containing assemblies listed:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="taskAssemblies" value="E:\MyTasks\SomeTasks.dll;E:\MyTasks\SomeOtherTasks.dll" />
  </appSettings>
</configuration>
```

Otherwise (or in addition to), assemblies may be provided on the command-line:

`dotnet PsiStoreTool.dll tasks -m E:\MyTasks\SomeTasks.dll`

### Example Store-Level Task

An example task operating on a store:

```c#
[Task("DoSomethingWithStore", "This demonstrates store-level task.")]
public static void DoIt(Importer store, Pipeline pipeline, int foo, string bar)
{
    // do something with the `store` and `pipeline`
    ...
}
```

Marked with the `TaskAttribute` giving it a name and description, this method represents a "task". There isn't much special about the signature other than being `static`.
It may be invoked using the tool:

`dotnet PsiStoreTool.dll exec -t DoSomethingWithStore -p E:\Data -d MyStore`

The tool will run and report progress on a `Pipeline` and will open `MyStore`. These will automatically be passed to the task as the `pipeline` and `store` parameters.
The user will be prompted for the remaining `camera` and `outputPath` parameters:

```bash
foo (int)? 42
bar (string)? testing
```

Again, containing assemblies may be listed in `PsiStoreTool.exe.config` and/or provided on the command-line:

`dotnet PsiStoreTool.dll exec -t DoSomethingWithStore -p E:\Data -d MyStore -m E:\MyTasks\SomeTasks.dll`

The `camera` and `outputPath` parameters may also be provided:

`dotnet PsiStoreTool.dll exec -t DoSomethingWithStore -p E:\Data -d MyStore -m E:\MyTasks\FaceTasks.dll -a Kinect.ColorCamera;E:\Data\Faces`

### Example Message-Level Task

Another type of task is one called with individual messages from a stream:

```c#
[Task("DoSomethingWithMessage", "This demonstrates message-level task.")]
public static void DoIt(double message, Envelope envelope, int foo)
{
    // do something with `message` and `envelope` of each message
    ...
}
```

Just as with store-level tasks, this is merely a static method marked with the `TaskAttribute`.
The difference is in the way that it's called; including a stream name (`-s`):

`dotnet PsiStoreTool.dll exec -t DoSomethingWithMessage -p E:\Data -d MyStore -s MyStream`

In this case the tool will run a pipeline and pass messages (and `Envelopes`) from `MyStream` to the task.

### Example Bare Task

A task may also merely do some work outside of the context of a pipeline, store or stream:

```c#
[Task("JustDoSomething", "This demonstrates a bare task.")]
public static void JustDoIt(double nike)
{
    // do something having nothing to do with a pipeline, store or stream
    ...
}
```

Just as with other tasks, this is marked with the same `TaskAttribute` and may be executed without path (-t), data store (-d) or stream (-s) arguments:

`dotnet PsiStoreTool.dll exec -t JustDoSomething`

In this case the tool simply calls the method without spinning up a pipeline.

### How the "Magic" Works

Task methods may be of a variety of signatures. Parameters are gathered automatically, from the command-line (`-a`) and/or interactively:

* If a task takes an `Importer` or `Pipeline` then the current instances will be automatically passed.
* If a task is applied to messages from a stream (`-s`):
    * If takes an `Envelope` then the current message envelope is passed.
    * The first non-`Importer`/`Pipeline`/`Envelope` parameter is passed the message.
* All other parameters are populated from:
    * The command-line `-arguments` (`-a`) in order
	* Or interactively prompted for by name/type

Extra parameters may be `double`, `int`, `bool`, `string`, `DateTime` or `TimeSpan`.
