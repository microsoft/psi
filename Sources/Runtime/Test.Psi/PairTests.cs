// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PairTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedPrimary()
        {
            using (var pipeline = Pipeline.Create())
            {
                var primary = Generators.Range(pipeline, 0, 5).Delay(TimeSpan.FromMilliseconds(100));
                var secondary = Generators.Range(pipeline, 0, 5);
                var paired = primary.Pair(secondary).ToObservable().ToListObservable();
                pipeline.Run();

                var results = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 4, 4, 4, 4, 4 }, ValueTuple.Create), results));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondary()
        {
            using (var pipeline = Pipeline.Create())
            {
                var primary = Generators.Range(pipeline, 0, 5);
                var secondary = Generators.Range(pipeline, 0, 5).Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary).ToObservable().ToListObservable();
                pipeline.Run();

                var results = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new ValueTuple<int, int>[] { }, results));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondaryWithInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                var primary = Generators.Range(pipeline, 0, 5);
                var secondary = Generators.Range(pipeline, 0, 5).Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary, 42).ToObservable().ToListObservable();
                pipeline.Run();

                var results = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 42, 42, 42, 42, 42 }, ValueTuple.Create), results));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedPrimaryWithOutputCreator()
        {
            using (var pipeline = Pipeline.Create())
            {
                var primary = Generators.Range(pipeline, 0, 5).Delay(TimeSpan.FromMilliseconds(100));
                var secondary = Generators.Range(pipeline, 0, 5);
                var paired = primary.Pair(secondary, (p, s) => p * 10 + s).ToObservable().ToListObservable();
                pipeline.Run();

                var results = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 04, 14, 24, 34, 44 }, results));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondryWithInitialValueAndOutputCreator()
        {
            using (var pipeline = Pipeline.Create())
            {
                var primary = Generators.Range(pipeline, 0, 5);
                var secondary = Generators.Range(pipeline, 0, 5).Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary, (p, s) => p * 10 + s, 7).ToObservable().ToListObservable();
                pipeline.Run();

                var results = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 07, 17, 27, 37, 47 }, results));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingPairWithInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(100));
                var sourceA = range.Select(x => $"A{x}");
                var sourceB = range.Select(x => $"B{x}");
                var sourceC = range.Select(x => $"C{x}");
                var sourceD = range.Select(x => $"D{x}");
                var sourceE = range.Select(x => $"E{x}");
                var sourceF = range.Select(x => $"F{x}");
                var sourceG = range.Select(x => $"G{x}");

                ListObservable<ValueTuple<string, string, string, string, string, string, string>> tuples = // expecting tuple flattening
                    sourceA
                        .Pair(sourceB, "B?")
                        .Pair(sourceC, "C?")
                        .Pair(sourceD, "D?")
                        .Pair(sourceE, "E?")
                        .Pair(sourceF, "F?")
                        .Pair(sourceG, "G?")
                        .ToObservable().ToListObservable();
                pipeline.Run();

                var results = tuples.AsEnumerable().ToArray();

                Assert.AreEqual(10, results.Length);

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in results)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingPairWithoutInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(100));
                var sourceA = range.Select(x => $"A{x}");
                var sourceB = range.Select(x => $"B{x}");
                var sourceC = range.Select(x => $"C{x}");
                var sourceD = range.Select(x => $"D{x}");
                var sourceE = range.Select(x => $"E{x}");
                var sourceF = range.Select(x => $"F{x}");
                var sourceG = range.Select(x => $"G{x}");

                ListObservable<ValueTuple<string, string, string, string, string, string, string>> tuples = // expecting tuple flattening
                    sourceA
                        .Pair(sourceB)
                        .Pair(sourceC)
                        .Pair(sourceD)
                        .Pair(sourceE)
                        .Pair(sourceF)
                        .Pair(sourceG)
                        .ToObservable().ToListObservable();
                pipeline.Run();

                // cannot validate length as above because without initial value, it is non-deterministic
                var results = tuples.AsEnumerable().ToArray();

                // cannot validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in results)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingReversedPairWithoutInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(100));
                var sourceA = range.Select(x => $"A{x}");
                var sourceB = range.Select(x => $"B{x}");
                var sourceC = range.Select(x => $"C{x}");
                var sourceD = range.Select(x => $"D{x}");
                var sourceE = range.Select(x => $"E{x}");
                var sourceF = range.Select(x => $"F{x}");
                var sourceG = range.Select(x => $"G{x}");

                var tuplesFG = sourceF.Pair(sourceG);
                var tuplesEFG = sourceE.Pair(tuplesFG);
                var tuplesDEFG = sourceD.Pair(tuplesEFG);
                var tuplesCDEFG = sourceC.Pair(tuplesDEFG);
                var tuplesBCDEFG = sourceB.Pair(tuplesCDEFG);
                var tuplesABCDEFG = sourceA.Pair(tuplesBCDEFG);
                ListObservable<ValueTuple<string, string, string, string, string, string, string>> tuples = tuplesABCDEFG.ToObservable().ToListObservable();
                pipeline.Run();

                // cannot validate length as above because without initial value, it is non-deterministic
                var results = tuples.AsEnumerable().ToArray();

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in results)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TupleCollapsingReversedPairWithInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                var range = Generators.Range(pipeline, 0, 10, TimeSpan.FromMilliseconds(100));
                var sourceA = range.Select(x => $"A{x}");
                var sourceB = range.Select(x => $"B{x}");
                var sourceC = range.Select(x => $"C{x}");
                var sourceD = range.Select(x => $"D{x}");
                var sourceE = range.Select(x => $"E{x}");
                var sourceF = range.Select(x => $"F{x}");
                var sourceG = range.Select(x => $"G{x}");

                var tuplesFG = sourceF.Pair(sourceG, "G?");
                var tuplesEFG = sourceE.Pair(tuplesFG, ValueTuple.Create("F?", "G?"));
                var tuplesDEFG = sourceD.Pair(tuplesEFG, ValueTuple.Create("E?", "F?", "G?"));
                var tuplesCDEFG = sourceC.Pair(tuplesDEFG, ValueTuple.Create("D?", "E?", "F?", "G?"));
                var tuplesBCDEFG = sourceB.Pair(tuplesCDEFG, ValueTuple.Create("C?", "D?", "E?", "F?", "G?"));
                var tuplesABCDEFG = sourceA.Pair(tuplesBCDEFG, ValueTuple.Create("B?", "C?", "D?", "E?", "F?", "G?"));
                ListObservable<ValueTuple<string, string, string, string, string, string, string>> tuples = tuplesABCDEFG.ToObservable().ToListObservable();
                pipeline.Run();

                var results = tuples.AsEnumerable().ToArray();

                Assert.AreEqual(10, results.Length);

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in results)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }
            }
        }
    }
}