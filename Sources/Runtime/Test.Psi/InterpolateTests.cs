// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class InterpolateTest
    {
        [TestMethod]
        [Timeout(60000)]
        public void LinearInterpolate()
        {
            var evenres = new List<(double, DateTime)>();
            var oddres = new List<(double, DateTime)>();
            var results = new List<double>();

            using (var p = Pipeline.Create())
            {
                var source = Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(50));
                var even = source.Where(i => i % 2 == 0).Select(v => (double)v).Do((m, e) => evenres.Add((m, e.OriginatingTime)));
                var odd = source.Where(i => i % 2 == 1).Select(v => (double)v).Do((m, e) => oddres.Add((m, e.OriginatingTime)));
                even
                    .Interpolate(odd, Reproducible.Linear())
                    .Do(results.Add);

                p.Run();
            }

            Assert.AreEqual(results.Count, 4);
            Assert.AreEqual(results[0], 1, 0.00000001);
            Assert.AreEqual(results[1], 3, 0.00000001);
            Assert.AreEqual(results[2], 5, 0.00000001);
            Assert.AreEqual(results[3], 7, 0.00000001);
        }
    }
}
