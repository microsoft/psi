---
layout: default
title:  EventSource Component
---

# EventSource Component

The `EventSource<TEventHandler, TOut>` component is used to generate a stream of messages triggered by an event to which it subscribes.
Messages are generated whenever the event fires via an event handler of type `TEventHandler` that posts messages of type `TOut`.

## Construction

Since it is not possible to pass the underlying event itself, the `EventSource<TEventHandler, TOut>` is constructed by passing to it two 
delegates of type `Action<TEventHandler>` that will take care of subscribing and unsubscribing to the event. The event handler is specified 
by a converter function of type `Func<Action<TOut>, TEventHandler>` which is supplied an action delegate to post a message on the output stream 
and creates an event handler of type `TEventHandler` compatible with the event being subscribed to. This event handler will typically just 
call the post delegate to post a message, possibly translating the underlying event arguments of type `TEventArgs` into values of type `TOut`. 
Note that output messages may simply be the underlying event arguments themselves, though this is not a requirement.

```csharp
public EventSource(
        Pipeline pipeline,
        Action<TEventHandler> subscribe,
        Action<TEventHandler> unsubscribe,
        Func<Action<TOut>, TEventHandler> converter)
        ...
```

## Usage

Use `EventSource` when you need to create a generator component tied to some underlying .NET event. A typical use case would
be feeding some UI event as a source in the \\psi pipeline. This may be accomplished by creating an `EventSource` where
`TEventHandler` is of the same type as the UI event, and the messages to be posted on the output stream are of type `TOut`, whose values are
derived from the event arguments. Define the event handler in the return value of the converter function, and hook/unhook the handler to the 
underlying event in the subscribe and unsubscribe delegates that are passed to the constructor.

The following example illustrates how an `EventSource` may be used to generate messages containing the value of a slider control whenever the value changes.

```csharp
// Hook up slider control ValueChanged event to an EventSource
var sliderValue = new EventSource<EventHandler<int>, double>(
        pipeline,
        handler => this.SliderControl.ValueChanged += handler, 
        handler => this.SliderControl.ValueChanged -= handler, 
        post => new RoutedPropertyChangedEventHandler<double>((sender, e) => post(e.NewValue))));

// Output value whenever there is a new message
sliderValue.Do(s => Console.WriteLine("Slider value: {0}", s));

```

## References

* [Observable.FromEvent](https://msdn.microsoft.com/en-us/library/hh229241%28v=vs.103%29.aspx?f=255&MSPPError=-2147217396)
