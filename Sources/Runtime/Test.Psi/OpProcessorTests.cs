// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
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
                    .Sequence(p, new[] { 100d, 50, 0 }, TimeSpan.FromTicks(1))
                    .Select(v => avg = avg + (v - avg) / (++count))
                    .Do(Console.WriteLine)
                    .Do(results.Add);

                p.Run();
            }

            CollectionAssert.AreEqual(new double[] { 100, 75, 50 }, results);
        }

        [TestMethod]
        [Timeout(60000)]
        public void StreamEditing()
        {
            using (var pipeline = Pipeline.Create())
            {
                var start = new DateTime(1971, 11, 03);
                var range = Generators.Sequence(pipeline, new[]
                {
                    ('B', start.AddSeconds(1)),
                    ('C', start.AddSeconds(2)),
                    ('D', start.AddSeconds(3)),
                    ('E', start.AddSeconds(4)),
                });
                var edited = range.EditStream(new[]
                {
                    (true, 'F', start.AddSeconds(5)), // insert F after E
                    (false, default(char), start.AddSeconds(2)), // delete C
                    (true, 'A', start), // insert A before B
                    (true, 'X', start.AddSeconds(3)), // update D to X
                }).ToObservable().ToListObservable();
                pipeline.Run();

                var editedResults = edited.AsEnumerable().ToArray();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 'A', 'B', 'X', 'E', 'F' }, editedResults));
            }
        }
    }
}
