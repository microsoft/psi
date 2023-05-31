// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JoinTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void JoinClosingSecondary()
        {
            using var p = Pipeline.Create();

            // primary    0       1       2       3       4       5       6       7       8       9
            // secondary  0   1   2   3   4   5   6   7   8   9
            // joined    (0,0)   (1,2)   (2,4)   (3,6)   (4,8)   (5,9)   (6,9)   (7,9)   (8,9)   (9,9)
            //                                                    ^       ^       ^       ^       ^
            //                                                    note: normally these would remain unpaired
            //                                                          until seeing next secondary message
            var primary = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(100));
            var secondary = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(50));
            var joined = primary.Join(secondary, RelativeTimeInterval.Infinite);
            var results = joined.Select(x => $"{x.Item1},{x.Item2}").ToObservable().ToListObservable();
            p.Run();

            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "0,0", "1,2", "2,4", "3,6", "4,8", "5,9", "6,9", "7,9", "8,9", "9,9" }, results));
        }

        [TestMethod]
        [Timeout(60000)]
        public void DynamicJoinClosingSecondaryOrDefault()
        {
            using var p = Pipeline.Create();

            // Setup a sequence with a parallel operator, with outputDefaultIfDropped = true, and ensure that
            // the "outputDefaultIfDropped" is correctly applied while the instance substream exists, but not outside of that existance.
            // This tests for making sure we are correctly tracking stream closings and the interpolator
            // in Join is doing the right thing based on the stream closing times.

            // key           N/A       1       1       1       1       1       1       N/A     N/A     N/A
            // value         N/A       1       2       3       4       5       6       N/A     N/A     N/A
            // gamma-result           [1       2       -       4       -       -]
            // out                     1       2       0       4       0       0
            var input = Generators.Sequence(
                p,
                new List<Dictionary<int, int>>()
                {
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>() { { 1, 1 } },
                        new Dictionary<int, int>() { { 1, 2 } },
                        new Dictionary<int, int>() { { 1, 3 } },
                        new Dictionary<int, int>() { { 1, 4 } },
                        new Dictionary<int, int>() { { 1, 5 } },
                        new Dictionary<int, int>() { { 1, 6 } },
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                },
                TimeSpan.FromTicks(1));

            var resultsParallelOrDefault = new List<int>();
            input.Parallel(s => s.Where(x => x != 3 && x <= 4), outputDefaultIfDropped: true).Do(d =>
            {
                if (d.Count() > 0)
                {
                    resultsParallelOrDefault.Add(d[1]);
                }
            });

            var resultsParallel = new List<int>();
            input.Parallel(s => s.Where(x => x != 3 && x <= 4)).Do(d =>
            {
                if (d.Count() > 0)
                {
                    resultsParallel.Add(d[1]);
                }
            });

            p.Run();

            Assert.IsTrue(Enumerable.SequenceEqual(resultsParallel, new[] { 1, 2, 4 }));
            Assert.IsTrue(Enumerable.SequenceEqual(resultsParallelOrDefault, new[] { 1, 2, 0, 4, 0, 0 }));
        }

        [TestMethod]
        [Timeout(60000)]
        public void ScalarJoin()
        {
            var resultsAB = new List<ValueTuple<int, int>>();
            var resultsAA = new List<ValueTuple<int, int>>();
            var resultsBA = new List<ValueTuple<int, int>>();
            var resultsBB = new List<ValueTuple<int, int>>();

            using (var p = Pipeline.Create())
            {
                var sourceA = Generators.Sequence(p, 0, i => i + 1, 30, TimeSpan.FromTicks(10));
                var sourceB = Generators.Sequence(p, 0, i => i + 1, 3, TimeSpan.FromTicks(100));
                sourceA
                    .Join(sourceB, TimeSpan.FromTicks(5))
                    .Do(t => resultsAB.Add(ValueTuple.Create(t.Item1, t.Item2)));
                sourceA
                    .Join(sourceA, TimeSpan.FromTicks(5))
                    .Do(t => resultsAA.Add(ValueTuple.Create(t.Item1, t.Item2)));
                sourceB
                    .Join(sourceA, TimeSpan.FromTicks(5))
                    .Do(t => resultsBA.Add(ValueTuple.Create(t.Item1, t.Item2)));
                sourceB
                    .Join(sourceB, TimeSpan.FromTicks(5))
                    .Do(t => resultsBB.Add(ValueTuple.Create(t.Item1, t.Item2)));
                p.Run(new ReplayDescriptor(DateTime.UtcNow, DateTime.MaxValue));
            }

            Assert.AreEqual(3, resultsAB.Count);
            Assert.AreEqual(ValueTuple.Create(0, 0), resultsAB[0]);
            Assert.AreEqual(ValueTuple.Create(10, 1), resultsAB[1]);
            Assert.AreEqual(ValueTuple.Create(20, 2), resultsAB[2]);

            Assert.AreEqual(3, resultsBA.Count);
            Assert.AreEqual(ValueTuple.Create(0, 0), resultsBA[0]);
            Assert.AreEqual(ValueTuple.Create(1, 10), resultsBA[1]);
            Assert.AreEqual(ValueTuple.Create(2, 20), resultsBA[2]);

            Assert.AreEqual(30, resultsAA.Count);
            for (int i = 0; i < 30; i++)
            {
                Assert.AreEqual(ValueTuple.Create(i, i), resultsAA[i]);
            }

            Assert.AreEqual(3, resultsBB.Count);
            Assert.AreEqual(ValueTuple.Create(0, 0), resultsBB[0]);
            Assert.AreEqual(ValueTuple.Create(1, 1), resultsBB[1]);
            Assert.AreEqual(ValueTuple.Create(2, 2), resultsBB[2]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void VectorJoin()
        {
            var results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                var sourceA = Generators.Sequence(p, 0, i => i + 1, 30, TimeSpan.FromTicks(10));
                var sourceB = Generators.Sequence(p, 0, i => i + 1, 3, TimeSpan.FromTicks(100));
                Operators
                    .Join(new[] { sourceA, sourceB, sourceA, sourceB }, TimeSpan.FromTicks(5))
                    .Do(t => results.Add(t.DeepClone()));
                p.Run(new ReplayDescriptor(DateTime.UtcNow, DateTime.MaxValue));
            }

            Assert.AreEqual(3, results.Count);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, results[0]);
            CollectionAssert.AreEqual(new[] { 10, 1, 10, 1 }, results[1]);
            CollectionAssert.AreEqual(new[] { 20, 2, 20, 2 }, results[2]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void VectorJoinWithArityOne()
        {
            var results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                var source = Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(1));
                Operators
                    .Join(new[] { source }, TimeSpan.FromTicks(5))
                    .Do(t => results.Add(t.DeepClone()));
                p.Run(new ReplayDescriptor(DateTime.UtcNow, DateTime.MaxValue));
            }

            Assert.AreEqual(10, results.Count);
            for (var i = 0; i < 10; i++)
            {
                CollectionAssert.AreEqual(new[] { i }, results[i]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingJoin()
        {
            using var pipeline = Pipeline.Create();
            var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(10));
            var sourceA = range.Select(x => $"A{x}");
            var sourceB = range.Select(x => $"B{x}");
            var sourceC = range.Select(x => $"C{x}");
            var sourceD = range.Select(x => $"D{x}");
            var sourceE = range.Select(x => $"E{x}");
            var sourceF = range.Select(x => $"F{x}");
            var sourceG = range.Select(x => $"G{x}");

            var tuples =
                sourceA
                    .Join(sourceB, Reproducible.Nearest<string>())
                    .Join(sourceC, Reproducible.Nearest<string>())
                    .Join(sourceD, Reproducible.Nearest<string>())
                    .Join(sourceE, Reproducible.Nearest<string>())
                    .Join(sourceF, Reproducible.Nearest<string>())
                    .Join(sourceG, Reproducible.Nearest<string>())
                    .ToObservable().ToListObservable();
            pipeline.Run();

            var results = tuples.AsEnumerable().ToArray();

            Assert.IsTrue(Enumerable.SequenceEqual(
                new ValueTuple<string, string, string, string, string, string, string>[]
                {
                        ValueTuple.Create("A0", "B0", "C0", "D0", "E0", "F0", "G0"),
                        ValueTuple.Create("A1", "B1", "C1", "D1", "E1", "F1", "G1"),
                        ValueTuple.Create("A2", "B2", "C2", "D2", "E2", "F2", "G2"),
                        ValueTuple.Create("A3", "B3", "C3", "D3", "E3", "F3", "G3"),
                        ValueTuple.Create("A4", "B4", "C4", "D4", "E4", "F4", "G4"),
                        ValueTuple.Create("A5", "B5", "C5", "D5", "E5", "F5", "G5"),
                        ValueTuple.Create("A6", "B6", "C6", "D6", "E6", "F6", "G6"),
                        ValueTuple.Create("A7", "B7", "C7", "D7", "E7", "F7", "G7"),
                        ValueTuple.Create("A8", "B8", "C8", "D8", "E8", "F8", "G8"),
                        ValueTuple.Create("A9", "B9", "C9", "D9", "E9", "F9", "G9"),
                },
                results));
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingReversedJoin()
        {
            using var pipeline = Pipeline.Create();
            var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(10));
            var sourceA = range.Select(x => $"A{x}");
            var sourceB = range.Select(x => $"B{x}");
            var sourceC = range.Select(x => $"C{x}");
            var sourceD = range.Select(x => $"D{x}");
            var sourceE = range.Select(x => $"E{x}");
            var sourceF = range.Select(x => $"F{x}");
            var sourceG = range.Select(x => $"G{x}");

            var tuplesFG = sourceF.Join(sourceG);
            var tuplesEFG = sourceE.Join(tuplesFG);
            var tuplesDEFG = sourceD.Join(tuplesEFG);
            var tuplesCDEFG = sourceC.Join(tuplesDEFG);
            var tuplesBCDEFG = sourceB.Join(tuplesCDEFG);
            var tuplesABCDEFG = sourceA.Join(tuplesBCDEFG);
            var tuples = tuplesABCDEFG.ToObservable().ToListObservable();
            pipeline.Run();

            var results = tuples.AsEnumerable().ToArray();

            Assert.IsTrue(Enumerable.SequenceEqual(
                new ValueTuple<string, string, string, string, string, string, string>[]
                {
                        ValueTuple.Create("A0", "B0", "C0", "D0", "E0", "F0", "G0"),
                        ValueTuple.Create("A1", "B1", "C1", "D1", "E1", "F1", "G1"),
                        ValueTuple.Create("A2", "B2", "C2", "D2", "E2", "F2", "G2"),
                        ValueTuple.Create("A3", "B3", "C3", "D3", "E3", "F3", "G3"),
                        ValueTuple.Create("A4", "B4", "C4", "D4", "E4", "F4", "G4"),
                        ValueTuple.Create("A5", "B5", "C5", "D5", "E5", "F5", "G5"),
                        ValueTuple.Create("A6", "B6", "C6", "D6", "E6", "F6", "G6"),
                        ValueTuple.Create("A7", "B7", "C7", "D7", "E7", "F7", "G7"),
                        ValueTuple.Create("A8", "B8", "C8", "D8", "E8", "F8", "G8"),
                        ValueTuple.Create("A9", "B9", "C9", "D9", "E9", "F9", "G9"),
                },
                results));
        }
    }
}
