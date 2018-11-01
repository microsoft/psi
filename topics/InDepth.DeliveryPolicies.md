---
layout: default
title:  Delivery Policies
---

# Delivery Policies

## Introduction

__Delivery policies__ allow the developer to control whether and when the messages flowing over streams in a Platform for Situated Intelligence application get dropped. This allows the developer to configure how an application may keep up with the incoming streams when not enough computational resources are available to process every message.

To introduce this concept, we will start with a simple example. Suppose we are processing a stream of data, but that the computation we are performing on each message is time-consuming, and takes more time than the interval between two consecutive messages on the stream. As a concrete example, here is a class representing a simple consumer-producer component (for more info see [__Writing Components__](/psi/topics/InDepth.WritingComponents) topic). In the receiver we sleep to simulate a time-consuming operation, which takes about 1 second for every message received.

```csharp
using Microsoft.Psi.Components;

public class IntensiveComponent : ConsumerProducer<int, int>
{
    public IntensiveComponent(Pipeline pipeline)
        : base(pipeline)
    {
    }

    protected override void Receive(int message, Envelope envelope)
    {
        // do some expensive computation in here. To simulate, we 
        // simply sleep for one second
        System.Threading.Thread.Sleep(1000);

        // post the same message as result
        this.Out.Post(message, envelope.OriginatingTime);
    }
}
```

Let us now connect this component to a source stream that generates messages with a faster cadence -- in this example every 100 milliseconds:

```csharp
using (var p = Pipeline.Create())
{
    // generate a sequence of 50 integers starting at zero, one every 100 milliseconds
    var source = Generators.Sequence(p, 0, x => x + 1, 50, TimeSpan.FromMilliseconds(100));

    // instantiate the intensive component and connect the source to its input
    var intensiveComponent = new IntensiveComponent(p);
    source.PipeTo(intensiveComponent.In);

    // output the results of the intensive component
    intensiveComponent.Do((m, e) => Console.WriteLine($"{m} @ {(e.Time - e.OriginatingTime).TotalSeconds:0.00} seconds latency"));

    // run the pipeline
    p.Run();
}
```

Because the component is not able to keep up with the source stream, the messages are queued in front of the component's receiver. The component processes one message per second, but during that time 10 more messages arrive and are queued at the receiver, waiting to be processed (recall that the receivers for a component execute exclusively, so a receiver will not execute again until it has completed). The component will keep dequeuing and processing messages one by one. Because the component is not able to keep up with the rate at which messages are produced on the incoming stream, the latency of each output from the component will keep increasing, as shown in the results written to the console (subsequent messages have to wait longer and longer in the queue until they are being processed):

```text
0 @ 1.04 seconds latency
1 @ 1.94 seconds latency
2 @ 2.84 seconds latency
3 @ 3.74 seconds latency
4 @ 4.64 seconds latency
5 @ 5.54 seconds latency
6 @ 6.44 seconds latency
7 @ 7.34 seconds latency
...
```

This default behavior, in which all messages are queued and eventually processed, corresponds to a subscription delivery policy called `DeliveryPolicy.Unlimited`. We can modify however this default behavior by specifying an alternative delivery policy when connecting the source stream to the component's receiver. Another commonly used policy is `DeliveryPolicy.LatestMessage`. So, for example:

```csharp
    source.PipeTo(intensiveComponent.In, DeliveryPolicy.LatestMessage)
```  

In this case, the maximum queue size for the `In` receiver is set to size one. If a new message arrives while an existing one is already queued, the older message will essentially be dropped and only the most recent message will be delivered to the receiver. If we re-run the program with this change, the results look like:

```text
0 @ 1.04 seconds latency
10 @ 1.04 seconds latency
20 @ 1.05 seconds latency
30 @ 1.05 seconds latency
40 @ 1.05 seconds latency
...
```

The latency at the output is now maintained constant, but a significant number of messages from the incoming stream are being dropped and not processed. For instance the messages for 1, 2, 3, .. 9 were dropped from the delivery queue and the second message processed by the `Do` operation was message 10. 

## Pipeline and Subscriber policies

Delivery policies can be specified for an entire pipeline when creating the pipeline via `Pipeline.Create()`, or for individual stream subscriptions, when connecting a particular emitter to a particular receiver via the `PipeTo()` operator. The delivery policy specified at the subscription level will overwrite the pipeline-level delivery policy.

By default the delivery policy for pipelines is `DeliveryPolicy.Unlimited`, which does not drop, and queues up messages at the receiver up to a maximum queue size of `int.MaxValue`. A different pipeline-level delivery policy can be specified during `Pipeline.Create()`, as shown below:

```charp
using (var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.LatestMessage))
{
    ...
}
```

The `PipeTo()` operators will use by default this pipeline-level delivery policy. This can also be overwritten by explicitly specifying a subscription-level delivery policy, for example:

```charp
source.PipeTo(intensiveComponent.In, DeliveryPolicy.LatestMessage);
```

Finally, note that the various stream operators such as `Select()`, `Where()`, `Process()` etc., also take an optional delivery policy parameter. The same holds true for all stream operators that follow the [__recommended design pattern__](/psi/topics/InDepth.WritingComponents#StreamOperators) for writing operators. For example:

```csharp
source.Select(x => x * 2, DeliveryPolicy.LatestMessage)
```

## Available Delivery Policies

In the discussion above we have highlighted two specific delivery policies, implemented as static members on the `DeliveryPolicy` class:

* `DeliveryPolicy.Unlimited`: queues all messages at the receiver (up to int.MaxValue messages).
* `DeliveryPolicy.LatestMessage`: only delivers the most recent message to the receiver, and drop all other ones.

Besides these two predefined policies, additional policies may be created using two available static factory methods:
* `DeliveryPolicy.LagConstrained(TimeSpan timeSpan)`: defines a lag-constrained delivery policy. Messages will be queued and are processed by the receiver as long as their latency is below the specified timespan. As time elapses, messages that exceed the lag will be dropped.
* `DeliveryPolicy.QueueSizeConstrained(int maximumQueueSize)`: defines a queue-size constrained delivery policy. Only the most recent messages up to the `maximumQueueSize` will be delivered to the receiver.