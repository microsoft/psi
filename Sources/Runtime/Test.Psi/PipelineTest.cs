// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PipelineTest
    {
        [TestMethod]
        [Timeout(60000)]
        public void Pass_Data_From_One_Pipeline_To_Another()
        {
            using (var p1 = Pipeline.Create("a"))
            using (var p2 = Pipeline.Create("b"))
            {
                var ready = new AutoResetEvent(false);
                var src = Generators.Sequence(p1, new[] { 1, 2, 3 }, TimeSpan.FromTicks(1));
                var dest = new Processor<int, int>(p2, (i, e, o) => o.Post(i, e.OriginatingTime));
                dest.Do(i => ready.Set());
                var connector = new Connector<int>(p1, p2);
                src.PipeTo(connector);
                connector.PipeTo(dest);

                p2.RunAsync();
                p1.Run();
                Assert.IsTrue(ready.WaitOne(100));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Perf_Of_Allocation()
        {
            var bytes = new byte[100];
            using (var p1 = Pipeline.Create("a"))
            {
                var count = 100;
                var ready = new AutoResetEvent(false);
                var src = Timers.Timer(p1, TimeSpan.FromMilliseconds(5));
                src
                    .Select(t => new byte[100], DeliveryPolicy.Unlimited)
                    .Select(b => b[50], DeliveryPolicy.Unlimited)
                    .Do(_ =>
                    {
                        if (count > 0)
                        {
                            count--;
                        }
                        else
                        {
                            ready.Set();
                        }
                    });

                p1.RunAsync();
                ready.WaitOne(-1);
                Assert.AreEqual(0, count);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Subpipelines()
        {
            using (var p = Pipeline.Create("root"))
            {
                using (var s = Subpipeline.Create(p, "sub"))
                {
                    // add to sub-pipeline
                    var seq = Generators.Sequence(s, new[] { 1, 2, 3 }, TimeSpan.FromTicks(1)).ToObservable().ToListObservable();
                    p.Run(); // run parent pipeline

                    Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, seq.AsEnumerable()));
                }
            }
        }

        public class TestReactiveCompositeComponent : Subpipeline
        {
            public TestReactiveCompositeComponent(Pipeline parent)
                : base(parent, "TestReactiveCompositeComponent")
            {
                var input = this.CreateInputConnectorFrom<int>(parent, "Input");
                var output = this.CreateOutputConnectorTo<int>(parent, "Output");
                this.In = input.In;
                this.Out = output.Out;
                input.Select(i => i * 2).PipeTo(output);
            }

            public Receiver<int> In { get; private set; }

            public Emitter<int> Out { get; private set; }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineAsReactiveComponent()
        {
            using (var p = Pipeline.Create("root"))
            {
                var doubler = new TestReactiveCompositeComponent(p);
                Assert.AreEqual(p, doubler.Out.Pipeline); // composite component shouldn't expose the fact that subpipeline is involved
                var seq = Generators.Sequence(p, new[] { 1, 2, 3 }, TimeSpan.FromTicks(1));
                seq.PipeTo(doubler.In);
                var results = doubler.Out.ToObservable().ToListObservable();
                p.Run(); // note that parent pipeline stops once sources complete (reactive composite-component subpipeline doesn't "hold open")

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 4, 6 }, results.AsEnumerable()));
            }
        }

        public class TestFiniteSourceCompositeComponent : Subpipeline
        {
            public TestFiniteSourceCompositeComponent(Pipeline parent)
                : base(parent, "TestFiniteSourceCompositeComponent")
            {
                var output = this.CreateOutputConnectorTo<int>(parent, "Output");
                this.Out = output.Out;
                Generators.Range(this, 0, 10, TimeSpan.FromTicks(1)).Out.PipeTo(output);
            }

            public Emitter<int> Out { get; private set; }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineAsFiniteSourceComponent()
        {
            using (var p = Pipeline.Create("root"))
            {
                var finite = new TestFiniteSourceCompositeComponent(p);
                Assert.AreEqual(p, finite.Out.Pipeline); // composite component shouldn't expose the fact that subpipeline is involved
                var results = finite.Out.ToObservable().ToListObservable();
                p.Run(); // note that parent pipeline stops once finite source composite-component subpipeline completes

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, results.AsEnumerable()));
            }
        }

        public class TestInfiniteSourceCompositeComponent : Subpipeline
        {
            public TestInfiniteSourceCompositeComponent(Pipeline parent)
                : base(parent, "TestInfiniteSourceCompositeComponent")
            {
                var output = this.CreateOutputConnectorTo<int>(parent, "Output");
                this.Out = output.Out;
                var timer = Timers.Timer(this, TimeSpan.FromMilliseconds(10));
                timer.Aggregate(0, (i, _) => i + 1).PipeTo(output);
            }

            public Emitter<int> Out { get; private set; }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineAsInfiniteSourceComponent()
        {
            ListObservable<int> results;
            var completed = false;

            using (var p = Pipeline.Create("root"))
            {
                var infinite = new TestInfiniteSourceCompositeComponent(p);
                Assert.AreEqual(p, infinite.Out.Pipeline); // composite component shouldn't expose the fact that subpipeline is involved
                results = infinite.Out.ToObservable().ToListObservable();
                p.PipelineCompleted += (_, __) => completed = true;
                p.RunAsync();
                Thread.Sleep(200);
                Assert.IsFalse(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            }

            Assert.IsTrue(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, results.AsEnumerable().Take(3))); // compare first few only
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineWithinSubpipeline()
        {
            using (var p = Pipeline.Create())
            {
                var subpipeline0 = Subpipeline.Create(p, "subpipeline0");
                var connectorIn0 = subpipeline0.CreateInputConnectorFrom<int>(p, "connectorIn0");
                var connectorOut0 = subpipeline0.CreateOutputConnectorTo<int>(p, "connectorOut0");

                var subpipeline1 = Subpipeline.Create(p, "subpipeline1");
                var connectorIn1 = subpipeline1.CreateInputConnectorFrom<int>(subpipeline0, "connectorIn1");
                var connectorOut1 = subpipeline1.CreateOutputConnectorTo<int>(subpipeline0, "connectorOut1");

                var results = new List<int>();
                Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, TimeSpan.FromTicks(1)).PipeTo(connectorIn0.In);
                connectorIn0.Out.PipeTo(connectorIn1.In);
                connectorIn1.Out.PipeTo(connectorOut1.In);
                connectorOut1.Out.PipeTo(connectorOut0.In);
                connectorOut0.Out.Do(x => results.Add(x));

                p.Run();

                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, results);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineWithinSubpipelineWithFiniteSource()
        {
            using (var p = Pipeline.Create())
            {
                var subpipeline0 = Subpipeline.Create(p, "subpipeline0");
                var connectorIn0 = subpipeline0.CreateInputConnectorFrom<int>(p, "connectorIn0");
                var connectorOut0 = subpipeline0.CreateOutputConnectorTo<int>(p, "connectorOut0");

                var subpipeline1 = Subpipeline.Create(p, "subpipeline1");
                var connectorIn1 = subpipeline1.CreateInputConnectorFrom<int>(subpipeline0, "connectorIn1");
                var connectorOut1 = subpipeline1.CreateOutputConnectorTo<int>(subpipeline0, "connectorOut1");

                // add a dummy finite source component to each subpipeline
                Generators.Return(subpipeline0, 0);
                Generators.Return(subpipeline1, 1);

                var results = new List<int>();
                Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, TimeSpan.FromTicks(1)).PipeTo(connectorIn0.In);
                connectorIn0.Out.PipeTo(connectorIn1.In);
                connectorIn1.Out.PipeTo(connectorOut1.In);
                connectorOut1.Out.PipeTo(connectorOut0.In);
                connectorOut0.Out.Do(x => results.Add(x));

                p.Run();

                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, results);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineWithinSubpipelineWithInfiniteSource()
        {
            using (var p = Pipeline.Create())
            {
                var subpipeline0 = Subpipeline.Create(p, "subpipeline0");
                var connectorIn0 = subpipeline0.CreateInputConnectorFrom<int>(p, "connectorIn0");
                var connectorOut0 = subpipeline0.CreateOutputConnectorTo<int>(p, "connectorOut0");

                var subpipeline1 = Subpipeline.Create(p, "subpipeline1");
                var connectorIn1 = subpipeline1.CreateInputConnectorFrom<int>(subpipeline0, "connectorIn1");
                var connectorOut1 = subpipeline1.CreateOutputConnectorTo<int>(subpipeline0, "connectorOut1");

                // add a dummy infinite source component to each subpipeline
                var infinite0 = new InfiniteTestComponent(subpipeline0);
                var infinite1 = new InfiniteTestComponent(subpipeline1);

                var results = new List<int>();
                Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, TimeSpan.FromTicks(1)).PipeTo(connectorIn0.In);
                connectorIn0.Out.PipeTo(connectorIn1.In);
                connectorIn1.Out.PipeTo(connectorOut1.In);
                connectorOut1.Out.PipeTo(connectorOut0.In);
                connectorOut0.Out.Do(x => results.Add(x));

                p.Run();

                CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, results);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineClock()
        {
            // This test verifies that a running sub-pipeline's clock is in sync with its parent.
            // There were cases in the past where this was not the case, leading to unwanted delays.

            using (var p = Pipeline.Create("parent"))
            {
                Clock clock1 = null;
                Clock clock2 = null;

                // Capture the clock from within a Do() operator to ensure that we get the clock
                // that is used when the pipeline is running since the clock could change as the
                // pipeline transitions from not_started -> running -> stopped states.
                Generators.Return(p, 0).Do(_ => clock1 = p.Clock);

                // create a sub-pipeline and capture its running clock
                var sub = Subpipeline.Create(p, "sub");
                Generators.Return(sub, 0).Do(_ => clock2 = sub.Clock);

                // run the pipeline to capture the clocks
                p.Run(ReplayDescriptor.ReplayAllRealTime);

                // now check the two clocks for equivalence
                var now = DateTime.UtcNow;
                Assert.AreEqual(clock1.Origin, clock2.Origin);
                Assert.AreEqual(clock1.RealTimeOrigin, clock2.RealTimeOrigin);
                Assert.AreEqual(clock1.ToVirtualTime(now), clock2.ToVirtualTime(now));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ComponentInitStartOrderingWhenExceedingSchedulerThreadPool()
        {
            // Starting this many generators will easily exceed the `maxThreadCount` and start filling the global queue
            // This used to cause an issue in which component start/initialize would be scheduled out of order and crash
            using (var pipeline = Pipeline.Create())
            {
                for (int i = 0; i < 1000; i++)
                {
                    var p = Generators.Sequence(pipeline, new int[] { }, TimeSpan.FromTicks(1));
                }

                pipeline.Run();
            }
        }

        private class FiniteToInfiniteTestComponent : ISourceComponent
        {
            private Action<DateTime> notifyCompletionTime;
            private ManualResetEvent started = new ManualResetEvent(false);

            public FiniteToInfiniteTestComponent(Pipeline pipeline)
            {
                // this component declares itself finite by may later switch to infinite
                pipeline.CreateEmitter<int>(this, "not really used");
            }

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                this.notifyCompletionTime = notifyCompletionTime;
                this.started.Set();
            }

            public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
            {
                this.started.Reset();
                notifyCompleted();
            }

            public void SwitchToInfinite()
            {
                this.started.WaitOne();
                this.notifyCompletionTime(DateTime.MaxValue);
            }
        }

        private class InfiniteTestComponent : ISourceComponent
        {
            public InfiniteTestComponent(Pipeline pipeline)
            {
                // this component declares itself infinite
                pipeline.CreateEmitter<int>(this, "not really used");
            }

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                notifyCompletionTime(DateTime.MaxValue);
            }

            public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
            {
                notifyCompleted();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineShutdownWithFiniteAndInfiniteSourceComponents()
        {
            // pipeline containing finite source should stop once completed
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing *no* finite sources should run until explicitly stopped
            using (var pipeline = Pipeline.Create())
            {
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsFalse(stopped);
            }

            // pipeline containing finite and infinite sources, but infinite notifying last should complete
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123); // finite
                var finiteToInfinite = new FiniteToInfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                var stopped = pipeline.WaitAll(1000);
                Assert.IsFalse(stopped); // waiting for remaining "finite" component

                finiteToInfinite.SwitchToInfinite();
                stopped = pipeline.WaitAll(1000);
                Assert.IsTrue(stopped); // now we complete
            }

            // pipeline containing finite and infinite sources should complete once all finite sources complete
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123); // finite
                var infinite = new InfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                var stopped = pipeline.WaitAll(1000);
                Assert.IsTrue(stopped); // should complete once finite component completes
            }

            // pipeline containing finite source that notifies as infinite, but no other finite sources have ever completed, should not complete
            using (var pipeline = Pipeline.Create())
            {
                var finiteToInfinite = new FiniteToInfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                finiteToInfinite.SwitchToInfinite();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsFalse(stopped); // now should not complete because no previous finite sources have completed (or existed)
            }

            // pipeline containing subpipeline which in turn contains finite sources should stop once completed
            using (var pipeline = Pipeline.Create())
            {
                var subpipeline = Subpipeline.Create(pipeline);
                Generators.Return(subpipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing subpipeline which in turn contains *only* infinite sources should run until explicitly stopped
            using (var pipeline = Pipeline.Create())
            {
                var subpipeline = Subpipeline.Create(pipeline);
                Generators.Repeat(subpipeline, 123, TimeSpan.FromMilliseconds(1));
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsFalse(stopped);
            }

            // pipeline containing subpipeline which in turn contains *no* sources should run until explicitly stopped
            using (var pipeline = Pipeline.Create())
            {
                var subpipeline = Subpipeline.Create(pipeline);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(1000);
                Assert.IsFalse(stopped);
            }
        }

        // A generator that provides a periodic stream with a configurable artificial latency
        public class GeneratorWithLatency : Generator
        {
            private readonly TimeSpan interval;
            private readonly TimeSpan latency;

            public GeneratorWithLatency(Pipeline pipeline, TimeSpan interval, TimeSpan latency)
                : base(pipeline, isInfiniteSource: true)
            {
                this.interval = interval;
                this.latency = latency;
                this.Out = pipeline.CreateEmitter<int>(this, nameof(this.Out));
            }

            public Emitter<int> Out { get; }

            protected override DateTime GenerateNext(DateTime currentTime)
            {
                // introduce a delay (in wall-clock time) to artificially slow down the generator
                Thread.Sleep(this.latency);

                this.Out.Post(0, currentTime);
                return currentTime + this.interval;
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineShutdownWithLatency()
        {
            using (var p = Pipeline.Create("root"))
            {
                // input sequence
                var generator = Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, TimeSpan.FromMilliseconds(50));

                // use a generator (with artificial latency) as the clock for densification
                // increase the latency TimeSpan value to cause the test to fail when shutdown doesn't account for slow sources
                var clock = new GeneratorWithLatency(p, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(500));

                // The densified stream which should contain five of every input value, except for the last value,
                // since the clock stream has a frequency of 5x the generated sequence stream.
                var densified = clock.Out.Join(generator.Out, RelativeTimeInterval.Past()).Select(x => x.Item2);
                var seq = densified.ToObservable().ToListObservable();

                p.Run();

                var results = seq.AsEnumerable().ToArray();
                CollectionAssert.AreEqual(
                    new[]
                    {
                        0, 0, 0, 0, 0,
                        1, 1, 1, 1, 1,
                        2, 2, 2, 2, 2,
                        3, 3, 3, 3, 3,
                        4, 4, 4, 4, 4,
                        5, 5, 5, 5, 5,
                        6, 6, 6, 6, 6,
                        7, 7, 7, 7, 7,
                        8, 8, 8, 8, 8,
                        9, 9, 9, 9, 9,
                        10,
                    }, results);
            }
        }

        // A composite component which contains a GeneratorWithLatency used to densify an input stream
        public class TestSourceComponentWithGenerator : Subpipeline
        {
            public TestSourceComponentWithGenerator(Pipeline parent, TimeSpan interval, TimeSpan latency)
                : base(parent, "sub")
            {
                var input = this.CreateInputConnectorFrom<int>(parent, "Input");
                var output = this.CreateOutputConnectorTo<int>(parent, "Output");
                this.In = input.In;
                this.Out = output.Out;

                // create a clock stream (with artificial latency) for densification of the input stream
                var clock = new GeneratorWithLatency(this, interval, latency);

                // densify the input stream by joining the clock stream with it
                var densified = clock.Out.Join(input.Out, RelativeTimeInterval.Past()).Select(x => x.Item2);
                densified.PipeTo(output.In);
            }

            public Receiver<int> In { get; }

            public Emitter<int> Out { get; }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineShutdownWithLatency()
        {
            using (var p = Pipeline.Create("root"))
            {
                // input sequence
                var generator = Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, TimeSpan.FromMilliseconds(50));

                // use a generator (with artificial latency) as the clock for densification
                // increase the latency TimeSpan value to cause the test to fail when shutdown doesn't account for slow sources
                var densifier = new TestSourceComponentWithGenerator(p, TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(50));
                generator.PipeTo(densifier.In);

                // the densified stream which should contain two of every input value, except for the last value
                var seq = densifier.Out.ToObservable().ToListObservable();

                p.Run();

                var results = seq.AsEnumerable().ToArray();
                CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 }, results);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineShutdownWithPendingMessage()
        {
            var mre = new ManualResetEvent(false);
            var p = Pipeline.Create("root");

            // Post two messages with an interval of 10 seconds
            Generators.Repeat(p, 0, 2, TimeSpan.FromSeconds(10000)).Do(_ => mre.Set());

            // Run the pipeline, and stop it as soon as the first message is seen
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            p.RunAsync();
            mre.WaitOne();
            p.Dispose();
            stopwatch.Stop();

            // The pipeline should shutdown without waiting for the Generator's loopback message
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DisallowAddingComponentsToAlreadyRunningPipeline()
        {
            using (var p = Pipeline.Create())
            {
                var gen = Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
                p.RunAsync();
                Assert.IsFalse(p.WaitAll(0)); // running

                // add generator while running
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestSimple()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=
                 *  | A |--->| B |
                 *  =---=    =---=
                 */
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log); // finalized 1st although constructed 2nd
                a.Generator.PipeTo(b.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            // A emitters should have closed before B emitters
            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestSelfCycle()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=
                 *  | A |---+
                 *  =---=   |
                 *    ^     |
                 *    |     |
                 *    +-----+
                 */
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(a.ReceiverX); // cycle to itself
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestLongCycle()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=    =---=
                 *  | A |--->| B |--->| C |---+
                 *  =---=    =---=    =---=   |
                 *    ^                       |
                 *    |                       |
                 *    +-----------------------+
                 */
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(a.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            // Emitters should have closed in C, A, B order
            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("AEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("AEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("AEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("AEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("AEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestDisjointCycles()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=    =---=    =---=
                 *  | A |--->| C |    | B |--->| D |
                 *  =---=    =---=    =---=    =---=
                 *    ^        |        ^        |
                 *    |        |        |        |
                 *    +--------+        +--------+
                 */
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(c.ReceiverX);
                b.Generator.PipeTo(d.ReceiverX);
                c.RelayFromX.PipeTo(a.ReceiverX);
                d.RelayFromX.PipeTo(b.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            // Emitters should have closed in D, C, then B or A order
            Assert.IsTrue(log.IndexOf("DEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("AEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("AEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("AEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("AEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("AEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestDisjointUpstreamSelfCycles()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *      =---=    =---=       =---=    =---=
                 *  +-->| A |--->| B |   +-->| C |--->| D |
                 *  |   =---=    =---=   |   =---=    =---=
                 *  |     |              |     |
                 *  +-----+              +-----+
                 */
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(p, "C", log); // finalized 1st although constructed 2nd
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(a.ReceiverX);
                a.RelayFromX.PipeTo(b.ReceiverX);
                c.Generator.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(d.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            // Emitters should have closed in C, A, then D or B order
            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("AEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("AEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("AEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("AEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("AEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestMultiUpstream()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=
                 *  | A |--
                 *  =---=  \   =---=
                 *          -->|   |
                 *             | C |
                 *          -->|   |
                 *  =---=  /   =---=
                 *  | B |--
                 *  =---=
                 */
                var c = new FinalizationTestComponent(p, "C", log); // finalized last although constructed 1st
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(c.ReceiverX);
                b.Generator.PipeTo(c.ReceiverY);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            // A and B emitters may close in any order relative to one another, but *before* C emitters
            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverY Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestFeedbackLoop()
        {
            // this exercises shutdown with active run-away feedback loops

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=
                 *  |   |--->|   |
                 *  | A |    | B |---+
                 *  |   |--->|   |   |
                 *  =---=    =---=   |
                 *    ^              |
                 *    |              |
                 *    +--------------+
                 *
                 *  Feeding time interval, plus feedback loop
                 */
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverY); // timer initiating on emitter Generator
                b.RelayFromAny.PipeTo(a.ReceiverX); // emitter RelayFromAny feeding back on receiver X
                a.RelayFromX.PipeTo(b.ReceiverX);
                p.Run();
            }

            // There should be exactly 3 original messages
            var countY = log.Where(line => line.StartsWith("BReceiveY")).Count();
            Assert.AreEqual(3, countY);

            // Additional are expected on X because of the feedback loop
            var countX = log.Where(line => line.StartsWith("BReceiveX")).Count();
            Assert.IsTrue(countX >= 1);

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverY Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithSubpipeline()
        {
            // this exercises traversal of Connector cross-pipeline bridges
            // internally, a node (PipelineElement) is created on each side with a shared state object (the Connector component)
            // finalization traverses these boundaries

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *         ..................
                 *  =---=  . =---=    =---= .  =---=
                 *  | A |--->| B |--->| C |--->| D |---+
                 *  =---=  . =---=    =---= .  =---=   |
                 *    ^    ..................          |
                 *    |                                |
                 *    +--------------------------------+
                 *
                 *  With B & C in subpipeline
                 */
                var sub = new Subpipeline(p);
                var cIn = new Connector<int>(p, sub);
                var cOut = new Connector<int>(sub, p);
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(sub, "C", log);
                var b = new FinalizationTestComponent(sub, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(cIn.In);
                cIn.Out.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(cOut.In);
                cOut.Out.PipeTo(d.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            // Emitters should have closed in A to D order
            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithNestedSubpipelines()
        {
            // this exercises message traversal and pipeline shutdown across nested subpipelines

            var log = new List<string>();
            using (var p = Pipeline.Create("pipeline"))
            {
                /*
                 *         ...........................
                 *         .        .........        .
                 *  =---=  . =---=  . =---= .  =---= .  =---=
                 *  | A |--->| B |--->| C |--->| D |--->| E |
                 *  =---=  . =---=  . =---= .  =---= .  =---=
                 *         .        .........        .
                 *         ...........................
                 *
                 *  With B & D in subpipeline1 and C in subpipeline2
                 */
                var sub1 = new Subpipeline(p, "subpipeline1");
                var cIn1 = new Connector<int>(p, sub1);
                var cOut1 = new Connector<int>(sub1, p);
                var sub2 = new Subpipeline(sub1, "subpipeline2");
                var cIn2 = new Connector<int>(sub1, sub2);
                var cOut2 = new Connector<int>(sub2, sub1);
                var e = new FinalizationTestComponent(p, "E", log);
                var d = new FinalizationTestComponent(sub1, "D", log);
                var c = new FinalizationTestComponent(sub2, "C", log);
                var b = new FinalizationTestComponent(sub1, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(cIn1.In);
                cIn1.In.Unsubscribed += time => sub1.Stop(time);
                cIn1.Out.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(cIn2.In);
                cIn2.In.Unsubscribed += time => sub2.Stop(time);
                cIn2.Out.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(cOut2.In);
                cOut2.Out.PipeTo(d.ReceiverX);
                d.RelayFromX.PipeTo(cOut1.In);
                cOut1.Out.PipeTo(e.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            Assert.IsTrue(log.Contains("EEmitterAny Closed"));
            Assert.IsTrue(log.Contains("EEmitterX Closed"));
            Assert.IsTrue(log.Contains("EEmitterY Closed"));
            Assert.IsTrue(log.Contains("EEmitterZ Closed"));
            Assert.IsTrue(log.Contains("EEmitterGen Closed"));

            // Emitters should have closed in A to E order
            Assert.IsTrue(log.IndexOf("DEmitterAny Closed") < log.IndexOf("EEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterX Closed") < log.IndexOf("EEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterY Closed") < log.IndexOf("EEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterZ Closed") < log.IndexOf("EEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterGen Closed") < log.IndexOf("EEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("EReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("EReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithCycleAndUpstreamNonCycle()
        {
            // this exercises detection of upstream cycles to origin, but
            // unable to finalize until upstream *non*-cycles are handled

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=
                 *  | A |--->| B |--
                 *  =---=    =---=  \   =---=
                 *                   -->|   |
                 *                      | E |
                 *                   -->|   |
                 *  =---=    =---=  /   =---=
                 *  | C |--->| D |--      |
                 *  =---=    =---=        |
                 *    ^                   |
                 *    |                   |
                 *    +-------------------+
                 */
                var e = new FinalizationTestComponent(p, "E", log);
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverX);
                c.RelayFromX.PipeTo(d.ReceiverX);
                b.RelayFromX.PipeTo(e.ReceiverX);
                d.RelayFromX.PipeTo(e.ReceiverY);
                e.Generator.PipeTo(c.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            Assert.IsTrue(log.Contains("EEmitterAny Closed"));
            Assert.IsTrue(log.Contains("EEmitterX Closed"));
            Assert.IsTrue(log.Contains("EEmitterY Closed"));
            Assert.IsTrue(log.Contains("EEmitterZ Closed"));
            Assert.IsTrue(log.Contains("EEmitterGen Closed"));

            // Emitters should have closed in A, B then (E | C | D) order
            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("EEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("EEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("EEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("EEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("EEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("EReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithUpstreamCycle()
        {
            // this exercises cycle detection apart from cycle back to origin (no infinite exploration)

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=    =---=    =---=
                 *  |   |--->| B |--->| C |
                 *  | A |    =---=    =---=
                 *  |   |      |
                 *  =---=      |
                 *    ^        |
                 *    |        |
                 *    +--------+
                 */
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log); // finalized 1st, though constructed second
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(a.ReceiverX);
                b.RelayFromX.PipeTo(c.ReceiverX);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            // Emitters should have closed in B then (A | C) order
            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("AEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("AEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("AEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("AEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("AEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithMultiCycles()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *             +--------+
                 *             |        |
                 *             v        |
                 *  =---=    =---=    =---=
                 *  | A |--->| B |--->| C |
                 *  =---=    =---=    =---=
                 *             ^        |
                 *             |        |
                 *             +--------+
                 */
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log); // finalized 1st, though constructed last
                a.Generator.PipeTo(b.ReceiverX);
                b.RelayFromAny.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(b.ReceiverY);
                c.RelayFromX.PipeTo(b.ReceiverZ);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            // Should finalize A first, then either B or C (in reality construction order: C then B)
            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverZ Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithNestedCycles()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *             +--------------------------+
                 *             |                          |
                 *             v                          |
                 *  =---=    =---=    =---=    =---=    =---=
                 *  | A |--->| B |--->| C |--->| D |--->| E |
                 *  =---=    =---=    =---=    =---=    =---=
                 *                      ^        |
                 *                      |        |
                 *                      +--------+
                 */
                var e = new FinalizationTestComponent(p, "E", log);
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverX);
                b.RelayFromAny.PipeTo(c.ReceiverX);
                c.RelayFromAny.PipeTo(d.ReceiverX);
                d.RelayFromX.PipeTo(e.ReceiverX);
                d.RelayFromX.PipeTo(c.ReceiverY);
                e.RelayFromX.PipeTo(b.ReceiverY);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            Assert.IsTrue(log.Contains("EEmitterAny Closed"));
            Assert.IsTrue(log.Contains("EEmitterX Closed"));
            Assert.IsTrue(log.Contains("EEmitterY Closed"));
            Assert.IsTrue(log.Contains("EEmitterZ Closed"));
            Assert.IsTrue(log.Contains("EEmitterGen Closed"));

            // Emitter A should have closed first
            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("BEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("BEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("BEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("BEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("BEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("EEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("EEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("EEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("EEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("EEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("EReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("EReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestWithSeparatedCycles()
        {
            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *            +----------+
                 *            | +------+ |
                 *            | |      | |
                 *            | v      | v
                 *  =---=    =---=    =---=    =---=
                 *  | A |--->| B |--->| C |--->| D |
                 *  =---=    =---=    =---=    =---=
                 *    ^        |        ^        |
                 *    |        |        |        |
                 *    +--------+        +--------+
                 */
                var d = new FinalizationTestComponent(p, "D", log);
                var c = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(a.ReceiverX);
                c.Generator.PipeTo(d.ReceiverX);
                d.RelayFromX.PipeTo(c.ReceiverX);
                b.Generator.PipeTo(c.ReceiverY);
                c.Generator.PipeTo(b.ReceiverY);
                b.RelayFromY.PipeTo(c.ReceiverZ);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            // Should finalize B first since it has the most active outputs, then A, then either C or D (in reality construction order: D then C)
            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("AEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("AEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("AEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("AEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("AEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverZ Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestUnsubscribedHandler()
        {
            // Tests delayed posting of messages during finalization

            var log = new List<string>();
            var collector = new List<(int data, Envelope env)>();
            var sequence = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            using (var p = Pipeline.Create())
            {
                // receiver collects received messages in a list but doesn't post them until it is unsubscribed
                var receiver = p.CreateReceiver<int>(collector, (d, e) => collector.Add((d, e)), "Receiver");
                var emitter = p.CreateEmitter<int>(collector, "Emitter");

                // on unsubscribe, post collected messages (with an artificial latency to slow them down)
                receiver.Unsubscribed += _ => collector.ForEach(
                    m =>
                    {
                        Thread.Sleep(33);
                        emitter.Post(m.data, m.env.OriginatingTime);
                    });

                // log posted messages
                emitter.Do((d, e) => log.Add($"{e.OriginatingTime.TimeOfDay}:{d}"));

                // generate a sequence of inputs to the receiver
                var generator = Generators.Sequence(p, sequence, TimeSpan.FromMilliseconds(10));
                generator.PipeTo(receiver);

                p.Run();
            }

            // all collected values should have been posted on unsubscribe
            CollectionAssert.AreEqual(collector.Select(m => string.Format($"{m.env.OriginatingTime.TimeOfDay}:{m.data}")).ToArray(), log);
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestStopFromHandler()
        {
            // Tests stopping a subpipeline from an unsubscribed handler during finalization

            // run test for a range of max worker thread counts
            for (int maxThreads = Environment.ProcessorCount * 2; maxThreads > 0; maxThreads >>= 1)
            {
                maxThreads = 1;
                var collector = new List<int>();
                var sequence = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

                using (var p = Pipeline.Create(null, null, maxThreads)) // should not block even if there is only a single worker thread
                {
                    var sub = Subpipeline.Create(p);
                    var cIn = new Connector<int>(p, sub);
                    var generator = Generators.Sequence(p, sequence, TimeSpan.FromTicks(1));
                    var receiver = sub.CreateReceiver<int>(collector, (d, e) => collector.Add(d), "Receiver");
                    generator.PipeTo(cIn.In);
                    cIn.Out.PipeTo(receiver);
                    cIn.In.Unsubscribed += time => sub.Stop(time);
                    p.Run();
                }

                CollectionAssert.AreEqual(sequence, collector);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FinalizationTestConcurrentShutdown()
        {
            // This tests shutting down a subpipeline via an unsubscribed handler on either inputs from A or B.
            // Either (or both) may trigger shutdown of the subpipeline, while the main pipeline is also in the
            // process of shutting down.

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                 *  =---=
                 *  | A |--  ...........................
                 *  =---=  \ . =---=                   .
                 *          -->|   |    =---=    =---= .
                 *           . | C |--->| D |--->| E | .
                 *          -->|   |    =---=    =---= .
                 *  =---=  / . =---=                   .
                 *  | B |--  ...........................
                 *  =---=
                 *
                 *  With C, D and E in subpipeline, which will be stopped upon unsubscribing either of its two input connectors
                 */
                var sub = new Subpipeline(p);
                var cInA = new Connector<int>(p, sub);
                var cInB = new Connector<int>(p, sub);
                var e = new FinalizationTestComponent(sub, "E", log);
                var d = new FinalizationTestComponent(sub, "D", log);
                var c = new FinalizationTestComponent(sub, "C", log);
                var b = new FinalizationTestComponent(p, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                a.Generator.PipeTo(cInA.In);
                cInA.In.Unsubscribed += time => sub.Stop(time);
                cInA.Out.PipeTo(c.ReceiverX);
                c.RelayFromX.PipeTo(d.ReceiverX);
                d.RelayFromX.PipeTo(e.ReceiverX);
                b.Generator.PipeTo(cInB.In);
                cInB.In.Unsubscribed += time => sub.Stop(time);
                cInB.Out.PipeTo(c.ReceiverY);
                c.RelayFromY.PipeTo(d.ReceiverY);
                d.RelayFromY.PipeTo(e.ReceiverY);
                p.Run();
            }

            // all emitters should have closed
            Assert.IsTrue(log.Contains("AEmitterAny Closed"));
            Assert.IsTrue(log.Contains("AEmitterX Closed"));
            Assert.IsTrue(log.Contains("AEmitterY Closed"));
            Assert.IsTrue(log.Contains("AEmitterZ Closed"));
            Assert.IsTrue(log.Contains("AEmitterGen Closed"));

            Assert.IsTrue(log.Contains("BEmitterAny Closed"));
            Assert.IsTrue(log.Contains("BEmitterX Closed"));
            Assert.IsTrue(log.Contains("BEmitterY Closed"));
            Assert.IsTrue(log.Contains("BEmitterZ Closed"));
            Assert.IsTrue(log.Contains("BEmitterGen Closed"));

            Assert.IsTrue(log.Contains("CEmitterAny Closed"));
            Assert.IsTrue(log.Contains("CEmitterX Closed"));
            Assert.IsTrue(log.Contains("CEmitterY Closed"));
            Assert.IsTrue(log.Contains("CEmitterZ Closed"));
            Assert.IsTrue(log.Contains("CEmitterGen Closed"));

            Assert.IsTrue(log.Contains("DEmitterAny Closed"));
            Assert.IsTrue(log.Contains("DEmitterX Closed"));
            Assert.IsTrue(log.Contains("DEmitterY Closed"));
            Assert.IsTrue(log.Contains("DEmitterZ Closed"));
            Assert.IsTrue(log.Contains("DEmitterGen Closed"));

            Assert.IsTrue(log.Contains("EEmitterAny Closed"));
            Assert.IsTrue(log.Contains("EEmitterX Closed"));
            Assert.IsTrue(log.Contains("EEmitterY Closed"));
            Assert.IsTrue(log.Contains("EEmitterZ Closed"));
            Assert.IsTrue(log.Contains("EEmitterGen Closed"));

            // Emitters should have closed in (A | B), then C to E order
            Assert.IsTrue(log.IndexOf("DEmitterAny Closed") < log.IndexOf("EEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterX Closed") < log.IndexOf("EEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterY Closed") < log.IndexOf("EEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterZ Closed") < log.IndexOf("EEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("DEmitterGen Closed") < log.IndexOf("EEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("CEmitterAny Closed") < log.IndexOf("DEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterX Closed") < log.IndexOf("DEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterY Closed") < log.IndexOf("DEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterZ Closed") < log.IndexOf("DEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("CEmitterGen Closed") < log.IndexOf("DEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("BEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("BEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            Assert.IsTrue(log.IndexOf("AEmitterAny Closed") < log.IndexOf("CEmitterAny Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterX Closed") < log.IndexOf("CEmitterX Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterY Closed") < log.IndexOf("CEmitterY Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterZ Closed") < log.IndexOf("CEmitterZ Closed"));
            Assert.IsTrue(log.IndexOf("AEmitterGen Closed") < log.IndexOf("CEmitterGen Closed"));

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("EReceiverY Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
            Assert.IsFalse(log.Contains("EReceiverZ Unsubscribed"));
        }

        [TestMethod]
        [Timeout(60000)]
        public void DiagnosticsTest()
        {
            var log = new List<string>();
            PipelineDiagnostics graph = null;

            using (var p = Pipeline.Create(enableDiagnostics: true, diagnosticsConfiguration: new DiagnosticsConfiguration() { SamplingInterval = TimeSpan.FromMilliseconds(1) }))
            {
                /*
                 *         .........
                 *  =---=  . =---= .  =---=
                 *  | A |--->| B |--->| D |---+
                 *  =---=  . =---= .  =---=   |
                 *    ^    .........          |
                 *    |                       |
                 *    +-----------------------+
                 */
                var sub = new Subpipeline(p);
                var cIn = new Connector<int>(p, sub);
                var cOut = new Connector<int>(sub, p);
                var d = new FinalizationTestComponent(p, "C", log);
                var b = new FinalizationTestComponent(sub, "B", log);
                var a = new FinalizationTestComponent(p, "A", log);
                var timer = Generators.Range(p, 0, 100, TimeSpan.FromMilliseconds(10));
                timer.PipeTo(cIn.In);
                cIn.Out.PipeTo(b.ReceiverX);
                b.RelayFromX.PipeTo(cOut.In);
                cOut.Out.PipeTo(d.ReceiverX);

                p.Diagnostics.Do(diag => graph = diag.DeepClone());

                p.RunAsync();
                while (graph == null)
                {
                    Thread.Sleep(10);
                }
            }

            Assert.AreEqual(2, graph.GetPipelineCount()); // total graphs
            Assert.AreEqual(11, graph.GetPipelineElementCount()); // total pipeline elements
            Assert.AreEqual(21, graph.GetEmitterCount()); // total emitters (not necessarily connected)
            Assert.AreEqual(6, graph.GetAllEmitterDiagnostics().Where(e => e.Targets.Length != 0).Count()); // total _connected_ emitters
            Assert.AreEqual(13, graph.GetReceiverCount()); // total receivers (not necessarily connected)
            Assert.AreEqual(6, graph.GetAllReceiverDiagnostics().Where(r => r.Source != null).Count()); // total _connected_ receivers
            Assert.IsTrue(graph.GetAllReceiverDiagnostics().Select(r => r.AvgDeliveryQueueSize).Sum() > 0); // usually 50+
            Assert.AreEqual(0, graph.GetDroppedMessageCount()); // total dropped
            Assert.AreEqual(0, graph.GetThrottledReceiverCount()); // total throttled receivers

            // example complex query: average latency at emitter across reactive components in leaf subpipelines
            var complex = graph.GetAllPipelineDiagnostics()
                                .Where(p => p.SubpipelineDiagnostics.Length == 0) // leaf subpipelines
                                .GetAllPipelineElements()
                                .Where(e => e.Kind == PipelineElementKind.Reactive) // reactive components
                                .GetAllReceiverDiagnostics()
                                .Select(r => r.AvgMessageCreatedLatency); // average creation latency into each component's receivers

            Assert.AreEqual("default", graph.Name);
            Assert.IsTrue(graph.IsPipelineRunning);
            Assert.AreEqual(8, graph.PipelineElements.Length);
            Assert.AreEqual(1, graph.SubpipelineDiagnostics.Length);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineProgressTestInfiniteReplay()
        {
            var progress = new List<double>();

            using (var pipeline = Pipeline.Create())
            {
                // pipeline with infinite source
                Generators.Repeat(pipeline, 0, TimeSpan.FromMilliseconds(10))
                    .Select(x => x)
                    .Do(x => { });

                // increase report frequency for testing purposes
                pipeline.ProgressReportInterval = TimeSpan.FromMilliseconds(50);

                // run pipeline for a bit
                pipeline.RunAsync(null, new Progress<double>(x => progress.Add(x)));
                pipeline.WaitAll(200);

                // pipeline is still running and latest progress should reflect this
                Assert.IsTrue(pipeline.IsRunning);
                Assert.IsTrue(progress[progress.Count - 1] < 1.0);
            }

            // Progress<T>.Report() is invoked on the thread-pool since this is a non-UI app,
            // so wait for a bit to ensure that the last progress report action completes.
            Thread.Sleep(100);

            double lastValue = 0;
            foreach (double value in progress)
            {
                Console.WriteLine($"Progress: {value * 100}%");

                // verify progress increases
                Assert.IsTrue(value >= lastValue);
                lastValue = value;
            }

            // verify final progress is 1.0
            Assert.AreEqual(1.0, lastValue);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineProgressTestFiniteReplay()
        {
            var progress = new List<double>();

            using (var pipeline = Pipeline.Create())
            {
                // pipeline with infinite source
                Generators.Repeat(pipeline, 0, 50, TimeSpan.FromMilliseconds(10))
                    .Select(x => x)
                    .Do(x => { });

                // increase report frequency for testing purposes
                pipeline.ProgressReportInterval = TimeSpan.FromMilliseconds(50);

                // create a finite replay descriptor
                var replay = new ReplayDescriptor(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(200));

                // run and wait for pipeline to complete
                pipeline.RunAsync(replay, new Progress<double>(x => progress.Add(x)));
                pipeline.WaitAll();
            }

            // Progress<T>.Report() is invoked on the thread-pool since this is a non-UI app,
            // so wait for a bit to ensure that the last progress report action completes.
            Thread.Sleep(100);

            double lastValue = 0;
            foreach (double value in progress)
            {
                Console.WriteLine($"Progress: {value * 100}%");

                // verify progress increases
                Assert.IsTrue(value >= lastValue);
                lastValue = value;
            }

            // verify final progress is 1.0
            Assert.AreEqual(1.0, lastValue);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineProgressTestSubpipeline()
        {
            var progress = new List<double>();

            using (var pipeline = Pipeline.Create())
            {
                // pipeline with finite source
                Generators.Repeat(pipeline, 0, 50, TimeSpan.FromMilliseconds(10))
                    .Select(x => x)
                    .Do(x => { });

                // subpipeline containing finite source
                var subpipeline = Subpipeline.Create(pipeline, "subpipeline");
                Generators.Repeat(subpipeline, 0, 100, TimeSpan.FromMilliseconds(1));

                // increase report frequency for testing purposes
                pipeline.ProgressReportInterval = TimeSpan.FromMilliseconds(50);

                // create a finite replay descriptor
                var replay = new ReplayDescriptor(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(200));

                // run and wait for pipeline to complete
                pipeline.RunAsync(replay, new Progress<double>(x => progress.Add(x)));

                // test adding a dynamic subpipeline after main pipeline has started
                var subpipeline2 = Subpipeline.Create(pipeline, "subpipeline2");

                pipeline.WaitAll();
            }

            // Progress<T>.Report() is invoked on the thread-pool since this is a non-UI app,
            // so wait for a bit to ensure that the last progress report action completes.
            Thread.Sleep(100);

            double lastValue = 0;
            foreach (double value in progress)
            {
                Console.WriteLine($"Progress: {value * 100}%");

                // verify progress increases
                Assert.IsTrue(value >= lastValue);
                lastValue = value;
            }

            // verify final progress is 1.0
            Assert.AreEqual(1.0, lastValue);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineWiringOnPipelineRun()
        {
            var results = new List<int>();

            using (var pipeline = Pipeline.Create())
            {
                var inputs = Generators.Range(pipeline, 0, 10, TimeSpan.FromTicks(1));
                var subSquare = Subpipeline.Create(pipeline);
                var connSquare = subSquare.CreateInputConnectorFrom<int>(pipeline, "square");
                var square = new Processor<int, int>(subSquare, (x, e, emitter) => emitter.Post(x * x, e.OriginatingTime));
                var subAddOne = Subpipeline.Create(pipeline);

                // wiring between parent pipeline and first child subpipeline
                subSquare.PipelineRun += (_, __) =>
                {
                    connSquare.PipeTo(square);
                    inputs.PipeTo(connSquare);
                };

                // second child subpipeline creates grandchild subpipeline
                subAddOne.PipelineRun += (_, __) =>
                {
                    var subSubAddOne = Subpipeline.Create(subAddOne);

                    // wiring from first child subpipeline to grandchild subpipeline, the back to parent pipeline
                    subSubAddOne.PipelineRun += (s, e) =>
                    {
                        var connAddOne = subSubAddOne.CreateInputConnectorFrom<int>(subSquare, "addOne");
                        var addOne = new Processor<int, int>(subSubAddOne, (x, env, emitter) => emitter.Post(x + 1, env.OriginatingTime));
                        var connResult = subSubAddOne.CreateOutputConnectorTo<int>(pipeline, "result");
                        square.PipeTo(connAddOne);
                        connAddOne.PipeTo(addOne);
                        addOne.PipeTo(connResult);

                        // capture result stream
                        connResult.Do(x => results.Add(x));
                    };
                };

                pipeline.Run();
            }

            // verify result stream y = x^2 + 1
            CollectionAssert.AreEqual(Enumerable.Range(0, 10).Select(x => (x * x) + 1).ToArray(), results);
        }

        [TestMethod]
        public void OnPipelineCompleted()
        {
            var output = new List<string>();
            using (var p = Pipeline.Create())
            {
                Generators.Return(p, 0);
                p.PipelineCompleted += (_, __) =>
                {
                    // slow handler to test that it completes execution before Pipeline.Run() returns
                    Thread.Sleep(100);
                    output.Add("Completed");
                };

                p.Run();
            }

            output.Add("Disposed");
            CollectionAssert.AreEqual(new[] { "Completed", "Disposed" }, output);
        }

        [TestMethod]
        [Timeout(60000)]
        public void NestedOnPipelineRun()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Range(pipeline, 0, 10, TimeSpan.FromTicks(1));

                var fired = false;
                pipeline.PipelineRun += (_, __) =>
                {
                    // additional handlers added *while* handling event must work
                    pipeline.PipelineRun += (_, __) =>
                    {
                        fired = true;
                    };
                };

                pipeline.Run();

                Assert.IsTrue(fired);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PipelineDispose()
        {
            var log = new List<string>();
            var p = Pipeline.Create();
            var c = new FinalizationTestComponent(p, "C", log);
            var b = new FinalizationTestComponent(p, "B", log);
            var a = new FinalizationTestComponent(p, "A", log);
            Assert.IsTrue(p.IsInitial);

            // Test dispose before starting the pipeline
            p.Dispose();
            Assert.IsTrue(p.IsCompleted);
            Assert.IsTrue(log.Count == 0);

            log.Clear();
            p = Pipeline.Create();
            c = new FinalizationTestComponent(p, "C", log);
            b = new FinalizationTestComponent(p, "B", log);
            a = new FinalizationTestComponent(p, "A", log);
            p.RunAsync();
            Assert.IsTrue(p.IsRunning);

            // Tests for resilience to double-dispose
            Parallel.For(0, 10, _ => p.Dispose());
            Assert.IsTrue(p.IsCompleted);
            Assert.IsTrue(log.Count == 15);
        }

        private class FinalizationTestComponent : ISourceComponent
        {
            private readonly Pipeline pipeline;
            private readonly string name;
            private readonly List<string> log;

            private Action<DateTime> notifyCompletionTime;
            private int count = 0;
            private System.Timers.Timer timer;
            private long lastTicks = 0;

            public FinalizationTestComponent(Pipeline pipeline, string name, List<string> log)
            {
                this.pipeline = pipeline;
                this.name = name;
                this.log = log;
                this.RelayFromAny = pipeline.CreateEmitter<int>(this, $"{name}EmitterAny");
                this.RelayFromAny.Closed += _ => this.Log($"{this.name}EmitterAny Closed");
                this.RelayFromX = pipeline.CreateEmitter<int>(this, $"{name}EmitterX");
                this.RelayFromX.Closed += _ => this.Log($"{this.name}EmitterX Closed");
                this.RelayFromY = pipeline.CreateEmitter<int>(this, $"{name}EmitterY");
                this.RelayFromY.Closed += _ => this.Log($"{this.name}EmitterY Closed");
                this.RelayFromZ = pipeline.CreateEmitter<int>(this, $"{name}EmitterZ");
                this.RelayFromZ.Closed += _ => this.Log($"{this.name}EmitterZ Closed");
                this.Generator = pipeline.CreateEmitter<int>(this, $"{name}EmitterGen");
                this.Generator.Closed += _ => this.Log($"{this.name}EmitterGen Closed");
                this.ReceiverX = pipeline.CreateReceiver<int>(this, this.ReceiveX, $"{name}ReceiverX");
                this.ReceiverX.Unsubscribed += _ => this.Log($"{this.name}ReceiverX Unsubscribed");
                this.ReceiverY = pipeline.CreateReceiver<int>(this, this.ReceiveY, $"{name}ReceiverY");
                this.ReceiverY.Unsubscribed += _ => this.Log($"{this.name}ReceiverY Unsubscribed");
                this.ReceiverZ = pipeline.CreateReceiver<int>(this, this.ReceiveZ, $"{name}ReceiverZ");
                this.ReceiverZ.Unsubscribed += _ => this.Log($"{this.name}ReceiverZ Unsubscribed");
            }

            public Receiver<int> ReceiverX { get; private set; } // relays to EmitterX and W

            public Receiver<int> ReceiverY { get; private set; } // relays to EmitterY and W

            public Receiver<int> ReceiverZ { get; private set; } // relays to EmitterY and W

            public Emitter<int> RelayFromAny { get; private set; } // relays from ReceiverX or Y

            public Emitter<int> RelayFromX { get; private set; } // relays from ReceiverX

            public Emitter<int> RelayFromY { get; private set; } // relays from ReceiverY

            public Emitter<int> RelayFromZ { get; private set; } // relays from ReceiverY

            public Emitter<int> Generator { get; private set; } // emits at 10ms intervals

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                this.notifyCompletionTime = notifyCompletionTime;

                if (this.Generator.HasSubscribers)
                {
                    this.timer = new System.Timers.Timer(1) { Enabled = true };
                    this.timer.Elapsed += this.Elapsed;
                }
                else
                {
                    notifyCompletionTime(DateTime.MaxValue);
                }
            }

            public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
            {
                if (this.timer != null)
                {
                    this.timer.Elapsed -= this.Elapsed;
                }

                notifyCompleted();
            }

            public override string ToString()
            {
                return this.name; // useful for debug logging
            }

            private void Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                lock (this)
                {
                    if (this.Generator.HasSubscribers && this.count < 3)
                    {
                        this.Generator.Post(this.count++, this.pipeline.GetCurrentTime());
                    }
                    else
                    {
                        this.timer.Enabled = false;
                        this.notifyCompletionTime(this.pipeline.GetCurrentTime());
                    }
                }
            }

            private void EmitFromEach(int m, DateTime time)
            {
                Thread.Sleep(1);
                this.lastTicks = Math.Max(time.Ticks + TimeSpan.TicksPerMillisecond, this.lastTicks + TimeSpan.TicksPerMillisecond);
                this.RelayFromAny.Post(m, new DateTime(this.lastTicks));
            }

            private void ReceiveX(int m, Envelope e)
            {
                this.Log($"{this.name}ReceiveX {m}");
                this.EmitFromEach(m, e.OriginatingTime);
                this.RelayFromX.Post(m, e.OriginatingTime);
            }

            private void ReceiveY(int m, Envelope e)
            {
                this.Log($"{this.name}ReceiveY {m}");
                this.EmitFromEach(m, e.OriginatingTime);
                this.RelayFromY.Post(m, e.OriginatingTime);
            }

            private void ReceiveZ(int m, Envelope e)
            {
                this.Log($"{this.name}ReceiveZ {m}");
                this.EmitFromEach(m, e.OriginatingTime);
                this.RelayFromZ.Post(m, e.OriginatingTime);
            }

            private void Log(string entry)
            {
                lock (this.log)
                {
                    this.log.Add(entry);
                }
            }
        }
    }
}
