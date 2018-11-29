---
layout: default
title:  Window Operators
---

__NOTE__: The operators described below take an optional `DeliveryPolicy` argument which allows the developer to control how the operators keep up with the incoming flow of messages when not enough computational resources are available to process them. More information is available in the [Delivery Policies](/psi/topics/InDepth.DeliveryPolicies) in-depth topic. Below, for improved readability, we simply omit this optional parameter from the operator descriptions.

`Window` operators produce streams of sliding windows over data. Window extents may be specified by either a message count relative to the current message, or by a time interval relative to the current message.

# Window by Relative Index Interval

Each of the below produce a stream of sliding windows of 10 values - an `IProducer<IEnumerable<_>>`.

```csharp
    // sliding future window of 10 messages (including current)
    source.Window(0, 9)
    source.Window(IntInterval.RightBounded(9))

    // sliding past windows 10 messages (including current)
    source.Window(-9, 0)
    source.Window(IntInterval.LeftBounded(-9))

    // sliding window of 10 messages (4 past, current, and future 5)
    source.Window(-4, 5)
    source.Window(new IntInterval(-4, 5))
```

The originating time used for each window is the one corresponding to the input message at relative index 0.
A purely forward-looking window will use the _first_ message, while a backward-looking window will used the _last_.
Windows may span in both forward- and backward-looking directions at once or may span an interval that doesn't cross the origin. In all cases the originating time taken from the the _origin_ message.

# Window by Relative Time Interval

In addition to windows of n-messages, `Window` may be given a `RelativeTimeInterval` in which to include messages.
This will produce potentially variable-length windows.
The originating time is that of the _origin_ message.

```csharp
    // sliding future 10 second window
    source.Window(TimeSpan.Zero, TimeSpan.FromSeconds(10))
    source.Window(RelativeTimeInterval.Future(TimeSpan.FromSeconds(10)))

    // sliding past 10 second window
    source.Window(TimeSpan.FromSeconds(-10), TimeSpan.Zero)
    source.Window(RelativeTimeInterval.Past(TimeSpan.FromSeconds(10)))

    // sliding 15 second window (past 10 seconds, future 5 seconds)
    source.Window(TimeSpan.FromSeconds(-10), TimeSpan.FromSeconds(5))
```

# Selector

A selector function of type `Func<IEnumerable<Message<T>, U>` may also be provided to transform windows of messages into another data type.
This selector is given windows of complete `Messages` including the `Envelopes` from which originating time information may be useful.
The originating time of the resulting value (of type `U`) is taken from the _origin_ message as usual.
For example a `Slope` operator may be implemented in terms of these by computing a true slope of a window of numeric types.

# Statistics Over Windows

It is very common to perform statistical operations over windows.
This may be done by composing \\psi operators with those from LINQ with a `Select`:

```csharp
    source.Window(-9, 0).Select(xs => xs.Average())
```

or

```csharp
    source.Window(RelativeTimeInterval.Past(TimeSpan.FromSeconds(10))).Select(xs => xs.Average())
```

In this case, `Window()` is the \\psi operator - `IProducer<IEnumerable<double>>` (assuming `source` is an `IProducer<double>`).
The `Average()` operator is LINQ's.
We can, of course, transform the windows of data this way using _any_ LINQ operation we like.

In the case of the most common statistical operations such as `Average()`, `Min()`, `Max()`, `Sum()`, ... we have operations directly on `IProducer<T>` where `T` is a numeric type.
The following are two ways to accomplish the same thing:

```csharp
    source.Average(10)
```

or

```csharp
    source.Average(TimeSpan.FromSeconds(10))
```

Note that these statistical operators are backward-looking. That is, `Average(10)` uses `Window(-9, 0)` internally and `Average(TimeSpan.FromSeconds(10))` uses `Window(RelativeTimeInterval.Past(TimeSpan.FromSeconds(10)))`.

Note also that, without a size (`int`) or `TimeSpan` given, these statistical operators instead give you a _running_ result (e.g. running average).