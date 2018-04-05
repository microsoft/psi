# ROS Arm Control Sample

This sample demonstrates leveraging existing ROS packages with \psi, using the [uArm Metal](http://ufactory.cc/#/en/uarm1).
The sample itself will run under the Core CLR (Mac/Linux/Windows), but depends on ROS running under Linux.
An overview of ROS and our ROS bridge is [covered in a separate document](https://microsoft.github.io/psi/topics/InDepth.PsiROSIntegration).

First we will build a simple class to talk to the uArm, then we'll expose this as a \psi component and will write a small app making use of it.

## Setup

We will need a Linux machine running ROS (at the time of writing, ROS doesn't work under the Linux Subsystem for Windows).
We'll set up a VM with COM port access to the arm, running an off-the-shelf ROS package.
Then we'll develop against this from the Windows side.

1) Download [Ubuntu 17.04 .iso image](https://www.ubuntu.com/download/desktop/thank-you?country=US&version=17.04&architecture=amd64)
2) Install [VirtualBox](https://www.virtualbox.org/wiki/Downloads) (easier to configure COM ports than Hyper-V)
3) Create VM running Ubuntu
4) Install [ROS Lunar](http://wiki.ros.org/lunar/Installation/Ubuntu)
5) Install [Aransena's `uarm_metal` ROS package](https://github.com/aransena/uarm_metal) and
   `rosrun uarm_metal uarm`

## ROS World

Listing the topics and services, we see:

```Shell
# rostopic list

/rosout
/rosout_agg
/uarm_metal/analog_inputs_read
/uarm_metal/attach
/uarm_metal/beep
/uarm_metal/digital_inputs_read
/uarm_metal/joint_angles_read
/uarm_metal/joint_angles_write
/uarm_metal/position_read
/uarm_metal/position_write
/uarm_metal/pump
/uarm_metal/string_read
/uarm_metal/string_write

# rosservice list

/rosout/get_loggers
/rosout/set_logger_level
/uarm_metal/get_loggers
/uarm_metal/set_logger_level
```

It looks like the interesting ones will be reading and writing position and maybe joint angles, as well as controlling the pump and making it beep.

### Pump

Let's start with a simple one; the pump. We can get info about the topic.

```Shell
# rostopic info /uarm_metal/pump

Type: std_msgs/Bool

Publishers: None

Subscribers:
    * /uarm_metal (http://my-dev-box:39969)
```

The `Type` is a standard `Bool`, which means the definition is already available in the ROS bridge library.

### Interface

Follow along in the `ArmControlROSSample` project or:

1) Create a new C# console project.
2) Reference `Microsoft.Ros.dll`

### `UArm` Class

First we'll make a simple class representing the UArm:

```C#
class UArm
{
    private const string NodeName = "/uarm_metal_sample";
    private RosNode.Node node;

    private const string PumpTopic = "/uarm_metal/pump";
    private RosPublisher.IPublisher pumpPublisher;

    public void Connect(string rosSlave, string rosMaster)
    {
        this.node = new RosNode.Node(NodeName, rosSlave, rosMaster);
        this.pumpPublisher = node.CreatePublisher(RosMessageTypes.Standard.Bool.Def, PumpTopic, false);
    }

    public void Disconnect()
    {
        this.node.UnregisterPublisher(PumpTopic);
    }


    public void Pump(bool pump)
    {
        this.pumpPublisher.Publish(RosMessageTypes.Standard.Bool.ToMessage(pump));
    }
}
```

A `Node` is created to connect to the ROS master and is used to manage subscribers and publishers.
You *can* create a `RosPublisher` directly, but it's much more convenient to do this through the `Node` and let it handle bookkeeping.
The publisher needs to know the message definition (for deserialization), the topic name, and whether to latch.

Standard message type definitions are included with the library.
The `RosMessageTypes.Standard.Bool` contains the definition (`Def`), the type information (`Kind`) and functions to construct (`ToMessage`) and destruct (`FromMessage`) the tree that comes over the wire.
Our `Pump()` method merely publishes a bool.

### App

The app will give a simple keyboard interface to a `UArm` instance. Obviously, replace the `rosSlave` and `rosMaster` IP addresses with your own.

```C#
class Program
{
    private const string rosSlave = "127.0.0.1"; // replace with your dev machine
    private const string rosMaster = "127.0.0.1"; // replace with your ROS machine

    static void Main(string[] args)
    {
        Console.WriteLine("UArm Metal Controller");
        Console.WriteLine();
        Console.WriteLine("P - Pump on/off");
        Console.WriteLine("Q - Quit");

        var uarm = new UArm(rosSlave, rosMaster);
        uarm.Connect();
        var pump = false;

        while (true)
        {
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.P:
                    pump = !pump;
                    uarm.Pump(pump);
                    break;
                case ConsoleKey.Q:
                    uarm.Disconnect();
                    return;
            }
        }
    }
}
```

Give it a try. It works!

### Beep

Another simple topic might be the `/uarm_metal/beep`. However, notice that this uses a non-standard message type:

```Shell
# rostopic info /uarm_metal/beep

Type: uarm_metal/Beep

Publishers: None

Subscribers:
    * /uarm_metal (http://my-dev-box:39507)
```

Inspecting this, it's pretty simple:

```Shell
# rosmsg info uarm_metal/Beep

float32 frequency
float32 duration
```

We'll also need the MD5 hash:

```Shell
# rosmsg md5 uarm_metal/Beep

8c872fbca0d0a5bd8ca8259935da556e
```

Following the pattern from above, we create a `beepPublisher` for this topic:

```C#
private const string BeepTopic = "/uarm_metal/beep";
private RosPublisher.IPublisher beepPublisher;
```

The tricky thing is that we don't have an available `MessageDef`.
Essentially, we need to provide a type name and MD5 hash along with a sequence of name/def tuples, where each `RosFieldDef` is created from built-in ROS types.
Fields may be compositions of types (`Variable/FixedArrayVal` or `StructVal`) but here they are simple scalers.
Looking in the ROS project, you may notice that this is done with very concise syntax in F#, but in C# we will need to write a few lines of code:

```C#
private RosMessage.MessageDef BeepMessageDef = RosMessage.CreateMessageDef(
    "uarm_metal/Beep",
    "8c872fbca0d0a5bd8ca8259935da556e",
    new[] {
        Tuple.Create("frequency", RosMessage.RosFieldDef.Float32Def),
        Tuple.Create("duration", RosMessage.RosFieldDef.Float32Def)
    });
```

This is enough to construct the publisher:

```C#
Connect(...)
{
    ...
    this.beepPublisher = node.CreatePublisher(this.BeepMessageDef, BeepTopic, false);
}

Disconnect()
{
    ...
    this.node.UnregisterPublisher(BeepTopic);
}
```

But to actually publish messages, we will need to be able to construct them.
A message is a sequence of name/val tuples (notice, `RosFieldVal` rather than `RosFieldDef` this time).
We'll make a simple helper function for this:

```C#
private Tuple<string, RosMessage.RosFieldVal>[] BeepMessage(float frequency, float duration)
{
    return new[]
    {
        Tuple.Create("frequency", RosMessage.RosFieldVal.NewFloat32Val(frequency)),
        Tuple.Create("duration", RosMessage.RosFieldVal.NewFloat32Val(duration))
    };
}
```

Then our `Beep()` method is simple:

```C#
public void Beep(float frequency, float duration)
{
    this.beepPublisher.Publish(this.BeepMessage(frequency, duration));
}
```

Wiring this to a key, we can give it a try:

```C#
case ConsoleKey.B:
    uarm.Beep(500, 0.1f);
    break;
```

Also works!

### Position

Let's try _subscribing_ to a topic now. The Cartesian position:

```Shell
# rostopic info /uarm_metal/position_read

Type: uarm_metal/Position

Publishers:
    * /uarm_metal (http://my-dev-box:36467)

Subscribers: None
```

Another non-standard message type:

```Shell
# rosmsg info uarm_metal/Position

float32 x
float32 y
float32 z

# rosmsg md5 uarm_metal/Position

cc153912f1453b708d221682bc23d9ac
```

Handled very similarly:

```C#
private const string PositionReadTopic = "/uarm_metal/position_read";
private RosSubscriber.ISubscriber positionSubscriber;
private RosMessage.MessageDef PositionMessageDef = RosMessage.CreateMessageDef(
    "uarm_metal/Position",
    "cc153912f1453b708d221682bc23d9ac",
    new[] {
        Tuple.Create("x", RosMessage.RosFieldDef.Float32Def),
        Tuple.Create("y", RosMessage.RosFieldDef.Float32Def),
        Tuple.Create("z", RosMessage.RosFieldDef.Float32Def)
    });
```

This time, we `Subscribe` to the topic, giving a callback (`PositionUpdate`).

```C#
Connect(...)
{
    ...
    this.positionSubscriber = node.Subscribe(this.PositionMessageDef, PositionReadTopic, PositionUpdate);
}

Disconnect()
{
    ...
    this.node.UnregisterSubscriber(PositionReadTopic);
}
```

The callback takes a sequence of name/value tuples:

```C#
public event EventHandler<Tuple<float, float, float>> PositionChanged;

private float x, y, z;

private void PositionUpdate(IEnumerable<Tuple<string, RosMessage.RosFieldVal>> position)
{
    foreach (var p in position)
    {
        var name = p.Item1;
        var val = RosMessage.GetFloat32Val(p.Item2);
        switch (name)
        {
            case "x":
                this.x = val;
                break;
            case "y":
                this.y = val;
                break;
            case "z":
                this.z = val;
                break;
        }
    }

    if (this.PositionChanged != null)
    {
        this.PositionChanged(this, Tuple.Create(this.x, this.y, this.z));
    }
}
```

You're responsible for "parsing" the sequence of field values, which may themselves be composite structures forming trees.
This is _much_ simpler to do in F# with destructuring bind and you can see plenty of examples of this in the ROS project.
But in this case, in C#, we'll just iterate the fields and store away the `x`/`y`/`z` values which will be used now to control the arm.

Notice that we surface the stream of position updates as a `PositionChanged` event. This seems reasonable.
In some cases, you may want to delay subscribing to the ROS topic until someone has actually bound the event, but we'll keep it simple here.
We'll make the app spew position info to the console:

```C#
uarm.PositionChanged += (_, p) =>
{
    Console.WriteLine($"Position: x={p.Item1} y={p.Item2} z={p.Item3}");
};
```

### Publishing

To _publish_ position updates, we can see that the `/uarm_metal/position_write` topic uses this same message type.

```Shell
# rostopic info /uarm_metal/position_write

Type: uarm_metal/Position

Publishers: None

Subscribers:
    * /uarm_metal (http://my-dev-box:32871)
```

We'll make a helper similar to `BeepMessage` for constructing these:

```C#
private Tuple<string, RosMessage.RosFieldVal>[] PositionMessage(float x, float y, float z)
{
    return new[]
    {
        Tuple.Create("x", RosMessage.RosFieldVal.NewFloat32Val(x)),
        Tuple.Create("y", RosMessage.RosFieldVal.NewFloat32Val(y)),
        Tuple.Create("z", RosMessage.RosFieldVal.NewFloat32Val(z))
    };
}
```

Create/teardown the publisher:

```C#
Connect(...)
{
    ...
    this.positionPublisher = node.CreatePublisher(this.PositionMessageDef, PositionWriteTopic, false);
}

Disconnect()
{
    ...
    this.node.UnregisterPublisher(PositionWriteTopic);
}
```

The method to set the position is simply:

```C#
public void AbsolutePosition(float x, float y, float z)
{
    this.positionPublisher.Publish(this.PositionMessage(x, y, z));
}
```

More convenient may be a relative position _nugde_:

```C#
public void RelativePosition(float x, float y, float z)
{
    this.AbsolutePosition(this.x + x, this.y + y, this.z + z);
}
```

Wiring this to keys, we can now control the arm!

```C#
case ConsoleKey.U:
    uarm.RelativePosition(0, 0, -10);
    break;

case ConsoleKey.D:
    uarm.RelativePosition(0, 0, 10);
    break;

case ConsoleKey.LeftArrow:
    uarm.RelativePosition(0, -10, 0);
    break;

case ConsoleKey.RightArrow:
    uarm.RelativePosition(0, 10, 0);
    break;

case ConsoleKey.UpArrow:
    uarm.RelativePosition(-10, 0, 0);
    break;

case ConsoleKey.DownArrow:
    uarm.RelativePosition(10, 0, 0);
    break;
```

Fun!

## \psi Component

So far, we've exposed subscriptions as events and have wrapped publications in methods. In the \psi world, these become `Receiver`s and `Emitter`s and the whole usage becomes stream-oriented.

First, add a reference to `Microsoft.Psi.dll`.
What we'll do is construct a simple _wrapper_ around the `UArm` class.
This is generally a good idea to first create a class containing all the real logic that lives outside of \psi, then to wrap this as a component to participate in the \psi world.

First, all our methods will become `Receiver`s and our `Event`s will become `Emitters`.

```C#
class UArmComponent
{
    private readonly UArm arm;

    public UArmComponent(Pipeline pipeline, UArm arm)
    {
        this.arm = arm;
    }

    public Receiver<Tuple<float, float>> Beep { get; private set; }

    public Receiver<bool> Pump { get; private set; }

    public Receiver<Tuple<float, float, float>> AbsolutePosition { get; private set; }

    public Receiver<Tuple<float, float, float>> RelativePosition { get; private set; }

    public Emitter<Tuple<float, float, float>> PositionChanged { get; private set; }
}
```

The `Receiver`s are merely be wired to methods on the `arm`:

```C#
public UArmComponent(Pipeline pipeline, UArm arm)
{
    ...
    this.Beep = pipeline.CreateReceiver<Tuple<float, float>>(this, (b, _) => this.arm.Beep(b.Item1, b.Item2), nameof(this.Beep));
    this.Pump = pipeline.CreateReceiver<bool>(this, (p, _) => this.arm.Pump(p), nameof(this.Pump));
    this.AbsolutePosition = pipeline.CreateReceiver<Tuple<float, float, float>>(this, (p, _) => this.arm.AbsolutePosition(p.Item1, p.Item2, p.Item3), nameof(this.AbsolutePosition));
    this.RelativePosition = pipeline.CreateReceiver<Tuple<float, float, float>>(this, (p, _) => this.arm.RelativePosition(p.Item1, p.Item2, p.Item3), nameof(this.RelativePosition));
    this.PositionChanged = pipeline.CreateEmitter<Tuple<float, float, float>>(this, nameof(this.PositionChanged));
}
```

We'll make an event handler for position changes that `Post`s on the `Emitter`:

```C#
private void OnPositionChanged(object sender, Tuple<float, float, float> position)
{
    this.PositionChanged.Post(position, this.pipeline.GetCurrentTime());
}
```

Finally, we'll make our component `IStartable` and `Connect()`/`Disconnect()` the arm as well as register/unregister the event handler upon `Start()`/`Stop()`:
In fact, it is _very_ important to ensure that nothing is `Post`ed when a component is not running (before `Start` or after `Stop`).

```C#
class UArmComponent : IStartable
{
    ...

    public void Start(Action onCompleted, ReplayDescriptor descriptor)
    {
        this.arm.Connect();
        this.arm.PositionChanged += OnPositionChanged;
    }

    public void Stop()
    {
        this.arm.Disconnect();
        this.arm.PositionChanged -= OnPositionChanged;
    }
}
```

That's it; a very light-weight \psi component.

## \psi App

The app will change to a purely stream-oriented approach.
Just as before, let's start with getting the pump to work.

```C#
using (var pipeline = Pipeline.Create())
{
    var arm = new UArmComponent(pipeline, uarm);
    var keys = Generators.Timer(pipeline, TimeSpan.FromMilliseconds(10), (_, __) => Console.ReadKey(true).Key);
    var pump = false;
    keys.Where(k => k == ConsoleKey.P).Select(_ => pump = !pump).PipeTo(arm.Pump);
    pipeline.Run();
}
```

Notice that we turn `Console.ReadKey()` into a stream of `Key`s. Notice also that the `Select(...)` is causing a side effect on the `pump`.
Perhaps a pure functional design could be constructed with a fold (`Aggregate`), but this is an entirely reasonable way to work in \psi too.

To allow quitting the app, we'll need to `RunAsync()` and allow disposing the pipeline upon pressing `Q`:

```C#
using (var pipeline = Pipeline.Create())
{
    ...
    var quit = false;
    keys.Where(k => k == ConsoleKey.Q).Do(_ => quit = true);
    pipeline.RunAsync();
    while (!quit) { Thread.Sleep(100); }
}
```

Setting up the beep is similar:

```C#
keys.Where(k => k == ConsoleKey.B).Select(_ => Tuple.Create(500f, 0.1f)).PipeTo(arm.Beep);
```

Reporting the positions is simply a side-effecting `Do()` on the `PositionChanged` stream:

```C#
arm.PositionChanged.Do(p => Console.WriteLine($"Position: x={p.Item1} y={p.Item2} z={p.Item3}"));
```

Finally, for control of the arm, we'll map keys to _nudge_ values and `PipeTo()` the arm:

```C#
keys.Select(k =>
{
    switch (k)
    {
        case ConsoleKey.U: return Tuple.Create(0f, 0f, -10f);
        case ConsoleKey.D: return Tuple.Create(0f, 0f, 10f);
        case ConsoleKey.LeftArrow: return Tuple.Create(0f, -10f, 0f);
        case ConsoleKey.RightArrow: return Tuple.Create(0f, 10f, 0f);
        case ConsoleKey.UpArrow: return Tuple.Create(-10f, 0f, 0f);
        case ConsoleKey.DownArrow: return Tuple.Create(10f, 0f, 0f);
        default: return null;
    }
}).Where(p => p != null).PipeTo(arm.RelativePosition);
```

Hopefully this has helped with understanding how to interface with ROS from Windows with the bridge library and how to then build \psi components to control robots.

Have fun!

## Links

* A simpler [tutorial using `turtlesim`](https://github.com/Microsoft/psi/blob/master/Samples/RosTurtleSample)
* [Blog covering uArm ROS library](http://www.aransena.com/blog/2016/9/13/uarm-controller)
* Official [`UArmForROS` package](https://github.com/uArm-Developer/UArmForROS) (not being used)