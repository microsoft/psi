// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    /// <summary>
    /// Runs a series of tests for stream generators.
    /// </summary>
    [TestClass]
    class GeneratorsTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void AlignedSequenceTest()
        {
            using (var p = Pipeline.Create())
            {
                var gen = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10), DateTime.MinValue);
                var ticksAlign = TimeSpan.FromMilliseconds(10).Ticks;
                gen.Do((x, e) =>
                {
                    Assert.AreEqual(e.OriginatingTime.TimeOfDay.Ticks % ticksAlign, 0);
                });
                p.Run();
            }
        }
    }
}
