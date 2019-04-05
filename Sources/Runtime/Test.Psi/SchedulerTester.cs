// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
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
            scheduler.Start(new Clock(), false);
            scheduler.PauseForQuiescence();
            scheduler.ResumeAfterQuiescence();
            scheduler.Stop();
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
                p.Run();
            }

            Assert.IsTrue(results.Count == 10);
            Assert.IsFalse(results.Any(l => l < 0));
        }

        // validate that items scheduled in the future get delivered imediately when the clock is not enforced
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
                p.Run(enforceReplayClock: false);
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
                p.Run(enableExceptionHandling: true);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ErrorHandlingTest()
        {
            using (var p = Pipeline.Create())
            {
                p.PipelineCompleted += (o, e) => Console.WriteLine("Error handled");
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
                        p.Run(enableExceptionHandling: true);
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
            var currentTime = DateTime.Now;
            var timerDelegate = new Time.TimerDelegate(
                (timerID, msg, userCtx, dw1, dw2) =>
                {
                    currentTime = Time.GetCurrentTime();

                    // currentTime += TimeSpan.FromMilliseconds(timerInterval);
                });
            var timer = Platform.Specific.TimerStart(timerInterval, timerDelegate);

            Console.ReadLine();
            timer.Stop();
            var lastTime = DateTime.Now;

            Console.WriteLine($"Current = {currentTime:H:mm:ss fff}, last = {lastTime:H:mm:ss fff}");
        }

        // [TestMethod, Timeout(60000)]
        public void TimerHighRateTest()
        {
            var lastTime = DateTime.Now;
            var currentTime = DateTime.Now;
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
        public void Stop()
        {
        }
    }
}
