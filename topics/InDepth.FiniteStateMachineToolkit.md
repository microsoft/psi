---
layout: default
title:  Finite State Machine Toolkit
---

# Finite State Machine Toolkit

The Finite State Machine toolkit is a very lightweight framework in which to build non-hierarchical state machines.
A `Machine` with a given `IContext` is initialized with `States` and transitions.
It is then driven by `Update()` to trigger transitions with provided context-transforming functions being called.
These may be tied to entry/exit of particular states or to transitions themselves.

## State

Instances of the `State<TContext>` class represent the classic FSM states in an application-specific system.
They’re given a name (for debugging and tracing) and have an (optional) `onEnter` and `onExit` function called upon transitions into and out of the state.
Each has a list of `Transitions` and an `Updated()` method called when the `Machine` updates.
Transitions consist of a condition (predicate function) triggering the transition, along with a state to which to move and an optional `onTransition` function.
When triggered, transitions produce a new (or the same) state and context.
This is done by simply walking the transition list, applying the predicate.
The first to return true wins (order matters) and a transition is made.
If none match then nothing happens.

## Context

There is expected to be an application-specific class representing context aside from the FSM states themselves.
It’s theoretically possible to do without this but any practical application will need it (especially for non-discrete states).
This will make more sense with the example in a moment but essentially the context may change because of external inputs or because of feedback loops in the FSM.
The `onEnter`/`onExit` functions are `context -> context` functions and the `State` class is parameterized by the application-specific context type.

## Transitions

Transitions are private to the `State`, which provides an `AddTransition()` method.
They have a `Condition` predicate, a `To` state and a `transitionFn`.
The `Traverse` method is given a context and merely threads it through the `transitionFn` and the `enterFn` of the To state.

## Machine

The `Machine` is an abstract class expected to be implemented with application-specific methods and properties representing inputs to the system and setting up the initial states and transitions of the system.
`Machine` contains the current `State` and, for convenience, the current context.
A single `Update()` method is expected to be called whenever the context changes due to external inputs.

The `Machine<TContext>` class is initialized with a context and state.
It causes an initial transition into the beginning state (causing `enterFn` calls and potential further transition).

## Example

As a simple example, consider a traffic light system with two directions of traffic controlled by red/yellow/green lights.
A primary road with thru traffic is given priority unless there is crossing traffic Waiting (detected by a pressure plate under the pavement).
The system of lights is _normally_ in a state of allowing thru traffic to flow.
Upon detected waiting vehicles, thru traffic cannot be immediately stopped.
Instead, we give them a yellow light and wait while in a "stopping thru traffic" state.
Finally crossing traffic is allowed to flow for a period of time before begin given a yellow light ("stopping cross traffic") and transitioning back.

![Diagram](/psi/topics/FiniteStateMachineDiagram.png)

First we create a context type of our choosing (`Lights`) used to drive the machine.
It will be threaded through all the `onEnter`/`onTransition`/`onExit` functions.
Two sets of signals for Through and `Crossing` traffic can each be `Red`/`Yellow`/`Green`.
We also flag whether crossing traffic is waiting (road pressure plate input) and the number of seconds since the last signal change.

```csharp
    enum Color
    {
        Red,
        Yellow,
        Green
    }

    class Lights
    {
        public Color Through;
        public Color Crossing;
        public int Seconds;
        public bool Waiting;

        public Lights ChangeColors(Color through, Color crossing)
        {
            Through = through;
            Crossing = crossing;
            Seconds = 0;
            return this;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", Waiting, Through, Crossing, Seconds);
        }
    }
```

We then derive from `Machine` and, in the constructor, create `State` instances for each of the circles on the diagram and adds transitions for each of the lines on the diagram.

Our `Traffic` FSM is a `Machine<Lights>` (which implies that all the state and transition function are `Lights -> Lights` functions). The two inputs to the system are the passage of time (manually tracked with `Tick()` which makes testing easier) and the pressure plate indicating whether `CrossTraffic(…)` is waiting.

```csharp
    class Traffic : Machine<Lights>
    {
        public Traffic()
        {
            // initial context

            this.context = new Lights()
            {
                Through = Color.Green,
                Crossing = Color.Red,
                Seconds = 0,
                Waiting = false
            };

            ...
        }
    }
```

The initial context is set with through traffic flowing and no cross traffic waiting.
If nobody is waiting to cross then thru traffic flows indefinitely.
If someone is waiting to cross and the thru traffic has been flowing for at least 20 seconds, then we begin the transition to switch traffic flows (giving a yellow for 10 seconds, then red + green).
After 20 seconds, we switch flows again (even if someone is waiting to cross; priority is given to thru traffic assuming it’s a larger or more important road).

The state diagram is very simple; cycling through giving one or the other directions of traffic a yellow, then red light and a green for the other vehicles.
The four states are very simple; each having an OnEnter function to change the lights (no OnExit function in this example):

```csharp
    class Traffic : Machine<Lights>
    {
        public Traffic()
        {
            ...

            // states

            var thruTrafficFlowing = state = new State<Lights>(
                "ThruTrafficFlowing",
                s => s.ChangeColors(Color.Green, Color.Red));
                
            var stoppingThruTraffic = new State<Lights>(
                "StoppingThruTraffic",
                s => s.ChangeColors(Color.Yellow, Color.Red));
                
            var crossTrafficFlowing = new State<Lights>(
                "CrossTrafficFlowing",
                s => s.ChangeColors(Color.Red, Color.Green));
                
            var stoppingCrossTraffic = new State<Lights>(
                "StoppingCrossTraffic",
                s => s.ChangeColors(Color.Red, Color.Yellow));

            ...
        }
    }
```

The four transitions can then be modeled with simple conditions (no OnTranstition function in this example).

```csharp
    class Traffic : Machine<Lights>
    {
        const int MaxRed = 20;
        const int MaxYellow = 10;

        public Traffic()
        {
            ...

            // transitions

            thruTrafficFlowing.AddTransition(
                "Stopping thru traffic for waiting cross traffic.",
                s => s.Waiting && s.Seconds >= MaxRed,
                stoppingThruTraffic);
                
            stoppingThruTraffic.AddTransition(
                "Let cross traffic go.",
                s => s.Seconds >= MaxYellow,
                crossTrafficFlowing);

            crossTrafficFlowing.AddTransition(
                "Stopping cross traffic for thru traffic.",
                s => s.Seconds >= MaxRed,
                stoppingCrossTraffic);
                
            stoppingCrossTraffic.AddTransition(
                "Let thru traffic go.",
                s => s.Seconds >= MaxYellow,
                thruTrafficFlowing);
        }
    }
```

The `enterFn`, `transitionFn` and `exitFn` are all `TContext -> TContext` functions given a chance to modify the context.
The `Machine.Initialize()` is given the initial state and context to kick everything off.
The derived `Machine` should have methods representing inputs to the system.

```csharp
    class Traffic : Machine<Lights>
    {
        public void Tick()
        {
            context.Seconds++;
            Update();
        }

        public void CrossTraffic(bool waiting)
        {
            if (context.Waiting != waiting)
            {
                context.Waiting = waiting;
                Update();
            }
        }

        ...
    }
```

Each may call `Update()` with a modified context to signal changes. Everything is driven by the context.

In general, these "input" functions should _not_ have side effects.
It's likely that the context is not immutable and so the input functions may poke at it, but none-the-less this is passed into `Update()` and is considered the new context.
The enter/transition/exit functions are also not to have side effects.
Instead, the `Update` method may be overridden.
In a good design, only this function will have side effects.
In practice, admittedly, it's much easier (not simpler) to have side-effecting lambdas.
In any case, these should be calling into injected functions so that they may be tested.

The amount of logic in the enter/transition/exit functions is very minimal; just tiny lambdas.
If necessary, helper methods are generally provided in the context to keep these to a minimum and as a way to share repetitive code between them.

### Validation

Validating the model is straight forward and can be done any time the context or state changes.

```csharp
    public bool IsValid()
    {
        // one set of traffic must always be stopped
        if (this.context.Through != Color.Red && this.context.Crossing != Color.Red)
            return false;

        // light should be yellow for 10 seconds max
        if (this.context.Through == Color.Yellow && this.context.Seconds > MaxYellow)
            return false;

        if (this.context.Crossing == Color.Yellow && this.context.Seconds > MaxYellow)
            return false;

        // through light should be red for 30 seconds max
        if (this.context.Through == Color.Red && this.context.Seconds > MaxRed + MaxYellow)
            return false;

        // through light should change within 20 seconds of vehicle waiting
        if (this.context.Waiting && this.context.Through == Color.Green && this.context.Seconds > MaxRed)
            return false;

        // crossing traffic should wait for 20 seconds before through traffic is given yellow
        if (this.context.Waiting && this.context.Through == Color.Green && this.context.Seconds > MaxRed)
            return false;

        // mode should match light states
        if (this.state.Name == "ThruTrafficFlowing" && this.context.Through != Color.Green)
            return false;

        if (this.state.Name == "StoppingThruTraffic" && this.context.Through != Color.Yellow)
            return false;

        if (this.state.Name == "CrossingTrafficFlowing" && this.context.Crossing != Color.Green)
            return false;

        if (this.state.Name == "StoppingCrossingTraffic" && this.context.Crossing != Color.Yellow)
            return false;

        return true;
    }
```

## Testing

Testing the model outside of any real system is also quite easy.

```csharp
    static void Assert(string expect, Traffic fsm)
    {
        var actual = fsm.ToString();
        if (expect != actual)
            Console.WriteLine("TEST FAILURE: {0} != {1}", expect, actual);
    }

    static void Test()
    {
        var fsm = new Traffic();

        // continuous through traffic
        Assert("False Green Red 0", fsm);
        for (var i = 0; i < 100; i++) fsm.Tick();
        Assert("False Green Red 100", fsm);

        // until cross traffic waiting
        fsm.CrossTraffic(true);
        Assert("True Yellow Red 0", fsm);

        // cross traffic goes after 10 sec.
        for (var i = 0; i < 10; i++) fsm.Tick();
        Assert("True Red Green 0", fsm);

        // cross traffic stopped after 20 sec.
        for (var i = 0; i < 20; i++) fsm.Tick();
        Assert("True Red Yellow 0", fsm);

        // through traffic continues after 10 more sec.
        for (var i = 0; i < 10; i++) fsm.Tick();
        Assert("True Green Red 0", fsm);

        // through traffic stops again after 20 sec.
        for (var i = 0; i < 20; i++) fsm.Tick();
        Assert("True Yellow Red 0", fsm);

        // cross traffic goes again after 10 sec.
        for (var i = 0; i < 10; i++) fsm.Tick();
        Assert("True Red Green 0", fsm);

        // cross traffic stopped again after 20 sec.
        for (var i = 0; i < 20; i++) fsm.Tick();
        Assert("True Red Yellow 0", fsm);

        // through traffic continues again after 10 more sec.
        for (var i = 0; i < 10; i++) fsm.Tick();
        Assert("True Green Red 0", fsm);

        // through traffic continues indifinitely while no cross traffic waiting
        fsm.CrossTraffic(false);
        for (var i = 0; i < 100; i++) fsm.Tick();
        Assert("False Green Red 100", fsm);
    }
```

## Embedding

Finally using the state machine in an application is a matter of wiring receivers to cause `Update()` calls and changes in context to cause emitters to post.
The general idea is to keep the context agnostic of any concrete connections to actual sensors and actuators, the network, UI elements, etc.
and to map these externally to the FSM.