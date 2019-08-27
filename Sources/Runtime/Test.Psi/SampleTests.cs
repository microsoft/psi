// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SampleTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void DenseSample()
        {
            List<int> results = new List<int>();

            using (var p = Pipeline.Create())
            {
                var source = Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(50));
                var filtered = source.Where(i => i % 2 == 0);
                filtered
                    .Sample(source, RelativeTimeInterval.Past())
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 0, 2, 2, 4, 4, 6, 6, 8, 8 }, results);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SparseSample()
        {
            List<int> results = new List<int>();

            using (var p = Pipeline.Create())
            {
                var source = Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(50));
                var filtered = source.Where(i => i % 3 == 0);
                source
                    .Sample(filtered, RelativeTimeInterval.Past())
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new int[] { 0, 3, 6, 9 }, results);
        }
    }
}
