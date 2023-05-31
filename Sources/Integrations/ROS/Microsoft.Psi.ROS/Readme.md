# ROS Bridge

ROS is the defacto standard for robotics work and has a *huge* community.
A bridge is provided between the ROS and .NET worlds to leverage that work.
Here we briefly explain the ROS architecture and the bridge layer.

The following tutorials may help to get started:

* [PsiRosTurtleSample](https://github.com/Microsoft/psi-samples/tree/main/Samples/RosTurtleSample/) - simple tutorial to control `turtlesim` (no hardware required)
* [ArmControlROSSample](https://github.com/Microsoft/psi-samples/tree/main/Samples/RosArmControlSample/) - tutorial to control the [uArm Metal](http://ufactory.cc/#/en/uarm1) (hardware required)

## ROS

Please familiarize yourself with these excellent [conceptual](http://wiki.ros.org/ROS/Concepts) and [technical](http://wiki.ros.org/ROS/Technical%20Overview) overviews here.

**Nodes**: ROS is an actor framework allowing loosely coupled modules to cooperate through message passing.
ROS Nodes are generally separate, and possibly distributed, processes (except less common "Nodelets") and communication is over TCP sockets.
There is a ROS Master (`roscore`) that coordinates Nodes.
This allows Nodes to discover each other, inspect the system as a whole, etc.
However, actual message passing is peer-to-peer.

**Topics**: In a real system there may be many publishers and subscribers to a single *Topic*.
You can _conceptually_ think of the Topic as a "channel" through with all communication happens - publishers push to Topics and subscribers pull from them. In reality, communication is peer-to-peer.

**Message Passing**: For the most part, ROS message passing is unidirectional and async.
ROS does have the concept of Services which are bidirectional, synchronous request/response messages.
The same mechanics are used as for regular message, but each published message is followed by a response of a success/failure byte and return message/error over the same TCP/UDP socket.

**Parameter Server**: maintains a set of key/value pairs accessible to any Node.
It is physically hosted by the ROS Master at the same host/port as the ROS Master's endpoint.

## Bridge

At its core, the library includes an `XmlRpc` reader/writer, ROS TCP header and body serializers, and classes to manage communication with the ROS Master, the Parameter Server, and ROS Node client.
These are the fundamental pieces from which everything else is built.

Moving up a layer, there are `Publisher` and `Subscriber` classes to manage many-to-many connections through topics.
These handle negotiation for ports, maintaining bookkeeping as publishers and subscribers come and go, etc.
Finally, a `RosNode` class manages multiple publications and subscriptions and all of the interaction with the ROS Master to maintain a proper node in the system.

## Usage

The general usage pattern is to create an instance of `RosNode.Node` to join the ROS world.
Using this, you may create a `RosPublisher.IPublisher` using `CreatePublisher(messageDef, topic, latching)`, to which you can then `Publish(...)` messages.
You may also create a `RosSubscriber.ISubscriber` using `Subscribe(messageDef, topic, callback)` or similarly a `RosServiceClient.IServiceClient` with `CreateServiceClient(serviceDef, topic, latching)`.

Note that when running a distributed ROS system in which nodes are running on machines other than the ROS master, then hostname/IP mappings for these must be added (e.g. `RosDns.add("my-hostname", "10.1.2.3")`).
Without such a mapping, the IP address of the master is assumed.

A parameter server client (`ParamClient`) is available on the node as well (via `Node.Param`).
This allows `Set(key, value)`, `Get(key)`, `Delete(key)`, etc. as well as `Search(key)`, enumerating with `GetNames()` and `Subscribe(key)/Unsubscribe(key)` for notifications.

Under the covers, the topics being published will be registered with the ROS Master and topics to which you subscribe will be discovered and peer-to-peer connections negotiated.
You may later `UnregisterPublisher(topic)`/`UnregisterSubscriber(topic)` upon disconnection.
Many-to-many connections are maintained and updated as the topology changes.
As a user you merely call `Publish(...)` and messages are fanned out and process subscription callbacks without worrying about the underlying protocol and connection bookkeeping.

The `RosMessageTypes` class contains many standard message schemas and is used to create your own as needed.
For example, many message types are available with something like `RosMessageTypes.Geometry.Twist.Def`, while custom schemas must be defined, e.g.:

```C#
var poseMessageDef = RosMessage.CreateMessageDef(
  "turtlesim/Pose",
  "863b248d5016ca62ea2e895ae5265cf9",
  new[] {
    Tuple.Create("x", RosMessage.RosFieldDef.Float32Def),
    Tuple.Create("y", RosMessage.RosFieldDef.Float32Def),
    Tuple.Create("theta", RosMessage.RosFieldDef.Float32Def),
    Tuple.Create("linear_velocity", RosMessage.RosFieldDef.Float32Def),
    Tuple.Create("angular_velocity", RosMessage.RosFieldDef.Float32Def)
  })
```

### Protocol

It is not necessary fully to understand the protocol to use the system.
However, below are the details:

There are two flavors of protocol used by ROS.
All of the APIs exposed by the ROS Master, the Parameter Server and by Nodes are [XmlRpc](https://en.wikipedia.org/wiki/XML-RPC).
This is a very simple protocol over HTTP.
For actual message passing a binary protocol is used over plain TCP/UDP sockets.

Here is an example of the exchange between entities to exchange messages between Nodes in the system.
It sounds complicated, but it is made from simple pieces.
Let's say Node `Foo` wants to publish messages to which Node `Bar` wants to subscribe.

* *ROS Master:* The ROS Master is started and listens, by default, for XmlRpc requests on port 11311
* *Publisher:* `Foo` is started and tells the ROS Master that it wants to publish a Topic (`registerPublisher`)
  * `Foo` only knows about the ROS Master and nothing about `Bar`
  * The Topic name and the host/port on which `Foo` will be listening for XmlRpc requests is given
* *Subscriber:* `Bar` is started and tells the ROS Master it wants to subscribe to a Topic (`registerSubscriber`)
  * `Bar` only knows about the ROS Master and nothing about `Foo`
  * The Topic name, message type and host/port on which `Bar` will be listening is given
  * The Master shares `Foo`'s endpoint with `Bar`. Now it's up to them to negotiate a connection
* *Peer-to-peer:* `Bar` asks `Foo` for the Topic (`requestTopic`)
  * The Topic name and `Bar`'s supported protocols (e.g. TCP, UDP, ...) are given
  * `Foo` replies with host/port information for the connection (different host/port from the XmlRpc endpoint, and possibly different per subscriber)
* *Negotiation:* `Bar` now negotiates the message format by sending an `md5sum`, `type` string, and full `message_definition` text
  * For backward compatibility, `Foo` should support older `md5sum`'s
  * `Foo` replies with the `md5sum` and `type` it intends to send and then begins pumping binary-packed messages

#### XmlRpc

[XmlRpc](https://en.wikipedia.org/wiki/XML-RPC) is a very simple protocol consisting of a `methodCall` POSTed over HTTP to a given host/port, followed by a `methodResponse`.
For example, to get the URI of the ROS Master:

```xml
<?xml version="1.0"?>
<methodCall>
  <methodName>getUri</methodName>
  <params>
    <param>
      <value>username</value>
    </param>
  </params>
</methodCall>

<?xml version='1.0'?>
<methodResponse>
  <params>
    <param>
      <value>
        <array>
          <data>
            <value>
              <int>1</int>
            </value>
            <value>
              <string></string>
            </value>
            <value>
              <string>http://my-dev-box-ubuntu:11311/</string>
            </value>
          </data>
        </array>
      </value>
    </param>
  </params>
</methodResponse>
```

There are encodings for the following types: `array`, `base64`, `boolean`, `dateTime.iso8601`, `double`, `integer`, `string`, `struct`, `nil`.
A `<value>` element without a type is assumed to be a `string`.
Structs have name/value pairs (`<member><name>...</name><value>...</value></member>`).
Arrays contain `<data>...</data>` with a list of anonymous values.

Atop this simple encoding, [ROS has a request/response format](http://wiki.ros.org/ROS/Master_Slave_APIs).
Requests contain a `caller_id` as the first parameter, followed by zero-or more arguments.
Responses containing an array of three values: an `int` (status code), `string` (status message - may be null), and a body value of any type.
Below, we'll refer only to the parameters other than `caller_id` and to just the response body portion.

#### TCPROS

The [TCPROS](http://wiki.ros.org/ROS/TCPROS) protocol is a binary-packed encoding for sending [headers](http://wiki.ros.org/ROS/Connection%20Header) and message body.
Each header is encoded as a UTF8, length-prefixed string in the form `foo=bar`.
The length prefix is a little-endian Int32.
The collection of headers is also length prefixed.

Once a topic endpoint has been found (`requestTopic`) then the protocol over the given host/port must be negotiated.
Subscribers send a set of headers containing their `callerid` and the `topic`/`type` they to which they subscribe, as well as an `md5sum` and complete `message_definition` to set expectations.
If there is confusion over the Topic name/type or if the MD5 hash or definition don't match something the publisher can provide then an error is sent back (`error` field and connection closed).
Subscribers may send `tcp_nodelay=1` to control the socket setting.

If the publisher agrees then it replies with a set of headers echoing back the `type` and `md5sum` it intends to send.

Service clients send similar header (though `service` rather than `topic`, and no `message_definition`) and receive similar responses (though only `callerid` is required) but prefixed by a success/fail (0/1) byte.
Normally, service connections are closed after a single request/response.
The client may send a `persist=1` header to keep it open.

#### UDPROS

The [UDPROS](http://wiki.ros.org/ROS/UDPROS) protocol is rarely used in practice and isn't currently supported.
The header and message formats are similar, but there is extra mechanics to piece together datagrams (message ID and block number headers), as an indication of whether a packet is the first or a subsequent datagram and explicit support for `PING` and `ERR` packets.

### ROS Master

[Communication with ROS master via XmlRpc methods](http://wiki.ros.org/ROS/Master_API) includes the following APIs.
Though not mentioned below, all requests contain the `caller_id` and a response body bundled with states code/message (see XmlRpc section above).

#### Register/Unregister

* `registerPublisher topic typ callerApi -> (subscriberApis: string list)`
* `unregisterPublisher topic callerApi -> (numUnregistered: int)`
* `registerService service serviceApi callerApi -> unit`
* `unregisterService service serviceApi -> (numUnregistrations: int)`
* `registerSubscriber topic typ callerApi -> (publishers: string list)`
* `unregisterSubscriber topic callerApi -> (numUnsubscribed: int)`

#### Name Service and System State

* `lookupNode name -> (uri: string)`
* `getPublishedTopics subgraph -> (topic * type) list`
* `getTopicTypes () -> (topic * type) list`
* `lookupService name -> (serviceUri: string)`
* `getUri () -> (rosMasterUri: string)`
* `getSystemState () -> { Publishers: topic * subscribers list; Subscribers: topic * subscriber list; Services: service: provider * list`

### Parameter Server

[Communication with ROS parameter server via XmlRpc methods](http://wiki.ros.org/ROS/Parameter%20Server%20API) includes the following APIs.
Though not mentioned below, all requests contain the `caller_id` and a response body bundled with states code/message (see XmlRpc section above).

* `setParam key value -> ()`
* `getParam key -> (value: XmlRpcValue)` *
* `deleteParam key -> ()`
* `hasParam key -> bool`
* `searchParam key ->` **
* `getParamNames () -> (names: string list)`
* `subscribeParam callerApi key -> (current: XmlRpcValue)` ***
* `unsubscribeParam callerApi key -> (numUnsubscribed: int)`

*If the `key` given to `getParam` is a namespace then the return is tree constructed of structs.

**Search is for first partial match, starting in caller's namespace.
Not found returns a -1 status code.
This is mapped an `Option` type in F#

***Subscription causes calls to `paramUpdate` in the ROS APIs (see below).

### ROS Nodes

[Communication with ROS nodes via XmlRpc methods](http://wiki.ros.org/ROS/Slave_API) includes the following APIs.
Though not mentioned below, all requests contain the `caller_id` and a response body bundled with states code/message (see XmlRpc section above).

* `getBusStats () -> bus_stats` (see docs)
* `getBusInfo () -> bus_info` (see docs)
* `getMasterUri () -> (masterUri: string)`
* `shutdown message -> ()`
* `getPid () -> int`
* `getPublications () -> (topic * type) list`
* `getSubscriptions () -> (topic * type) list`
* `requestTopic topic protocols -> (protocol: string * param list) list`

#### Updates (only called by ROS Master)

Note: `paramUpdate` and `publisherUpdate` are unsupported (should only come from ROS master)

* `paramUpdate key value -> unit`
* `publisherUpdate topic publishers -> unit`

## Notes

The reality is that all communication is Node-to-Node directly after having negotiated a connection via the Topic.
A Topic is nothing more than a name.
The ROS Master will give the subscribers a *list* of publishers.
Connections are negotiated with *each* of them separately.
This makes for a very distributed and scalable system, but means that N subscribers to M publishers create a N*M pipes between Nodes.
For this reason, sometimes there are aggregator noted (e.g. `rosout_agg`) that relay messages, making for N+M connections.

There are a lot of other APIs and communication that happen mainly between the ROS Master and the ROS Nodes.
The Master may be queried to `lookupNode` or `lookupService`, `getPublishedTopics`, `getTopicTypes`, `getSystemState`, etc.
Nodes may be queried to `getBusStats`, `getBusInfo`, `getMasterUri`, `getPid`, `getPublications`, `getSubscriptions`, etc.
These queries are used by various tools and by Nodes to inspect a running system and to discover and dynamically construct the graph.
Nodes may be notified when things change in the system via `paramUpdate` and `publisherUpdate`.
Nodes may also be told by the Master to `shutdown` (in case of conflicts, for example).
However, there is no mechanism to start Nodes dynamically and, in fact, the Master doesn't maintain any information about Nodes that are not running (locations on disk, etc.).
This seems like an area of automation that could be useful.
