---
layout: default
title:  Buffers and History
---

# Buffers and History

`Buffer` and `History` operators produce streams of sliding windows over data:

```csharp
    source.Buffer(10)
    source.History(10)
```

Each of the above produce a stream of sliding windows of _up to_ 10 values - an `IProducer<IEnumerable<_>>`.
At the start of the source stream, the window contains a single value and grow up to 10.

The difference between `Buffer` and `History` is the timestamp used for each window.
`Buffer` uses the time of the _first_ message, while `History` uses the _last_.

To use some other timestamp of your own each takes an optional selector function that, given the window, returns a `DateTime`.
This selector function may also transform the window into another data type.
For example a `Slope` operator may be implemented in terms of these by computing a true slope of a window of numeric types along with a mean timestamp.

In addition to windows of n-messages, `History` (but not `Buffer`) may be given a `TimeSpan` in which to include messages.
This will produce potentially variable-length windows.

```csharp
    source.History(TimeSpan.FromSeconds(10))
```

# Statistics History

It is very common to perform statistical operations over windows.
This may be done by composing \\psi operators with those from LINQ with a `Select`:

```csharp
    source.History(10).Select(xs => xs.Average())
```

or

```csharp
    source.History(TimeSpan.FromSeconds(10)).Select(xs => xs.Average())
```

In this case, `History()` is the \\psi operator - `IProducer<IEnumerable<double>>` (assuming `source` is an `IProducer<double>`).
The `Average()` operator is LINQ's.
We can, of course, transform the windows of data this way using any LINQ operation we like.

In the case of statistical operations such as `Average()`, `Min()`, `Max()`, `Sum()`, ... we have operations directly on `IProducer<T>` where `T` is a numeric type.
The following are two ways to accomplish the same thing:

```csharp
    source.Average(10)
```

or

```csharp
    source.Average(TimeSpan.FromSeconds(10))
```

Note that, without a size (`int`) or `TimeSpan` given, these statistical operators instead give you a running result (e.g. running average).

# Previous

A simple, but very convenient, usage of `History` is to retrieve the nth message back in time; e.g. ten messages back. This can be achieved with a combination of `History`, `Where` and `Select`, but for convenience:

```csharp
Previous(this IProducer<T> source, int index, DeliveryPolicy policy) -> IProducer<T>
```

For example:

```csharp
var tenthBack = myStream.Previous(10);
```
