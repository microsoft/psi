---
layout: default
title:  Brief Introduction
---

# A Brief Introduction to Plaform for Situated Intelligence

This tutorial is meant to help you get started with writing you own C# applications using the Platform for Situated Intelligence (or \\psi). Some of the topics that are touched upon here are explained in more depth in other documents.

The tutorial is structured in the following easy steps:

1. [**A simple \\psi application**](/psi/tutorials#SimpleApplication) - describes first steps in creating a very simple \\psi application.
2. [**Synchronization**](/psi/tutorials#Synchronization) - describes how to fuse and synchronize multiple streams.
3. [**Saving Data**](/psi/tutorials#SavingData) - explains how to persists streams to disk.
4. [**Offline Visualization**](/psi/tutorials#OfflineVisualization) - explains how to use PsiStudio to visualize persisted data.
5. [**Live Visualization**](/psi/tutorials#LiveVisualization) - explains how to visualize data live, from a running application.
6. [**Replaying Data**](/psi/tutorials#ReplayingData) - explains how to replay data from a persisted store.
7. [**Further Reading**](/psi/tutorials#FurtherReading) - provides pointers to further in-depth topics.

<a name="SimpleApplication"></a>

## 1. A simple \psi application

Before writing up our first \\psi app, we will need to setup a Visual Studio project for it. Follow these steps to set it up:

1. First, create a simple .NET Core console app (for the examples below, a console app will suffice). You can do so by going to _File -> New Project -> Visual C# -> Console App (.NET Core)_. (.NET Core is a cross-platform solution; if you want to createa .NET Framework solution which runs only on Windows, make sure you change your project settings to target .NET Framework 4.7 or above.)

2. Add a reference to the `Microsoft.Psi.Runtime` NuGet package that contains the core Platform for Situated Intelligence infrastructure. You can do so by going to References (under your project), right-click, then _Manage NuGet Packages_, then go to _Browse_. Make sure the _Include prerelease_ checkbox is checked. Type in `Microsoft.Psi.Runtime` and install.

3. As the final step, add a `using` clause at the beginning of your program as follows:

```csharp
using Microsoft.Psi;
```

You are now ready to start writing your first \\psi application!

A \\psi application generally consists of a computation graph that contains a set of components (nodes in the graph), connected via time-aware streams of data (edges in the graph). Most \\psi applications use various sensor components (like cameras, microphones) to generate source streams, which are then further processed and transformed by other components in the pipeline. We will start simple though, by creating and displaying a simple source stream: a timer stream that posts messages every 100 ms.

To do so, we start by creating the __pipeline__ object, which represents and manages the graph of components that make up our application. Among other things, the pipeline is responsible for starting and stopping the components, for running them concurrently and for coordinating message passing between them. To create the pipeline, we use the `Pipeline.Create` factory method, like below:

```csharp
static void Main(string[] args)
{
    using (var p = Pipeline.Create())
    {
    }
}
```

Now, let's add the logic for constructing the timer stream, and for displaying the messages on it:

```csharp
static void Main(string[] args)
{
    using (var p = Pipeline.Create())
    {
        var timer = Timers.Timer(p, TimeSpan.FromSeconds(0.1));
        timer.Do(t => Console.WriteLine(t));
        p.Run();
    }
}
```

Try it out!

The `Timers.Timer` factory method creates a source timer stream. In this case, the first parameter in the call is the pipeline object `p` (a common pattern in \\psi when instantiating components) and the second parameter is the time interval to use when generating messages is specified as the second parameter.

__*Streams*__ are a fundamental construct and a first-order citizen in the \\psi runtime. They can be generated and processed via various operators and they link components together. Streams are a generic type (you can have a stream of any type `T`) and are strongly typed, and therefore the connections between components are statically checked. Messages posted on streams are time-stamped and streams can be persisted to disk and visualized (more on that in a moment).

In the example above `Do()` is a __*stream operator*__, which executes a function for each message posted on the stream. In this case, the function is specified inline, via an anonymous delegate `t => Console.WriteLine(t)` and simply writes the message to the console. Under the covers, each stream operator is backed by a component: the `Do()` extension method creates a `Do` component that subscribes to the `Timer` component. In reality, the pipeline we have just constructed looks like this:

![Example pipeline](/psi/tutorials/SimplePipeline.png)

The delegate passed to the `Do` operator takes a single parameter, which corresponds to the data arriving on the stream. Another overload of the `Do` operator also gives access to the message envelope, which contains the time-stamp information. To see how this works, change the `timer.Do` line in the example above to:

```csharp
timer.Do((t, e) => Console.WriteLine($"{t} at time {e.OriginatingTime.TimeOfDay}"));
```

This time the timestamp for each message will be displayed. You may notice that the timestamps don't correspond to your local, wall-clock time. This is because \\psi messages are timestamped in UTC.

The `p.Run()` line simply runs the pipeline. A number of overloads and other (asynchronous) methods for running the pipeline are available, but for now, we use the simple `Run()` method which essentially tells the pipeline to execute to completion. This causes the generator to start generating messages, which in turn are processed by the `Do` operators. Since the generator outputs a timer stream that is infinite this application will continue running indefinitely until the console is closed by the user.

There are many basic, generic stream operators available in the \\psi runtime (for an overview, see [Basic Stream Operators](/psi/topics/InDepth.BasicStreamOperators), and also operators for processing various types of streams (like operators for turning a stream of color images into a corresponding stream of grayscale images). Besides `Do`, another operator that is often used is `Select`. The `Select` operator allows you to transform the messages on a stream by simply applying a function to them. Consider the example below (you will have to also add a `using System.Linq;` directive at the top of your program to access the `Enumerable` class):

```csharp
static void Main(string[] args)
{
    using (var p = Pipeline.Create())
    {
        var sequence = Generators.Sequence(p, 0d, x => x + 0.1, 100, TimeSpan.FromMilliseconds(100));
        sequence
            .Select(t => Math.Sin(t))
            .Do(t => Console.WriteLine($"Sin: {t}"));
        p.Run();
    }
}
```

Before we discuss the `Select`, note that we have used a different generator this time to create a source stream that outputs a sequence of values. The `Generators.Sequence` factory method creates a stream that posts a sequence of messages. In this particular case the sequence is specified via an initial value (`0d`), an update function (`x => x + 0.1`) and a total count of messages to be generated (`100`). The messages will be posted at 100 millisecond intervals. Note also that in this case, when we run the pipeline, the sequence and hence the source stream is finite, and as a result the pipeline will complete and the program will exit once the end of the sequence is reached.

The `Select` operator we have injected transforms the messages from the sequence stream by computing the sine function over the messages. Try this and check out the results! You should see in the console a sequence of outputs that fluctuate between -1 and +1, like the sinus function.

Beyond `Do` and `Select`, \\psi contains many operators: single stream operators like `Where`, `Aggregate`, `Parallel` etc. (similar with Linq and Rx), stream generators (`Sequence`, `Enumerate`, `Timer`) as well as operators for combining streams (`Join`, `Sample`, `Repeat`). Like `Do`, some of these operators also have overloads that can surface the message envelope information. Check out the [Basic Stream Operators](/psi/topics/InDepth.BasicStreamOperators) page for an in-depth look at all of them.

Finally, so far we have highlighted the language of stream operators, which encapsulate simple components that perform transformations on streams. In the more general case, \\psi components can have multiple inputs and outputs and can be wired into a pipeline via the `PipeTo` operator. For instance, in the example below we instantiate an audio source component for microphone capture, and connect its output to a voice activity detector component, after which we display the results.

```csharp
using (var p = Pipeline.Create())
{
    // Create an audio source component
    var audioSource = new AudioSource(p,
        new AudioSourceConfiguration() { OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm() });

    // Create a voice activity detector component
    var vad = new SystemVoiceActivityDetector(p);

    // Pipe the output of the audio source component into the input of the voice activity detector
    audioSource.Out.PipeTo(vad.In);

    // Process the output of the voice activity detector and print the messages to the console
    vad.Out.Do(m => Console.WriteLine(m));

    // Run the pipeline
    p.Run();
}
```

Writing \\psi application will often involve authoring a pipeline that connects various existing components and processes and transforms data via stream operators. In some cases, you may need to write your own components. The [Writing Components](/psi/topics/InDepth.WritingComponents) page has more in-depth information on this topic.

<a name="Synchronization"></a>

## 2. Synchronization

Building systems that operate over streaming data often requires that we do stream-fusion, i.e., join data arriving on two different streams to form a third stream. Because execution of the various components in a \\psi pipeline is asynchronous and parallel, __*synchronization*__ becomes paramount, i.e., how do we match (or pair-up) the messages that arrive on the different streams to be joined?

The answer lies in the fact that \\psi streams are time-aware: timing information is carried along with the data on each stream, enabling both the developer and the runtime to correctly reason about the timing of the data. Specifically, the way this works is that the data collected by sensors (the data on source streams) is timestamped with an __*originating time* that captures when the data captured by the sensor occured in the real world. For instance, messages posted on the output stream of the video camera components will time-stamp each frame with the originating time of when that image frame was collected by the sensor. As the image message is processed by a downstream `ToGray()` component, the originating time is propagated forward on the resulting grayscale image message. This way, a downstream component that receives the grayscale image will know what was the time of this data in the real world. And, if the component has to fuse information from the grayscale image with results from processing an audio-stream (which may arrive at a different latency), because the component has access to originating times on both streams, it can reason about time and pair the streams.

To facilite this type of synchronization \\psi provides a special `Join` stream operator that allows you to fuse multiple streams and control the synchronization mechanism used in this process.

To illustrate this, let's look at the following example:

```csharp
using (var p = Pipeline.Create())
{
    var sequence = Generators.Sequence(p, 0d, x => x + 0.1, 100, TimeSpan.FromMilliseconds(100));

    var sin = sequence.Select(t => Math.Sin(t));
    var cos = sequence.Select(t => Math.Cos(t));

    var joined = sin.Join(cos);
    joined.Do(m =>
    {
        var (sinValue, cosValue) = m;
        Console.WriteLine($"Sum of squares: {(sinValue * sinValue) + (cosValue * cosValue)}");
    });
    p.Run();
}
```

The example computes two streams, containing the sin and cos values for the original sequence. The streams are then joined via the join operator: `sin.Join(cos)`. The join operator synchronizes the streams: the messages arriving on the sin and cos streams are paired based on matching originating times. The `Join` operator produces an output stream containing a C# tuple of double values, i.e. `(double, double)` - you can see this by hovering over the joined variable in Visual Studio. This is generally the case: when joining two streams of type `TA` and `TB`, the output stream will be a tuple of `(TA,TB)`.

Try the example out! You should be getting a printout that looks like:

```shell
Sum of squares: 1.0000
Sum of squares: 1.0000
Sum of squares: 1.0000
Sum of squares: 1.0000
```

The answer is always one. Yes, Pythagora's theorem still holds: sin ^ 2 + cos ^ 2 = 1. But there is in fact something more interesting going on here that we would like to highlight. The reason we get the correct answer is because the `Join` operator does the proper synchronization and pairs us the messages with the same originating time, regardless essentially of when they arrive (this is important because all the operators execute concurrently respective to each other). The pipeline from the example above contains 5 components, like this:

![Example pipeline](/psi/tutorials/JoinExample.png)

All of these components execute concurrently. The generator publishes the values, and these are sent to the two `Select` operators. These two operators publish independently the values computed but maintain the originating time on the messages that get sent downstream. The join operator receives the messages on two different streams. Because of concurrent execution, we do not know the relative order that the messages arrive over the two streams at the Join operator. For instance, one of the computations (say cosinus) might take longer which may create a larger latency (in wall clock time) on that stream. However, because originating times are carried through, `Join` can pair the messages correctly and hence the sin ^ 2 + cos ^ 2 = 1 equality holds over the output.

To more vividly demonstrate what would happen without this originating-time based synchronization, modify the join line like below:

```csharp
var joined = sin.Pair(cos);
```

Re-run the program. This time, the results are not always 1. In this case we have used a different operator, `Pair` that does not actually reason about the originating times but rather pairs each message arriving on the `sin` stream with the last message that was delivered on the `cos` stream. Because of asynchronous execution of operators in the pipeline, this time the pairing might not correspond to the a match in the originating times and in that case the result from summing up the squares is different from 1.

We have illustrated above how to do an _exact_ join (the originating times must match exactly for the messages to be paired). There are many ways to do originating-time based synchronization, with important consequences for the behavior of the pipeline. In this introductory guide, we limit this discussion here, but we recommend that you read the more in-depth topic on the [Synchronization](/psi/topics/InDepth.Synchronization) page.

<a name="SavingData"></a>

## 3. Saving Data

While the examples provided so far operate with synthetic data for simplicity, this is not generally the case. Usually, the data is acquired from sensors or external systems otherwise connected to the real world. In most cases we want to save the data for analysis and replay. \\psi provides a very simple way of doing this, as illustrated in the following example:

```csharp
using (var p = Pipeline.Create())
{
    // Create a store to write data to (change this path as you wish - the data will be stored there)
    var store = Store.Create(p, "demo", "c:\\recordings");

    var sequence = Generators.Sequence(p, 0d, x => x + 0.1, 100, TimeSpan.FromMilliseconds(100));

    var sin = sequence.Select(t => Math.Sin(t));
    var cos = sequence.Select(t => Math.Cos(t));

    // Write the sin and cos streams to the store
    sequence.Write("Sequence", store);
    sin.Write("Sin", store);
    cos.Write("Cos", store);

    Console.WriteLine("Program will run for 10 seconds and terminate. Please wait ...");
    p.Run();
}
```

The example creates and saves the _sequence_ and _sin_ and _cos_ streams of double values. This is done by creating a store component with a given name and folder path via the `Store.Create` factory method, and then using it with the `Write` stream operator. The store component knows how to serialize and write to disk virtually any .Net data type (including user-provided types) in an efficient way.

The data is written to disk in the specified location (in this case `c:\\recordings`, in a folder called `demo.0000`. The `Store.Create` API creates this folder and increases the counter to `demo.0001`, `demo.0002` etc. if you run the application repeatedly. Inside the `demo.0000` folder you will find Catalog, Data and Index files. Together, these files constitute the store.

<a name="OfflineVisualization"></a>

## 4. Offline Visualization

Visualization of multimodal streaming data plays a central role in developing multimodal, integrative-AI applications. Visualization scenarios in \\psi are enabled by the __Platform for Situated Intelligence Studio__ (which we will refer to in short as PsiStudio)

__Notes__:
* Kinect for Windows SDK is required to run PsiStudio which can be found [here](https://www.microsoft.com/en-us/download/details.aspx?id=44561).
* Currently, PsiStudio runs only on Windows (although a future cross-platform version is planned - see more in our [Roadmap](/psi/Roadmap) document.) The tool is not currently shipped as an executable, so if you want to use it you will need to build the codebase; instructions for building the code are available [here](/psi/BuildingPsi). The tool is implemented by the `Microsoft.Psi.PsiStudio` project in the `Psi.sln` solution tree under `Sources\Tools\PsiStudio`. To run it, simply run this project after building it. To enable live visualization scenarios (more on that in a second), Platform for Situated Intelligence Studio is a COM server and registers itself as such with the Windows Registry the first time it is launched. You may receive a prompt to allow access for the tool to register itself on first launch.

PsiStudio enables compositing multiple visualizers of different types (from simple streams of doubles to images, depth maps, etc.). In this section we will give a very brief introduction to this tool.

Once you run the application, you will see a window that looks similar to the image below. To open a store, go to the _File_ -> _Open Store_ and navigate to the location you have specified in the example above, e.g. `C:\\recordings\demo.####` (the last folder corresponds to the last run) and open the Catalog file, e.g. `C:\\recordings\demo.####\demo.Catalog_000000.psi`. The PsiStudio window should now look like this:

![PsiStudio (when opening the demo recording)](/psi/tutorials/PsiStudio.Start.png)

The PsiStudio application has a toolbar, a time-navigator (more on that in a second) and a visualization canvas on the left hand side. On the right hand side, you will find a Visualization and Datasets tab. When opening a store, PsiStudio automatically wraps a __*dataset*__ around it (more information on datasets is available in the [Datasets](/psi/topics/InDepth.Datasets) page), with the name _Untitled Dataset_. Double-clicking on _Untitled Dataset_ will open up the underlying demo session and demo partition will reveal the set of streams available in the store, in this case Sequence, Sin and Cos. Right-clicking on the Sin stream will bring up a popup-menu, and selecting _Plot_ will allow you to visualize the Sin stream, like below:

![PsiStudio (visualizing a stream)](/psi/tutorials/PsiStudio.SinVisualization.png)

The plot command has created a timeline visualization panel and inside it a visualizer for the Sin stream. Moving the mouse over the panel moves the data cursor (which is synchronized across all panels).

If we repeat the operation on the Cos stream, a visualizer for this stream will be overlaid on the current timeline panel, resulting in a visualization like this :

![PsiStudio (two streams and legend)](/psi/tutorials/PsiStudio.SinCosLegendVisualization.png)

To display the legend that's visible in the image above, simply right click on the timeline panel and select _Show/Hide Legend_.

You will notice that as you move the cursor around over the timeline panel, the legend updates with the current values under the cursor. Navigation can be done via mouse: moving the mouse moves the cursor, and the scroll wheel zooms the timeline view in and out. As you zoom in, you will notice that the time navigator visuals change, to indicate where you are in the data (the blue region in the top-half).

As we have seen before, new visualizations will by default be overlaid in the same panel. Suppose however that we wanted to visualize the Cos stream in a different panel. Start first by selecting the _Visualizations_ tab (on the left, next to _Datasets_). This will bring you to a hierarchical view of the panels and visualizations currently displayed. It shows that currently there is one timeline panel in the canvas. If you expand the _Timeline Panel_ item at the top of the hierarchy, and it will show two stream visualizers underneath, for Sin and Cos, like below:

![PsiStudio (Visualizers Tab)](/psi/tutorials/PsiStudio.VisualizersTab.png)

Right-clicking on the Cos visualizer brings up a context-menu that currently allows you to remove this visualizer. Try it out. This should make the Cos stream disappear from the panel. Next, click on the _Insert Timeline Panel_ button in the toolbar, highlighted in the image above. This will add a new timeline panel. If you go back to _Datasets_, then right-click on Cos and click _Plot_ again, the Cos stream will appear in the second (current) panel.

Come back to the _Visualizations_ tab and highlight the Cos visualizer. On the bottom-right side, the set of properties for this visualizer are available for inspection and modification. You can change various properties of the visualize, like the color of the line and the marker style to use. For instance, here we have changed the _LineColor_ and _MarkerColor_ properties to red, and the _MarkerStyle_ to Square:

![PsiStudio (two panels)](/psi/tutorials/PsiStudio.TwoPanels.png)

<a name="LiveVisualization"></a>

## 5. Live Visualization

While so far we have discussed how to use PsiStudio to visualize previously collected data, the tool can also be used to visualize the streams persisted by an application _live_, while the application is running. The following example shows how to set this up.

__Notes__: 
* To run this example, PsiStudio must have been compiled and must have ran at least once beforehand, as this will ensure the tool is registered as a COM server. (see note at the beginning of the [Offline Visualization](/psi/tutorials#OfflineVisualization) section above).
* For now, your app should target x64 platform and have a direct dependency on Newtonsoft.Json (>= 11.0.0) until this [issue](https://github.com/Microsoft/psi/issues/7) is resolved.

First, we will have to setup a new console application that this time is .NET Framework 4.7 (as previously mentioned the visualization system is not yet cross-platform). Follow the steps below:

* Create the app by going to _File -> New Project -> Visual C# -> Console App (.NET Framework)_
* In _Project Properties_ -> _Application_ -> _Target framework_, set the target to _.NET Framework 4.7_
* Go to _Project_ -> _Manage NuGet Packages..._ and add `Microsoft.Psi.Visualization.Windows` (See [here](https://microsoft.github.io/psi/UsingWithNuget).)

Finally, add the following using clauses at the top of your file:

```csharp
using System.Windows.Media;
using Microsoft.Psi.Visualization.Client;
using Microsoft.Psi.Visualization.Common;
```

The `Microsoft.Psi.Visualization.Client` and `Microsoft.Psi.Visualization.Common` namespace provides access to visualization APIs. The `System.Windows.Media` namespace will be used for `Color` type (we will be configuring plot colors)

We are now setup to use live visualization. The example code is below:

```csharp
using (var p = Pipeline.Create())
{
    // Create a store to write data to (change this path as you wish - the data will be stored there)
    var store = Store.Create(p, "demo", "c:\\recordings");
    var sequence = Generators.Sequence(p, 0d, x => x + 0.1, 100000, TimeSpan.FromMilliseconds(100)).Write("Sequence", store);

    var sin = sequence.Select(t => Math.Sin(t)).Write("Sin", store);
    var cos = sequence.Select(t => Math.Cos(t)).Write("Cos", store);

    // Instantiate the visualizer, clear it and set it to live mode
    var visualizer = new VisualizationClient();
    visualizer.ClearAll();
    visualizer.SetLiveMode();

    // Plot the sin stream
    sin.Show(visualizer);

    // Add a new timeline panel
    visualizer.AddTimelinePanel();

    // Plot the cos stream and adjust some properties
    var plotCos = cos.Show(visualizer);
    plotCos.Configuration.YMin = -1;
    plotCos.Configuration.YMax = +1;
    plotCos.Configuration.LineColor = Colors.Red;
    plotCos.Configuration.MarkerStyle = MarkerStyle.Diamond;
    plotCos.Configuration.MarkerColor = Colors.Red;

    // Run the pipeline
    p.Run();
}
```

Note that we have changed the number of messages to generate on the source stream to a much larger number, just to let the application run for longer than 10 seconds.

The code snippet shows how to initialize the visualization client, and set it up in live mode. Next, the line `sin.Show(visualizer)` will plot the `sin` stream. The \\psi visualization system currently knows how to visualize a number of stream types, like video, audio, etc.

The next line, `visualizer.AddTimelinePanel()` create a new timeline panel where we will plot the _cos_ stream. Finally, the block plots the cos stream. Note that the call to `Show` returns a visualization object that can be used to configure various properties of the visualization. These are the same properties that are accessible and configurable via the UI from the PsiStudio properties panel. For instance, in the code above:

* we configure the Y-axis bounds of the visualization manually (by default this is automatically adjusted to the range of visible points unless manually specified)
* we configure the color of the plot to `Red` (by default the plot will iterate through a set of colors)
* we configure the plot markers to `Diamond` style (by default the plot marker style is `None`, i.e. markers are not shown)
* we configure the plot markers color to `Red`.

<a name="ReplayingData"></a>

## 6. Replaying Data

Data written to disk in the manner described above can be played back with similar ease. Assuming that the  example described in the [Saving Data section](/psi/tutorials/#SavingData) was executed at least once, the following code will read and replay the data, computing and displaying the sin function.

```csharp
using (var p = Pipeline.Create())
{
    // Open the store
    var store = Store.Open(p, "demo", "c:\\recordings");

    // Open the Sequence stream
    var sequence = store.OpenStream<double>("Sequence");

    // Compute derived streams
    var sin = sequence.Select(Math.Sin).Do(t => Console.WriteLine($"Sin: {t}"));
    var cos = sequence.Select(Math.Cos);

    // Run the pipeline
    p.Run();
}
```

An existing store is open with the `Store.Open` factory method, and streams within the store can be retrieved by name using the `OpenStream` method (you will have to know the name and type of the stream you want to access). The streams can then be processed as if they were just generated from a source.

This method of replaying data preserves the relative timing and order of the messages, and by default plays back data at the same speed as it was produced. When you run the program, you will see the Sin values being displayed by the `Do` operator.

We can control the speed of the execution of the pipeline, via a replay descriptor parameter passed to the `Run()` method. If noparameter is specified the pipeline uses the `ReplayDescriptor.ReplayAllRealTime`, which plays back the data at the same speed as it was produced. Try replacing the call to `p.Run()` with `p.Run(ReplayDescriptor.ReplayAll)`. In this case, the data will play backfrom the store at maximum speed, regardless of the speed at which it was generated. Running the program will display the Sin values much faster now. Note that the originating times are nevertheless preserved on the messages being replayed from the store.

<a name="FurtherReading"></a>

## 7. Next steps

After going through this first brief tutorial, it may be helpful to look through the set of [Samples](/psi/samples) provided. While some of the samples address specialized topics like how to leverage speech recognition components or how to bridge to ROS, reading them will give you more insights into programming with Platform for Situated Intelligence.

Finally, additional information is provided in a set of [In-Depth Topics](/psi/topics) that dive into more details in various aspects of the framework, like synchronization, persistence, remoting, visualization etc.
