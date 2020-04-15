// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Runs a series of tests for stream generators.
    /// </summary>
    [TestClass]
    public class GeneratorsTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void RepeatWithClockEnforcement()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;
            DateTime lastMessageTime = DateTime.MinValue;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 31, 5, TimeSpan.FromMilliseconds(50)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                        lastMessageTime = e.Time;
                    });
                pipeline.Run(null, enforceReplayClock: true);

                // capture pipeline start time for later
                startTime = pipeline.StartTime;
            }

            // verify output values and times
            var expectedValues = Enumerable.Repeat(31, 5).ToList();
            var expectedTimes = Enumerable.Range(0, 5).Select(x => startTime.AddMilliseconds(x * 50)).ToList();
            CollectionAssert.AreEqual(expectedValues, values);
            CollectionAssert.AreEqual(expectedTimes, originatingTimes);

            // Verify that pipeline is played back at real speed. The creation time of the last
            // message should be at or after its originating time.
            Assert.IsTrue(lastMessageTime >= originatingTimes.Last());
        }


        [TestMethod]
        [Timeout(60000)]
        public void RepeatNoClockEnforcement()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;
            DateTime lastMessageTime = DateTime.MinValue;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 31, 5, TimeSpan.FromMilliseconds(50)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                        lastMessageTime = e.Time;
                    });
                pipeline.Run(null, enforceReplayClock: false);

                // capture pipeline start time for later
                startTime = pipeline.StartTime;
            }

            // verify output values and times
            var expectedValues = Enumerable.Repeat(31, 5).ToList();
            var expectedTimes = Enumerable.Range(0, 5).Select(x => startTime.AddMilliseconds(x * 50)).ToList();
            CollectionAssert.AreEqual(expectedValues, values);
            CollectionAssert.AreEqual(expectedTimes, originatingTimes);

            // Verify that pipeline is played back at real speed. The creation time of the last
            // message should be before its originating time.
            Assert.IsTrue(lastMessageTime < originatingTimes.Last());
        }

        [TestMethod]
        [Timeout(60000)]
        public void RepeatMinInterval()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;

            using (var pipeline = Pipeline.Create())
            {
                // one tick is the smallest allowed interval
                Generators.Repeat(pipeline, 13, 5, TimeSpan.FromTicks(1)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                    });
                pipeline.Run();

                // capture pipeline start time for later
                startTime = pipeline.StartTime;
            }

            // verify output values and times
            var expectedValues = Enumerable.Repeat(13, 5).ToList();
            var expectedTimes = Enumerable.Range(0, 5).Select(x => startTime.AddTicks(x)).ToList();
            CollectionAssert.AreEqual(expectedValues, values);
            CollectionAssert.AreEqual(expectedTimes, originatingTimes);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(AggregateException))]
        public void RepeatIllegalInterval()
        {
            using (var pipeline = Pipeline.Create())
            {
                // zero intervals are illegal
                Generators.Repeat(pipeline, 13, 5, TimeSpan.Zero);

                try
                {
                    pipeline.Run();
                }
                catch (AggregateException ae)
                {
                    // pipeline containing a Generators.Repeat with an illegal interval should throw
                    Assert.IsInstanceOfType(ae.InnerException, typeof(InvalidOperationException));
                    throw;
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SequenceNoStartTime()
        {
            var values = new List<int>();
            var originatingTimes = new List<long>();
            var messageTimes = new List<long>();
            var utcNowTicks = DateTime.UtcNow.Ticks;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Sequence(
                    pipeline,
                    new[]
                    {
                        (0, new DateTime(1000)),
                        (1, new DateTime(1001)),
                        (2, new DateTime(1002)),
                        (3, new DateTime(1003)),
                    }).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime.Ticks);
                        messageTimes.Add(e.Time.Ticks);
                    });
                pipeline.Run();
                Assert.AreEqual(utcNowTicks, pipeline.StartTime.Ticks, TimeSpan.TicksPerSecond);
            }

            // verify output values and times
            CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3 }, values);
            CollectionAssert.AreEqual(new long[] { 1000, 1001, 1002, 1003 }, originatingTimes);

            // verify message latencies are of the expected magnitude (i.e. extremely large)
            Assert.IsTrue(messageTimes[0] - originatingTimes[0] > utcNowTicks);
            Assert.IsTrue(messageTimes[1] - originatingTimes[1] > utcNowTicks);
            Assert.IsTrue(messageTimes[2] - originatingTimes[2] > utcNowTicks);
            Assert.IsTrue(messageTimes[3] - originatingTimes[3] > utcNowTicks);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SequenceWithStartTime()
        {
            var values = new List<int>();
            var originatingTimes = new List<long>();
            var messageTimes = new List<long>();

            using (var pipeline = Pipeline.Create())
            {
                Generators.Sequence(
                    pipeline,
                    new[]
                    {
                        (0, new DateTime(1000)),
                        (1, new DateTime(1001)),
                        (2, new DateTime(1002)),
                        (3, new DateTime(1003)),
                    },
                    new DateTime(1000)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime.Ticks);
                        messageTimes.Add(e.Time.Ticks);
                    });
                pipeline.Run();
                Assert.AreEqual(1000, pipeline.StartTime.Ticks);
            }

            // verify output values and times
            CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3 }, values);
            CollectionAssert.AreEqual(new long[] { 1000, 1001, 1002, 1003 }, originatingTimes);

            // verify message latencies are of the expected magnitude (i.e. small)
            Assert.IsTrue(messageTimes[0] - originatingTimes[0] < (100 * TimeSpan.TicksPerMillisecond));
            Assert.IsTrue(messageTimes[1] - originatingTimes[1] < (100 * TimeSpan.TicksPerMillisecond));
            Assert.IsTrue(messageTimes[2] - originatingTimes[2] < (100 * TimeSpan.TicksPerMillisecond));
            Assert.IsTrue(messageTimes[3] - originatingTimes[3] < (100 * TimeSpan.TicksPerMillisecond));
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        [Timeout(60000)]
        public void SequenceOutOfOrder()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Sequence(
                    pipeline,
                    new[]
                    {
                        // timestamps that are not monotonically increasing are illegal
                        (0, new DateTime(1000)),
                        (1, new DateTime(1001)),
                        (2, new DateTime(1003)),
                        (3, new DateTime(1002)),
                    });

                try
                {
                    pipeline.Run();
                }
                catch (AggregateException ae)
                {
                    // pipeline containing a Generators.Sequence with out of order timestamps should throw
                    Assert.IsInstanceOfType(ae.InnerException, typeof(InvalidOperationException));
                    throw;
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FiniteSequence()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Sequence(pipeline, 0, x => x + 1, 10, TimeSpan.FromMilliseconds(1)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                    });
                pipeline.RunAsync();

                // capture pipeline start time for later
                startTime = pipeline.StartTime;

                // pipeline containing a finite Generators.Sequence should stop
                var stopped = pipeline.WaitAll(500);
                Assert.IsTrue(stopped);
            }

            // verify output values and times
            var expectedValues = Enumerable.Range(0, 10).ToList();
            var expectedTimes = Enumerable.Range(0, 10).Select(x => startTime.AddMilliseconds(x)).ToList();
            CollectionAssert.AreEqual(expectedValues, values);
            CollectionAssert.AreEqual(expectedTimes, originatingTimes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void InfiniteSequence()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;

            // pipeline containing an infinite Generators.Sequence should not stop
            using (var pipeline = Pipeline.Create())
            {
                Generators.Sequence(pipeline, 0, x => x + 1, TimeSpan.FromMilliseconds(1)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                    });
                pipeline.RunAsync();

                // capture pipeline start time for later
                startTime = pipeline.StartTime;

                // pipeline containing an infinite Generators.Sequence should not stop
                var stopped = pipeline.WaitAll(500);
                Assert.IsFalse(stopped);
            }

            Assert.IsTrue(values.Count >= 400); // ensure we are in the ballpark in case the pipeline runs slow

            // verify output values and times
            var expectedValues = Enumerable.Range(0, 400).ToList();
            var expectedTimes = Enumerable.Range(0, 400).Select(x => startTime.AddMilliseconds(x)).ToList();
            CollectionAssert.AreEqual(expectedValues, values.Take(400).ToList());
            CollectionAssert.AreEqual(expectedTimes, originatingTimes.Take(400).ToList());
        }

        [TestMethod]
        [Timeout(60000)]
        public void Once()
        {
            int value = 0;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Once(pipeline, 123).Do(x => value = x);
                pipeline.RunAsync();

                // pipeline containing Generators.Once should not stop once post happens
                var stopped = pipeline.WaitAll(500);
                Assert.IsFalse(stopped);
            }

            // verify value was posted
            Assert.AreEqual(123, value);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Return()
        {
            int value = 0;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123).Do(x => value = x);
                pipeline.RunAsync();

                // pipeline containing Generators.Return should stop once post happens
                var stopped = pipeline.WaitAll(500);
                Assert.IsTrue(stopped);
            }

            // verify value was posted
            Assert.AreEqual(123, value);
        }

        [TestMethod]
        [Timeout(60000)]
        public void FiniteRepeat()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 123, 10, TimeSpan.FromMilliseconds(1)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                    });
                pipeline.RunAsync();

                // capture pipeline start time for later
                startTime = pipeline.StartTime;

                // pipeline containing a finite Generators.Repeat should stop
                var stopped = pipeline.WaitAll(500);
                Assert.IsTrue(stopped);
            }

            // verify output values and times
            var expectedValues = Enumerable.Repeat(123, 10).ToList();
            var expectedTimes = Enumerable.Range(0, 10).Select(x => startTime.AddMilliseconds(x)).ToList();
            CollectionAssert.AreEqual(expectedValues, values);
            CollectionAssert.AreEqual(expectedTimes, originatingTimes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void InfiniteRepeat()
        {
            var values = new List<int>();
            var originatingTimes = new List<DateTime>();
            DateTime startTime;

            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 123, TimeSpan.FromMilliseconds(1)).Do(
                    (x, e) =>
                    {
                        values.Add(x);
                        originatingTimes.Add(e.OriginatingTime);
                    });
                pipeline.RunAsync();

                // capture pipeline start time for later
                startTime = pipeline.StartTime;

                // pipeline containing an infinite Generators.Repeat should not stop
                var stopped = pipeline.WaitAll(500);
                Assert.IsFalse(stopped);
            }

            // verify output values and times
            var expectedValues = Enumerable.Repeat(123, 400).ToList();
            var expectedTimes = Enumerable.Range(0, 400).Select(x => startTime.AddMilliseconds(x)).ToList();
            CollectionAssert.AreEqual(expectedValues, values.Take(400).ToList());
            CollectionAssert.AreEqual(expectedTimes, originatingTimes.Take(400).ToList());
        }

        [TestMethod]
        [Timeout(60000)]
        public void FiniteAndInfiniteGenerators()
        {
            // pipeline containing Generators.Return and Generators.Once should stop, b/c Return stops.
            using (var pipeline = Pipeline.Create())
            {
                Generators.Once(pipeline, 123);
                Generators.Return(pipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(500);
                Assert.IsTrue(stopped);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FiniteGeneratorWithKeepOpen()
        {
            // pipeline containing Generators.Return with keepOpen = true should not stop
            using (var pipeline = Pipeline.Create())
            {
                Generators.Once(pipeline, 123);
                Generators.Repeat(pipeline, 123, 1, default, keepOpen: true);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(500);
                Assert.IsFalse(stopped);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void InfiniteGeneratorWithReplayDescriptor()
        {
            // pipeline containing infinite Generators.Repeat should still respect replay descriptor
            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 123, TimeSpan.FromMilliseconds(1));
                pipeline.RunAsync(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(100));
                var stopped = pipeline.WaitAll(500);
                Assert.IsTrue(stopped);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void AlignedSequenceTest()
        {
            using (var p = Pipeline.Create())
            {
                // align with a time that is < start time
                var gen = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10), DateTime.MinValue);
                var ticksAlign = TimeSpan.FromMilliseconds(10).Ticks;
                gen.Do((x, e) =>
                {
                    Assert.AreEqual(e.OriginatingTime.TimeOfDay.Ticks % ticksAlign, 0);
                });
                p.Run();
            }

            using (var p = Pipeline.Create())
            {
                // align with a time that is > end time
                var gen2 = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10), DateTime.MaxValue);
                var ticksAlign = TimeSpan.FromMilliseconds(10).Ticks;
                gen2.Do((x, e) =>
                {
                    Assert.AreEqual(e.OriginatingTime.Ticks % ticksAlign, 99999);
                });
                p.Run();
            }

            using (var p = Pipeline.Create())
            {
                var startTime = DateTime.UtcNow;

                // align with a time that is < start time and already aligned with replay start time
                var gen1 = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10), startTime.AddMilliseconds(-10));
                var expectedTime = startTime;
                gen1.Do((x, e) =>
                {
                    Assert.AreEqual(expectedTime, e.OriginatingTime);
                    expectedTime = expectedTime.AddMilliseconds(10);
                });
                p.Run(startTime);
            }

            using (var p = Pipeline.Create())
            {
                var startTime = DateTime.UtcNow;

                // align with a time that is > end time and already aligned with replay start time
                var gen2 = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10), startTime.AddMilliseconds(10));
                var expectedTime = startTime;
                gen2.Do((x, e) =>
                {
                    Assert.AreEqual(expectedTime, e.OriginatingTime);
                    expectedTime = expectedTime.AddMilliseconds(10);
                });
                p.Run(startTime);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Enumerator()
        {
            var sequence = new List<(int, DateTime)> { (1, new DateTime(1)), (2, new DateTime(2)), (3, new DateTime(3)) };

            using (var enumerator = new Generator<int>.Enumerator(sequence.GetEnumerator()))
            {
                // test ability to get the first value before enumerating
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(default, enumerator.Current.time);
                Assert.AreEqual(1, enumerator.Next.value);
                Assert.AreEqual(1, enumerator.Next.time.Ticks);

                // test invariance of current and next
                Assert.AreEqual(1, enumerator.Next.value);
                Assert.AreEqual(1, enumerator.Next.time.Ticks);
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(default, enumerator.Current.time);

                // move to the first value and check the next value
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(1, enumerator.Current.value);
                Assert.AreEqual(1, enumerator.Current.time.Ticks);
                Assert.AreEqual(2, enumerator.Next.value);
                Assert.AreEqual(2, enumerator.Next.time.Ticks);

                // move to the second value and check the next value
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(3, enumerator.Next.value);
                Assert.AreEqual(3, enumerator.Next.time.Ticks);
                Assert.AreEqual(2, enumerator.Current.value);
                Assert.AreEqual(2, enumerator.Current.time.Ticks);

                // move to the last value - next value should be the end value
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(3, enumerator.Current.value);
                Assert.AreEqual(3, enumerator.Current.time.Ticks);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);

                // move past the end - current and next values should be the end value
                Assert.IsFalse(enumerator.MoveNext());
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Current.time);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);

                // test invariance at the end of the enumerator
                Assert.IsFalse(enumerator.MoveNext());
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Current.time);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);

                // reset to beginning
                enumerator.Reset();
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(default, enumerator.Current.time);
                Assert.AreEqual(1, enumerator.Next.value);
                Assert.AreEqual(1, enumerator.Next.time.Ticks);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EnumeratorEmpty()
        {
            var sequence = new List<(int, DateTime)>();

            using (var enumerator = new Generator<int>.Enumerator(sequence.GetEnumerator()))
            {
                // current value is default, next value is end value
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(default, enumerator.Current.time.Ticks);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);

                // move past the end - current and next values should be the end value
                Assert.IsFalse(enumerator.MoveNext());
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Current.time);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);

                // reset to beginning
                enumerator.Reset();
                Assert.AreEqual(default, enumerator.Current.value);
                Assert.AreEqual(default, enumerator.Current.time.Ticks);
                Assert.AreEqual(default, enumerator.Next.value);
                Assert.AreEqual(DateTime.MaxValue, enumerator.Next.time);
            }
        }
    }
}
