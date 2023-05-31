// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi.Arrays;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ArrayTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void RangeTest()
        {
            var r1 = new Range(100, 200);
            Assert.AreEqual(100, r1.Start);
            Assert.AreEqual(200, r1.End);
            Assert.IsTrue(r1.IsIncreasing);
            Assert.IsFalse(r1.IsSingleValued);
            Assert.AreEqual(101, r1.Size);
            Assert.AreEqual(new Range(100, 200), r1);
            Assert.AreNotEqual(new Range(100, 100), r1);

            var r2 = new Range(200, 100);
            Assert.AreEqual(200, r2.Start);
            Assert.AreEqual(100, r2.End);
            Assert.IsFalse(r2.IsIncreasing);
            Assert.IsFalse(r2.IsSingleValued);
            Assert.AreEqual(101, r2.Size);
            Assert.AreEqual(new Range(200, 100), r2);
            Assert.AreNotEqual(r1, r2);

            var r3 = new Range(100, 100);
            Assert.AreEqual(100, r3.Start);
            Assert.AreEqual(100, r3.End);
            Assert.IsTrue(r3.IsIncreasing);
            Assert.IsTrue(r3.IsSingleValued);
            Assert.AreEqual(1, r3.Size);
            Assert.AreEqual(new Range(100, 100), r3);
            Assert.AreNotEqual(r1, r3);
        }

        [TestMethod]
        [Timeout(60000)]
        public void IndexDefinitionTest()
        {
            var def1 = new RangeIndexDefinition(5);
            Assert.IsTrue(def1.Values.SequenceEqual(new[] { 0, 1, 2, 3, 4 }));
            Assert.AreEqual(0, def1.Start);
            Assert.AreEqual(4, def1.End);
            Assert.AreEqual(5, def1.Count);
            Assert.AreEqual(1, def1.ElementStride);
            Assert.IsTrue(def1.Ranges.SequenceEqual(new Range[] { (0, 4) }));

            var def2 = new RangeIndexDefinition((5, 10), 10);
            Assert.IsTrue(def2.Values.SequenceEqual(new[] { 5, 6, 7, 8, 9, 10 }));
            Assert.AreEqual(5, def2.Start);
            Assert.AreEqual(10, def2.End);
            Assert.AreEqual(6, def2.Count);
            Assert.AreEqual(10, def2.ElementStride);
            Assert.IsTrue(def2.Ranges.SequenceEqual(new Range[] { (5, 10) }));

            var def3 = (RangeIndexDefinition)def2.Slice((1, 3));
            Assert.IsTrue(def3.Values.SequenceEqual(new[] { 6, 7, 8 }));
            Assert.AreEqual(6, def3.Start);
            Assert.AreEqual(8, def3.End);
            Assert.AreEqual(3, def3.Count);
            Assert.AreEqual(10, def3.ElementStride);
            Assert.IsTrue(def3.Ranges.SequenceEqual(new Range[] { (6, 8) }));

            var def4 = def2.Take(1, 3);
            Assert.IsTrue(def4.Values.SequenceEqual(new[] { 6, 8 }));
            Assert.AreEqual(2, def4.Count);
            Assert.AreEqual(10, def4.ElementStride);
            Assert.IsTrue(def4.Ranges.SequenceEqual(new Range[] { (6, 6), (8, 8) }));

            var def5 = def4.Slice((0, 1));
            Assert.IsTrue(def5.Values.SequenceEqual(new[] { 6, 8 }));
            Assert.AreEqual(2, def5.Count);
            Assert.AreEqual(10, def5.ElementStride);
            Assert.IsTrue(def5.Ranges.SequenceEqual(new Range[] { (6, 6), (8, 8) }));
        }

        [TestMethod]
        [Timeout(60000)]
        public void Indexer2dTest()
        {
            var indexer = new Indexer2d(10, 10);
            Assert.AreEqual(100, indexer.Count);
            Assert.IsTrue(indexer.Ranges.SequenceEqual(new Range[] { (0, 99) }));
            Assert.IsTrue(indexer.Values.SequenceEqual(Enumerable.Range(0, indexer.Count)));

            var idx2 = indexer.Slice((2, 3), (1, 5));
            Assert.AreEqual(10, idx2.Count);
            Assert.IsTrue(idx2.Ranges.SequenceEqual(new Range[] { (21, 25), (31, 35) }));
            Assert.IsTrue(idx2.Values.SequenceEqual(new[] { 21, 22, 23, 24, 25, 31, 32, 33, 34, 35 }));

            var idx3 = idx2.Slice(Range.All, (1, 2));
            Assert.AreEqual(4, idx3.Count);
            Assert.IsTrue(idx3.Ranges.SequenceEqual(new Range[] { (22, 23), (32, 33) }));
            Assert.IsTrue(idx3.Values.SequenceEqual(new[] { 22, 23, 32, 33 }));

            indexer = new Indexer2d(2, 5, 8);
            Assert.AreEqual(10, indexer.Count);
            Assert.IsTrue(indexer.Ranges.SequenceEqual(new Range[] { (0, 4), (8, 12) }));
            Assert.IsTrue(indexer.Values.SequenceEqual(new[] { 0, 1, 2, 3, 4, 8, 9, 10, 11, 12 }));

            idx2 = indexer.Slice(Range.All, (0, 2));
            Assert.AreEqual(6, idx2.Count);
            Assert.IsTrue(idx2.Ranges.SequenceEqual(new Range[] { (0, 2), (8, 10) }));
            Assert.IsTrue(idx2.Values.SequenceEqual(new[] { 0, 1, 2, 8, 9, 10 }));
        }

        [TestMethod]
        [Timeout(60000)]
        public void IndexerNdTest()
        {
            var indexer = new IndexerNd(10, 10, 10);
            Assert.AreEqual(1000, indexer.Count);
            Assert.IsTrue(indexer.Ranges.SequenceEqual(new Range[] { (0, 999) }));
            Assert.IsTrue(indexer.Values.SequenceEqual(Enumerable.Range(0, indexer.Count)));

            var idx2 = indexer.Slice((0, 1), Range.All, Range.All);
            Assert.AreEqual(200, idx2.Count);
            Assert.IsTrue(idx2.Ranges.SequenceEqual(new Range[] { (0, 199) }));
            Assert.IsTrue(idx2.Values.SequenceEqual(Enumerable.Range(0, idx2.Count)));

            var idx3 = indexer.Slice((0, 1), (1, 2), Range.All);
            Assert.AreEqual(40, idx3.Count);
            Assert.IsTrue(idx3.Ranges.SequenceEqual(new Range[] { (10, 29), (110, 129) }));

            var idx4 = indexer.Slice((1, 0), Range.All, Range.All);
            Assert.AreEqual(200, idx4.Count);
            Assert.IsTrue(idx4.Ranges.SequenceEqual(new Range[] { (100, 199), (0, 99) }));
        }
    }
}
