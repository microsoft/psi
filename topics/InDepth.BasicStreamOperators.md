---
layout: default
title:  Basic Stream Operators
---

# Basic Stream Operators

The ability to manipulate streams of data plays a central role in Platform for Situated Intelligence. This document provides a brief overview of the basic stream operators currently available. With a few exceptions (like generators and joins), stream operators generally transform one stream into another. The operators can be grouped into several classes:

* [Generating](/psi/topics/InDepth.BasicStreamOperators#Generating): these operators provide the means for creating various source streams.
* [Mapping](/psi/topics/InDepth.BasicStreamOperators#Mapping): these operators transform messages from the input stream.
* [Filtering](/psi/topics/InDepth.BasicStreamOperators#Filtering): these operators filter messages from the input stream.
* [Aggregating](/psi/topics/InDepth.BasicStreamOperators#Aggregating): these operators aggregate messages from the input stream.
* [Window Computations]](/psi/topics/InDepth.BasicStreamOperators#WindowComputations): these operators aggregate windows of messages from the input stream.
* [Actuating](/psi/topics/InDepth.BasicStreamOperators#Actuating): these operators allow for actuating based on messages in a stream.
* [Synchronizing](/psi/topics/InDepth.BasicStreamOperators#Synchronizing): these operators allow for synchronizing multiple streams.
* [Sampling](/psi/topics/InDepth.BasicStreamOperators#Sampling): these operators allow for sampling over a stream.
* [Parallel](/psi/topics/InDepth.BasicStreamOperators#Parallel): these operators allow for vector-parallel computations.
* [Time Related](/psi/topics/InDepth.BasicStreamOperators#TimeRelated): these operators provide timing information.
* [Miscellaneous](/psi/topics/InDepth.BasicStreamOperators#Miscellaneous): these operators provide various other functionalities.

<a name="Generating"></a>

## 1. Generating

Most often source streams are produced by various sensor components, like cameras or microphones. However, several generic stream-producing operators are also available and can be used, via static methods on the static `Generators` class.

### Return(...)

This operator merely returns a stream on which a _single_ value is posted once:

```csharp
Generators.Return(Pipeline p, T value) -> IProducer<T>
```

For example, this produces a stream on which a single message of type `int` with value `42` is posted when the pipeline `p` is started:

```csharp
var life = Generators.Return(p, 42);
```

### Timer(...)

It's surprisingly common to need a simple timer signal stream. This is done with:

```csharp
Generators.Timer(Pipeline p, TimeSpan interval) -> IProducer<TimeSpan>
Generators.Timer(Pipeline p, TimeSpan interval, Func<DateTime, TimeSpan, T> generatorFn) -> IProducer<T>
```

For example, the following produces a stream of elapsed `TimeSpan` values at 10 millisecond intervals:

```csharp
var timer = Generators.Timer(p, TimeSpan.FromMilliseconds(10));
```

### Range(...)

This generator produces a stream consisting of a range of values at a given interval:

```csharp
Generators.Range(Pipeline p, int start, int count, TimeSpan interval) -> IProducer<int>
```

For example, the following produces a stream of `int` values 0 to 9 at 100 millisecond intervals.

```csharp
var range = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(100));
```

### Repeat(...)

This generator produces a stream of repeated values (of any type) at a given interval:

```csharp
Generators.Repeat(Pipeline p, T value, int count, TimeSpan interval) -> IProducer<T>
Generators.Repeat(this IProducer<T> source, IProducer<TClock> clock, bool useInitialValue = false, T initialValue = default(int), DeliveryPolicy policy) -> IProducer<T>
```

For example, this produces a stream of ten `"foo"` string values, 10 milliseconds apart:

```csharp
var rep = Generators.Repeat(p, "foo", 10, TimeSpan.FromMilliseconds(10));
```

For another example, taking a `Range` and `Timer` (see above), the following produces messages at 10 millisecond increments taken from the `range` stream:

```csharp
var range = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(100));
var timer = Generators.Timer(p, TimeSpan.FromMilliseconds(10));
var rep = range.Repeat(timer);
```

Notice that the `timer` fires every 10 milliseconds while the `range` produces values only every 100 milliseconds. The result is that each value from the `range` is repeated ten times.

Essentially, every time the `timer` (`clock` argument) produces a value, the `Repeat(...)` operator produces the _last seen_ value from the `range`. What happens it nothing has yet been seen from the input stream? By default, _nothing_ is produced. An initial value may be specified (or `default(T)`) if desired:

```csharp
var rep = range.Repeat(timer, true, 42); // produce 42 until range begins
```

### Sequence(...)

This operator converts values lazily generated by an `IEnumerable` or from a concrete collection into a \\psi streams at a given interval or from value/time tuples. Also, generation by unfolding with a seed value and `Func<T, T>` is possible:

```csharp
Generators.Sequence(Pipeline p, IEnumerable<T> enumerable, TimeSpan interval) -> IProducer<T>
Generators.Sequence(Pipeline p, IEnumerator<T> enumerator, TimeSpan interval) -> IProducer<T>
Generators.Sequence(Pipeline p, IEnumerable<ValueTuple<T, DateTime>> enumerable) -> IProducer<T>
Generators.Sequence(Pipeline p, IEnumerator<ValueTuple<T, DateTime>> enumerator) -> IProducer<T>
Generators.Sequence(Pipeline p, T initialValue, Func<T, T> generateNext, int count, TimeSpan interval) -> IProducer<T>
```

For example, this produces a stream of `int` values taken from the given collection:

```csharp
var seq = Generators.Sequence(p, new int[] { 1, 2, 3, 7, 42 }, TimeSpan.FromMilliseconds(10));
```

This produces a stream of 1000 multiples of 5:

```csharp
var fives = Generators.Sequence(p, 0, x => x + 5, 1000, TimeSpan.FromMilliseconds(10));
```

<a name="Mapping"></a>

## 2. Mapping

The triumvirate of functional programming is _map_, _filter_, and _fold_. First, mapping operators apply a function to each element on the stream, producing a stream of resulting values.

### Select(...)

As with LINQ, `Select` is the operation to apply a mapping to a sequence values:

```csharp
Select(this IProducer<TIn> source, Func<TIn, TOut> selector, DeliveryPolicy policy) -> IProducer<TOut>
Select(this IProducer<TIn> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy policy) -> IProducer<TOut>
```

For example, one solution to the ["fizz buzz"](https://en.wikipedia.org/wiki/Fizz_buzz) problem is:

```csharp
var naturals = Generators.Sequence(p, 1, x => x + 1, 100);
var fizzbuzz = naturals.Select(x => x % 15 == 0 ? "fizzbuzz" :
                                    x % 3  == 0 ? "fizz" :
                                    x % 5  == 0 ? "buzz" : x.ToString());
```

An overload for the `Select` operator gives access to message envelope, including `Time`, `OriginatingTime`, `SequenceId`, etc.:

```csharp
myStream.Select((m, e) =>
{
    Console.WriteLine($"Message: {m} ({e.OriginatingTime})");
    return m * 2;
});
```

In the example above the `Select` operator multiplies the value of the input message by 2, but also, as a side effect displays its value and the `OriginatingTime` corresponding to the message.

### SelectMany(...)

The `Select` operator (above) provides a one-to-one mapping. Each input value produces a _single_ output value. No more and no less. With `SelectMany` it is possible to map a single value to _many_ values (or to none):

```csharp
SelectMany(this IProducer<TIn> source, Func<TIn, IEnumerable<TOut>> selector, DeliveryPolicy policy) -> IProducer<TOut>
SelectMany(this IProducer<TIn> source, Func<TIn, Envelope, IEnumerable<TOut>> selector, DeliveryPolicy policy) -> IProducer<TOut>
```

For example, to turn a stream of the natural number into reals with in between values:

```csharp
var naturals = Generators.Sequence(p, 1, x => x + 1, 100);
var many = naturals.SelectMany(x => new double[] { x, x + 0.5 });
```

The result is not a stream of collections of values. Instead each `IEnumerable` resulting from the mapping is _flattened_ and zero to many values are emitted as individual messages. This is similar to what some languages actually call a "flat map" operation.

Again, overloads are provided giving access to the message envelope.

### NullableSelect(...)

To make dealing with streams of nullable values, we have `NullableSelect`. Nulls pass through, but when messages have an actual value, it is unpacked, the mapping function is applied and the result is repacked as a nullable.

```csharp
NullableSelect(this IProducer<Nullable<TIn>> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy policy) -> IProducer<Nullable<TOut>>
NullableSelect(this IProducer<Nullable<TIn>> source, Func<TIn, TOut> selector, DeliveryPolicy policy) -> IProducer<Nullable<TOut>>
```

For example:

```csharp
var squaredNullableStream = myNullableStream.NullableSelect(x => x * x);
```

Like with `Select`, there are overloads giving access to the message envelope.

<a name="Filtering"></a>

## 3. Filtering

Next in the functional triumvirate is _filter_; applying a predicate function to each element, producing a (potentially) thinned stream of values.

### Where(...)

Much like LINQ, the basic filtering operation is `Where(...)`. Others are specializations of this.

```csharp
Where(this IProducer<T> source, Func<T, Envelope, Boolean> condition, DeliveryPolicy policy) -> IProducer<T>
Where(IProducer<T> source, Predicate<T> condition, DeliveryPolicy policy) -> IProducer<T>
```

For example, to thin a range to even numbers:

```csharp
var range = Generators.Range(p, 0, 100, TimeSpan.FromMilliseconds(100));
var even = range.Where(x => x % 2 == 0);
```

As with the other operators above, there is an overload giving access to the message envelope. For example, to thin to the first ten messages:

```csharp
var range = Generators.Range(p, 0, 100, TimeSpan.FromMilliseconds(100));
var first100 = range.Where((_, e) => e.SequenceId <= 10);
```

### First()

Another filtering operator is `First`, which produces a resulting stream that contains only the first message on the input stream.

```csharp
First(this IProducer<T> source) -> IProducer<T> // stream consisting of single first value
```

<a name="Aggregating"></a>

## 4. Aggregating

The final piece in the functional triumvirate is _fold_ operation, or aggregation. These operators aggregate state, and accumulate a value over a stream. They apply a function that, given the currently accumulated value and a message from the stream will produce the next accumulated value.

### Aggregate(...)

As in LINQ, the name for the _fold_ operation is `Aggregate`:

```csharp
Aggregate(this IProducer<T> source, Func<T, T, T> func, DeliveryPolicy policy) -> IProducer<T>
Aggregate(this IProducer<TIn> source, TAcc seed, Func<TAcc, TIn, TAcc> func, Func<TAcc, TOut> selector) -> IProducer<TOut>
Aggregate(this IProducer<TIn> source, TAccumulate seed, Func<TAccumulate, TIn, Envelope, Emitter<TOut>, TAccumulate> func, DeliveryPolicy policy) -> IProducer<TOut>
Aggregate(this IProducer<TIn> source, TOut seed, Func<TOut, TIn, TOut> func, DeliveryPolicy policy) -> IProducer<TOut>
```

For example, to produce a stream of the running count of messages (seeded with `0`):

```csharp
var count = myStream.Aggregate(0, (acc, _) => acc + 1);
```

Or to produce the running sum (seeded with the first value; no seed provided):

```csharp
var sum = myStream.Aggregate((acc, x) => acc + x);
```

Producing a stream of the current minimum value:

```csharp
var min = myStream.Aggregate((acc, x) => x < acc ? x : acc);
```

Interestingly, this lambda delegate is essentially taking the accumulated value (`acc`) and the current message (`x`) and returning the minimum of the two. `Math.Min(...)` is already such a function, so the following would work as well:

```csharp
var min = myStream.Aggregate(Math.Min);
```

### Specialized Aggregations

The `Aggregate` operator is very general and can be used to accomplish many operations depending of accumulated state. For convenience, a myriad of common specializations are provided, including `Count`/`LongCount`, `Sum`, `Min`/`Max`, `Average`, `Std`, `Abs`, `Log`, ...

```csharp
Count(this IProducer<_> source) -> IProducer<int>
Count(this IProducer<_> source, Predicate<T> condition) -> IProducer<int>
LongCount(this IProducer<_> source) -> IProducer<long>
LongCount(this IProducer<_> source, Predicate<T> condition) -> IProducer<long>
Sum(this IProducer<_> source) -> IProducer<_>
Sum(this IProducer<_> source, Predicate<_> condition) -> IProducer<_>
Average(this IProducer<_> source) -> IProducer<_>
Average(this IProducer<_> source, Predicate<_> condition) -> IProducer<_>
Max(this IProducer<_> source) -> IProducer<_>
Max(this IProducer<_> source, Predicate<_> condition) -> IProducer<_>
Min(this IProducer<_> source) -> IProducer<_>
Min(this IProducer<_> source, Predicate<_> condition) -> IProducer<_>
Std(this IProducer<_> source) -> IProducer<_>
Abs(this IProducer<double> source) -> IProducer<double>
Log(this IProducer<double> source) -> IProducer<double>
Log(this IProducer<double> source, double newBase) -> IProducer<double>
```

<a name="WindowComputations"></a>

## 5. Window computations

While `Aggregate` accumulates values over the _entire_ stream, it is much more common to do this over some sliding window. The [`Buffer` and `History` operators](/psi/topics/InDepth.BuffersAndHistory) are very useful for producing these sliding windows (as `IEnumerable`) by count or `TimeSpan` over which to operate.

As a convenience, some of the above operations are available over windows by `size` or `timeSpan`. Under the covers, all of these are implemented using [`Buffer` and `History`](/psi/topics/InDepth.BuffersAndHistory):

```csharp
Sum(this IProducer<_> source, int size) -> IProducer<_>
Sum(this IProducer<_> source, TimeSpan timeSpan) -> IProducer<_>
Sum(this IProducer<IEnumerable<_>> source, TimeSpan timeSpan) -> IProducer<_>
Average(this IProducer<_> source, int size) -> IProducer<_>
Average(this IProducer<_> source, TimeSpan timeSpan) -> IProducer<_>
Average(this IProducer<IEnumerable<_> source>) -> IProducer<_>
Max(this IProducer<_> source, int size) -> IProducer<_>
Max(this IProducer<_> source, TimeSpan timeSpan) -> IProducer<_>
Max(this IProducer<IEnumerable<_> source>) -> IProducer<_>
Min(this IProducer<_> source, int size) -> IProducer<_>
Min(this IProducer<_> source, TimeSpan timeSpan) -> IProducer<_>
Min(this IProducer<IEnumerable<_> source>) -> IProducer<_>
Std(this IProducer<_> source, int size) -> IProducer<_>
Std(this IProducer<_> source, TimeSpan timeSpan) -> IProducer<_>
Std(this IProducer<IEnumerable<_> source>) -> IProducer<_>
```

<a name="Actuating"></a>

## 6. Actuating

Streams, produced by sensors or generators, are processed and eventually make their way to some kind of "actuator." The `Do` operator, as well as various and bridging operators can be suitable for this task.

### Do(...)

The `Do` operator allows actuation by executing a delegate function every time a message is received:

```csharp
Do(this IProducer<T> source, Action<T, Envelope> action, DeliveryPolicy policy) -> IProducer<T>
Do(this IProducer<T> source, Action<T> action, DeliveryPolicy policy) -> IProducer<T>
```

Note that the result of a `Do(...)` operation is another stream that will contain the same messages as the input stream itself. The only effect of `Do` is a _side effect_.

For example, the code below simply outputs the values from a stream:

```csharp
myStream.Do(m => Console.WriteLine($"Message: m"));
```

Like with other operators we have seen, an overload gives access to message envelope, including `Time`, `OriginatingTime`, `SequenceId`, etc.:

```csharp
myStream.Do((m, e) => Console.WriteLine($"Message: {m} ({e.OriginatingTime})"));
```

### ToEnumerable() and ToObservable()

Streams may also be bridges to other "worlds" to be wired for actuation this way (e.g. to UI events). \\psi streams may be converted to lazy `IEnumerable` or `IObservable` values or to events to facilitate bridging to other stream-like systems:

* `ToEnumerable(stream: IProducer<T>, condition: Func<T, Boolean>) -> IEnumerable<T>`
* `ToObservable(stream: IProducer<T>) -> IObservable<T>`

For example, simply:

```csharp
myStream.ToEnumerable();
myStream.ToObservable();
```

[Bridging to events is a bit more involved.](/psi/topics/InDepth.EventSource), requiring construction of an `EventSource` component and providing lambdas to subscribe/unsubscribe. This is due to events not being first class values in C# (as they are in F#).

<a name="Synchronizing"></a>

## 7. Synchronizing

Platform for Situated Intelligence provides operators that allow for fusing and synchronizing multiple streams in a variety of ways. The synchronization operators are `Join` and `Pair`. Given the complexity and importance of the topic, these operators are described in more detail in a separate [in-depth document on synchronization](/psi/topics/InDepth.Synchronization).

<a name="Sampling"></a>

## 8. Sampling

### Sample(...)

To sample values from a dense stream at some presumably sparser interval use `Sample`. A `clock` signal drives the sampling. At each signal, a value is taken from the source stream. If messages do not line up in time exactly, an `interpolator`, `matchWindow` or `tolerance` may be given specifying a policy (much like with [other synchronization operators](/psi/topics/InDepth.Synchronization)).

```csharp
Sample(this IProducer<T> source, IProducer<TClock> clock, Interpolator<T> interpolator, DeliveryPolicy policy) -> IProducer<T>
Sample(this IProducer<T> source, IProducer<TClock> clock, RelativeTimeInterval matchWindow, DeliveryPolicy policy) -> IProducer<T>
Sample(this IProducer<T> source, IProducer<TClock> clock, TimeSpan tolerance, DeliveryPolicy policy) -> IProducer<T>
Sample(this IProducer<T> source, TimeSpan samplingInterval, Interpolator<T> interpolator, DeliveryPolicy policy) -> IProducer<T>
Sample(this IProducer<T> source, TimeSpan samplingInterval, TimeSpan matchTolerance, DeliveryPolicy policy) -> IProducer<T>
Sample(this IProducer<T> source, TimeSpan samplingInterval, RelativeTimeInterval matchWindow, DeliveryPolicy policy) -> IProducer<T>
```

For example:

```csharp
var sample = myStream.Sample(TimeSpan.FromMilliseconds(100));
```

<a name="Parallel"></a>

## 9. Parallel

The `Parallel` operator enables parallel execution over dense or sparse vectors (i.e., arrays or dictionaries).

#### Dense vector parallel processing

When using parallel over a stream of dense vectors, i.e. a stream of arrays, a separate pipeline is instantiated for each element in the vector. Each element is processed in parallel and the results are merged at the end back into a vector of the output type. 

```csharp
Parallel<TIn, TOut>(
    this IProducer<TIn[]> source,
    int vectorSize,
    Func<int, IProducer<TIn>, IProducer<TOut>> transformSelector,
    DeliveryPolicy policy = null,
    bool joinOrDefault = true) -> IProducer<TOut>
```

In this case, we need to specify the vector size and a function that given an index in the array and an input stream of type `TIn` produces an output stream of type `TOut`. This function essentially allows us to  specify the sub-pipeline that will be created for the input index.

For instance, conside the example below:

```csharp
var streamOfArray = Generators.Sequence(p, new[] { 0, 1, 2 }, r => new[] { r[0] + 1, r[1] + 1, r[2] + 1 }, 100);
streamOfArray.Parallel(3, (int index, IProducer<int> s) => {
    if(index == 0)
    {
        return s;
    }
    else if(index == 1)
    {
        return s.Select(m => m * 2);
    }
    else
    {
        return s.Select(m => m * 3);
    }
});

```

Here, the `streamOfArray` looks like this:

```
0  1  2  3  4
1  2  3  4  5
2  3  4  5  6
```

The `Parallel` operator defines a index-specific pipeline, which keeps the values for index 1 (we return the original stream as the output stream; doubles the values for index 2 via a `Select` operator, and triples the values for index 3 again via a `Select` operator. The results will look like this:

```
0  1  2  3  4
2  4  6  8 10
6  9 12 15 18
```

#### Sparse vector parallel processing

A similar `Parallel` operator is available to process sparse vectors, i.e. dictionaries `Dictionary<TKey, TValue>`. The signature is:

```csharp
Parallel<TIn, TKey, TOut>(
    this IProducer<Dictionary<TKey, TIn>> source,
    Func<TKey, IProducer<TIn>, IProducer<TOut>> transformSelector,
    DeliveryPolicy policy = null,
    bool joinOrDefault = true) -> IProducer<Dictionary<TKey, TOut>>
```


<a name="TimeRelated"></a>

## 10. Time-related operators

### TimeOf()

The `TimeOf` operator returns a stream that contains the originating times of the messages on the input stream.

```csharp
TimeOf(this IProducer<T> source) -> IProducer<DateTime>
```

For example:

```csharp
myStream.TimeOf();
```

This operator is simply implemented based on a `Select` that picks up the originating time from the message envelope.

### Delay(...)

The `Delay` operator produces a "delayed" stream by shifting the originating times by a specified interval.

```csharp
Delay(this IProducer<T> source, TimeSpan delay, DeliveryPolicy policy) -> IProducer<T> // delay start of stream
```

For example, the code below returns a stream where the messages are offset by 200 ms:

```csharp
myStream.Delay(TimeSpan.FromMilliseconds(200));
```

### Latency(...)

The `Latency` operator computes the latency on a given stream. The messages on the output stream are of type `TimeSpan` and correspond to the difference between the time the message was created (captured by the `Time` member of the message envelope) and the originating time of the message.

```csharp
Latency(this IProducer<T> source) -> IProducer<TimeSpan>
```

<a name="Miscellaneous"></a>

## 11. Miscellaneous

### PipeTo(...)

The `PipeTo` operator allows for connecting streams to various component receivers. 

```csharp
PipeTo<TIn, TC>(this IProducer<TIn> source, TC consumer, DeliveryPolicy policy = null);
```

For example:

```csharp
myStream.PipeTo(myComponent.SomeReceiver);
```

If the component is an `IConsumer<T>`, then the `PipeTo` operator can be used to directly connect to the `In` receiver of the component:

```csharp
myStream.PipeTo(myComponent);
```

### Process(...)

The `Process` operator allows for writing more general stream processors. It's signature is as follows:

```csharp
Process(this IProducer<TIn> source, Action<TIn, Envelope, Emitter<TOut>> transform, DeliveryPolicy policy) -> IProducer<TOut>
```

The second parameter is an action that takes as parameters the message and envelope from the originating stream as well as the output emitter. When using process you control when and what you post on the output emitter. As an example, consider the code snippet below:

```csharp
var sequence = Generators.Sequence(p, 0, x => x + 1, 100);
sequence.Process<int, int>((m, e, o) =>
{
    if (m % 2 == 0)
    {
        o.Post(m * 2, e.OriginatingTime);
    }
});
```

In this case `sequence` is a stream of increasing integers, e.g. 1, 2, 3, 4, ... The delegate given to `Process` checks if the input message is even, and if so doubles the value and posts the result. The resulting stream is 4, 8, 12, ... This could have been in this case accomplished with a `Where` and `Select`, like:

```csharp
sequence.Where(m => m % 2 == 0).Select(m => m * 2)
```

but `Process` does all this in a single operator, and is useful for more complex cases. 

### Name(...)

The `Name` operator allows for naming streams, which can be helpful when debugging.

```csharp
Name(this IProducer<T> source, string name) -> IProducer<T> // name a stream
```

For instance:

```csharp
myStream.Name("My stream");
```

