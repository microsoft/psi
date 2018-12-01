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
        //// To be fixed along with bug 53200 IStartable closing message itself is throttled [TestMethod]
        [Timeout(60000)]
        public void Throttled()
        {
            // this test may expose a pipeline shutdown bug with startable / external threads.
            var throttlePolicy = new DeliveryPolicy(1, 1, null, true, false);
            using (var p = Pipeline.Create())
            {
                int countA = 0, countB = 0, countC = 0;
                Timers.Timer(p, TimeSpan.FromMilliseconds(1), (dt, ts) => countA++)
                    .Do(_ => countB++, throttlePolicy)
                    .Do(
                        _ =>
                        {
                            Thread.Sleep(5);
                            countC++;
                        }, throttlePolicy);

                p.Run(TimeSpan.FromMilliseconds(100));

                Assert.AreNotEqual(countA, countB);
                Assert.AreEqual(countC, countB);
                Assert.AreNotEqual(0, countA);
            }
        }
    }
}
