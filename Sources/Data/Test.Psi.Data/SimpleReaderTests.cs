// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class SimpleReaderTests
    {
        private string path = Path.Combine(Environment.CurrentDirectory, nameof(SimpleReaderTests));

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(this.path);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestRunner.SafeDirectoryDelete(this.path, true);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SimpleReader()
        {
            var count = 100;
            var before = new Envelope[count];
            var after = new Envelope[count];
            var name = nameof(this.SimpleReader);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 0, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq", writeStore);
                seq.Do((m, e) => before[m] = e);
                p.Run();
            }

            // now read using the simple reader
            using (var reader = new SimpleReader(name, this.path))
            {
                reader.OpenStream<int>("seq", (s, e) => after[s] = e);
                reader.ReadAll(ReplayDescriptor.ReplayAll);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(before[i], after[i]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SimpleReaderLargeStream()
        {
            var count = 10;
            var name = nameof(this.SimpleReaderLargeStream);
            var size = 10240;
            var bytes = new byte[size];

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 0, i => i + 1, count, TimeSpan.FromTicks(1));
                var big = seq.Select(i => bytes.Select(_ => i).ToArray());
                seq.Write("seq", writeStore);
                big.Write("big", writeStore, largeMessages: true);
                p.Run();
            }

            // now replay the contents and verify we get something
            List<IndexEntry> index = new List<IndexEntry>();

            // now read using the simple reader
            var result = new int[size];
            using (var reader = new SimpleReader(name, this.path))
            {
                reader.OpenStreamIndex<int[]>("big", (ie, e) => index.Add(ie));
                reader.ReadAll(ReplayDescriptor.ReplayAll);

                Assert.AreEqual(count, index.Count());
                var probe = count / 2;
                var entry = index[probe];
                reader.Read<int[]>(entry, ref result);
                Assert.AreEqual(result.Sum(x => x), probe * size);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void RetrieveStreamSupplementalMetadata()
        {
            var name = nameof(this.RetrieveStreamSupplementalMetadata);

            // create store with supplemental meta
            using (var p = Pipeline.Create("write"))
            {
                var store = Store.Create(p, name, this.path);
                var stream0 = Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
                var stream1 = Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
                stream0.Write("NoMeta", store, true);
                stream1.Write(("Favorite irrational number", Math.E), "WithMeta", store);
            }

            // read it back with a simple reader
            var reader = new SimpleReader(name, this.path);
            Assert.IsNull(reader.GetMetadata("NoMeta").SupplementalMetadataTypeName);
            Assert.AreEqual(typeof(ValueTuple<string, double>).AssemblyQualifiedName, reader.GetMetadata("WithMeta").SupplementalMetadataTypeName);
            var supplemental1 = reader.GetSupplementalMetadata<(string, double)>("WithMeta");
            Assert.AreEqual("Favorite irrational number", supplemental1.Item1);
            Assert.AreEqual(Math.E, supplemental1.Item2);
        }
    }
}
