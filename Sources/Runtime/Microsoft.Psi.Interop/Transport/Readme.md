# Interop Transports

Here we describe several ways of getting data in and out of Psi in order to interop with other languages and platforms.

## Generic File Source/Writer Component

The simplest transport is via the file system. This is most appropriate for offline/batch processing. A generic `FileWriter<T>` component is provided that, when given an `IPersistentFormatSerializer`, will persist a message stream to disk. Similarly, a generic `FileSource<T>` component is provided, taking an `IPersistentFormatDeserializer`, to reconstitute such a persisted file as a Psi stream.

For example, a stream of messages of any type may be written using the `FileWriter<T>`:

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

This may then be read back into a proper \\psi stream using a `FileSource<T>`:

```csharp
using (var p = Pipeline.Create())
{
    var reader = new FileSource<double>(p, "TestFile.json", JsonFormat.Instance);
    reader.Do(Console.WriteLine);
    p.Run();
}
```

## Message Queue Components

Message queues are most appropriate for live interop. Currently, only ZeroMQ is supported. In the future, Azure Storage Queue and/or Service Bus as well as Amazon SMQ may be supported. While the Psi `RemoteExporter`/`Importer` is an excellent, high performance means of remoting, it assumes .NET on both ends. To facilitate remoting to Python and others, we provide message queuing components. These components are `IConsumer<T>` and simply push to a message queue or are `IProducer<T>` and take messages from a queue. Given an `IFormatSerializer`/`Deserializer`, they handle packing/unpacking messages.

### ZeroMQ/NetMQ

An implementation for ZeroMQ (calleg NetMQ for .NET) is provided. These components are configured with a URI, a topic name and an `IFormatSerializer`.

#### `NetMQWriter`

A stream of messages of any type may be piped to a `NetMQWriter`. These will be serialized and sent to the queue for consumption outside of Psi.

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

This component uses the [NetMQ Pub/Sub](https://netmq.readthedocs.io/en/latest/pub-sub/) pattern, in which messages convey _topic_ information (much like ROS). Subscribers may then subscribe by topic name.

The generic `NetMQWriter<T>` component has a single `In` receiver which sends to the topic specified at construction. This is the most common case. To facilitate multiple topics over one channel, the `AddTopic<T>(...)` method may be called; returning an additional `IReceiver<T>` to which to pipe.

```csharp
    var cos = gen.Select(x => Math.Cos(x / 100.0));
    var topic = mq.AddTopic<double>("cos-topic");
    cos.PipeTo(topic);
```

Alternatively, the non-generic `NetMQWriter` may be used, which has _no_ receivers. Each must be created with `AddTopic<T>(...)`.

```csharp
using (var p = Pipeline.Create())
{
    var gen = Generators.Range(p, 0, 1000);
    var sin = gen.Select(x => Math.Sin(x / 100.0));
    var cos = gen.Select(x => Math.Cos(x / 100.0));

    var mq = new NetMQWriter(p, "sin-topic", "tcp://localhost:12345", JsonFormat.Instance);
    sin.PipeTo(mq.AddTopic<double>("sin-topic");
    cos.PipeTo(mq.AddTopic<double>("cos-topic");

    p.Run();
}
```

Then from Python, for example, messages may be consumed using `pyzmq` with something like:

```python
import zmq, json

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://localhost:12345")
socket.setsockopt(zmq.SUBSCRIBE, '') # '' means all topics, otherwise 'sin-topic'/'cos-topic'

while True:
    [topic, message] = socket.recv_multipart()
    j = json.loads(message)
    print "Message: ", repr(j['message'])
    print "Originating Time: ", repr(j['originatingTime'])
```

#### `NetMQSource`

Psi may also consume message that have been produced from "outside." For example, the below Python code produces an infinite stream of random doubles:

```python
import zmq, random, datetime, json

context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.bind('tcp://127.0.0.1:45678')

while True:
    payload = {}
    payload['message'] = random.uniform(0, 1)
    payload['originatingTime'] = datetime.datetime.utcnow().isoformat()
    socket.send_multipart(['test-topic'.encode(), json.dumps(payload).encode('utf-8')])
```

Notice that the [Pub/Sub](https://netmq.readthedocs.io/en/latest/pub-sub/) pattern is expected here as well; using `socket.send_multipart` with a topic name and the JSON-encoded data. Also notice that the schema must match that expected by the `JsonFormat` `IFormatDeserializer` described above:

```json
{ "message": <message>, "originatingTime": <time> }
```

The schema of the `message` itself is arbitrary, but the fact that there is a root-level `"message"` and `"originatingTime"` is required and the time is expected to be ISO 8601 formated. Also, remember that the payload is not a string, but UTF-8 encoded bytes.

The stream of random doubles can then be easily consumed in Psi:

```csharp
using (var p = Pipeline.Create())
{
    var mq = new NetMQSource<double>(p, "test-topic", "tcp://localhost:45678", JsonFormat.Instance);
    mq.Do(x => Console.WriteLine($"Message: {x}"));
    p.Run();
}
```

Notice that in the above example, the Python code generates timestamps with `datetime.utcnow().isoformat()`. This is fine when messages _originate_ in Python. This timestamp represents the _originating_ time of the message in UTC time. In the case where Python code is consuming a \\psi stream, doing some work with it and producing a resulting output stream it is more appropriate to use the original time of the message from \\psi as the originating time for the output. This allows for joins and other time algebra to work correctly back in \\psi. For example:

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
