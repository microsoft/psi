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
                Generators.Range(pipeline, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var secondary = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1));
                var primary = secondary.Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary).ToObservable().ToListObservable();
                var fused = primary.Fuse(secondary, Available.Last<int>()).ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 4, 4, 4, 4, 4 }, ValueTuple.Create), pairedResults));

                var fusedResults = fused.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 4, 4, 4, 4, 4 }, ValueTuple.Create), fusedResults));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondary()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Range(pipeline, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var primary = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1));
                var secondary = primary.Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary).ToObservable().ToListObservable();
                var fused = primary.Fuse(secondary, Available.Last<int>()).ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new ValueTuple<int, int>[] { }, pairedResults));

                var fusedResults = fused.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new ValueTuple<int, int>[] { }, fusedResults));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondaryWithInitialValue()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Range(pipeline, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var primary = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1));
                var secondary = primary.Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary, 42).ToObservable().ToListObservable();
                var fused = primary.Fuse(secondary, Available.LastOrDefault(42)).ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 42, 42, 42, 42, 42 }, ValueTuple.Create), pairedResults));

                var fusedResults = fused.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 0, 1, 2, 3, 4 }.Zip(new[] { 42, 42, 42, 42, 42 }, ValueTuple.Create), pairedResults));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedPrimaryWithOutputCreator()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Range(pipeline, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var secondary = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1));
                var primary = secondary.Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary, (p, s) => p * 10 + s).ToObservable().ToListObservable();
                var fused = primary.Fuse(secondary, Available.Last<int>(), (p, s) => p * 10 + s).ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 04, 14, 24, 34, 44 }, pairedResults));

                var fusedResults = fused.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 04, 14, 24, 34, 44 }, fusedResults));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PairDelayedSecondryWithInitialValueAndOutputCreator()
        {
            using (var pipeline = Pipeline.Create())
            {
                Generators.Range(pipeline, 0, 2, TimeSpan.FromSeconds(1)); // hold pipeline open
                var primary = Generators.Range(pipeline, 0, 5, TimeSpan.FromTicks(1));
                var secondary = primary.Delay(TimeSpan.FromMilliseconds(100));
                var paired = primary.Pair(secondary, (p, s) => p * 10 + s, 7).ToObservable().ToListObservable();
                var fused = primary.Fuse(secondary, Available.LastOrDefault(7), (p, s) => p * 10 + s).ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = paired.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 07, 17, 27, 37, 47 }, pairedResults));

                var fusedResults = fused.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 07, 17, 27, 37, 47 }, fusedResults));
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

                var pairedTuples = // expecting tuple flattening
                    sourceA
                        .Pair(sourceB, "B?")
                        .Pair(sourceC, "C?")
                        .Pair(sourceD, "D?")
                        .Pair(sourceE, "E?")
                        .Pair(sourceF, "F?")
                        .Pair(sourceG, "G?")
                        .ToObservable().ToListObservable();

                var fusedTuples = // expecting tuple flattening
                    sourceA
                        .Fuse(sourceB, Available.LastOrDefault("B?"))
                        .Fuse(sourceC, Available.LastOrDefault("C?"))
                        .Fuse(sourceD, Available.LastOrDefault("D?"))
                        .Fuse(sourceE, Available.LastOrDefault("E?"))
                        .Fuse(sourceF, Available.LastOrDefault("F?"))
                        .Fuse(sourceG, Available.LastOrDefault("G?"))
                        .ToObservable().ToListObservable();
                pipeline.Run();

                var pairedResults = pairedTuples.AsEnumerable().ToArray();

                Assert.AreEqual(10, pairedResults.Length);

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in pairedResults)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }

                var fusedResults = fusedTuples.AsEnumerable().ToArray();

                Assert.AreEqual(10, fusedResults.Length);

                // can't really validate content ordering as with Join because Fuse is inherently non-deterministic
                foreach (var r in fusedResults)
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

                var pairedTuples = // expecting tuple flattening
                    sourceA
                        .Pair(sourceB)
                        .Pair(sourceC)
                        .Pair(sourceD)
                        .Pair(sourceE)
                        .Pair(sourceF)
                        .Pair(sourceG)
                        .ToObservable().ToListObservable();

                var fusedTuples = // expecting tuple flattening
                    sourceA
                        .Fuse(sourceB, Available.Last<string>())
                        .Fuse(sourceC, Available.Last<string>())
                        .Fuse(sourceD, Available.Last<string>())
                        .Fuse(sourceE, Available.Last<string>())
                        .Fuse(sourceF, Available.Last<string>())
                        .Fuse(sourceG, Available.Last<string>())
                        .ToObservable().ToListObservable();
                pipeline.Run();

                // cannot validate length as above because without initial value, it is non-deterministic
                var pairedResults = pairedTuples.AsEnumerable().ToArray();

                // cannot validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in pairedResults)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }

                // cannot validate length as above because without initial value, it is non-deterministic
                var fusedResults = fusedTuples.AsEnumerable().ToArray();

                // cannot validate content ordering as with Join because Fuse is inherently non-deterministic
                foreach (var r in fusedResults)
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

                var pairedTuplesFG = sourceF.Pair(sourceG);
                var pairedTuplesEFG = sourceE.Pair(pairedTuplesFG);
                var pairedTuplesDEFG = sourceD.Pair(pairedTuplesEFG);
                var pairedTuplesCDEFG = sourceC.Pair(pairedTuplesDEFG);
                var pairedTuplesBCDEFG = sourceB.Pair(pairedTuplesCDEFG);
                var pairedTuplesABCDEFG = sourceA.Pair(pairedTuplesBCDEFG);
                var pairedTuples = pairedTuplesABCDEFG.ToObservable().ToListObservable();

                var fusedTuplesFG = sourceF.Fuse(sourceG, Available.Last<string>());
                var fusedTuplesEFG = sourceE.Fuse(fusedTuplesFG, Available.Last<(string, string)>());
                var fusedTuplesDEFG = sourceD.Fuse(fusedTuplesEFG, Available.Last<(string, string, string)>());
                var fusedTuplesCDEFG = sourceC.Fuse(fusedTuplesDEFG, Available.Last<(string, string, string, string)>());
                var fusedTuplesBCDEFG = sourceB.Fuse(fusedTuplesCDEFG, Available.Last<(string, string, string, string, string)>());
                var fusedTuplesABCDEFG = sourceA.Fuse(fusedTuplesBCDEFG, Available.Last<(string, string, string, string, string, string)>());
                var fusedTuples = fusedTuplesABCDEFG.ToObservable().ToListObservable();

                pipeline.Run();

                // cannot validate length as above because without initial value, it is non-deterministic
                var pairedResults = pairedTuples.AsEnumerable().ToArray();

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in pairedResults)
                {
                    Assert.IsTrue(r.Item1.StartsWith("A"));
                    Assert.IsTrue(r.Item2.StartsWith("B"));
                    Assert.IsTrue(r.Item3.StartsWith("C"));
                    Assert.IsTrue(r.Item4.StartsWith("D"));
                    Assert.IsTrue(r.Item5.StartsWith("E"));
                    Assert.IsTrue(r.Item6.StartsWith("F"));
                    Assert.IsTrue(r.Item7.StartsWith("G"));
                }

                // cannot validate length as above because without initial value, it is non-deterministic
                var fusedResults = fusedTuples.AsEnumerable().ToArray();

                // can't really validate content ordering as with Join because Pair is inherently non-deterministic
                foreach (var r in fusedResults)
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