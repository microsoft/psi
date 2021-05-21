// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Scheduling;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SchedulerTester : ISourceComponent
    {
        [TestMethod]
        [Timeout(60000)]
        public void StartStopTest()
        {
            var scheduler = new Scheduler(error => { throw new AggregateException(error); });
            var context = new SchedulerContext();
            scheduler.Start(new Clock(), false);
            scheduler.StartScheduling(context);
            scheduler.PauseForQuiescence(context);
            scheduler.StopScheduling(context);
        }

        [TestMethod]
        [Timeout(60000)]
        public void StartStopOrderingTest()
        {
            // Run test for a range of scheduler thread counts, including 1. This prevents the
            // output from appearing in the correct order by fluke due to concurrent scheduling.
            for (int maxThreads = Environment.ProcessorCount * 2; maxThreads > 0; maxThreads >>= 1)
            {
                var cLog = new ConcurrentQueue<string>();

                using (var p = Pipeline.Create("P", null, maxThreads))
                {
                    var a = new TestSourceComponent(p, "A", cLog);
                    var b = new TestSourceComponent(p, "B", cLog);
                    var c = new TestSourceComponent(p, "C", cLog);

                    a.Emitter.Do(m => cLog.Enqueue(m));
                    b.Emitter.Do(m => cLog.Enqueue(m));
                    c.Emitter.Do(m => cLog.Enqueue(m));

                    Generators.Repeat(p, "PGen", 10, TimeSpan.FromTicks(1)).Do(m => cLog.Enqueue(m));

                    p.Run();
                }

                var log = cLog.ToList();
                log.ForEach(m => Console.WriteLine(m));

                // all components should have started
                Assert.IsTrue(log.Contains("AStart"));
                Assert.IsTrue(log.Contains("BStart"));
                Assert.IsTrue(log.Contains("CStart"));

                // messages should only be delivered after all components have started
                int latestStartIndex = new[] { log.IndexOf("AStart"), log.IndexOf("BStart"), log.IndexOf("CStart") }.Max();
                Assert.IsTrue(latestStartIndex < log.IndexOf("APostFromStart"));
                Assert.IsTrue(latestStartIndex < log.IndexOf("BPostFromStart"));
                Assert.IsTrue(latestStartIndex < log.IndexOf("CPostFromStart"));
                Assert.IsTrue(latestStartIndex < log.IndexOf("PGen"));

                // all components should be stopped after they were started
                Assert.IsTrue(log.IndexOf("AStart") < log.IndexOf("AStop"));
                Assert.IsTrue(log.IndexOf("BStart") < log.IndexOf("BStop"));
                Assert.IsTrue(log.IndexOf("CStart") < log.IndexOf("CStop"));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void StartStopOrderingTestWithSubpipelines()
        {
            // Run test for a range of scheduler thread counts, including 1. This prevents the
            // output from appearing in the correct order by fluke due to concurrent scheduling.
            for (int maxThreads = Environment.ProcessorCount * 2; maxThreads > 0; maxThreads >>= 1)
            {
                var cLog = new ConcurrentQueue<string>();

                using (var p = Pipeline.Create("P", null, maxThreads))
                using (var q = Subpipeline.Create(p, "Q"))
                using (var r = Subpipeline.Create(q, "R"))
                {
                    var a = new TestSourceComponent(p, "A", cLog);
                    var b = new TestSourceComponent(p, "B", cLog);
                    var c = new TestSourceComponent(p, "C", cLog);
                    var d = new TestSourceComponent(q, "D", cLog);
                    var e = new TestSourceComponent(q, "E", cLog);
                    var f = new TestSourceComponent(q, "F", cLog);
                    var g = new TestSourceComponent(r, "G", cLog);
                    var h = new TestSourceComponent(r, "H", cLog);
                    var i = new TestSourceComponent(r, "I", cLog);

                    a.Emitter.Do(m => cLog.Enqueue(m));
                    b.Emitter.Do(m => cLog.Enqueue(m));
                    c.Emitter.Do(m => cLog.Enqueue(m));
                    d.Emitter.Do(m => cLog.Enqueue(m));
                    e.Emitter.Do(m => cLog.Enqueue(m));
                    f.Emitter.Do(m => cLog.Enqueue(m));
                    g.Emitter.Do(m => cLog.Enqueue(m));
                    h.Emitter.Do(m => cLog.Enqueue(m));
                    i.Emitter.Do(m => cLog.Enqueue(m));

                    Generators.Repeat(p, "PGen", 10, TimeSpan.FromTicks(1)).Do(m => cLog.Enqueue(m));
                    Generators.Repeat(q, "QGen", 10, TimeSpan.FromTicks(1)).Do(m => cLog.Enqueue(m));
                    Generators.Repeat(r, "RGen", 10, TimeSpan.FromTicks(1)).Do(m => cLog.Enqueue(m));

                    p.Run();
                }

                var log = cLog.ToList();
                log.ForEach(m => Console.WriteLine(m));

                // all components should have started
                Assert.IsTrue(log.Contains("AStart"));
                Assert.IsTrue(log.Contains("BStart"));
                Assert.IsTrue(log.Contains("CStart"));
                Assert.IsTrue(log.Contains("DStart"));
                Assert.IsTrue(log.Contains("EStart"));
                Assert.IsTrue(log.Contains("FStart"));
                Assert.IsTrue(log.Contains("GStart"));
                Assert.IsTrue(log.Contains("HStart"));
                Assert.IsTrue(log.Contains("IStart"));

                int pLatestStartIndex = new[] { log.IndexOf("AStart"), log.IndexOf("BStart"), log.IndexOf("CStart") }.Max();
                int qLatestStartIndex = new[] { log.IndexOf("DStart"), log.IndexOf("EStart"), log.IndexOf("FStart") }.Max();
                int rLatestStartIndex = new[] { log.IndexOf("GStart"), log.IndexOf("HStart"), log.IndexOf("IStart") }.Max();

                // messages should only be delivered after all components have started within their respective pipelines
                Assert.IsTrue(pLatestStartIndex < log.IndexOf("APostFromStart"));
                Assert.IsTrue(pLatestStartIndex < log.IndexOf("BPostFromStart"));
                Assert.IsTrue(pLatestStartIndex < log.IndexOf("CPostFromStart"));
                Assert.IsTrue(pLatestStartIndex < log.IndexOf("PGen"));
                Assert.IsTrue(qLatestStartIndex < log.IndexOf("DPostFromStart"));
                Assert.IsTrue(qLatestStartIndex < log.IndexOf("EPostFromStart"));
                Assert.IsTrue(qLatestStartIndex < log.IndexOf("FPostFromStart"));
                Assert.IsTrue(qLatestStartIndex < log.IndexOf("QGen"));
                Assert.IsTrue(rLatestStartIndex < log.IndexOf("GPostFromStart"));
                Assert.IsTrue(rLatestStartIndex < log.IndexOf("HPostFromStart"));
                Assert.IsTrue(rLatestStartIndex < log.IndexOf("IPostFromStart"));
                Assert.IsTrue(rLatestStartIndex < log.IndexOf("RGen"));

                // all components should be stopped after they were started
                Assert.IsTrue(log.IndexOf("AStart") < log.IndexOf("AStop"));
                Assert.IsTrue(log.IndexOf("BStart") < log.IndexOf("BStop"));
                Assert.IsTrue(log.IndexOf("CStart") < log.IndexOf("CStop"));
                Assert.IsTrue(log.IndexOf("DStart") < log.IndexOf("DStop"));
                Assert.IsTrue(log.IndexOf("EStart") < log.IndexOf("EStop"));
                Assert.IsTrue(log.IndexOf("FStart") < log.IndexOf("FStop"));
                Assert.IsTrue(log.IndexOf("GStart") < log.IndexOf("GStop"));
                Assert.IsTrue(log.IndexOf("HStart") < log.IndexOf("HStop"));
                Assert.IsTrue(log.IndexOf("IStart") < log.IndexOf("IStop"));
            }
        }

        // Test source component that posts from its start method
        private class TestSourceComponent : ISourceComponent
        {
            private readonly Pipeline pipeline;
            private readonly string name;
            private readonly ConcurrentQueue<string> log;

            public TestSourceComponent(Pipeline pipeline, string name, ConcurrentQueue<string> log)
            {
                this.pipeline = pipeline;
                this.name = name;
                this.log = log;
                this.Emitter = pipeline.CreateEmitter<string>(this, $"{name}Emitter");
            }

            public Emitter<string> Emitter { get; private set; }

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                var startTime = this.pipeline.GetCurrentTime();
                this.log.Enqueue($"{this.name}Start");
                this.Emitter.Post($"{this.name}PostFromStart", startTime);

                // Component doesn't post anything (other than the PostFromStart message), so we can
                // notify its completion time as the originating time of the PostFromStart message.
                // This also ensures that the pipeline is kept running until this message is delivered.
                notifyCompletionTime(startTime);
            }

            public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
            {
                this.log.Enqueue($"{this.name}Stop");
                notifyCompleted();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void BasicTest()
        {
            int total = 0;
            using (var p = Pipeline.Create())
            {
                var d = p.CreateReceiver<int>(this, t => { total += t.Data; }, "receiver");
                var e = p.CreateEmitter<int>(this, "emitter");
                e.PipeTo(d, DeliveryPolicy.Unlimited);

                p.RunAsync();

                // queue some messages
                for (int i = 0; i < 10; i++)
                {
                    e.Post(i, Time.GetCurrentTime());
                }
            }

            Assert.AreEqual(45, total);
        }

        // validate that items scheduled in the future don't get delivered too soon when the clock is enforced
        [TestMethod]
        [Timeout(60000)]
        public void FutureSchedulingWithClockEnforcement()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var results = new List<long>();
            using (var p = Pipeline.Create())
            {
                var generator = Generators.Sequence(p, 0, i => i++, 10, delay)
                    .Do((i, e) => results.Add(p.GetCurrentTime().Ticks - e.OriginatingTime.Ticks));
                p.Run(null, enforceReplayClock: true);
            }

            Assert.IsTrue(results.Count == 10);
            Assert.IsFalse(results.Any(l => l < 0));
        }

        // validate that items scheduled in the future get delivered immediately when the clock is not enforced
        [TestMethod]
        [Timeout(60000)]
        public void FutureSchedulingWithoutClockEnforcement()
        {
            var delay = TimeSpan.FromMilliseconds(10000); // a large value, to prove that the scheduler will not wait for it
            var results = new List<long>();
            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 2, TimeSpan.FromSeconds(10)); // hold pipeline open
                var generator = Generators.Sequence(p, 0, i => i++, 10, delay)
                    .Do((i, e) => results.Add(p.GetCurrentTime().Ticks - e.OriginatingTime.Ticks));
                p.Run(null, enforceReplayClock: false);
            }

            Assert.IsTrue(results.Count == 10);
            Assert.IsFalse(results.Skip(1).Any(l => l > 0));
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(AggregateException))]
        public void ErrorPropagationTest()
        {
            using (var p = Pipeline.Create())
            {
                Generators.Return(p, 1).Select(i => i / 0);
                p.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ErrorHandlingTest()
        {
            using (var p = Pipeline.Create())
            {
                p.PipelineExceptionNotHandled += (o, e) => Console.WriteLine("Error handled");
                Generators.Return(p, 1).Select(i => i / 0); // shouldn't throw because of completion event handler
                p.RunAsync();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ErrorHandlingWithSubpipelineTest()
        {
            using (var p = Pipeline.Create("root"))
            {
                using (var s = Subpipeline.Create(p, "sub"))
                {
                    var caughtException = false;
                    try
                    {
                        Generators.Return(p, 1).Select(i => i / 0);
                        p.Run();
                    }
                    catch (AggregateException exception)
                    {
                        caughtException = true;
                        if (exception.InnerException == null)
                        {
                            Assert.Fail("AggregateException contains no inner exception");
                        }
                        else
                        {
                            Assert.IsInstanceOfType(exception.InnerException, typeof(DivideByZeroException), "Unexpected inner exception type: {0}", exception.InnerException.GetType().ToString());
                        }
                    }

                    Assert.IsTrue(caughtException);
                }
            }
        }

        // [TestMethod, Timeout(60000)]
        public void TimerPerfTest()
        {
            uint timerInterval = 1;
            var currentTime = DateTime.UtcNow;
            var timerDelegate = new Time.TimerDelegate(
                (timerID, msg, userCtx, dw1, dw2) =>
                {
                    currentTime = Time.GetCurrentTime();

                    // currentTime += TimeSpan.FromMilliseconds(timerInterval);
                });
            var timer = Platform.Specific.TimerStart(timerInterval, timerDelegate);

            Console.ReadLine();
            timer.Stop();
            var lastTime = DateTime.UtcNow;

            Console.WriteLine($"Current = {currentTime:H:mm:ss fff}, last = {lastTime:H:mm:ss fff}");
        }

        // [TestMethod, Timeout(60000)]
        public void TimerHighRateTest()
        {
            var lastTime = DateTime.UtcNow;
            DateTime currentTime;
            int count = 0;
            double sum = 0;
            while (true)
            {
                currentTime = Time.GetCurrentTime();
                sum += currentTime.Ticks - lastTime.Ticks;
                count++;
                if (sum >= 100000)
                {
                    Console.WriteLine(sum / count);
                    count = 0;
                    sum = 0;
                }

                lastTime = currentTime;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }
    }
}
