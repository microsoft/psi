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
                p.PipelineCompletionEvent += ((_, __) => completed = true);
                p.RunAsync();
                Thread.Sleep(200);
                Assert.IsFalse(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            }

            Assert.IsTrue(completed); // note that infinite source composite-component subpipeline never completes (parent pipeline must be disposed explicitly)
            Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, results.AsEnumerable().Take(3))); // compare first few only
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
                // the underlying Sequence component is an IFiniteSourceComponent and requires RegisterPipelineStartHandler
                Generators.Range(p, 0, 10);
            }
        }
    }
}
