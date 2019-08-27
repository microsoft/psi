// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Runs a series of tests for stream generators.
    /// </summary>
    [TestClass]
    public class GeneratorsTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void FiniteAndInfiniteGeneratorsTest()
        {
            // pipeline containing Generators.Return should stop once post happens
            using (var pipeline = Pipeline.Create())
            {
                Generators.Return(pipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(2000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing Generators.Once should not stop once post happens
            using (var pipeline = Pipeline.Create())
            {
                Generators.Once(pipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(2000);
                Assert.IsFalse(stopped);
            }

            // pipeline containing Generators.Return and Generators.Once should stop, b/c Return stops.
            using (var pipeline = Pipeline.Create())
            {
                Generators.Once(pipeline, 123);
                Generators.Return(pipeline, 123);
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(2000);
                Assert.IsTrue(stopped);
            }

            // pipeline containing an infinite Generators.Repeat should not stop
            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 123, TimeSpan.FromMilliseconds(1));
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(2000);
                Assert.IsFalse(stopped);
            }

            // pipeline containing a finite Generators.Repeat should stop
            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 123, 10, TimeSpan.FromMilliseconds(1));
                pipeline.RunAsync();
                var stopped = pipeline.WaitAll(2000);
                Assert.IsTrue(stopped);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void GeneratorSequenceCountTest()
        {
            var list = new List<int>();
            using (var pipeline = Pipeline.Create())
            {
                Generators.Repeat(pipeline, 0, 5, TimeSpan.FromMilliseconds(1)).Do(x => list.Add(x));
                pipeline.Run();
            }

            CollectionAssert.AreEqual(list, new int[] { 0, 0, 0, 0, 0 });
        }


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
