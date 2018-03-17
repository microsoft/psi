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
    public class OpProcessorTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void SelectClosure()
        {
            List<double> results = new List<double>();

            using (var p = Pipeline.Create())
            {
                double avg = 0;
                int count = 0;
                Generators
                    .Sequence(p, new[] { 100d, 50, 0 })
                    .Select(v => avg = avg + (v - avg) / (++count))
                    .Do(Console.WriteLine)
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new double[] { 100, 75, 50 }, results);
        }
    }
}
