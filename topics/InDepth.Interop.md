---
layout: default
title:  Interop
---

# Interop

Below we describe the infrastructure provided in the `Microsoft.Psi.Interop` namespace to facilitate interoperation of Platform for Situated Intelligence with other languages and platforms. \\psi clearly supports .NET languages (C#/F#/...) on Linux/Mac/Windows. Within .NET we recommend the \\psi store format and using [\\psi remoting](https://microsoft.github.io/psi/topics/InDepth.Remoting) to convey data over process/machine boundaries. Outside of .NET, aside from the specialized [ROS Bridge](https://github.com/Microsoft/psi/tree/master/Sources/Integrations/ROS/Microsoft.Psi.ROS), we provide very general interop facilities. These may be useful to, for example, interop with Python for data science and external ML or with JavaScript for web "dashboards" and Node.js, among others.

Interop is accomplished by translating \\psi data into standard (though less efficient) formats such as [JSON](http://www.json.org/) (text), [MessagePack](https://msgpack.org) (binary) and comma-separated values (flattened). Data may be persisted to flat files or conveyed over standard transports such as [ZeroMQ](http://zeromq.org). Additional custom formats and transports may be dovetailed into the system by way of the provided [serialization](https://github.com/Microsoft/psi/tree/master/Sources/Runtime/Microsoft.Psi.Interop/Serialization/Readme.md) and [transport](https://github.com/Microsoft/psi/tree/master/Sources/Runtime/Microsoft.Psi.Interop/Transport/Readme.md) interfaces.

__NOTE__: The documentation below assumes familiarity with basic \\psi concepts, such as streams, operators, pipelines, etc., described in [A Brief Introduction to Platform for Situated Intelligence](/psi/tutorials/index).

# Examples

Suppose we want to consume a \\psi stream in Python. We have several methods by which to gain access to the data. One approach is to convert our stream to a file as JSON, MessagePack or CSV. A stream of messages of any type may be written using the `FileWriter<T>`:

```csharp
using (var p = Pipeline.Create())
{
    var gen = Generators.Range(p, 0, 1000);
    var sin = gen.Select(x => Math.Sin(x / 100.0));
    var writer = new FileWriter<double>(p, "TestFile.json", JsonFormat.Instance);
    sin.PipeTo(writer);
    p.Run();
}
```

This produces a JSON file containing something like:

```json
[
    {
        "originatingTime": "2018-11-12T22:48:58.3770983Z",
        "message": 0.0
    },
    {
        "originatingTime": "2018-11-12T22:48:58.3770984Z",
        "message": 0.0099998333341666645
    },
    {
        "originatingTime": "2018-11-12T22:48:58.3770985Z",
        "message": 0.01999866669333308
    },
    ...
]
```

The same could be accomplished by persisting to a regular \\psi store:

```csharp
var store = Store.Create(p, "TestStore", @"D:\Data");
store.Write<double>(sin.Out, "Sin");
```

Then using the [PsiStoreTool](https://github.com/Microsoft/psi/tree/master/Sources/Tools/PsiStoreTool) to do the conversion:

```bash
> dotnet PsiStoreTool.dll save -p D:\Data -d TestStore -s Sin -f TestFile.json -m json
```

Either way, the `TestFile.json` may then be read back as a proper \\psi stream using a `FileSource<T>`:

```csharp
using (var p = Pipeline.Create())
{
    var reader = new FileSource<double>(p, "TestFile.json", JsonFormat.Instance);
    reader.Do(Console.WriteLine);
    p.Run();
}
```

We may instead choose to shuttle "live" data over a standard transport such as ZeroMQ. This may be accomplished with the `NetMQWriter<T>`:

```csharp
using (var p = Pipeline.Create())
{
    var gen = Generators.Range(p, 0, 1000);
    var sin = gen.Select(x => Math.Sin(x / 100.0));
    var mq = new NetMQWriter<double>(p, "sin-topic", "tcp://localhost:12345", JsonFormat.Instance);
    sin.PipeTo(mq);
    p.Run();
}
```

Here we've conveyed the `sin` stream over ZeroMQ using `JsonFormat`. Alternatively, this could done with the [PsiStoreTool](https://github.com/Microsoft/psi/tree/master/Sources/Tools/PsiStoreTool):

```bash
> dotnet PsiStoreTool.dll send -p E:\Data -d TestStore -s Sin -a tcp://localhost:12345 -t sin-topic -m json
```

One thing not supported by the tool is sending multiple topics over the same channel. Additional topics may be added programmatically:

```csharp
    var cos = gen.Select(x => Math.Cos(x / 100.0));
    var topic = mq.AddTopic("cos-topic");
    cos.PipeTo(topic);
```

It may be more natural to construct the `NetMQWriter` without specifying a topic and instead add each topic manually:

```csharp
using (var p = Pipeline.Create())
{
    var mq = new NetMQWriter<double>(p, "tcp://localhost:12345", JsonFormat.Instance);
    var gen = Generators.Range(p, 0, 1000);
    var sin = gen.Select(x => Math.Sin(x / 100.0));
    var cos = gen.Select(x => Math.Cos(x / 100.0));
    sin.PipeTo(mq.AddTopic("sin-topic"));
    cos.PipeTo(mq.AddTopic("cos-topic"));
    p.Run();
```

Whichever method is use to create the ZeroMQ channel, whether programmatically or with the tool, we can then consume the messages in Python using `pyzmq` with something like:

```python
import zmq, json

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://localhost:12345")
socket.setsockopt(zmq.SUBSCRIBE, '') # '' means all topics

while True:
    [topic, message] = socket.recv_multipart()
    j = json.loads(message)
    print "Message: ", repr(j['message'])
    print "Originating Time: ", repr(j['originatingTime'])
```

\\psi may also consume message that have been produced from "outside." For example, the below Python code produces an infinite stream of random doubles:

```python
import zmq, random, datetime, json

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind('tcp://127.0.0.1:12345')

while True:
    payload = {}
    payload['message'] = random.uniform(0, 1)
    payload['originatingTime'] = datetime.datetime.now().isoformat()
    socket.send_multipart(['test-topic'.encode(), json.dumps(payload).encode('utf-8')])
```

The schema of the `message` itself is arbitrary, but the fact that there is a root-level `"message"` and `"originatingTime"` is required and the time is expected to be ISO 8601 formated. Also, remember that the payload is not a string, but UTF-8 encoded bytes.

The stream of random doubles can then be easily consumed in \\psi:

```csharp
using (var p = Pipeline.Create())
{
    var mq = new NetMQSource<double>(p, "test-topic", "tcp://localhost:12345", JsonFormat.Instance);
    mq.Do(x => Console.WriteLine($"Message: {x}"));
    p.Run();
}
```

Notice that in the above example, the Python code generates timestamps with `datetime.now().isoformat()`. This is fine when messages _originate_ in Python. The timestamp represents the _originating_ time of the message. In the case where Python code is consuming a \\psi stream, doing some work with it and producing a resulting output stream it is more appropriate to use the original time of the message from \\psi as the originating time for the output. This allows for joins and other time algebra to work correctly back in \\psi. For example:

```python
while True:
    [topic, message] = socket.recv_multipart()
    j = json.loads(message)
    result = DoSomeWork(message)
    payload = {}
    payload['message'] = result
    payload['originatingTime'] = j['originatingTime'] # use original time
    socket.send_multipart(['test-topic'.encode(), json.dumps(payload).encode('utf-8')])
```

# Alternatives to \\psi Store Format

As mentioned in the examples above, alternative data formats to the \\psi store include the [JSON](http://www.json.org/) (text), [MessagePack](https://msgpack.org) (binary) and comma-separated values (flattened). Potential future formats include Python Pickle, Protobuf, and others. These, and other custom formats, may be added by way of the provided [serialization interfaces](https://github.com/Microsoft/psi/tree/master/Sources/Runtime/Microsoft.Psi.Interop/Serialization/Readme.md).

## JsonFormat

A single `JsonFormat` class provides an implementation of all of the interfaces for JSON. Message records are of the following form where `<message>` is a JSON-serialized message and `<originatingTime>` is an ISO 8601 string representing the originating time:

```json
{ "originatingTime": <originatingTime>, "message": <message> }
```

For example, messages of the form:

```csharp
var message = new
{
    ID = 123,
    Confidence = 0.92,
    Face = new
    {
        X = 213,
        Y = 107,
        Width = 42,
        Height = 61
    }
};
```

Would serialize as the following:

```json
{
    "originatingTime": "1971-11-03T00:00:00Z",
    "message": {
        "ID": 123,
        "Confidence": 0.92,
        "Face": {
            "X": 213,
            "Y": 107,
            "Width": 42,
            "Height": 61
        }
    }
}
```

The persisted format is an array of such records:

```json
[
  <record>,
  <record>,
  ...
]
```

## MessagePack Format

A single `MessagePackFormat` class provides implementations for [MessagePack](https://msgpack.org) binary records. The persisted form contains an `int32` length prefix (little endian) to each record and is terminated by a zero-length record. That is:

```text
<length><record bytes><length><record bytes>...0
```

The format is very compact and is recommended when data transfer bandwidth or persisted file size is a concern. The expressiveness of the format is very similar to JSON. It's difficult to give examples of the serialized form, given that it is a byte-level encoding, roughly type-tagged fields with various encoding strategies. The whole payload is then LZ4 compressed, making it quite opaque to humans.

However, there are serialization libraries for MessagePack in 50+ languages, making it *very* portable; a recommended format for streams of structured data.

## CsvFormat

A single `CsvFormat` class provides implementations for comma-separated-values encoded according to [RFC 4180](https://tools.ietf.org/html/rfc4180). Records are of the form:

```csv
<_OriginatingTime_>,<header>,<header>,<header>, ...
<originatingTime>,<field>,<field>,<field>, ...
...
```

The first column is the originating time of the messages and is named `_OriginatingTime_`.

There are several limitations to the CSV format. Much of the type information is lost and hierarchical values and collection properties are not (currently) allowed - such properties are merely skipped. For example a face tracker message type containing an `ID`, a `Confidence` and a `Face` property, each with a `Rect` having an `X` and `Y` location and `Width`/`Height`, would only serialize the root primitive properties; in this case only the `ID` and `Confidence` (along with originating time):

```csv
_OriginatingTime_,ID,Confidence
2018-09-06T00:39:19.0883965Z,123,0.92
2018-09-06T00:39:19.0983965Z,123,0.89
...
```

Given a stream of face tracker results, it is easy enough to select out a "flattened" view:

```csharp
var flattened = faces.Select(f => new { ID = f.ID,
                                        Confidence = f.Confidence,
                                        FaceX=f.Rect.X,
                                        FaceY=f.Rect.Y,
                                        FaceWidth=f.Rect.Width,
                                        FaceHeight=f.Rect.Height });
```

Serializing this would then produce a stream of messages in the form:

```csv
_OriginatingTime_,ID,Confidence,FaceX,FaceY,FaceWidth,FaceHeight
2018-09-06T00:39:19.0883965Z,123,0.92,213,107,42,61
```

```csv
_OriginatingTime_,ID,Confidence,FaceX,FaceY,FaceWidth,FaceHeight
2018-09-06T00:39:19.0983965Z,123,0.89,215,101,44,63
```

Notice that each message includes the header row. Deserializing these will give messages represented as an `ExpandoObject` with named `dynamic` properties for each column (except `_OriginatingTime_`).

### Simple Primitives

In the case of a simple stream of primitive types (e.g. `double` with `faces.Select(f => f.Confidence)`), the persisted form looks like:

```csv
_OriginatingTime_,_Value_
2018-09-06T00:39:19.0883965Z,0.92
```

Notice the special field name `_Value_` used to distinguish this case. This deserializes back to a simple primitive; `double` in this case.

### Numeric Collections

A very special case, which is commonly used in ML with \\psi, is a single collection of numeric types such as `IEnumerable<double>`. For example, `new double[] { 1, 2, 3, 4, 5 }` serializes to:

```csv
_OriginatingTime_,_Column0_,_Column1_,_Column2_,_Column3_,_Column4_
2018-09-06T12:12:25.7463172-07:00,1,2,3,4,5
```

Notice the `_Column0_`, `_Column1_`, ... headers. This deserializes back into a simple `double[]`.

### Persistent Form

CSV is generally used as a persisted format, but may also be used for transport (over a message queue, etc.). 

```csv
_OriginatingTime_,ID,Confidence,FaceX,FaceY,FaceWidth,FaceHeight
2018-09-06T00:39:19.0883965Z,123,0.92,213,107,42,61
2018-09-06T00:39:19.0983761Z,123,0.89,215,101,44,63
2018-09-06T00:39:19.1183762Z,123,0.90,212,104,43,62
...
```

Records in the persisted form are delimited by `\r\n` (or appropriate `Environment.NewLine` for the executing platform).

# Transports

We have several ways of getting data in and out of \\psi in order to interop with other languages and platforms. Streams in any of these formats may be persisted to or read from disk, or may be communicated over a message queue.

## Generic File Source/Writer Component

The simplest transport is via the file system. This is most appropriate for offline/batch processing.

A generic `FileWriter<T>` component is provided that, when given an `IPersistentFormatSerializer`, will persist a message stream to disk.

Similarly, a generic `FileSource<T>` component is provided, taking an `IPersistentFormatDeserializer`, to reconstitute such a persisted file as a \\psi stream.

## Message Queue Components

Message queues are most appropriate for live interop. Currently, only ZeroMQ is supported. In the future, Azure Storage Queue and/or Service Bus as well as Amazon SMQ may be supported.

While the \\psi `RemoteExporter`/`Importer` is an excellent, high performance means of remoting, it assumes .NET on both ends. To facilitate remoting to Python and others, we provide message queuing components.

These components are `IConsumer<T>` and simply push to a message queue or are `IProducer<T>` and take messages from a queue. Given an `IFormatSerializer`/`Deserializer`, they handle packing/unpacking messages.

### ZeroMQ/NetMQ

An implementation for ZeroMQ (calleg NetMQ for .NET) is provided. These components are configured with a URI, a topic name and an `IFormatSerializer`.

As in the above examples, a stream of messages of any type may be piped to a `NetMQWriter` and data from "outside" may be consumed by `NetMQReader`. These will be serialized and sent to the queue for consumption outside of \\psi.

These components use the [NetMQ Pub/Sub](https://netmq.readthedocs.io/en/latest/pub-sub/) pattern, in which messages convey _topic_ information (much like ROS). Subscribers may then subscribe by topic name.

By default the writer component has a single `In` receiver which sends to the topic specified at construction. This is the most common case. To facilitate multiple topics over one channel, the `AddTopic(...)` method may be called; returning an additional `IReceiver<T>` to which to pipe.

# CLI Tool

The [PsiStoreTool](https://github.com/Microsoft/psi/tree/master/Sources/Tools/PsiStoreTool) exposes the above facilities as a command-line tool. It may be used to explore available streams in a store, convert [to other formats (MessagePack, JSON, CSV)](https://github.com/Microsoft/psi/tree/master/Sources/Runtime/Microsoft.Psi.Interop/Format/Readme.md)
and [persist to disk or send over a message queue](https://github.com/Microsoft/psi/tree/master/Sources/Runtime/Microsoft.Psi.Interop/Transport/Readme.md) for consumption by other platforms and languages.