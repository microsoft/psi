// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Reactive;
    using System.Reactive.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
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
                var src = Generators.Sequence(p1, new[] { 1, 2, 3 });
                var dest = new Processor<int, int>(p2, (i, e, o) => o.Post(i, e.OriginatingTime));
                dest.Do(i => ready.Set());
                src.PipeTo(dest);

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
                    var seq = Generators.Sequence(s, new[] { 1, 2, 3 }).ToObservable().ToListObservable();
                    p.Run(); // run parent pipeline

                    Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, seq.AsEnumerable()));
                }
            }
        }

        public class TestReactiveCompositeComponent : Subpipeline
        {
            public Receiver<int> In { private set; get; }

            public Emitter<int> Out { private set; get; }

            public TestReactiveCompositeComponent(Pipeline parent)
                : base(parent, "TestReactiveCompositeComponent")
            {
                var input = parent.CreateInputConnector<int>(this, "Input");
                var output = this.CreateOutputConnector<int>(parent, "Output");
                this.In = input.In;
                this.Out = output.Out;
                input.Select(i => i * 2).PipeTo(output);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SubpipelineAsReactiveComponent()
        {
            using (var p = Pipeline.Create("root"))
            {
                var doubler = new TestReactiveCompositeComponent(p);
                Assert.AreEqual(p, doubler.Out.Pipeline); // composite component shouldn't expose the fact that subpipeline is involved
                var seq = Generators.Sequence(p, new[] { 1, 2, 3 });
                seq.PipeTo(doubler.In);
                var results = doubler.Out.ToObservable().ToListObservable();
                p.Run(); // note that parent pipeline stops once sources complete (reactive composite-component subpipeline doesn't "hold open")

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 4, 6 }, results.AsEnumerable()));
            }
        }

        public class TestFiniteSourceCompositeComponent : Subpipeline
        {
            public Emitter<int> Out { private set; get; }

            public TestFiniteSourceCompositeComponent(Pipeline parent)
                : base(parent, "TestFiniteSourceCompositeComponent")
            {
                var output = this.CreateOutputConnector<int>(parent, "Output");
                this.Out = output.Out;
                Generators.Range(this, 0, 10).Out.PipeTo(output);
            }
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
            public Emitter<int> Out { private set; get; }

            public TestInfiniteSourceCompositeComponent(Pipeline parent)
                : base(parent, "TestInfiniteSourceCompositeComponent")
            {
                var output = this.CreateOutputConnector<int>(parent, "Output");
                this.Out = output.Out;
                var timer = Timers.Timer(this, TimeSpan.FromMilliseconds(10));
                timer.Aggregate(0, (i, _) => i + 1).PipeTo(output);
            }
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
                p.PipelineCompleted += ((_, __) => completed = true);
                p.RunAsync();
                Thread.Sleep(200);
                Assert.IsFalse(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            }

            Assert.IsTrue(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, results.AsEnumerable().Take(3))); // compare first few only
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
                var now = DateTime.Now;
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
                    var p = Generators.Sequence(pipeline, new int[] { });
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

            public void Stop()
            {
                this.started.Reset();
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

            public void Stop()
            {
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
                var stopped = pipeline.WaitAll(5000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing *no* finite sources should run until explicitly stopped
            using (var pipeline = Pipeline.Create())
            {
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(5000);
                Assert.IsFalse(stopped);
            }

            // pipeline containing finite and infinite sources, but infinite notifying last should complete
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123); // finite
                var finiteToInfinite = new FiniteToInfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                var stopped = pipeline.WaitAll(5000);
                Assert.IsFalse(stopped); // waiting for remaining "finite" component

                finiteToInfinite.SwitchToInfinite();
                stopped = pipeline.WaitAll(5000);
                Assert.IsTrue(stopped); // now we complete
            }

            // pipeline containing finite and infinite sources should complete once all finite sources complete
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123); // finite
                var infinite = new InfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                var stopped = pipeline.WaitAll(5000);
                Assert.IsTrue(stopped); // should complete once finite component completes
            }

            // pipeline containing finite source that notifies as infinite, but no other finite sources have ever completed, should not complete
            using (var pipeline = Pipeline.Create())
            {
                var finiteToInfinite = new FiniteToInfiniteTestComponent(pipeline);
                pipeline.RunAsync();

                finiteToInfinite.SwitchToInfinite();
                var stopped = pipeline.WaitAll(5000);
                Assert.IsFalse(stopped); // now should not complete because no previous finite sources have completed (or existed)
            }

            // pipeline containing subpipeline which in turn contains finite sources should stop once completed
            using (var pipeline = Pipeline.Create())
            {
                var subpipeline = Subpipeline.Create(pipeline);
                Generators.Return(subpipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(5000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing subpipeline which in turn contains *no* finite sources should run until explicitly stopped
            using (var pipeline = Pipeline.Create())
            {
                var subpipeline = Subpipeline.Create(pipeline);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(5000);
                Assert.IsFalse(stopped);
            }
        }

        private IEnumerator<(int, DateTime)> Gen()
        {
            DateTime now = DateTime.UtcNow;
            int i = 0;
            while (true)
            {
                yield return (i, now);
                i++;
                now = now + TimeSpan.FromMilliseconds(5);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DisallowAddingComponentsToAlreadyRunningPipeline()
        {
            using (var p = Pipeline.Create())
            {
                var gen = Generators.Range(p, 0, 10);
                p.RunAsync();
                Assert.IsFalse(p.WaitAll(0)); // running

                // add generator while running
                Generators.Range(p, 0, 10);
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
            // this exercised traversal of Connector cross-pipeline bridges
            // internally, a node (PipelineElement) is created on each side with a shared state object (the Connector component)
            // finalization traverses these boundaries

            var log = new List<string>();
            using (var p = Pipeline.Create())
            {
                /*
                *          ..................
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
                var c = new FinalizationTestComponent(p, "C", log); // finalized last, though constructed first
                var b = new FinalizationTestComponent(p, "B", log); // finalized 2nd
                var a = new FinalizationTestComponent(p, "A", log); // finalized 1st, though constructed last
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

            // Emitters should have closed in (A | B) then C order
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

            // subscribed receivers should have been unsubscribed
            Assert.IsTrue(log.Contains("AReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("BReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverX Unsubscribed"));
            Assert.IsTrue(log.Contains("CReceiverY Unsubscribed"));
            Assert.IsTrue(log.Contains("DReceiverX Unsubscribed"));

            // non-subscribed receivers should have done *nothing*
            Assert.IsFalse(log.Contains("AReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("AReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("BReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("CReceiverZ Unsubscribed"));

            Assert.IsFalse(log.Contains("DReceiverY Unsubscribed"));
            Assert.IsFalse(log.Contains("DReceiverZ Unsubscribed"));
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
                this.ReceiverY.Unsubscribed +=_ => this.Log($"{this.name}ReceiverY Unsubscribed");
                this.ReceiverZ = pipeline.CreateReceiver<int>(this, this.ReceiveZ, $"{name}ReceiverZ");
                this.ReceiverZ.Unsubscribed += _ => this.Log($"{this.name}ReceiverZ Unsubscribed");
            }

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                if (this.Generator.HasSubscribers)
                {
                    this.notifyCompletionTime = notifyCompletionTime;
                    this.timer = new System.Timers.Timer(1) { Enabled = true };
                    this.timer.Elapsed += this.Elapsed;
                }
                else
                {
                    notifyCompletionTime(DateTime.MaxValue);
                }
            }

            public void Stop()
            {
                if (timer != null)
                {
                    this.timer.Elapsed -= this.Elapsed;
                }
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
                EmitFromEach(m, e.OriginatingTime);
                this.RelayFromX.Post(m, e.OriginatingTime);
            }

            private void ReceiveY(int m, Envelope e)
            {
                this.Log($"{this.name}ReceiveY {m}");
                EmitFromEach(m, e.OriginatingTime);
                this.RelayFromY.Post(m, e.OriginatingTime);
            }

            private void ReceiveZ(int m, Envelope e)
            {
                this.Log($"{this.name}ReceiveZ {m}");
                EmitFromEach(m, e.OriginatingTime);
                this.RelayFromZ.Post(m, e.OriginatingTime);
            }

            private void Log(string entry)
            {
                lock (this.log)
                {
                    this.log.Add(entry);
                }
            }

            public Receiver<int> ReceiverX { get; private set; } // relays to EmitterX and W

            public Receiver<int> ReceiverY { get; private set; } // relays to EmitterY and W

            public Receiver<int> ReceiverZ { get; private set; } // relays to EmitterY and W

            public Emitter<int> RelayFromAny { get; private set; } // relays from ReceiverX or Y

            public Emitter<int> RelayFromX { get; private set; } // relays from ReceiverX

            public Emitter<int> RelayFromY { get; private set; } // relays from ReceiverY

            public Emitter<int> RelayFromZ { get; private set; } // relays from ReceiverY

            public Emitter<int> Generator { get; private set; } // emits at 10ms intervals
        }
    }
}
