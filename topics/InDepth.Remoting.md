---
layout: default
title:  Remoting
---

# Remoting

Remoting allows \\psi streams to be serialized and transmitted between processes and/or machines.
The transport may be TCP, UDP or Named Pipes.

## Data Store Basis

The remoting system is based on local stores on each end.
Metadata and messages are relayed between endpoints.
The result is that a store on the sending side is written to, relayed and appears _magically_ in a store on the receiving side where the streams may then be opened.
The default is to use volatile stores on both ends.
This allows for long-running processes remoting sensor data for eternity without depleting storage space.

One advantage of backing with data stores is fault tolerance.
If the connection is broken then messages continue to be written locally and will be relayed upon reconnection.
The client reconnects under the covers while continuing to write to the same store.
The client tracks the latest timestamp and restarts the connection with a replay interval covering only what may have been missed.
Intermittent network connections appear as delayed messages to the app.

A similar benefit is that the timing of process startup between multiple communicating apps is coordinated by replay intervals.
A late-connecting app will be replayed anything missed.

## Usage

On the sending side, create a `RemoteExporter` and write to it's `Exporter` as you would any store:

```csharp
    var sender = new RemoteExporter(pipeline);
    Store.Write(myStream, "SomeName", sender.Exporter);
```

Then on the receiving side, create a `RemoteImporter`, wait for it to connect and `OpenStream` from it's `Importer`:

```csharp
    var receiver = new RemoteImporter(pipeline, ReplayDescriptor.ReplayAll.Interval, "my-dev-box");
    receiver.Connected.WaitOne();
    var myRemoteStream = receiver.Importer.OpenStream<float>("SomeName");
```

`RemoteImporter` may be given a complete `TimeInterval` (as above) or just the `replayEnd` (`DateTime`) or nothing (defaulting to `DateTime.MaxValue`). In this case the start is a special behavior that is `DateTime.UtcNow` _at the sending `RemoteExporter`_. This will ensure that regardless of startup and connection time, the first message sent will be the most current _at the sender._

`RemoteImporter` may be constructed with a custom TCP port (default is 11411, much like ROS's 11311 - both being palindromic primes, BTW), and `TransportKind` (TCP, UDP, Named Pipes). It may also be given an existing `Exporter`, in which case you may control the name, path and whether a subdirectory is created.

`RemoteImporter` may also be constructed with a different `port`. It also may be given an existing `Importer` in which case you may control the name and path.

# Backpressure

If the network cannot keep up with messages being written to a `RemoteExporter.Exporter` then backpressure will propagate up the pipeline and messages will be handled according to the `DeliveryPolicy` given to `Store.Write(...)`.
You may also pass an arbitrary bytes-per-second (BPS) quota when constructing the `RemoteExporter` (defaults to `long.MaxValue`).
In this case backpressure will propagate when the average BPS exceeds this value.
BPS is computed as an incremental average with a given time window (`bytesPerSecondSmoothingWindowSeconds` - defaults to `5.0`).`

Note: This throttling applies also to an `Exporter` given at construction-time, which means that *all* writers (even those that may be unaware that the store is being remoted) will see backpressure.

## Protocol

It is not necessary to understand the protocol in order to use the system.
However, below are the details:

`RemoteExporter` listens for connections over TCP (default port 11411).
Clients connecting on this port are expected to send a protocol version (`0` for now) and the replay interval as a pair of `long` start/end ticks.

| Version |Start (ticks) | End (ticks) |
|:--|:--|:--|
| `int16` | `int64` | `int64` |

A start tick count of `-1` signifies the special value (mentioned above) of `DateTime.UtcNow` _at the sending `RemoteExporter`_; ensuring that regardless of startup and connection time, the first message sent will be the most current _at the sender._

In reply, `RemoteExporter` sends a length-prefixed packet of information including the client ID (GUID: 16 bytes) and information about the transport over which to get the data stream (name : `string`, followed by transport-specific parameters - e.g. port number).

| Length | ID | Transport | Parameters |
|:--|:--|:--|:--|
| `int32` | `Guid` (16-bytes) | `string` (`int32` + UTF-8 encoded bytes) | transport-specific |

For TCP and UDP, the parameter is just the port number (`int32`). For Named Pipes it is a name (`string`).

### Metadata

Once the handshake is complete over, the TCP channel is used to relay `PsiStreamMetadata`.
Each is a length-prefixed serialized instance.
A zero-length indicates a _pause_ in metadata updates.
For example, after having initially loaded the catalog.
More updates may come as streams are dynamically added at runtime.

| Length | Serialized `PsiStreamMetadata` |
|:--|:--|
| `int32` | Name, ID, TypeName, Version, etc. |

### Data

Upon establishing the data connection (by whatever transport was given in the handshake), the ID (GUID) is sent:

|  ID |
|:--|
| `Guid` (16-bytes) |

This is used by the `RemoteExporter` to correlate meta and data connections over separate transports (that is, meta is _always_ TCP, but data may be UDP, named pipes, etc.).
They share a `StoreReader` under the covers.

A stream of message data then commences: `Envelope` followed by a length-prefixed serialized message.

| Envelope | Length | Message |
|:--|:--|:--|
| Serialized `Envelope` | `int32` | message bytes |

## Transports

The protocol always uses TCP for the meta channel, but may use Named Pipes, UDP or TCP for the data channel.
Each has it's pros and cons.

### TCP

The parameter header contains the port on which to connect.
Message delivery is guaranteed.

### UDP

The parameter header contains the port on which to connect.
Message delivery is _not_ guaranteed.

Messages are broken into ~64KB packets.
Each is given an `id`, `count` and `num` field.
The `id` is for the "batch" of packets representing a single message.
The `count` is the number of packets in the "batch" and the `num` is which packet in the "batch" (they my be delivered out of order).
The `DataChunker` handles splitting messages into "batches" and `DataUnchunker` handles reassembly as they're received.

Individual UDP packets may be dropped, delivered multiple times or delivered out of order.
The `DataUnchunker` will reassemble them, however if a packet is received for an ID not currently being assembled then the whole message is dropped and assembly begins for the new ID.
In practice this is possible to reproduce with a large payload (many packets) and a small time interval between messages.

### Named Pipes

The parameter header contains the pipe name, which is uniquely generated (`Guid.NewGuid().ToString()`).
Message delivery is guaranteed.
Packet size is restricted to 64K, but ordering and reassembly into a `Stream` interface is done by the BCL (no `DataChunker`/`DataUnchuncker` as in UDP).