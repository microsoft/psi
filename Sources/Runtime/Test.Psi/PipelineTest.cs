// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PipelineTest
    {
        [TestMethod]
        [Timeout(60000)]
        public void Pass_Data_From_One_Pipeline_To_Another()
        {
            using (var p1 = Pipeline.Create("a"))
            using (var p2 = Pipeline.Create("b"))
            {
                var ready = new AutoResetEvent(false);
                var src = Generators.Sequence(p1, new[] { 1, 2, 3 });
                var dest = new Processor<int, int>(p2, (i, e, o) => o.Post(i, e.OriginatingTime));
                dest.Do(i => ready.Set());
                src.PipeTo(dest);

                p2.RunAsync();
                p1.Run();
                Assert.IsTrue(ready.WaitOne(100));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Perf_Of_Allocation()
        {
            var bytes = new byte[100];
            using (var p1 = Pipeline.Create("a"))
            {
                var count = 100;
                var ready = new AutoResetEvent(false);
                var src = Generators.Timer(p1, TimeSpan.FromMilliseconds(5));
                src
                    .Select(t => new byte[100], DeliveryPolicy.Unlimited)
                    .Select(b => b[50], DeliveryPolicy.Unlimited)
                    .Do(_ =>
                    {
                        if (count > 0)
                        {
                            count--;
                        }
                        else
                        {
                            ready.Set();
                        }
                    });

                p1.RunAsync();
                ready.WaitOne(-1);
                Assert.AreEqual(0, count);
            }
        }

        private IEnumerator<(int, DateTime)> Gen()
        {
            DateTime now = DateTime.UtcNow;
            int i = 0;
            while (true)
            {
                yield return (i, now);
                i++;
                now = now + TimeSpan.FromMilliseconds(5);
            }
        }
    }
}
