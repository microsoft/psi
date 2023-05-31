// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
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
                var caughtException = false;
                try
                {
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
                        Assert.IsInstanceOfType(exception.InnerException, typeof(InvalidOperationException), "Unexpected inner exception type: {0}", exception.InnerException.GetType().ToString());
                    }
                }

                Assert.IsTrue(caughtException);
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

            List<Dictionary<int, int>> results = new List<Dictionary<int, int>>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => (i % 2 == 0) ? even : odd)
                    .Parallel(s => s.Select(val => val / 100), true)
                    .Do(x => results.Add(x.DeepClone()));

                p.Run();
            }

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 0, 0 }, { 2, 2 }, { 4, 4 } }, results[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 3, 3 }, { 5, 5 } }, results[9]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorWithGapParallel()
        {
            var sequence = new List<Dictionary<int, int>>()
            {
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 }, { 2, 200 } },
                new Dictionary<int, int> { { 1, 100 }, { 2, 200 } },
                new Dictionary<int, int> { { 1, 100 }, { 2, 200 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int>(),
                new Dictionary<int, int>(),
                new Dictionary<int, int>(),
                new Dictionary<int, int> { { 3, 300 } },
                new Dictionary<int, int> { { 3, 300 } },
                new Dictionary<int, int> { { 3, 300 } },
            };

            List<Dictionary<int, int>> results = new List<Dictionary<int, int>>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, sequence, TimeSpan.FromMilliseconds(1))
                    .Parallel(s => s.Select(val => val / 100), true)
                    .Do(x => results.Add(x.DeepClone()));

                p.Run();
            }

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[0]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[1]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[2]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, results[3]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, results[4]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, results[5]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[6]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[7]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, results[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, results[9]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, results[10]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, results[11]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 3, 3 } }, results[12]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 3, 3 } }, results[13]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 3, 3 } }, results[14]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorBranchTerminationPolicy()
        {
            var sequence = new List<Dictionary<int, int>>()
            {
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 } },
                new Dictionary<int, int> { { 1, 100 }, { 2, 200 } },
                new Dictionary<int, int> { { 1, 100 }, { 2, 200 } },
                new Dictionary<int, int> { { 2, 200 } },
                new Dictionary<int, int> { { 2, 200 } },
                new Dictionary<int, int>(),
                new Dictionary<int, int>(),
                new Dictionary<int, int>(),
            };

            List<Dictionary<int, int>> resultsDefault = new List<Dictionary<int, int>>();
            List<Dictionary<int, int>> resultsAfterKeyNotPresentOnce = new List<Dictionary<int, int>>();
            List<Dictionary<int, int>> resultsNever = new List<Dictionary<int, int>>();

            using (var p = Pipeline.Create())
            {
                var source = Generators.Sequence(p, sequence, TimeSpan.FromMilliseconds(1));

                source.Parallel(s => s.Select(val => val / 100), true)
                    .Do(x => resultsDefault.Add(x.DeepClone()));
                source.Parallel(s => s.Select(val => val / 100), true, branchTerminationPolicy: BranchTerminationPolicy<int, int>.AfterKeyNotPresent(1))
                    .Do(x => resultsAfterKeyNotPresentOnce.Add(x.DeepClone()));
                source.Parallel(s => s.Select(val => val / 100), true, branchTerminationPolicy: BranchTerminationPolicy<int, int>.Never())
                    .Do(x => resultsNever.Add(x.DeepClone()));

                p.Run();
            }

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsDefault[0]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsDefault[1]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsDefault[2]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsDefault[3]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsDefault[4]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 2, 2 } }, resultsDefault[5]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 2, 2 } }, resultsDefault[6]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, resultsDefault[7]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, resultsDefault[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, resultsDefault[9]);

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsAfterKeyNotPresentOnce[0]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsAfterKeyNotPresentOnce[1]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsAfterKeyNotPresentOnce[2]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsAfterKeyNotPresentOnce[3]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsAfterKeyNotPresentOnce[4]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 2 } }, resultsAfterKeyNotPresentOnce[5]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 2, 2 } }, resultsAfterKeyNotPresentOnce[6]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 2, 0 } }, resultsAfterKeyNotPresentOnce[7]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, resultsAfterKeyNotPresentOnce[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { }, resultsAfterKeyNotPresentOnce[9]);

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsNever[0]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsNever[1]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 } }, resultsNever[2]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsNever[3]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 1 }, { 2, 2 } }, resultsNever[4]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 2 } }, resultsNever[5]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 2 } }, resultsNever[6]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 0 } }, resultsNever[7]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 0 } }, resultsNever[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 0 }, { 2, 0 } }, resultsNever[9]);
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

            List<Dictionary<int, int>> results = new List<Dictionary<int, int>>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => frames[i])
                    .Parallel(stream => stream.Aggregate(0, (prev, v) => v + prev), true)
                    .Do(x => results.Add(x.DeepClone()));

                p.Run();
            }

            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 10 }, { 3, 30 } }, results[0]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 20 }, { 3, 60 }, { 5, 50 } }, results[1]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 30 }, { 2, 20 }, { 3, 90 }, { 4, 40 }, { 5, 100 } }, results[2]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 40 }, { 2, 40 }, { 3, 120 }, { 4, 80 }, { 5, 150 } }, results[3]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 50 }, { 2, 60 }, { 3, 150 }, { 4, 120 }, }, results[4]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 60 }, { 2, 80 }, { 3, 180 }, { 4, 160 }, { 5, 50 } }, results[5]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 70 }, { 2, 100 }, { 3, 210 }, { 4, 200 }, { 5, 100 } }, results[6]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 80 }, { 2, 120 }, { 3, 240 } }, results[7]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 90 }, { 2, 140 } }, results[8]);
            CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 100 }, { 2, 160 } }, results[9]);
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
                    new Dictionary<int, int> {            { 2, 29 } },
                };

            var results = new List<string>();
            var lastOriginatingTime = new DateTime[3];

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, frames.Length, TimeSpan.FromMilliseconds(10))
                    .Select(i => frames[i])
                    .Parallel((i, stream) =>
                    {
                        // verify unsubscribed originating time matches last message on stream
                        var receiver = stream.Out.Pipeline.CreateReceiver<int>(this, (_, e) => lastOriginatingTime[i] = e.OriginatingTime, string.Empty);
                        receiver.Unsubscribed += originatingTime => Assert.AreEqual(lastOriginatingTime[i], originatingTime);
                        stream.Out.PipeTo(receiver);

                        return stream.Window(-TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
                    })
                    .Do(x =>
                    {
                        var sb = new StringBuilder();
                        foreach (var k in x.Keys.OrderBy(v => v)) // ensure keys are in sorted order for comparison
                        {
                            sb.Append($" {k}->");
                            foreach (var v in x[k])
                            {
                                sb.Append($"{v},");
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
        public void SparseVectorTestJoinOutputDefaultIfDropped()
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
            // If parallel is using the regular Join (instead of outputDefaultIfDropped: true)
            // then we should have 5 messages out, like this
            // 1: 0 2 4  6  8
            // 2: 0 4 8 12 16
            // When parallel operates correctly with a outputDefaultIfDropped: true, we should get
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

            Assert.IsTrue(results.Count == 10);

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

                var parallelSparse = new ParallelSparseSelect<Dictionary<int, int>, int, int, int, Dictionary<int, int>>(p, _ => _, (i, prod) => prod, _ => _, false);
                Assert.AreEqual(p, parallelSparse.Out.Pipeline); // composite components shouldn't expose subpipelines
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseVectorWithGammaCreatingHolesAndOutputDefaultIfDropped()
        {
            var frames =
                new[]
                {
                    //                           full        end         start       mid         intermittent
                    new Dictionary<int, char> { { 1, 'A' },             { 3, 'C' }                         },
                    new Dictionary<int, char> { { 1, 'A' },             { 3, 'C' },             { 5, 'E' } },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' },            },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }                         },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }                                     },
                    new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }                                     },
                };

            var results = new List<Dictionary<int, char>>();

            using (var p = Pipeline.Create())
            {
                var stepper = new ManualSteppingGenerator<Dictionary<int, char>>(p, frames);
                stepper.Out
                    .Parallel(stream => stream.Join(stream.Count()).Where(x => x.Item2 != 3).Select(x => x.Item1), true) // drop 3rd value
                    .Do(x => results.Add(x.DeepClone()));

                //  Where filter introduces "holes" when > 100
                //
                // A   C
                // A   C   E
                // - B - D E
                // A B C D -
                // A - C -
                // A B C D E
                // A B C D E
                // A B C
                // A B
                // A B

                p.RunAsync();

                void Step(int expected)
                {
                    stepper.Step();
                    while (results.Count != expected)
                    {
                        Thread.Sleep(10);
                    }

                    Assert.AreEqual<int>(expected, results.Count);
                }

                Assert.AreEqual<int>(0, results.Count);

                Step(1); // A   C
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 3, 'C' } }, results[0]);

                Step(2); // A   C   E
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 3, 'C' }, { 5, 'E' } }, results[1]);

                Step(2); // - B - D E (holes) no new results

                Step(3); // A B C D - (hole) stream 5/E closed
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, '\0' }, { 2, 'B' }, { 3, '\0' }, { 4, 'D' }, { 5, 'E' } }, results[2]); // note default '\0' values

                Step(4); // A - C -
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, '\0' } }, results[3]); // note default '\0' value

                Step(6); // A B C D E stream 5/E "reopens" two outputs
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, '\0' }, { 3, 'C' }, { 4, '\0' } }, results[4]); // note default '\0' values
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } }, results[5]);

                Step(7); // A B C D E
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' }, { 4, 'D' }, { 5, 'E' } }, results[6]);

                Step(8); // A B C streams 4/D and 5/E closed
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' }, { 3, 'C' } }, results[7]);

                Step(9); // A B stream 3/C closed
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' } }, results[8]);

                Step(10); // A B streams 1/A and 2/B now closed
                CollectionAssert.AreEquivalent(new Dictionary<int, char> { { 1, 'A' }, { 2, 'B' } }, results[9]);

                Assert.IsFalse(stepper.Step()); // we're done

                p.WaitAll();
            }
        }

        private class ManualSteppingGenerator<T> : IProducer<T>, ISourceComponent
        {
            private readonly Pipeline pipeline;
            private readonly List<T> sequence;

            private int index = 0;
            private Action<DateTime> notifyCompletionTime;
            private DateTime lastTime;

            public ManualSteppingGenerator(Pipeline pipeline, IEnumerable<T> sequence)
            {
                this.pipeline = pipeline;
                this.sequence = sequence.ToList();
                this.Out = pipeline.CreateEmitter<T>(this, "Out");
            }

            public Emitter<T> Out { get; private set; }

            public bool Step()
            {
                if (this.index < this.sequence.Count)
                {
                    this.lastTime = this.pipeline.GetCurrentTime();
                    this.Out.Post(this.sequence[this.index++], this.lastTime);
                    return true;
                }

                this.notifyCompletionTime(this.lastTime);
                return false;
            }

            public void Start(Action<DateTime> notifyCompletionTime)
            {
                this.notifyCompletionTime = notifyCompletionTime;
            }

            public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
            {
                notifyCompleted();
            }
        }
    }
}
