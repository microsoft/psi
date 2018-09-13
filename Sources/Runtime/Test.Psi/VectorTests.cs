// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VectorTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void VectorStatelessParallel()
        {
            List<double[]> results = new List<double[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, new[] { 100, 1, 0.01 }, r => new[] { r[0] + 100, r[1] + 1, r[2] + 0.01 }, 10, TimeSpan.FromTicks(10))
                    .Parallel(3, s => s.Select(d => d * 100), true)
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new double[] { 100000, 1000, 10 }, results[9]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void VectorStatefulParallel()
        {
            List<int[]> results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, new[] { 100, 10, 1 }, r => new[] { r[0] + 100, r[1] + 10, r[2] + 1 }, 10, TimeSpan.FromTicks(10))
                    .Parallel(3, (int index, IProducer<int> s) => s.Aggregate(0, (prev, v) => v - prev), true)
                    .Do(x => results.Add(x.DeepClone()));

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 500, 50, 5 }, results[9]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void VectorIncorrectSizeParallel()
        {
            List<double> results = new List<double>();

            using (var p = Pipeline.Create())
            {
                var x = Enumerable.Range(0, 4).ToArray();

                // Runs a parallel over the vector, but specifying only size 2. This should throw an exception.
                Generators.Return(p, x).Parallel(2, s => s.Select(d => d * 10), true);
                try
                {
                    p.Run(enableExceptionHandling: true);
                }
                catch (AggregateException exception)
                {
                    Assert.IsInstanceOfType(exception.InnerException, typeof(InvalidOperationException), "Unexpected exception type: {0}", exception.InnerException.GetType().ToString());
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void VariableLengthVectorStatelessParallel()
        {
            List<int> results = new List<int>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, Enumerable.Range(0, 1).ToArray(), r => Enumerable.Range(0, r.Length + 1).ToArray(), 5, TimeSpan.FromTicks(10))
                    .Parallel((i, s) => s.Select(x => x * 10), true)
                    .Do(x => results.Add(x.Sum()));

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 10, 30, 60, 100 }, results.ToArray());
        }


        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorStatelessParallel()
        {
            var odd = new Dictionary<int, int> { { 1, 100 }, { 3, 300 }, { 5, 500 } };
            var even = new Dictionary<int, int> { { 0, 0 }, { 2, 200 }, { 4, 400 } };

            List<int[]> results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => (i % 2 == 0) ? even : odd)
                    .Parallel(s => s.Select(val => val / 100), true)
                    .Do(x => results.Add(x.Values.ToArray()));

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 2, 4 }, results[8]);
            CollectionAssert.AreEqual(new int[] { 1, 3, 5 }, results[9]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorStatefulParallel()
        {
            var frames =
                new[]
                {
                    //                           full       end        start      mid        intermittent
                    new Dictionary<int, int> { { 1, 10 },            { 3, 30 }                       }, //  10      30
                    new Dictionary<int, int> { { 1, 10 },            { 3, 30 },            { 5, 50 } }, //  20      60      50
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 5, 50 } }, //  30  20  90  40 100
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 5, 50 } }, //  40  40 120  80 150
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 },           }, //  50  60 150 120
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 5, 50 } }, //  60  80 180 160  50
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }, { 4, 40 }, { 5, 50 } }, //  70 100 210 200 100
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }, { 3, 30 }                       }, //  80 120 240
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }                                  }, //  90 140
                    new Dictionary<int, int> { { 1, 10 }, { 2, 20 }                                  }, // 100 160
                };

            List<int[]> results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => frames[i])
                    .Parallel(stream => stream.Aggregate(0, (prev, v) => v + prev), true)
                    .Do(x => results.Add(x.Values.ToArray()));

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 10, 30 }, results[0]);
            CollectionAssert.AreEqual(new int[] { 20, 60, 50 }, results[1]);
            CollectionAssert.AreEqual(new int[] { 30, 20, 90, 40, 100 }, results[2]);
            CollectionAssert.AreEqual(new int[] { 40, 40, 120, 80, 150 }, results[3]);
            CollectionAssert.AreEqual(new int[] { 50, 60, 150, 120, }, results[4]);
            CollectionAssert.AreEqual(new int[] { 60, 80, 180, 160, 50 }, results[5]);
            CollectionAssert.AreEqual(new int[] { 70, 100, 210, 200, 100 }, results[6]);
            CollectionAssert.AreEqual(new int[] { 80, 120, 240 }, results[7]);
            CollectionAssert.AreEqual(new int[] { 90, 140 }, results[8]);
            CollectionAssert.AreEqual(new int[] { 100, 160 }, results[9]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorBufferCompletionTest()
        {
            var frames =
                new[]
                {
                    new Dictionary<int, int> { { 1, 10 }            },
                    new Dictionary<int, int> { { 1, 11 }            },
                    new Dictionary<int, int> { { 1, 12 }            },
                    new Dictionary<int, int> { { 1, 13 }            },
                    new Dictionary<int, int> { { 1, 14 }            },
                    new Dictionary<int, int> { { 1, 15 }, { 2, 20 } },
                    new Dictionary<int, int> { { 1, 16 }, { 2, 21 } },
                    new Dictionary<int, int> { { 1, 17 }, { 2, 22 } },
                    new Dictionary<int, int> { { 1, 18 }, { 2, 23 } },
                    new Dictionary<int, int> { { 1, 19 }, { 2, 24 } },
                    new Dictionary<int, int> {            { 2, 25 } },
                    new Dictionary<int, int> {            { 2, 26 } },
                    new Dictionary<int, int> {            { 2, 27 } },
                    new Dictionary<int, int> {            { 2, 28 } },
                    new Dictionary<int, int> {            { 2, 29 } }
                };

            var results = new List<string>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 15, TimeSpan.FromMilliseconds(10))
                    .Select(i => frames[i])
                    .Parallel((_, stream) => stream.Window(new RelativeTimeInterval(-TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20))))
                    .Do(x =>
                    {
                        var sb = new StringBuilder();
                        foreach (var k in x.Keys)
                        {
                            sb.Append($" {k}->");
                            foreach (var v in x[k])
                            {
                                sb.Append($"{v.Data},");
                            }
                            sb.Remove(sb.Length - 1, 1);
                        }
                        results.Add(sb.ToString().Substring(1));
                    });
                p.Run();
            }

            Assert.AreEqual("1->10,11,12", results[0]);
            Assert.AreEqual("1->10,11,12,13", results[1]);
            Assert.AreEqual("1->11,12,13,14", results[2]);
            Assert.AreEqual("1->12,13,14,15", results[3]);
            Assert.AreEqual("1->13,14,15,16", results[4]);
            Assert.AreEqual("1->14,15,16,17 2->20,21,22", results[5]);
            Assert.AreEqual("1->15,16,17,18 2->20,21,22,23", results[6]);
            Assert.AreEqual("1->16,17,18,19 2->21,22,23,24", results[7]); // most importantly, these last frames (final of first stream)
            Assert.AreEqual("1->17,18,19 2->22,23,24,25", results[8]);
            Assert.AreEqual("1->18,19 2->23,24,25,26", results[9]);
            Assert.AreEqual("2->24,25,26,27", results[10]);
            Assert.AreEqual("2->25,26,27,28", results[11]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorTestJoinOrDefault()
        {
            // I'm generating a dictionary with 2 keys (1 and 2) that over time
            // streams like this
            // 1: 0 1 2 3 4  5  6  7  8  9
            // 2: 0 2 4 6 8 10 12 14 16 18
            // On this I am then doing a parallel and inside the parallel pipeline
            // i am dropping messages (keeping only the even ones with a Where clause)
            // So with drops things look like this
            // 1: 0 . 2 . 4  .  6  .  8  .
            // 2: 0 2 4 6 8 10 12 14 16 18
            // If parallel is using the regular Join (instead of JoinOrDefault)
            // then we should have 5 messages out, like this
            // 1: 0 2 4  6  8
            // 2: 0 4 8 12 16
            // When parallel operates correctly with a JoinOrDefault, we should get
            // all ten messages out, where the dropped messages are replaced with
            // default(int) which is 0, so like this.
            // 1: 0 0 2 0 4  0  6  0  8  0
            // 2: 0 2 4 6 8 10 12 14 16 18
            // Unfortunately currently we are only getting 9 messages out, and this
            // is probably because the Join doesn't know that it still needs to post
            // the last one until the next message arrives (which never does).
            var values = new Dictionary<int, int>[10];
            for (int i = 0; i < 10; i++)
            {
                values[i] = new Dictionary<int, int>() { { 1, i }, { 2, 2 * i } };
            }

            var original = new List<Dictionary<int, int>>();
            var results = new List<Dictionary<int, int>>();
            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => values[i])
                    .Do(dict => original.Add(dict.DeepClone()))
                    .Parallel(v => v.Where(value => value % 2 == 0), true)
                    .Do(dict => results.Add(dict.DeepClone()));
                p.Run();
            }

            // In reality the test here should test for 10, but that doesn't work because
            // the joinOrDefault does not push the default message until the next one arrives
            // and for the last message the next one never arrives
            Assert.IsTrue(results.Count == 9);

            original.Clear();
            results.Clear();
            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => values[i])
                    .Do(dict => original.Add(dict.DeepClone()))
                    .Parallel(v => v.Where(value => value % 2 == 0), false)
                    .Do(dict => results.Add(dict.DeepClone()));
                p.Run();
            }

            // Frames that do not match on the two branches are dropped
            Assert.IsTrue(results.Count == 5);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ParallelComponentIssolationTest()
        {
            // verify that Parallel* components don't expose emitters of inner Subpipeline
            using (var p = Pipeline.Create())
            {
                var parallel = new ParallelFixedLength<int, int>(p, 10, (i, prod) => prod, false);
                Assert.AreEqual(p, parallel.Out.Pipeline); // composite components shouldn't expose subpipelines

                var parallelVarLen = new ParallelVariableLength<int, int>(p, (i, prod) => prod, false);
                Assert.AreEqual(p, parallelVarLen.Out.Pipeline); // composite components shouldn't expose subpipelines

                var parallelSparse = new ParallelSparse<int, int, int>(p, (i, prod) => prod, false);
                Assert.AreEqual(p, parallelSparse.Out.Pipeline); // composite components shouldn't expose subpipelines
            }
        }
    }
}
