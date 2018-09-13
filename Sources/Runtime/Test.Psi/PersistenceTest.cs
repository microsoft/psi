// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
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
    public class PersistenceTest
    {
        private string path = Path.Combine(Environment.CurrentDirectory, nameof(PersistenceTest));

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
        public void InfiniteFileTest()
        {
            const int extentSize = 10 * 1024;
            const int messagesPerExtent = 3;
            const int messageSize = (int)(extentSize * 0.9) / messagesPerExtent; // not a multiple of 4k and not a divisor of initial file size, to force truncation
            const int extentCount = 5;
            const int iterations = messagesPerExtent * extentCount;
            using (var infFile = new InfiniteFileWriter(this.path, nameof(this.InfiniteFileTest), extentSize))
            {
                byte[] bytes = new byte[messageSize];
                byte val = 100;
                for (int i = 0; i < iterations; i++)
                {
                    bytes[messageSize - 1] = val;
                    val++;
                    infFile.ReserveBlock(bytes.Length);
                    infFile.WriteToBlock(bytes);
                    infFile.CommitBlock();
                }
            }

            using (var infFile = new InfiniteFileReader(this.path, nameof(this.InfiniteFileTest)))
            {
                byte[] bytes = new byte[messageSize];
                byte val = 100;
                for (int i = 0; i < iterations; i++)
                {
                    infFile.MoveNext();
                    infFile.ReadBlock(ref bytes);
                    Assert.AreEqual(0, bytes[0]);
                    Assert.AreEqual(val, bytes[messageSize - 1]);
                    val++;
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void InfiniteFileInMemory()
        {
            using (var infFile = new InfiniteFileWriter(nameof(this.InfiniteFileInMemory), 128, 0))
            {
                using (var infFile2 = new InfiniteFileReader(nameof(this.InfiniteFileInMemory)))
                {
                    byte[] bytes = new byte[100];
                    byte val = 100;
                    int pos = 10;
                    for (int i = 0; i < 4; i++)
                    {
                        bytes[pos] = val;
                        infFile.ReserveBlock(bytes.Length);
                        infFile.WriteToBlock(bytes);
                        infFile.CommitBlock();

                        infFile2.MoveNext();
                        infFile2.ReadBlock(ref bytes);
                        Assert.AreEqual(0, bytes[0]);
                        Assert.AreEqual(val, bytes[pos]);

                        val++;
                    }
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EmptyStore()
        {
            var name = nameof(this.EmptyStore);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i, 1); // note that this is not written to the store
                p.Run();
            }

            // now replay the contents and verify we get something
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = Store.Open(p2, name, this.path);
                p2.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MultipleWriteAttempts()
        {
            var name = nameof(this.MultipleWriteAttempts);
            using (var p = Pipeline.Create())
            {
                var writeStore1 = Store.Create(p, name, this.path, true);

                // as long as an incremented path is generated, this should work
                var writeStore2 = Store.Create(p, name, this.path, true);

                try
                {
                    // without auto-incrementing, this should fail
                    var writeStore3 = Store.Create(p, name, writeStore1.Path, false);
                    Assert.Fail("The attempt to create a second writable store instance with the same file unexpectedly succeeded.");
                }
                catch (IOException)
                {
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PersistSingleStream()
        {
            var count = 100;
            var name = nameof(this.PersistSingleStream);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count);
                seq.Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we get something
            double sum = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = Store.Open(p2, name, this.path);
                var seq2 = readStore.OpenStream<int>("seq");
                var verifier = seq2.Do(s => sum = sum + s);
                p2.Run();
            }

            Assert.AreEqual(count * (count + 1) / 2, sum);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PersistMultipleStreams()
        {
            var count = 10;
            var factor = 1000;
            var name = nameof(this.PersistSingleStream);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count);
                var mul = seq.Select(i => i * factor);
                var tuple = seq.Select(i => (i, i.ToString()));
                seq.Write("seq", writeStore);
                mul.Write("big", writeStore);
                tuple.Write("tuple", writeStore);
                p.Run();
            }

            // now replay the contents and verify we get something
            double sum = 0;
            double sum2 = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = Store.Open(p2, name, this.path);
                var seq2 = readStore.OpenStream<int>("seq");
                var mul2 = readStore.OpenStream<int>("big");
                var tuple2 = readStore.OpenStream<(int, string)>("tuple");
                var verifier1 = seq2.Do(s => sum = sum + s);
                var verifier2 = mul2.Do(s => sum2 = sum2 + s);
                p2.Run();
            }

            Assert.AreEqual(count * (count + 1) / 2, sum);
            Assert.AreEqual(factor * count * (count + 1) / 2, sum2);
        }

        [TestMethod]
        [Timeout(60000)]
        public void Seek()
        {
            var count = 100; // to make sure there are more than one page
            var interval = 1;
            var name = nameof(this.Seek);
            var factors = new[] { 1f, 0.5f, 0.1f };
            var buffer = new byte[1024];
            using (var p = Pipeline.Create("seek"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 0, i => i + 1, count, TimeSpan.FromMilliseconds(interval));
                seq.Select(i => buffer).Write("unused", writeStore); // a second stream, written but unused, to increase the number of pages in the file
                seq.Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we can seek
            TimeInterval range;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = Store.Open(p2, name, this.path);
                range = readStore.OriginatingTimeInterval;
            }

            Assert.AreEqual((count - 1) * interval, range.Span.TotalMilliseconds);

            foreach (var factor in factors)
            {
                int recount = 0;
                var desc = new ReplayDescriptor(range.ScaleLeft(factor), true);
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = Store.Open(p2, name, this.path);

                    var seq2 = readStore.OpenStream<int>("seq");
                    var verifier = seq2.Do(s => recount++);
                    p2.Run(desc);
                }

                Assert.AreEqual(count * factor, recount);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReadWhilePersistingToDisk()
        {
            this.ReadWhilePersisting(this.path);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReadWhilePersistingInMemory()
        {
            this.ReadWhilePersisting(null);
        }

        // with a null path, the file is only in memory (system file). With a non-null path, the file is also written to disk
        public void ReadWhilePersisting(string path)
        {
            var count = 100;
            var name = nameof(this.ReadWhilePersisting);

            double sum = 0;

            var p = Pipeline.Create("write");
            {
                var writeStore = Store.Create(p, name, path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count);
                seq.Select(i => i * i).Write("sqr", writeStore);
                seq.Write("seq", writeStore);

                // now replay the contents and verify we get something
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = Store.Open(p2, name, path);
                    var seq2 = readStore.OpenStream<int>("seq");
                    var verifier = seq2.Do(s => sum = sum + s);
                    p2.RunAsync();
                    p.Run();
                    p.Dispose(); // to dispose the writer, and thus close the file
                    p2.WaitAll();
                }
            }

            Assert.AreEqual(count * (count + 1) / 2, sum);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PersistLargeStream()
        {
            var count = 10;
            var name = nameof(this.PersistLargeStream);
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i, count);
                var big = seq.Select(i => bytes);
                seq.Write("seq", writeStore);
                big.Write("big", writeStore, largeMessages: true);
                p.Run();
            }

            // now replay the contents and verify we get something
            double sum = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = Store.Open(p2, name, this.path);
                var seq2 = readStore.OpenStream<int>("seq");
                var big = readStore.OpenStream<byte[]>("big");
                var verifier2 = big.Do(s => sum = sum + s[5]);
                p2.Run();
            }

            Assert.AreEqual(count * bytes[5], sum);
        }

        [TestMethod]
        [Timeout(300000)]
        public void UnalignedWrite()
        {
            byte[] b = new byte[65537]; // this size was picked to ensure an unaligned block
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = 0xFF;
            }

            int count = 25;
            var p = Pipeline.Create("write");
            {
                var writeStore = Store.Create(p, "test", null);
                var source = Generators.Sequence(p, 1, i => i, count);
                var largeBlockGenerator = source.Select(t => b);
                largeBlockGenerator.Write("lbg", writeStore);

                // now replay the contents and verify we get something
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = Store.Open(p2, "test", null);
                    var lbg = readStore.OpenStream<byte[]>("lbg");
                    var verifier = lbg.Do(s => count--);
                    p2.RunAsync();
                    p.Run();
                    p.Dispose();
                    while (count != 0)
                    {
                        Thread.Yield();
                    }

                    p2.WaitAll();
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EstimateRange()
        {
            // test setup
            string appName = nameof(this.EstimateRange);

            string sourceName = "source";

            // Step1: run a pipeline and persist some data
            using (var p = Pipeline.Create("test"))
            {
                var writeStore = Store.Create(p, appName, this.path);
                var source = Generators.Sequence(p, 1, i => i, 100);
                source.Write(sourceName, writeStore);
                p.Run();
            }

            // Step2: now open the store and check the range
            using (var p = Pipeline.Create("test"))
            {
                var readStore = Store.Open(p, appName, this.path);
                var source = readStore.OpenStream<int>(sourceName);
                var sourceInfo = readStore.GetMetadata(sourceName);
                var range = readStore.ActiveTimeInterval;
                Assert.AreEqual(sourceInfo.ActiveLifetime.Left, range.Left);
                Assert.AreEqual(sourceInfo.ActiveLifetime.Right, range.Right);
                Assert.AreNotEqual(range.Left, range.Right);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void SimultaneousWriteReadWithRelativePath()
        {
            var relative = new DirectoryInfo(this.path).Name;
            var count = 100;
            var before = new Envelope[count];
            var after = new Envelope[count];
            var name = nameof(this.SimultaneousWriteReadWithRelativePath);

            var pipelineWrite = Pipeline.Create("write");
            var writeStore = Store.Create(pipelineWrite, name, relative);
            var seq = Generators.Sequence(pipelineWrite, 0, i => i + 1, count);
            seq.Write("seq", writeStore);
            seq.Do((m, e) => before[m] = e);

            using (var pipelineRead = Pipeline.Create("read"))
            {
                var reader = Store.Open(pipelineRead, name, relative);
                reader.OpenStream<int>("seq").Select((s, e) => after[s] = e);

                pipelineRead.RunAsync();
                pipelineWrite.Run();
                pipelineWrite.Dispose();
                pipelineRead.WaitAll();
            }

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(before[i].SequenceId, after[i].SequenceId);
                Assert.AreEqual(before[i].OriginatingTime, after[i].OriginatingTime);
            }
        }

        [TestMethod]
        [TestCategory("Stress")]
        public void SimultaneousWriteReadWithRelativePathStress()
        {
            for (int i = 0; i < 1000; i++)
            {
                this.SimultaneousWriteReadWithRelativePath();

                // clear the test folder for the next iteration
                foreach (string folder in Directory.GetDirectories(this.path, nameof(this.SimultaneousWriteReadWithRelativePath) + "*"))
                {
                    TestRunner.SafeDirectoryDelete(folder, true);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MetadataTest()
        {
            var name = nameof(this.MetadataTest);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, null);
                var seq = Generators.Sequence(p, 0, i => i + 1, 1);
                seq.Write("seq", writeStore);
                var sel = seq.Select((m, e) => m);
                p.Run();

                PsiStreamMetadata meta;
                Assert.IsTrue(Store.TryGetMetadata(seq, out meta));
                Assert.IsNotNull(meta);
                Assert.AreEqual(meta.Id, seq.Out.Id);
                Assert.AreEqual(meta.PartitionName, name);
                Assert.IsFalse(Store.TryGetMetadata(sel, out meta));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void RealTimePlayback()
        {
            var factors = new[] { 1f, 0.25f, 2f };
            foreach (var factor in factors)
            {
                var count = 10;
                var spacing = TimeSpan.FromMilliseconds(5);
                var name = nameof(this.RealTimePlayback);
                using (var p = Pipeline.Create("write"))
                {
                    var writeStore = Store.Create(p, name, this.path);
                    var seq = Generators.Sequence(p, 1, i => i + 1, count, spacing);
                    seq.Write("seq", writeStore);
                    p.Run();
                }

                // now replay the contents and verify we get something
                int replayCount = 0;
                bool spaced = true;
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = Store.Open(p2, name, this.path);
                    var playbackInterval = readStore.ActiveTimeInterval;
                    var seq2 = readStore.OpenStream<int>("seq");
                    var verifier = seq2.Do(
                        (s, e) =>
                        {
                            var now = Time.GetCurrentTime();
                            var realTimeDelta = (now - p2.Clock.RealTimeOrigin).Ticks;
                            var messageDelta = (e.Time - p2.Clock.Origin).Ticks;
                            spaced = spaced && realTimeDelta >= messageDelta / factor;
                            replayCount++;
                        });
                    p2.Run(playbackInterval, false, true, factor);
                }

                Assert.IsTrue(spaced);
                Assert.AreEqual(count, replayCount);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void CopyStore()
        {
            var count = 100;
            var before = new Envelope[count + 1];
            var after = new Envelope[count + 1];
            var name = nameof(this.CopyStore);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = Store.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count);
                seq.Write("seq", writeStore);
                seq.Select(i => i.ToString()).Write("seqString", writeStore);
                seq.Do((m, e) => before[m] = e);
                p.Run();
            }

            // copy to a second store
            using (var p = Pipeline.Create("write2"))
            {
                var readStore = Store.Open(p, name, this.path);
                var writeStore = Store.Create(p, name, this.path);
                readStore.CopyStream("seq", writeStore);
                readStore.CopyStream("seqString", writeStore);
                p.Run();
            }

            // now read the latest using the simple reader
            bool intStreamCorrect = true;
            bool stringStreamCorrect = true;
            using (var p2 = Pipeline.Create("read2"))
            {
                var readStore = Store.Open(p2, name, this.path);
                readStore.OpenStream<int>("seq").Do((s, e) =>
                {
                    after[s] = e;
                    intStreamCorrect &= e.SequenceId == s;
                });
                readStore.OpenStream<string>("seqString").Do((s, e) => { stringStreamCorrect &= e.SequenceId.ToString() == s; });
                p2.Run(ReplayDescriptor.ReplayAll);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(before[i].SequenceId, after[i].SequenceId);
                Assert.AreEqual(before[i].OriginatingTime, after[i].OriginatingTime);
            }

            Assert.IsTrue(intStreamCorrect);
            Assert.IsTrue(stringStreamCorrect);
        }
    }
}
