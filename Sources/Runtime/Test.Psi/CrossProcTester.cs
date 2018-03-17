// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CrossProcTester
    {
        private const int TicksPerMs = 10000;

        // [TestMethod, Timeout(60000)]
        public void MMFTestServer()
        {
            // var source = PsiFactory.CreateTimer("source", 10, (dt, ts) => ts.Ticks);
            // var output = source.Select(l => Math.Sin(l));
            // using (var p = PsiFactory.CompileAndRun("test", output))
            // {
            //     Console.WriteLine("Publishing... Press enter to exit.");
            //     Console.ReadLine();
            // }
        }

        // [TestMethod, Timeout(60000)]
        public void MMFTestClient()
        {
            // var sr = new PsiReader("test");
            // var s = sr.GetStream<double>("Select1.Out"); // "output"
            // var final = s.Do(l => { Console.WriteLine(l); });
            // using (var p = PsiFactory.CompileAndRun("test2", final))
            // {
            //     while (!Console.KeyAvailable)
            //     {
            //     }
            // }
        }
    }
}