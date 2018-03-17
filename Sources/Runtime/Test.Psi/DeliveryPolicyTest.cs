// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DeliveryPolicyTest
    {
        [TestMethod]
        [Timeout(60000)]
        public void Throttled()
        {
            // this test may exposes a pipeline shutdown bug with startable / external threads.
            var dp = DeliveryPolicy.Throttled;
            using (var p = Pipeline.Create())
            {
                int countA = 0, countB = 0, countC = 0;
                Generators.Timer(p, TimeSpan.FromMilliseconds(1), (dt, ts) => countA++)
                    .Do(_ => countB++, dp)
                    .Do(
                        _ =>
                        {
                            Thread.Sleep(5);
                            countC++;
                        }, dp);

                p.Run(TimeSpan.FromMilliseconds(100));

                Assert.AreNotEqual(countA, countB);
                Assert.AreEqual(countC, countB);
                Assert.AreNotEqual(0, countA);
            }
        }
    }
}
