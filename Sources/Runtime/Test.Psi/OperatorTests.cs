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
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OperatorTests
    {
        private event EventHandler<int> EventSourceTestEvent;

        [TestMethod]
        [Timeout(60000)]
        public void DelayOperator()
        {
            List<DateTime> results = new List<DateTime>();
            List<DateTime> delayedResults = new List<DateTime>();
            int resultCount = 11;

            using (var p = Pipeline.Create("test"))
            {
                Generators.Range(p, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var source = Generators.Range(p, 0, resultCount, TimeSpan.FromTicks(10));
                var delayedSource = source.Delay(TimeSpan.FromMilliseconds(50));

                // Capture times of source and delayed streams (originating times are supposed to be the same)
                Operators.Do(delayedSource, (d, e) =>
                {
                    results.Add(e.OriginatingTime);
                    delayedResults.Add(e.CreationTime);
                });

                p.Run();
            }

            Assert.AreEqual(resultCount, results.Count);
            Assert.AreEqual(resultCount, delayedResults.Count);

            for (int i = 0; i < resultCount; ++i)
            {
                // Check that delayed results are delayed by 50 ms from original
                Assert.IsTrue((delayedResults[i] - results[i]).TotalMilliseconds >= 50);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void RepeatTest()
        {
            using (var pipeline = Pipeline.Create(nameof(this.RepeatTest)))
            {
                var startTime = DateTime.UtcNow;

                // clock for repeater contains 3 values at 0, 500 and 1000 ms
                var clock = Generators.Range(pipeline, 0, 3, TimeSpan.FromMilliseconds(500));

                // input stream contains 2 values { 250, 750 } at 250 ms and 750 ms
                var input1 = Generators.Sequence(pipeline, new[] { (250, startTime.AddMilliseconds(250)), (750, startTime.AddMilliseconds(750)) });
                var output1 = clock.Pair(input1, -1).Item2().Select((x, e) => (x, e.OriginatingTime)).ToObservable().ToListObservable();
                var output1_NoInitialValue = clock.Pair(input1).Item2().Select((x, e) => (x, e.OriginatingTime)).ToObservable().ToListObservable();

                // input stream contains one value { 750 } at 750 ms
                var input2 = Generators.Sequence(pipeline, new[] { (750, startTime.AddMilliseconds(750)) });
                var output2 = clock.Pair(input2, -1).Item2().Select((x, e) => (x, e.OriginatingTime)).ToObservable().ToListObservable();
                var output2_NoInitialValue = clock.Pair(input2).Item2().Select((x, e) => (x, e.OriginatingTime)).ToObservable().ToListObservable();

                pipeline.Run(new ReplayDescriptor(startTime));

                // check initial value (-1) is output at 0 ms
                CollectionAssert.AreEqual(new[] { -1, 250, 750 }, output1.Select(o => o.Item1).ToList());
                CollectionAssert.AreEqual(new[] { startTime, startTime.AddMilliseconds(500), startTime.AddMilliseconds(1000) }, output1.Select(o => o.Item2).ToList());

                // check initial value is not used and the 2 input values are output with the last 2 originating times of the input stream
                CollectionAssert.AreEqual(new[] { 250, 750 }, output1_NoInitialValue.Select(o => o.Item1).ToList());
                CollectionAssert.AreEqual(new[] { startTime.AddMilliseconds(500), startTime.AddMilliseconds(1000) }, output1_NoInitialValue.Select(o => o.Item2).ToList());

                // check initial value (-1) is output at 0 ms and 500 ms
                CollectionAssert.AreEqual(new[] { -1, -1, 750 }, output2.Select(o => o.Item1).ToList());
                CollectionAssert.AreEqual(new[] { startTime, startTime.AddMilliseconds(500), startTime.AddMilliseconds(1000) }, output2.Select(o => o.Item2).ToList());

                // check initial value is not used and only one value is output with the last originating time of the input stream
                CollectionAssert.AreEqual(new[] { 750 }, output2_NoInitialValue.Select(o => o.Item1).ToList());
                CollectionAssert.AreEqual(new[] { startTime.AddMilliseconds(1000) }, output2_NoInitialValue.Select(o => o.Item2).ToList());
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EventSource()
        {
            List<double> results = new List<double>();

            using (var p = Pipeline.Create("test"))
            {
                var eventSource = new EventSource<EventHandler<int>, double>(
                        p,
                        handler => this.EventSourceTestEvent += handler,
                        handler => this.EventSourceTestEvent -= handler,
                        post => new EventHandler<int>((sender, e) => post(e / 10.0)));
                eventSource.Out.Do(f => results.Add(f));
                p.RunAsync();

                for (var i = 0; i < 10; i++)
                {
                    this.EventSourceTestEvent.Invoke(this, i);
                }
            }

            Assert.AreEqual(10, results.Count);
            CollectionAssert.AreEqual(new double[] { 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 }, results);
            Assert.IsNull(this.EventSourceTestEvent);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ObservableStream()
        {
            ListObservable<int> results0, results1;
            var expected = new int[] { 0, 1, 2, 3, 4, 5, 6 };

            // test simple single subscriber
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                results0 = range.ToObservable().ToListObservable(); // terminating (given `pipeline`)
                results1 = range.ToObservable().Take(7).ToListObservable(); // non-terminating (hence, `Take(7)`)
                pipeline.Run();
            }

            Assert.IsTrue(Enumerable.SequenceEqual(results0.AsEnumerable(), expected));
            Assert.IsTrue(Enumerable.SequenceEqual(results1.AsEnumerable(), expected));

            // test multiple subscribers
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var obs = range.ToObservable(); // single observable
                results0 = obs.ToListObservable(); // subscribe once
                results1 = obs.ToListObservable(); // subscribe twice
                pipeline.Run();
            }

            Assert.IsTrue(Enumerable.SequenceEqual(results0.AsEnumerable(), expected));
            Assert.IsTrue(Enumerable.SequenceEqual(results1.AsEnumerable(), expected));

            // test unsubscribe
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var obs = range.ToObservable();
                Assert.IsFalse(((Operators.StreamObservable<int>)obs).HasSubscribers);
                var sub = obs.Subscribe();
                Assert.IsTrue(((Operators.StreamObservable<int>)obs).HasSubscribers);
                sub.Dispose(); // unsubscribe
                Assert.IsFalse(((Operators.StreamObservable<int>)obs).HasSubscribers);
                results0 = obs.ToListObservable(); // implicit subscribe
                Assert.IsTrue(((Operators.StreamObservable<int>)obs).HasSubscribers);
                pipeline.Run();
            }

            Assert.IsTrue(Enumerable.SequenceEqual(results0.AsEnumerable(), expected));

            // test errors
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, -5, 7, TimeSpan.FromTicks(1)); // note -5 .. +1
                results0 = range.ToObservable().Select(x => 10 / x).ToListObservable(); // divide by zero!
                results1 = range.ToObservable().Select(x => 10 / x).OnErrorResumeNext(Observable.Empty<int>()).ToListObservable(); // terminate without exception
                pipeline.Run();
            }

            bool threwDivByZero = false;
            try
            {
                Assert.IsTrue(Enumerable.SequenceEqual(results0.AsEnumerable(), expected));
            }
            catch (DivideByZeroException)
            {
                threwDivByZero = true;
            }

            Assert.IsTrue(threwDivByZero);
            Assert.IsTrue(Enumerable.SequenceEqual(results1.AsEnumerable(), new int[] { -2, -2, -3, -5, -10 })); // truncated sequence
        }

        [TestMethod]
        [Timeout(60000)]
        public void Count()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var simpleCount = range.Count().ToObservable().ToListObservable();
                var conditionalCount = range.Count(x => x % 2 == 0).ToObservable().ToListObservable();
                var simpleLongCount = range.LongCount().ToObservable().ToListObservable();
                var conditionalLongCount = range.LongCount(x => x % 2L == 0L).ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3, 4, 5, 6, 7 }, simpleCount.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3, 4 }, conditionalCount.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 1L, 2L, 3L, 4L, 5L, 6L, 7L }, simpleLongCount.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 1L, 2L, 3L, 4L }, conditionalLongCount.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Sum()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var intSum = intRange.Sum().ToObservable().ToListObservable(); // sum 0 1 2 3 4 5 6
                var conditionalIntSum = intRange.Sum(x => x % 2 == 0).ToObservable().ToListObservable(); // sum 0 2 4 6

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (long)x);
                var longSum = longRange.Sum().ToObservable().ToListObservable(); // sum 0 1 2 3 4 5 6
                var conditionalLongSum = longRange.Sum(x => x % 2L == 0L).ToObservable().ToListObservable(); // sum 0 2 4 6

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (float)x);
                var floatSum = floatRange.Sum().ToObservable().ToListObservable(); // sum 0 1 2 3 4 5 6
                var conditionalFloatSum = floatRange.Sum(x => x % 2L == 0L).ToObservable().ToListObservable(); // sum 0 2 4 6

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (double)x);
                var doubleSum = doubleRange.Sum().ToObservable().ToListObservable(); // sum 0 1 2 3 4 5 6
                var conditionalDoubleSum = doubleRange.Sum(x => x % 2L == 0L).ToObservable().ToListObservable(); // sum 0 2 4 6

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (decimal)x);
                var decimalSum = decimalRange.Sum().ToObservable().ToListObservable(); // sum 0 1 2 3 4 5 6
                var conditionalDecimalSum = decimalRange.Sum(x => x % 2L == 0L).ToObservable().ToListObservable(); // sum 0 2 4 6

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 3, 6, 10, 15, 21 }, intSum.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 2, 6, 12 }, conditionalIntSum.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 1, 3, 6, 10, 15, 21 }, longSum.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 2, 6, 12 }, conditionalLongSum.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 3, 6, 10, 15, 21 }, doubleSum.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 2, 6, 12 }, conditionalDoubleSum.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 1, 3, 6, 10, 15, 21 }, floatSum.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 2, 6, 12 }, conditionalFloatSum.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 1, 3, 6, 10, 15, 21 }, decimalSum.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 2, 6, 12 }, conditionalDecimalSum.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Average()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var intAverage = intRange.Average().ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (long)x);
                var longAverage = longRange.Average().ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (float)x);
                var floatAverage = floatRange.Average().ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (double)x);
                var doubleAverage = doubleRange.Average().ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (decimal)x);
                var decimalAverage = decimalRange.Average().ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5f, 1, 1.5f, 2, 2.5f, 3 }, intAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5f, 1, 1.5f, 2, 2.5f, 3 }, longAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 0.5f, 1, 1.5f, 2, 2.5f, 3 }, floatAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5, 1, 1.5, 2, 2.5, 3 }, doubleAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 0.5m, 1, 1.5m, 2, 2.5m, 3 }, decimalAverage.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void AverageWithCondition()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1));
                var intAverage = intRange.Average(i => i % 2 == 0).ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (long)x);
                var longAverage = longRange.Average(i => i % 2 == 0).ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (float)x);
                var floatAverage = floatRange.Average(i => i % 2 == 0).ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (double)x);
                var doubleAverage = doubleRange.Average(i => i % 2 == 0).ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromTicks(1)).Select(x => (decimal)x);
                var decimalAverage = decimalRange.Average(i => i % 2 == 0).ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 2, 3 }, intAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 2, 3 }, longAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 1, 2, 3 }, floatAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 2, 3 }, doubleAverage.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 1, 2, 3 }, decimalAverage.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void AverageOverHistory()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1));
                var intAverageHistoryBySize = intRange.Average(4).ToObservable().ToListObservable();
                var intAverageHistoryByTime = intRange.Average(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableIntRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (int?)x);
                var nullableIntAverageHistBySize = nullableIntRange.Average(3).ToObservable().ToListObservable();
                var nullableIntAverageHistByTime = nullableIntRange.Average(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (long)x);
                var longAverageHistoryBySize = longRange.Average(4).ToObservable().ToListObservable();
                var longAverageHistoryByTime = longRange.Average(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableLongRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (long?)x);
                var nullableLongAverageHistBySize = nullableLongRange.Average(3).ToObservable().ToListObservable();
                var nullableLongAverageHistByTime = nullableLongRange.Average(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (float)x);
                var floatAverageHistoryBySize = floatRange.Average(4).ToObservable().ToListObservable();
                var floatAverageHistoryByTime = floatRange.Average(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableFloatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (float?)x);
                var nullableFloatAverageHistBySize = nullableFloatRange.Average(3).ToObservable().ToListObservable();
                var nullableFloatAverageHistByTime = nullableFloatRange.Average(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (double)x);
                var doubleAverageHistoryBySize = doubleRange.Average(4).ToObservable().ToListObservable();
                var doubleAverageHistoryByTime = doubleRange.Average(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDoubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (double?)x);
                var nullableDoubleAverageHistBySize = nullableDoubleRange.Average(3).ToObservable().ToListObservable();
                var nullableDoubleAverageHistByTime = nullableDoubleRange.Average(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (decimal)x);
                var decimalAverageHistoryBySize = decimalRange.Average(4).ToObservable().ToListObservable();
                var decimalAverageHistoryByTime = decimalRange.Average(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDecimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (decimal?)x);
                var nullableDecimalAverageHistBySize = nullableDecimalRange.Average(3).ToObservable().ToListObservable();
                var nullableDecimalAverageHistByTime = nullableDecimalRange.Average(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 1.5, 2.5, 3.5, 4.5 }, intAverageHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5, 1, 1.5, 2, 3, 4 }, intAverageHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, 3, 3.5, 4, 5 }, nullableIntAverageHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, null, null, 3, 3.5, 4, 4.5 }, nullableIntAverageHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 1.5, 2.5, 3.5, 4.5 }, longAverageHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5, 1, 1.5, 2, 3, 4 }, longAverageHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, 3, 3.5, 4, 5 }, nullableLongAverageHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, null, null, 3, 3.5, 4, 4.5 }, nullableLongAverageHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 1.5f, 2.5f, 3.5f, 4.5f }, floatAverageHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 0.5f, 1, 1.5f, 2f, 3f, 4f }, floatAverageHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, 3, 3.5f, 4, 5 }, nullableFloatAverageHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, null, null, 3, 3.5f, 4, 4.5f }, nullableFloatAverageHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 1.5, 2.5, 3.5, 4.5 }, doubleAverageHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0.5, 1, 1.5, 2, 3, 4 }, doubleAverageHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, 3, 3.5, 4, 5 }, nullableDoubleAverageHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, null, null, 3, 3.5, 4, 4.5 }, nullableDoubleAverageHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 1.5m, 2.5m, 3.5m, 4.5m }, decimalAverageHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 0.5m, 1, 1.5m, 2m, 3m, 4m }, decimalAverageHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, 3, 3.5m, 4, 5 }, nullableDecimalAverageHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, null, null, 3, 3.5m, 4, 4.5m }, nullableDecimalAverageHistByTime.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void CountOverHistory()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1));
                var countHistoryByTime = range.Count(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();
                var longCountHistoryByTime = range.LongCount(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3, 4, 5, 5, 5 }, countHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 1, 2, 3, 4, 5, 5, 5 }, longCountHistoryByTime.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SumOverHistory()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1));
                var intSumHistoryBySize = intRange.Sum(4).ToObservable().ToListObservable();
                var intSumHistoryByTime = intRange.Sum(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableIntRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (int?)x);
                var nullableIntSumHistBySize = nullableIntRange.Sum(3).ToObservable().ToListObservable();
                var nullableIntSumHistByTime = nullableIntRange.Sum(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (long)x);
                var longSumHistoryBySize = longRange.Sum(4).ToObservable().ToListObservable();
                var longSumHistoryByTime = longRange.Sum(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableLongRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (long?)x);
                var nullableLongSumHistBySize = nullableLongRange.Sum(3).ToObservable().ToListObservable();
                var nullableLongSumHistByTime = nullableLongRange.Sum(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (float)x);
                var floatSumHistoryBySize = floatRange.Sum(4).ToObservable().ToListObservable();
                var floatSumHistoryByTime = floatRange.Sum(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableFloatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (float?)x);
                var nullableFloatSumHistBySize = nullableFloatRange.Sum(3).ToObservable().ToListObservable();
                var nullableFloatSumHistByTime = nullableFloatRange.Sum(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (double)x);
                var doubleSumHistoryBySize = doubleRange.Sum(4).ToObservable().ToListObservable();
                var doubleSumHistoryByTime = doubleRange.Sum(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDoubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (double?)x);
                var nullableDoubleSumHistBySize = nullableDoubleRange.Sum(3).ToObservable().ToListObservable();
                var nullableDoubleSumHistByTime = nullableDoubleRange.Sum(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (decimal)x);
                var decimalSumHistoryBySize = decimalRange.Sum(4).ToObservable().ToListObservable();
                var decimalSumHistoryByTime = decimalRange.Sum(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDecimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (decimal?)x);
                var nullableDecimalSumHistBySize = nullableDecimalRange.Sum(3).ToObservable().ToListObservable();
                var nullableDecimalSumHistByTime = nullableDecimalRange.Sum(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 6, 10, 14, 18 }, intSumHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 3, 6, 10, 15, 20 }, intSumHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { 0, 3, 7, 12, 15 }, nullableIntSumHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { 0, 0, 0, 3, 7, 12, 18 }, nullableIntSumHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 6, 10, 14, 18 }, longSumHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 1, 3, 6, 10, 15, 20 }, longSumHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { 0, 3, 7, 12, 15 }, nullableLongSumHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { 0, 0, 0, 3, 7, 12, 18 }, nullableLongSumHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 6, 10, 14, 18 }, floatSumHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 1, 3, 6, 10, 15, 20 }, floatSumHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { 0, 3, 7, 12, 15 }, nullableFloatSumHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { 0, 0, 0, 3, 7, 12, 18 }, nullableFloatSumHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 6, 10, 14, 18 }, doubleSumHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 3, 6, 10, 15, 20 }, doubleSumHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { 0, 3, 7, 12, 15 }, nullableDoubleSumHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { 0, 0, 0, 3, 7, 12, 18 }, nullableDoubleSumHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 6, 10, 14, 18 }, decimalSumHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 1, 3, 6, 10, 15, 20 }, decimalSumHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { 0, 3, 7, 12, 15 }, nullableDecimalSumHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { 0, 0, 0, 3, 7, 12, 18 }, nullableDecimalSumHistByTime.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MinOverHistory()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1));
                var intMinHistoryBySize = intRange.Min(4).ToObservable().ToListObservable();
                var intMinHistoryByTime = intRange.Min(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableIntRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (int?)x);
                var nullableIntMinHistBySize = nullableIntRange.Min(3).ToObservable().ToListObservable();
                var nullableIntMinHistByTime = nullableIntRange.Min(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (long)x);
                var longMinHistoryBySize = longRange.Min(4).ToObservable().ToListObservable();
                var longMinHistoryByTime = longRange.Min(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableLongRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (long?)x);
                var nullableLongMinHistBySize = nullableLongRange.Min(3).ToObservable().ToListObservable();
                var nullableLongMinHistByTime = nullableLongRange.Min(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (float)x);
                var floatMinHistoryBySize = floatRange.Min(4).ToObservable().ToListObservable();
                var floatMinHistoryByTime = floatRange.Min(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableFloatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (float?)x);
                var nullableFloatMinHistBySize = nullableFloatRange.Min(3).ToObservable().ToListObservable();
                var nullableFloatMinHistByTime = nullableFloatRange.Min(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (double)x);
                var doubleMinHistoryBySize = doubleRange.Min(4).ToObservable().ToListObservable();
                var doubleMinHistoryByTime = doubleRange.Min(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDoubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (double?)x);
                var nullableDoubleMinHistBySize = nullableDoubleRange.Min(3).ToObservable().ToListObservable();
                var nullableDoubleMinHistByTime = nullableDoubleRange.Min(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (decimal)x);
                var decimalMinHistoryBySize = decimalRange.Min(4).ToObservable().ToListObservable();
                var decimalMinHistoryByTime = decimalRange.Min(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDecimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (decimal?)x);
                var nullableDecimalMinHistBySize = nullableDecimalRange.Min(3).ToObservable().ToListObservable();
                var nullableDecimalMinHistByTime = nullableDecimalRange.Min(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2, 3 }, intMinHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 0, 0, 0, 0, 1, 2 }, intMinHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { null, 3, 3, 3, 4 }, nullableIntMinHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { null, null, null, 3, 3, 3, 3 }, nullableIntMinHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 1, 2, 3 }, longMinHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 0, 0, 0, 0, 1, 2 }, longMinHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { null, 3, 3, 3, 4 }, nullableLongMinHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { null, null, null, 3, 3, 3, 3 }, nullableLongMinHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 1, 2, 3 }, floatMinHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 0, 0, 0, 0, 1, 2 }, floatMinHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, 3, 3, 3, 4 }, nullableFloatMinHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, null, null, 3, 3, 3, 3 }, nullableFloatMinHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 2, 3 }, doubleMinHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 0, 0, 0, 0, 1, 2 }, doubleMinHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, 3, 3, 3, 4 }, nullableDoubleMinHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, null, null, 3, 3, 3, 3 }, nullableDoubleMinHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 1, 2, 3 }, decimalMinHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 0, 0, 0, 0, 1, 2 }, decimalMinHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, 3, 3, 3, 4 }, nullableDecimalMinHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, null, null, 3, 3, 3, 3 }, nullableDecimalMinHistByTime.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MaxOverHistory()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1));
                var intMaxHistoryBySize = intRange.Max(4).ToObservable().ToListObservable();
                var intMaxHistoryByTime = intRange.Max(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableIntRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (int?)x);
                var nullableIntMaxHistBySize = nullableIntRange.Max(3).ToObservable().ToListObservable();
                var nullableIntMaxHistByTime = nullableIntRange.Max(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var longRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (long)x);
                var longMaxHistoryBySize = longRange.Max(4).ToObservable().ToListObservable();
                var longMaxHistoryByTime = longRange.Max(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableLongRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (long?)x);
                var nullableLongMaxHistBySize = nullableLongRange.Max(3).ToObservable().ToListObservable();
                var nullableLongMaxHistByTime = nullableLongRange.Max(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var floatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (float)x);
                var floatMaxHistoryBySize = floatRange.Max(4).ToObservable().ToListObservable();
                var floatMaxHistoryByTime = floatRange.Max(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableFloatRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (float?)x);
                var nullableFloatMaxHistBySize = nullableFloatRange.Max(3).ToObservable().ToListObservable();
                var nullableFloatMaxHistByTime = nullableFloatRange.Max(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var doubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (double)x);
                var doubleMaxHistoryBySize = doubleRange.Max(4).ToObservable().ToListObservable();
                var doubleMaxHistoryByTime = doubleRange.Max(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDoubleRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (double?)x);
                var nullableDoubleMaxHistBySize = nullableDoubleRange.Max(3).ToObservable().ToListObservable();
                var nullableDoubleMaxHistByTime = nullableDoubleRange.Max(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                var decimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => (decimal)x);
                var decimalMaxHistoryBySize = decimalRange.Max(4).ToObservable().ToListObservable();
                var decimalMaxHistoryByTime = decimalRange.Max(TimeSpan.FromMilliseconds(4)).ToObservable().ToListObservable();

                var nullableDecimalRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(1)).Select(x => x < 3 ? null : (decimal?)x);
                var nullableDecimalMaxHistBySize = nullableDecimalRange.Max(3).ToObservable().ToListObservable();
                var nullableDecimalMaxHistByTime = nullableDecimalRange.Max(TimeSpan.FromMilliseconds(3)).ToObservable().ToListObservable();

                pipeline.Run();

                var x0 = intMaxHistoryBySize.AsEnumerable().ToArray();
                var x1 = intMaxHistoryByTime.AsEnumerable().ToArray();
                var x2 = nullableIntMaxHistBySize.AsEnumerable().ToArray();
                var x3 = nullableIntMaxHistByTime.AsEnumerable().ToArray();
                var x4 = longMaxHistoryBySize.AsEnumerable().ToArray();
                var x5 = longMaxHistoryByTime.AsEnumerable().ToArray();
                var x6 = nullableLongMaxHistBySize.AsEnumerable().ToArray();
                var x7 = nullableLongMaxHistByTime.AsEnumerable().ToArray();
                var x8 = floatMaxHistoryBySize.AsEnumerable().ToArray();
                var x9 = floatMaxHistoryByTime.AsEnumerable().ToArray();
                var x10 = nullableFloatMaxHistBySize.AsEnumerable().ToArray();
                var x11 = nullableFloatMaxHistByTime.AsEnumerable().ToArray();
                var x12 = doubleMaxHistoryBySize.AsEnumerable().ToArray();
                var x13 = doubleMaxHistoryByTime.AsEnumerable().ToArray();
                var x14 = nullableDoubleMaxHistBySize.AsEnumerable().ToArray();
                var x15 = nullableDoubleMaxHistByTime.AsEnumerable().ToArray();
                var x16 = decimalMaxHistoryBySize.AsEnumerable().ToArray();
                var x17 = decimalMaxHistoryByTime.AsEnumerable().ToArray();
                var x18 = nullableDecimalMaxHistBySize.AsEnumerable().ToArray();
                var x19 = nullableDecimalMaxHistByTime.AsEnumerable().ToArray();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 3, 4, 5, 6 }, intMaxHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2, 3, 4, 5, 6 }, intMaxHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { null, 3, 4, 5, 6 }, nullableIntMaxHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int?[] { null, null, null, 3, 4, 5, 6 }, nullableIntMaxHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 3, 4, 5, 6 }, longMaxHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long[] { 0, 1, 2, 3, 4, 5, 6 }, longMaxHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { null, 3, 4, 5, 6 }, nullableLongMaxHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new long?[] { null, null, null, 3, 4, 5, 6 }, nullableLongMaxHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 3, 4, 5, 6 }, floatMaxHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float[] { 0, 1, 2, 3, 4, 5, 6 }, floatMaxHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, 3, 4, 5, 6 }, nullableFloatMaxHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new float?[] { null, null, null, 3, 4, 5, 6 }, nullableFloatMaxHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 3, 4, 5, 6 }, doubleMaxHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 0, 1, 2, 3, 4, 5, 6 }, doubleMaxHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, 3, 4, 5, 6 }, nullableDoubleMaxHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double?[] { null, null, null, 3, 4, 5, 6 }, nullableDoubleMaxHistByTime.AsEnumerable()));

                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 3, 4, 5, 6 }, decimalMaxHistoryBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal[] { 0, 1, 2, 3, 4, 5, 6 }, decimalMaxHistoryByTime.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, 3, 4, 5, 6 }, nullableDecimalMaxHistBySize.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new decimal?[] { null, null, null, 3, 4, 5, 6 }, nullableDecimalMaxHistByTime.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Reduce()
        {
            using (var pipeline = Pipeline.Create())
            {
                var factorials = Generators.Range(pipeline, 1, 7, TimeSpan.FromTicks(1)).Aggregate((x, y) => x * y).ToObservable().ToListObservable();
                var single = Generators.Range(pipeline, 1, 1, TimeSpan.FromTicks(1)).Aggregate((x, y) => x * y).ToObservable().ToListObservable();
                var empty = Generators.Range(pipeline, 1, 0, TimeSpan.FromTicks(1)).Aggregate((x, y) => x * y).ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 6, 24, 120, 720, 5040 }, factorials.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1 }, single.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { }, empty.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MinMax()
        {
            using (var pipeline = Pipeline.Create())
            {
                var seq = Generators.Sequence(pipeline, new int[] { 5, 6, 4, 2, 3, 1, 9 }, TimeSpan.FromTicks(1));
                var min = seq.Min().ToObservable().ToListObservable();
                var max = seq.Max().ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 5, 5, 4, 2, 2, 1, 1 }, min.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 5, 6, 6, 6, 6, 6, 9 }, max.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MinMaxWithComparer()
        {
            var reverseComparer = new CompareIntsReversed();
            using (var pipeline = Pipeline.Create())
            {
                var seq = Generators.Sequence(pipeline, new int[] { 5, 6, 4, 2, 3, 1, 9 }, TimeSpan.FromTicks(1));
                var min = seq.Min(reverseComparer).ToObservable().ToListObservable();
                var max = seq.Max(reverseComparer).ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 5, 6, 6, 6, 6, 6, 9 }, min.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 5, 5, 4, 2, 2, 1, 1 }, max.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MinMaxWithCondition()
        {
            Predicate<int> isEvenCondition = i => i % 2 == 0;
            using (var pipeline = Pipeline.Create())
            {
                var seq = Generators.Sequence(pipeline, new int[] { 5, 6, 4, 2, 3, 1, 8 }, TimeSpan.FromTicks(1));
                var min = seq.Min(isEvenCondition).ToObservable().ToListObservable();
                var max = seq.Max(isEvenCondition).ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 6, 4, 2, 2 }, min.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 6, 6, 6, 8 }, max.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MinMaxWithConditionAndComparer()
        {
            Predicate<int> isEvenCondition = i => i % 2 == 0;
            var reverseComparer = new CompareIntsReversed();
            using (var pipeline = Pipeline.Create())
            {
                var seq = Generators.Sequence(pipeline, new int[] { 5, 6, 4, 2, 3, 1, 8 }, TimeSpan.FromTicks(1));
                var min = seq.Min(isEvenCondition, reverseComparer).ToObservable().ToListObservable();
                var max = seq.Max(isEvenCondition, reverseComparer).ToObservable().ToListObservable();
                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 6, 6, 6, 8 }, min.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 6, 4, 2, 2 }, max.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferBySizeInclusive()
        {
            using (var pipeline = Pipeline.Create())
            {
                var inclusiveIntInterval = new IntInterval(0, true, 2, true);
                var buffers = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Window(inclusiveIntInterval).ToObservable().ToListObservable();
                var timestamps = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Select((_, e) => e.OriginatingTime).Window(0, 2).Select((m, e) => Tuple.Create(m.ToArray(), e.OriginatingTime)).ToObservable().ToListObservable();
                pipeline.Run();

                var bufferResults = buffers.AsEnumerable().ToArray();
                Assert.AreEqual(3, bufferResults.Length);
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2 }, bufferResults[0]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, bufferResults[1]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 3, 4 }, bufferResults[2]));

                var timestampResults = timestamps.AsEnumerable().ToArray();
                Assert.AreEqual(3, timestampResults.Length);
                foreach (var buf in timestamps)
                {
                    // buffer timestamp matches _first_ message in buffer
                    Assert.AreEqual(buf.Item1.First(), buf.Item2);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferBySizeExclusive()
        {
            using (var pipeline = Pipeline.Create())
            {
                var inclusiveIntInterval = new IntInterval(0, true, 2, false);
                var buffers = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Window(inclusiveIntInterval).ToObservable().ToListObservable();
                var timestamps = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Select((_, e) => e.OriginatingTime).Window(0, 2).Select((m, e) => Tuple.Create(m.ToArray(), e.OriginatingTime)).ToObservable().ToListObservable();
                pipeline.Run();

                var bufferResults = buffers.AsEnumerable().ToArray();
                Assert.AreEqual(4, bufferResults.Length);
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1 }, bufferResults[0]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2 }, bufferResults[1]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 3 }, bufferResults[2]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 3, 4 }, bufferResults[3]));

                var timestampResults = timestamps.AsEnumerable().ToArray();
                Assert.AreEqual(3, timestampResults.Length);
                foreach (var buf in timestamps)
                {
                    // buffer timestamp matches _first_ message in buffer
                    Assert.AreEqual(buf.Item1.First(), buf.Item2);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FutureWindowBySizeWithSelector()
        {
            using (var pipeline = Pipeline.Create())
            {
                var sums = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Window(0, 2, ms => ms.Select(m => m.Data).Sum()).ToObservable().ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(3, results.Length);
                Assert.AreEqual(3, results[0]);
                Assert.AreEqual(6, results[1]);
                Assert.AreEqual(9, results[2]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void HistoryBySize()
        {
            using (var pipeline = Pipeline.Create())
            {
                var buffers = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Window(-2, 0).ToObservable().ToListObservable();
                var timestamps = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Select((_, e) => e.OriginatingTime).Window(-2, 0).Select((m, e) => Tuple.Create(m.ToArray(), e.OriginatingTime)).ToObservable().ToListObservable();
                pipeline.Run();

                var bufferResults = buffers.AsEnumerable().ToArray();
                Assert.AreEqual(3, bufferResults.Length);
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2 }, bufferResults[0]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, bufferResults[1]));
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 3, 4 }, bufferResults[2]));

                var timestampResults = timestamps.AsEnumerable().ToArray();
                Assert.AreEqual(3, timestampResults.Length);
                foreach (var buf in timestamps)
                {
                    // buffer timestamp matches _last_ message in buffer
                    Assert.AreEqual(buf.Item1.Last(), buf.Item2);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void HistoryBySizeWithSelector()
        {
            using (var pipeline = Pipeline.Create())
            {
                var sums = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1)).Window(-2, 0, ms => ms.Select(m => m.Data).Sum()).ToObservable().ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(3, results.Length);
                Assert.AreEqual(3, results[0]);
                Assert.AreEqual(6, results[1]);
                Assert.AreEqual(9, results[2]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void HistoryByTimeInclusive()
        {
            using (var pipeline = Pipeline.Create())
            {
                var inclusiveRelativeTime = RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(2));
                var buffers = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1), keepOpen: true).Window(inclusiveRelativeTime).ToEnumerable();
                var timestamps = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1), keepOpen: true).Select((_, e) => e.OriginatingTime).Window(inclusiveRelativeTime).Select((m, e) => Tuple.Create(m.ToArray(), e.OriginatingTime)).ToEnumerable();

                // use RunAsync and keepOpen: true in Generators to test for final output *before* pipeline close
                pipeline.RunAsync();

                // Enumerate the collection manually to verify that all the expected results are available
                // *while* the collector is still subscribed (enumerator will block if the last result is
                // not present in the collection, implying that the window is still buffering).
                var results = buffers.GetEnumerator();
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1, 2 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2, 3 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 3, 4 }, results.Current));

                var timestampResults = timestamps.GetEnumerator();
                for (int i = 0; i < 5; i++)
                {
                    Assert.IsTrue(timestampResults.MoveNext());
                    var buf = timestampResults.Current;

                    // buffer timestamp matches _last_ message in buffer
                    Assert.AreEqual(buf.Item1.Last(), buf.Item2);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void HistoryByTimeExclusive()
        {
            using (var pipeline = Pipeline.Create())
            {
                var exclusiveRelativeTime = RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(2), false);
                var buffers = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1), keepOpen: true).Window(exclusiveRelativeTime).ToEnumerable();
                var timestamps = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1), keepOpen: true).Select((_, e) => e.OriginatingTime).Window(exclusiveRelativeTime).Select((m, e) => Tuple.Create(m.ToArray(), e.OriginatingTime)).ToEnumerable();

                // use RunAsync and keepOpen:true in Generators to test for final output *before* pipeline close
                pipeline.RunAsync();

                // Enumerate the collection manually to verify that all the expected results are available
                // *while* the collector is still subscribed (enumerator will block if the last result is
                // not present in the collection, implying that the window is still buffering).
                var results = buffers.GetEnumerator();
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 0, 1 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 1, 2 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 2, 3 }, results.Current));
                Assert.IsTrue(results.MoveNext());
                Assert.IsTrue(Enumerable.SequenceEqual(new int[] { 3, 4 }, results.Current));

                var timestampResults = timestamps.GetEnumerator();
                for (int i = 0; i < 5; i++)
                {
                    Assert.IsTrue(timestampResults.MoveNext());
                    var buf = timestampResults.Current;

                    // buffer timestamp matches _last_ message in buffer
                    Assert.AreEqual(buf.Item1.Last(), buf.Item2);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void NonOriginSpanningPastWindowByTime()
        {
            using (var pipeline = Pipeline.Create())
            {
                // window from -3 to -1 ms (not spanning origin)
                var sums = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1))
                                     .Window<int, int>(TimeSpan.FromMilliseconds(-3), TimeSpan.FromMilliseconds(-1), ms => ms.Select(m => m.Data).Sum())
                                     .ToObservable()
                                     .ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(0, results[0]); // empty
                Assert.AreEqual(0, results[1]); // 0
                Assert.AreEqual(1, results[2]); // 0 + 1
                Assert.AreEqual(3, results[3]); // 0 + 1 + 2
                Assert.AreEqual(6, results[4]); // 1 + 2 + 3
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void NonOriginSpanningFutureWindowByTime()
        {
            using (var pipeline = Pipeline.Create())
            {
                // window from +1 to +3 ms (not spanning origin)
                var sums = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1))
                                     .Window<int, int>(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(3), ms => ms.Select(m => m.Data).Sum())
                                     .ToObservable()
                                     .ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(6, results[0]); // 1 + 2 + 3
                Assert.AreEqual(9, results[1]); // 2 + 3 + 4
                Assert.AreEqual(7, results[2]); // 3 + 4
                Assert.AreEqual(4, results[3]); // 4
                Assert.AreEqual(0, results[4]); // empty
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void BackwardAndForwardWindowByIndex()
        {
            using (var pipeline = Pipeline.Create())
            {
                var sums = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(1))
                                     .Window<int, int>(2, -2, ms => ms.Select(m => m.Data).Sum()) // note negative interval form to incidentally test normalization
                                     .ToObservable()
                                     .ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(6, results.Length);
                Assert.AreEqual(10, results[0]); // 0 + 1 + [2] + 3 + 4
                Assert.AreEqual(15, results[1]); // 1 + 2 + [3] + 4 + 5
                Assert.AreEqual(20, results[2]); // 2 + 3 + [4] + 5 + 6
                Assert.AreEqual(25, results[3]); // 3 + 4 + [5] + 6 + 7
                Assert.AreEqual(30, results[4]); // 4 + 5 + [6] + 7 + 8
                Assert.AreEqual(35, results[5]); // 5 + 6 + [7] + 8 + 9
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void NonOriginSpanningPastWindowByIndex()
        {
            using (var pipeline = Pipeline.Create())
            {
                // window from -3 to -1 (not spanning origin)
                var sums = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(1))
                                     .Window<int, int>(-3, -1, ms => ms.Select(m => m.Data).Sum())
                                     .ToObservable()
                                     .ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(7, results.Length);
                Assert.AreEqual(3, results[0]); // 0 + 1 + 2 (origin 3)
                Assert.AreEqual(6, results[1]); // 1 + 2 + 3 (origin 4)
                Assert.AreEqual(9, results[2]); // 2 + 3 + 4 (origin 5)
                Assert.AreEqual(12, results[3]); // 3 + 4 + 5 (origin 6)
                Assert.AreEqual(15, results[4]); // 4 + 5 + 6 (origin 7)
                Assert.AreEqual(18, results[5]); // 5 + 6 + 7 (origin 8)
                Assert.AreEqual(21, results[6]); // 6 + 7 + 8 (origin 9)
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void NonOriginSpanningFutureWindowByIndex()
        {
            using (var pipeline = Pipeline.Create())
            {
                // window from -3 to -1 (not spanning origin)
                var sums = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(1))
                                     .Window<int, int>(1, 3, ms => ms.Select(m => m.Data).Sum())
                                     .ToObservable()
                                     .ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(7, results.Length);
                Assert.AreEqual(6, results[0]); // 1 + 2 + 3 (origin 0)
                Assert.AreEqual(9, results[1]); // 2 + 3 + 4 (origin 1)
                Assert.AreEqual(12, results[2]); // 3 + 4 + 5 (origin 2)
                Assert.AreEqual(15, results[3]); // 4 + 5 + 6 (origin 3)
                Assert.AreEqual(18, results[4]); // 5 + 6 + 7 (origin 4)
                Assert.AreEqual(21, results[5]); // 6 + 7 + 8 (origin 5)
                Assert.AreEqual(24, results[6]); // 7 + 8 + 9 (origin 6)
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void HistoryByTimeWithSelector()
        {
            using (var pipeline = Pipeline.Create())
            {
                var sums = Generators.Range(pipeline, 0, 5, TimeSpan.FromMilliseconds(1)).Window<int, int>(RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(2)), ms => ms.Select(m => m.Data).Sum()).ToObservable().ToListObservable();
                pipeline.Run();

                var results = sums.AsEnumerable().ToArray();
                Assert.AreEqual(5, results.Length);
                Assert.AreEqual(0, results[0]);
                Assert.AreEqual(1, results[1]);
                Assert.AreEqual(3, results[2]);
                Assert.AreEqual(6, results[3]);
                Assert.AreEqual(9, results[4]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Window()
        {
            using (var pipeline = Pipeline.Create())
            {
                var intRange = Generators.Range(pipeline, 0, 7, TimeSpan.FromMilliseconds(10));
                var intWindowPlus35 = intRange.Window(new RelativeTimeInterval(TimeSpan.Zero, TimeSpan.FromMilliseconds(35))).Average().ToObservable().ToListObservable();
                var intWindowMinus15Plus25 = intRange.Window(new RelativeTimeInterval(TimeSpan.FromMilliseconds(-15), TimeSpan.FromMilliseconds(25))).Average().ToObservable().ToListObservable();

                pipeline.Run();

                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 1.5, 2.5, 3.5, 4.5, 5.0, 5.5, 6.0 }, intWindowPlus35.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new double[] { 1, 1.5, 2.5, 3.5, 4.5, 5.0, 5.5 }, intWindowMinus15Plus25.AsEnumerable()));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void DynamicWindow()
        {
            // test sets of window and expected result pairs
            void Test(((int, int, int) Window, int[] Expected)[] pairs, bool leftInclusive, bool rightInclusive)
            {
                // data is a 0..9 range, windows are start/end indexes into this + obsolete index
                int[][] GetWindowedData(IEnumerable<(int Obsolete, int Start, int End)> windows)
                {
                    // indexes mapped to originating time
                    DateTime ToTime(int i)
                    {
                        return DateTime.MinValue.AddMilliseconds(i * 10);
                    }

                    using (var p = Pipeline.Create())
                    {
                        var output = Generators.Sequence(p, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Select(d => (d, ToTime(d))))
                            .Window(
                                Generators.Sequence(p, windows, TimeSpan.FromMilliseconds(10)),
                                m => (new TimeInterval(ToTime(m.Data.Start), leftInclusive, ToTime(m.Data.End), rightInclusive), ToTime(m.Data.Obsolete)))
                            .Select(ms => ms.Select(m => m.Data).ToArray())
                            .ToObservable().ToListObservable();
                        p.Run();
                        return output.AsEnumerable().ToArray();
                    }
                }

                // get windowed data and compare with expected results
                var results = GetWindowedData(pairs.Select(p => p.Window));
                Assert.AreEqual(results.Length, pairs.Count());
                for (var i = 0; i < results.Length; i++)
                {
                    Assert.IsTrue(Enumerable.SequenceEqual(results[i], pairs[i].Expected));
                }
            }

            // test growing (right) window - pairs are (obsolete, start, end) windows with expected windowed data
            Test(
                new[]
                {
                    ((2, 2, 4), new[] { 2, 3, 4 }), // 2..4 growing to right
                    ((2, 2, 5), new[] { 2, 3, 4, 5 }), // 2..5
                    ((2, 2, 6), new[] { 2, 3, 4, 5, 6 }), // 2..6
                },
                true,
                true);

            // test left inclusivity
            Test(
                new[]
                {
                    ((2, 2, 4), new[] { 3, 4 }),
                    ((2, 2, 5), new[] { 3, 4, 5 }),
                    ((2, 2, 6), new[] { 3, 4, 5, 6 }),
                },
                false /* not including left */,
                true);

            // test right inclusivity
            Test(
                new[]
                {
                    ((2, 2, 4), new[] { 2, 3 }),
                    ((2, 2, 5), new[] { 2, 3, 4 }),
                    ((2, 2, 6), new[] { 2, 3, 4, 5 }),
                },
                true,
                false /* not including right */);

            // test left & right inclusivity
            Test(
                new[]
                {
                    ((2, 2, 4), new[] { 3 }),
                    ((2, 2, 5), new[] { 3, 4 }),
                    ((2, 2, 6), new[] { 3, 4, 5 }),
                },
                false /* not including left */,
                false /* not including right */);

            // test left-most window
            Test(new[] { ((0, 0, 2), new[] { 0, 1, 2 }), }, true, true);

            // test right-most window
            Test(new[] { ((7, 7, 9), new[] { 7, 8, 9 }), }, true, true);

            // test beyond right-most window
            Test(new[] { ((7, 7, 100), new[] { 7, 8, 9 }), }, true, true);

            // test sliding window
            Test(
                new[]
                {
                    ((2, 2, 4), new[] { 2, 3, 4 }), // 2..4 sliding to right
                    ((3, 3, 5), new[] { 3, 4, 5 }), // 3..6
                    ((4, 4, 6), new[] { 4, 5, 6 }), // 4..7
                },
                true,
                true);

            // test growing (left) window
            Test(
                new[]
                {
                    ((1, 3, 5), new[] { 3, 4, 5 }), // 3..5 growing to left
                    ((1, 2, 5), new[] { 2, 3, 4, 5 }), // 2..4
                    ((1, 1, 5), new[] { 1, 2, 3, 4, 5 }), // 1..5
                },
                true,
                true);

            // invalid if obsolete time moves backward!
            try
            {
                Test(
                    new[]
                    {
                        ((3, 3, 5), new[] { 3, 4, 5 }), // 3..5 growing to left
                        ((2, 3, 5), new[] { 3, 4, 5 }), // 3..4 boom! (2 earlier than previous [3] obsolete)
                    },
                    true,
                    true);
                Assert.Fail("Expected exception due to obsolete time backtracking");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.InnerException.Message, "Dynamic window with obsolete time prior to previous window.");
            }

            // invalid if window requests are before previous obsolete time
            try
            {
                Test(
                    new[]
                    {
                        ((3, 3, 5), new[] { 3, 4, 5 }), // 3..5 growing to left
                        ((3, 2, 5), new[] { 1, 3, 4, 5 }), // 2..5 boom! (2 has already been obsoleted)
                    },
                    true,
                    true);
                Assert.Fail("Expected exception due to window request into obsoleted inputs");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.InnerException.Message, "Dynamic window must not extend before previous obsolete time.");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void StdOverIEnumerable()
        {
            Assert.IsTrue(Math.Abs(new[] { 727.7m, 1086.5m, 1091.0m, 1361.3m, 1490.5m, 1956.1m }.Std() - 420.96248961952256m) < 0.0000000001m); // decimal
            Assert.IsTrue(Math.Abs(new[] { 727.7, 1086.5, 1091.0, 1361.3, 1490.5, 1956.1 }.Std() - 420.96248961952256) < double.Epsilon); // double
            Assert.IsTrue(Math.Abs(new[] { 727.7f, 1086.5f, 1091.0f, 1361.3f, 1490.5f, 1956.1f }.Std() - 420.96248961952256f) < float.Epsilon); // float
            Assert.AreEqual(0, new double[] { }.Std());
        }

        [TestMethod]
        [Timeout(60000)]
        public void StdOverWindows()
        {
            using (var pipeline = Pipeline.Create())
            {
                var windows = Generators.Sequence(pipeline, new double[][] { new[] { 727.7, 1086.5, 1091.0, 1361.3, 1490.5, 1956.1 }, new double[] { } }, TimeSpan.FromTicks(1));
                var std = windows.Std().ToObservable().ToListObservable();
                pipeline.Run();

                var results = std.AsEnumerable().ToArray();
                Assert.AreEqual(2, results.Length);
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 420.96248961952256, 0 }, results));
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void ZipAndMerge()
        {
            var zipped = new List<string[]>();
            var merged = new List<string>();

            // Zipped and ordered by originating time (with stream ID tie-breaker)
            var zippedShouldBe = new[]
            {
                new string[] { "A0", "B0", "C0" },
                new string[] { "A1" },
                new string[] { "B1" },
                new string[] { "A2", "C1" },
                new string[] { "A3", "B2" },
                new string[] { "A4", "C2" },
                new string[] { "B3" },
                new string[] { "A5" },
                new string[] { "A6", "B4", "C3" },
            };

            using (var p = Pipeline.Create())
            {
                // A0  A1  A2  A3  A4  A5  A6
                // B0    B1    B2    B3    B4
                // C0      C1      C2      C3 (arriving 100ms late in wall-clock time)
                //
                // Zipped order: [A0 B0 C0] [A1] [B1] [A2 C1] [A3 B2] [A4 C2] [B3] [A5] [A6 B4 C3]
                // Several places align *exactly* in originating time (e.g. A0-B0-C0, A2-C1, A3-B2, etc.) these are ordered by stream ID within the same tick
                var sourceA = Generators.Range(p, 0, 7, TimeSpan.FromMilliseconds(10), keepOpen: true).Select(i => $"A{i}");
                var sourceB = Generators.Range(p, 0, 5, TimeSpan.FromMilliseconds(15), keepOpen: true).Select(i => $"B{i}");
                var sourceC = Generators.Range(p, 0, 4, TimeSpan.FromMilliseconds(20), keepOpen: true).Select(i => $"C{i}").Delay(TimeSpan.FromMilliseconds(100));

                Operators.Merge(new[] { sourceA, sourceB, sourceC }).Do(x => merged.Add(x.Data.DeepClone())); // non-deterministic order
                Operators.Zip(new[] { sourceA, sourceB, sourceC }).Do(x => zipped.Add(x.DeepClone())); // ordered by originating time, then stream ID within single tick

                p.RunAsync();
                while (zipped.Count != zippedShouldBe.Length)
                {
                }
            }

            Assert.AreEqual(zipped.Count, zippedShouldBe.Length);
            for (int i = 0; i < zipped.Count; i++)
            {
                Assert.IsTrue(Enumerable.SequenceEqual(zipped[i], zippedShouldBe[i]));
            }

            // Since merging is non-deterministic, all we test here is that all messages arrive
            Assert.IsTrue(Enumerable.SequenceEqual(
                new[]
                {
                    "A0", "A1", "A2", "A3", "A4", "A5", "A6",
                    "B0", "B1", "B2", "B3", "B4",
                    "C0", "C1", "C2", "C3",
                },
                merged.OrderBy(_ => _)));
        }

        [TestMethod]
        [Timeout(60000)]
        public void FirstOperator()
        {
            using var p = Pipeline.Create();
            var source = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10));
            var first1 = source.First().ToObservable().ToListObservable();
            var first5 = source.First(5).ToObservable().ToListObservable();
            var firstN = source.First(int.MaxValue).ToObservable().ToListObservable();

            p.Run();

            CollectionAssert.AreEqual(new[] { 0 }, first1.AsEnumerable().ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, first5.AsEnumerable().ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, firstN.AsEnumerable().ToArray());
        }

        [TestMethod]
        [Timeout(60000)]
        public void LastOperator()
        {
            using var p = Pipeline.Create();
            var source = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10));
            var last1 = source.Last().ToObservable().ToListObservable();
            var last5 = source.Last(5).ToObservable().ToListObservable();
            var lastN = source.Last(int.MaxValue).ToObservable().ToListObservable();

            p.Run();

            CollectionAssert.AreEqual(new[] { 9 }, last1.AsEnumerable().ToArray());
            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 9 }, last5.AsEnumerable().ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, lastN.AsEnumerable().ToArray());
        }

        private static DateTime SelectMiddleTimestamp(IEnumerable<DateTime> times)
        {
            return times.Skip(times.Count() / 2).First();
        }

        /// <summary>
        /// Specifies a message to be output on a stream.
        /// </summary>
        /// <typeparam name="T">The underlying type of the message.</typeparam>
        public class MessageSpec<T> : IEquatable<MessageSpec<T>>
        {
            public T Data;
            public int OriginatingTimeOffsetMs;
            public int GenerateTimeOffsetMs;
            public string Source;

            public MessageSpec(T data, int originatingTimeOffsetMs, int generateTimeOffsetMs, string name = "")
            {
                this.Data = data;
                this.OriginatingTimeOffsetMs = originatingTimeOffsetMs;
                this.GenerateTimeOffsetMs = generateTimeOffsetMs;
                this.Source = name;
            }

            public bool Equals(MessageSpec<T> other)
            {
                // we only compare data and originating times for now
                return EqualityComparer<T>.Default.Equals(this.Data, other.Data) &&
                    (this.OriginatingTimeOffsetMs == other.OriginatingTimeOffsetMs);
            }

            public override bool Equals(object obj)
            {
                return (obj is MessageSpec<T>) && this.Equals((MessageSpec<T>)obj);
            }

            public override int GetHashCode()
            {
                return this.Data.GetHashCode() ^ this.OriginatingTimeOffsetMs.GetHashCode();
            }

            public override string ToString()
            {
                return $"{this.GenerateTimeOffsetMs}: {this.Source} -> {this.Data}:{this.OriginatingTimeOffsetMs}";
            }
        }

        public class CompareIntsReversed : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x == y ? 0 : x < y ? 1 : -1; // reverse ordering
            }
        }
    }
}
