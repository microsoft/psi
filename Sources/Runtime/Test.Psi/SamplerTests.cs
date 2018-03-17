// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SamplerTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void DenseSample()
        {
            List<int> results = new List<int>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(100))
                    .Sample(TimeSpan.FromTicks(50), Match.Best<int>(RelativeTimeInterval.RightBounded(TimeSpan.Zero)))
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9 }, results);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseSample()
        {
            List<int> results = new List<int>();

            using (var p = Pipeline.Create())
            {
                Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(100))
                    .Sample(TimeSpan.FromTicks(300), Match.Best<int>(RelativeTimeInterval.RightBounded(TimeSpan.Zero)))
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 3, 6, 9 }, results);
        }
    }
}
