// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Scenarios and usage around PerfCounters.
    /// </summary>
    [TestClass]
    public class APITester
    {
        /// <summary>
        /// Perf counter test.
        /// </summary>
        /// <remarks>Run "Test.Psi.Windows.exe !PerfCounters" with admin rights to execute this test.</remarks>
        public void PerfCounters()
        {
            using (var p = Pipeline.Create("perf counters"))
            {
                // run forever
                var generate = Generators.Sequence(p, 0, i => i + 1, int.MaxValue, TimeSpan.FromMilliseconds(25));
                var mul = generate.Select(i => i * i, DeliveryPolicy.Unlimited);
                ((IConsumer<int>)mul).In.EnablePerfCounters("mul", new PerfCounters<ReceiverCounters>());

                // start and run the pipeline
                p.Run();
            }
        }
    }
}
