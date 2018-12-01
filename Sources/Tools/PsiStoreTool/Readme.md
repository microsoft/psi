# Platform for Situated Intelligence Store Tool

This command-line tool allows exploration of available streams in a store, conversion [to other formats (MessagePack, JSON, CSV)](../../Runtime/Microsoft.Psi.Interop/Format/Readme.md)
and [persisting to disk or sending over a message queue](../../Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages.
It works with *any* types found within; converting to `dynamic`/`ExpandoObject` streams.

## Verbs and Options

The following verbs are supported:

| Verb | Description |
| - | - |
| `list` | List streams within a Psi data store. |
| `info` | Display stream information (metadata). |
| `messages` | Display messages in stream. |
| `save` | Save messages to file system. |
| `send` | Send messages to message queue (ZeroMQ/NetMQ). |
| `help` | Display more information on a specific command. |
| `version` | Display version information. |

The following options are available:

| Option | Abbr | Description |
| - | - | - |
| `p` | `path` | File path to Psi data store (default=working directory). |
| `s` | `stream` | Name Psi stream within data store. |
| `m` | `format` | Format specifier (msg, json, csv). |
| `n` | `number` | Include first n messages (optional). |
| `f` | `file` | File to which to persist data. |
| `t` | `topic` | Topic name to which to send messages (default=''). |
| `a` | `address` | Connection address to which to send messages (e.g. 'tcp://localhost:12345'). |

## Exploring

To list the streams within a store:

```bash
> dotnet PsiStoreTool.dll list -p "E:\Data" -d "MyStore"

Available Streams (store=MyStore, path=E:\Data)
MyStream (System.Double)
AnotherStream (MyNamespace.MyType)
...
Count: 3394
```

This displays the name and .NET type of each stream.

To get info about a particular stream:

```bash
> dotnet PsiStoreTool.dll info -p "E:\Data" -d "MyStore" -s "MyStream"

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
> dotnet PsiStoreTool.dll messages -p "E:\Data" -d "MyStore" -s "MyStream" -n 10

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
> dotnet PsiStoreTool.dll save -p "E:\Data" -d "MyStore" -s "MyStream" -f "MyData.json" -m json

Saving Stream Messages (stream=MyStream, store=MyStore, path=E:\Data, file=MyData.json, format=json)
........................................
```

## Sending

Streams may be [sent over a message queue (ZeroMQ/NetMQ)](../../Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages. For example:

```bash
> dotnet PsiStoreTool.dll send -p "E:\Data" -d "MyStore" -s "MyStream" -a "tcp://localhost:12345" -t "MyTopic" -m json

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

Similarly, `ZmqSocket.js` can be used from JavaScript:

* TODO: JS Example
