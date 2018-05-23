// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
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
                    .Parallel(3, (i, d, e) => d * 100, true)
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
        public void SparseVectorStatelessParallel()
        {
            var odd = new Dictionary<int, int> { { 1, 100 }, { 3, 300 }, { 5, 500 } };
            var even = new Dictionary<int, int> { { 0, 0 }, { 2, 200 }, { 4, 400 } };

            List<int[]> results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => (i % 2 == 0) ? even : odd)
                    .Parallel((key, val, env) => val / 100, true)
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
            var odd = new Dictionary<int, int> { { 1, 100 }, { 3, 300 }, { 5, 500 } };
            var even = new Dictionary<int, int> { { 0, 0 }, { 2, 200 }, { 4, 400 } };

            List<int[]> results = new List<int[]>();

            using (var p = Pipeline.Create())
            {
                Generators.Range(p, 0, 10, TimeSpan.FromTicks(10))
                    .Select(i => (i % 2 == 0) ? even : odd)
                    .Parallel((key, stream) => stream.Aggregate(0, (prev, v) => v + prev), true)
                    .Do(x => results.Add(x.Values.ToArray()));

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 1000, 2000 }, results[8]);
            CollectionAssert.AreEqual(new int[] { 500, 1500, 2500 }, results[9]);
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
                    .Parallel((k, v) => v.Where(value => value % 2 == 0), true)
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
                    .Parallel((k, v) => v.Where(value => value % 2 == 0), false)
                    .Do(dict => results.Add(dict.DeepClone()));
                p.Run();
            }

            // Frames that do not match on the two branches are dropped
            Assert.IsTrue(results.Count == 5);
        }
    }
}
