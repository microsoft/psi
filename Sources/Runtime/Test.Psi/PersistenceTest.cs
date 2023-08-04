// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Runtime.Serialization;
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
        public void InfiniteFileTruncation()
        {
            const int extentSize = 10 * 1024;
            using (var infFile = new InfiniteFileWriter(this.path, nameof(this.InfiniteFileTest), extentSize))
            {
                // write less data than would fill an extent
                byte[] bytes = new byte[1024];
                infFile.ReserveBlock(bytes.Length);
                infFile.WriteToBlock(bytes);
                infFile.CommitBlock();
            }

            // get the full pathname of the first (and only) extent file
            string extentPath = Path.Combine(this.path, string.Format("{0}_{1:000000}.psi", nameof(this.InfiniteFileTest), 0));

            // verify that the extent file has been truncated (to the nearest 4096 byte block)
            Assert.AreEqual(4096, new FileInfo(extentPath).Length);
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
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i, 1, TimeSpan.FromTicks(1)); // note that this is not written to the store
                p.Run();
            }

            // now replay the contents and verify we get something
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                Assert.AreEqual(0, readStore.AvailableStreams.Count());
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void MultipleWriteAttempts()
        {
            var name = nameof(this.MultipleWriteAttempts);
            using (var p = Pipeline.Create())
            {
                var writeStore1 = PsiStore.Create(p, name, this.path, true);

                // as long as an incremented path is generated, this should work
                var writeStore2 = PsiStore.Create(p, name, this.path, true);

                try
                {
                    // without auto-incrementing, this should fail
                    var writeStore3 = PsiStore.Create(p, name, writeStore1.Path, false);
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
            // generate a sequence from 1 .. 100, simulating also a latency and write to store
            var count = 100;
            var name = nameof(this.PersistSingleStream);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we get something
            double sum = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
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
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
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
                var readStore = PsiStore.Open(p2, name, this.path);
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
        public void CopyStore()
        {
            // generate a sequence from 1 .. 100
            var count = 100;
            var name = nameof(this.CopyStore);
            var supplemental = Math.E; // supplemental metadata
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1))
                    .Select(
                        x =>
                        {
                            Thread.Sleep(1);
                            return x;
                        },
                        DeliveryPolicy.Unlimited);
                seq.Write(supplemental, "seq", writeStore);
                p.Run();
            }

            var copyName = $"{name}Copy";
            PsiStore.Copy((name, this.path), (copyName, this.path));

            // now replay the contents and verify we get the right sum
            double sum = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, copyName, this.path);
                var seq2 = readStore.OpenStream<int>("seq");
                Assert.AreEqual(supplemental, readStore.GetSupplementalMetadata<double>("seq"), double.Epsilon);
                var verifier = seq2.Do(s => sum += s);
                p2.Run();
            }

            Assert.AreEqual(count * (count + 1) / 2, sum);
        }

        [TestMethod]
        [Timeout(60000)]
        public void EditStore()
        {
            var name = nameof(this.EditStore);
            var supplemental = Math.E; // supplemental metadata
            var now = DateTime.UtcNow;
            var originatingTimes = new DateTime[]
            {
                now,
                now.AddMilliseconds(1),
                now.AddMilliseconds(2),
                now.AddMilliseconds(3),
                now.AddMilliseconds(4),
                now.AddMilliseconds(5),
            };
            IEnumerable<Envelope> stream0InitialEnvelopes, stream1InitialEnvelopes;
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var stream0 = Generators.Sequence(p, new[]
                    {
                        (7, originatingTimes[0]),
                        (42, originatingTimes[1]),
                        (1, originatingTimes[2]),
                        (123, originatingTimes[3]),
                        (1971, originatingTimes[4]),
                        (0, originatingTimes[5]),
                    });
                stream0InitialEnvelopes = stream0.Select((m, e) => e).ToEnumerable();
                stream0.Write("stream0", writeStore);
                var stream1 = Generators.Sequence(p, new[]
                    {
                        ('C', originatingTimes[0]),
                        ('D', originatingTimes[1]),
                        ('?', originatingTimes[2]),
                        ('!', originatingTimes[3]),
                        ('F', originatingTimes[4]),
                        ('G', originatingTimes[5]),
                    });
                stream1InitialEnvelopes = stream1.Select((m, e) => e).ToEnumerable();
                stream1.Write(supplemental, "stream1", writeStore);
                p.Run();
            }

            var edits = new Dictionary<string, IEnumerable<(bool, dynamic, DateTime)>>();
            edits.Add(
                "stream1",
                new (bool, dynamic, DateTime)[]
                {
                    (true, 'B', originatingTimes[0].AddTicks(-1)), // insert B before C (before start of original stream)
                    (true, 'A', originatingTimes[0].AddTicks(-2)), // insert A before B (before start of original stream)
                    (false, null, originatingTimes[2]), // delete ?
                    (true, 'E', originatingTimes[3]), // update E between D and F (in place of !)
                    (true, 'H', originatingTimes[5].AddTicks(1)), // insert H after G (beyond end of original stream)
                    (true, 'I', originatingTimes[5].AddTicks(2)), // insert I after H (beyond end of original stream)
                });

            var editName = $"{name}Edited";
            PsiStore.Edit((name, this.path), (editName, this.path), edits);

            // now replay the contents and verify we get the right values
            IEnumerable<Envelope> stream0FinalEnvelopes, stream1FinalEnvelopes;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, editName, this.path);
                var stream0 = readStore.OpenStream<int>("stream0");
                var stream1 = readStore.OpenStream<char>("stream1");
                stream0FinalEnvelopes = stream0.Select((m, e) => e).ToEnumerable();
                stream1FinalEnvelopes = stream1.Select((m, e) => e).ToEnumerable();
                var stream0List = readStore.OpenStream<int>("stream0").ToObservable().ToListObservable();
                var stream1List = readStore.OpenStream<char>("stream1").ToObservable().ToListObservable();
                Assert.AreEqual(supplemental, readStore.GetSupplementalMetadata<double>("stream1"), double.Epsilon);
                p2.Run();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 7, 42, 1, 123, 1971, 0 }, stream0List.AsEnumerable()));
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I' }, stream1List.AsEnumerable()));
            }

            var stream0Initial = stream0InitialEnvelopes.ToArray();
            var stream1Initial = stream1InitialEnvelopes.ToArray();
            var stream0Final = stream0FinalEnvelopes.ToArray();
            var stream1Final = stream1FinalEnvelopes.ToArray();

            // verify that originating/creation times and sequence IDs are unchanged (source ID does change)
            Assert.AreEqual(stream0Initial.Length, stream0Final.Length);
            for (var i = 0; i < stream0Initial.Length; i++)
            {
                Assert.AreEqual(stream0Initial[i].OriginatingTime, stream0Final[i].OriginatingTime);
                Assert.AreEqual(stream0Initial[i].CreationTime, stream0Final[i].CreationTime);
                Assert.AreEqual(stream0Initial[i].SequenceId, stream0Final[i].SequenceId);
            }

            // verify that unedited message (e.g. 'C') is unchanged
            var j = 2; // 'C'
            Assert.AreEqual(stream0Initial[j].OriginatingTime, stream0Final[j].OriginatingTime);
            Assert.AreEqual(stream0Initial[j].CreationTime, stream0Final[j].CreationTime);
            Assert.AreEqual(stream0Initial[j].SequenceId, stream0Final[j].SequenceId);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AddStreamToStore()
        {
            var name = nameof(this.AddStreamToStore);
            var existingStreamName = "existingStream";
            var newStreamName = "newStream";
            var now = DateTime.UtcNow;
            var originatingTimes = new DateTime[]
            {
                now,
                now.AddMilliseconds(1),
                now.AddMilliseconds(2),
                now.AddMilliseconds(3),
                now.AddMilliseconds(4),
                now.AddMilliseconds(5),
            };

            // create store with existing stream
            IEnumerable<Envelope> streamInitialEnvelopes;
            Pipeline.SetLastStreamId(0);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var stream = Generators.Sequence(p, new[]
                    {
                        (7, originatingTimes[0]),
                        (42, originatingTimes[1]),
                        (1, originatingTimes[2]),
                        (123, originatingTimes[3]),
                        (1971, originatingTimes[4]),
                        (0, originatingTimes[5]),
                    });
                streamInitialEnvelopes = stream.Select((m, e) => e).ToEnumerable();
                stream.Write(existingStreamName, writeStore);

                // add extra streams, attempting to intentionally conflict with added stream ID later
                for (var i = 0; i < 20; i++)
                {
                    stream.Select(x => x).Write($"{existingStreamName}{i}", writeStore);
                }

                p.Run();
            }

            // add new stream
            var addedName = $"{name}StreamAdded";
            Pipeline.SetLastStreamId(0);
            PsiStore.AddStream<string, int>((name, this.path), (addedName, this.path), newStreamName, 42);

            // now replay the contents and verify we get the right values
            IEnumerable<Envelope> streamFinalEnvelopes;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, addedName, this.path);
                var stream = readStore.OpenStream<int>(existingStreamName);
                var newStream = readStore.OpenStream<string>(newStreamName);
                streamFinalEnvelopes = stream.Select((m, e) => e).ToEnumerable();
                var streamList = readStore.OpenStream<int>(existingStreamName).ToObservable().ToListObservable();
                Assert.AreEqual(42, readStore.GetSupplementalMetadata<int>(newStreamName));
                p2.Run();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 7, 42, 1, 123, 1971, 0 }, streamList.AsEnumerable()));
            }

            var streamInitial = streamInitialEnvelopes.ToArray();
            var streamFinal = streamFinalEnvelopes.ToArray();

            // verify that originating/creation times and sequence IDs are unchanged (source ID does change)
            Assert.AreEqual(streamInitial.Length, streamFinal.Length);
            for (var i = 0; i < streamInitial.Length; i++)
            {
                Assert.AreEqual(streamInitial[i].OriginatingTime, streamFinal[i].OriginatingTime);
                Assert.AreEqual(streamInitial[i].CreationTime, streamFinal[i].CreationTime);
                Assert.AreEqual(streamInitial[i].SequenceId, streamFinal[i].SequenceId);
                ////Assert.AreEqual(streamInitial[i].SourceId, streamFinal[i].SourceId);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EditEmptyStore()
        {
            var name = nameof(this.EditEmptyStore);
            var supplemental = Math.E; // supplemental metadata
            var now = DateTime.UtcNow;
            DateTime[] originatingTimes = new DateTime[]
            {
                now,
                now.AddMilliseconds(1),
                now.AddMilliseconds(2),
            };

            // Create empty stream of char
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var stream0 = Generators.Sequence(p, new (char, DateTime)[] { });
                stream0.Write(supplemental, "stream0", writeStore);
                p.Run();
            }

            // Add 3 new messages to the stream
            var edits = new Dictionary<string, IEnumerable<(bool, dynamic, DateTime)>>();
            edits.Add(
                "stream0",
                new (bool, dynamic, DateTime)[]
                {
                    (true, 'A', originatingTimes[0]),
                    (true, 'B', originatingTimes[1]),
                    (true, 'C', originatingTimes[2]),
                });

            var editName = $"{name}Edited";
            PsiStore.Edit((name, this.path), (editName, this.path), edits);

            // now replay the contents and verify we get the right sum
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, editName, this.path);
                var stream0 = readStore.OpenStream<char>("stream0").ToObservable().ToListObservable();
                Assert.AreEqual(supplemental, readStore.GetSupplementalMetadata<double>("stream0"), double.Epsilon);
                p2.Run();
                Assert.IsTrue(Enumerable.SequenceEqual(new[] { 'A', 'B', 'C' }, stream0.AsEnumerable()));
            }
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
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 0, i => i + 1, count, TimeSpan.FromMilliseconds(interval));
                seq.Select(i => buffer).Write("unused", writeStore); // a second stream, written but unused, to increase the number of pages in the file
                seq.Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we can seek
            TimeInterval range;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                range = readStore.MessageOriginatingTimeInterval;
            }

            Assert.AreEqual((count - 1) * interval, range.Span.TotalMilliseconds);

            foreach (var factor in factors)
            {
                int recount = 0;
                var desc = new ReplayDescriptor(range.ScaleLeft(factor), true);
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = PsiStore.Open(p2, name, this.path);

                    var seq2 = readStore.OpenStream<int>("seq");
                    var verifier = seq2.Do(s => recount++);
                    p2.Run(desc);
                }

                Assert.AreEqual(count * factor, recount);
            }

            // manually read the index entries
            List<IndexEntry> indexEntries = new List<IndexEntry>();
            using (var indexReader = new InfiniteFileReader(PsiStore.GetPathToLatestVersion(name, this.path), PsiStoreCommon.GetIndexFileName(name)))
            {
                while (indexReader.MoveNext())
                {
                    IndexEntry indexEntry;
                    unsafe
                    {
                        indexReader.Read((byte*)&indexEntry, sizeof(IndexEntry));
                    }

                    indexEntries.Add(indexEntry);
                }
            }

            // verify seeking to the index entry boundaries (skip the zero index entry)
            for (int i = 1; i < indexEntries.Count; i++)
            {
                var time = indexEntries[i].OriginatingTime;
                var desc = new ReplayDescriptor(time, true);
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = PsiStore.Open(p2, name, this.path);

                    var seq2 = readStore.OpenStream<int>("seq");
                    var verifier = seq2.First().Do((_, e) => Assert.AreEqual(time, e.OriginatingTime));
                    p2.Run(desc);
                }
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReadWhilePersistingToDisk()
        {
            this.ReadWhilePersisting(nameof(this.ReadWhilePersistingToDisk), this.path);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReadWhilePersistingInMemory()
        {
            this.ReadWhilePersisting(nameof(this.ReadWhilePersistingInMemory), null);
        }

        // with a null path, the file is only in memory (system file). With a non-null path, the file is also written to disk
        public void ReadWhilePersisting(string name, string path)
        {
            var count = 100;

            double sum = 0;

            var p = Pipeline.Create("write");
            {
                var writeStore = PsiStore.Create(p, name, path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromMilliseconds(1));
                seq.Select(i => i * i).Write("sqr", writeStore);
                seq.Write("seq", writeStore);

                // now replay the contents and verify we get something
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = PsiStore.Open(p2, name, path);
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
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i, count, TimeSpan.FromTicks(1));
                var big = seq.Select(i => bytes);
                seq.Write("seq", writeStore);
                big.Write("big", writeStore, largeMessages: true);
                p.Run();
            }

            // now replay the contents and verify we get something
            double sum = 0;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
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
                var writeStore = PsiStore.Create(p, "test", null);
                var source = Generators.Sequence(p, 1, i => i, count, TimeSpan.FromMilliseconds(1));
                var largeBlockGenerator = source.Select(t => b);
                largeBlockGenerator.Write("lbg", writeStore);

                // now replay the contents and verify we get something
                using (var p2 = Pipeline.Create("read"))
                {
                    var readStore = PsiStore.Open(p2, "test", null);
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
                var writeStore = PsiStore.Create(p, appName, this.path);
                var source = Generators.Sequence(p, 1, i => i, 100, TimeSpan.FromTicks(1));
                source.Write(sourceName, writeStore);
                p.Run();
            }

            // Step2: now open the store and check the range
            using (var p = Pipeline.Create("test"))
            {
                var readStore = PsiStore.Open(p, appName, this.path);
                var source = readStore.OpenStream<int>(sourceName);
                var sourceInfo = readStore.GetMetadata(sourceName);
                var range = readStore.MessageCreationTimeInterval;
                Assert.AreEqual(sourceInfo.FirstMessageCreationTime, range.Left);
                Assert.AreEqual(sourceInfo.LastMessageCreationTime, range.Right);
                Assert.AreNotEqual(range.Left, range.Right);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void EstimateStreamRange()
        {
            // test setup
            string appName = nameof(this.EstimateStreamRange);

            string source0Name = "source0";
            string source1Name = "source1";

            // Step1: run a pipeline and persist some data
            using (var p = Pipeline.Create("test"))
            {
                var writeStore = PsiStore.Create(p, appName, this.path);

                // source0 - a stream of messages covering a longer time span
                var source0 = Generators.Sequence(p, 0, i => i, 50, TimeSpan.FromMilliseconds(10));

                // source1 - a stream of messages covering a shorter time span than source0,
                // and which has a lifetime that begins after and ends before source0 ends.
                var source1 = Generators.Sequence(p, 1, i => i, 10, TimeSpan.FromMilliseconds(10)).Delay(TimeSpan.FromMilliseconds(100));

                source0.Write(source0Name, writeStore);
                source1.Write(source1Name, writeStore);
                p.Run();
            }

            // Step2: now open the store and check the ranges
            using (var p = Pipeline.Create("test"))
            {
                var output = new List<Envelope>();

                // open the store, but only open the shorter stream (source1)
                var readStore = PsiStore.Open(p, appName, this.path);
                var source1 = readStore.OpenStream<int>(source1Name).Do((m, e) => output.Add(e));

                // get the metadata for both streams
                var sourceInfo0 = readStore.GetMetadata(source0Name);
                var sourceInfo1 = readStore.GetMetadata(source1Name);

                // get the active time range for the entire store
                var range = readStore.MessageCreationTimeInterval;

                // verify that the longer stream defines the store's range
                Assert.AreEqual(sourceInfo0.FirstMessageCreationTime, range.Left);
                Assert.AreEqual(sourceInfo0.LastMessageCreationTime, range.Right);

                // verify that the shorter stream is a sub-range of the store's range
                Assert.IsTrue(sourceInfo1.FirstMessageCreationTime > range.Left);
                Assert.IsTrue(sourceInfo1.LastMessageCreationTime < range.Right);

                // run the pipeline to read from source1
                p.Run();

                // verify that the replay descriptor corresponds only to the range of the opened stream
                var replayDesc = p.ReplayDescriptor;
                Assert.AreEqual(sourceInfo1.FirstMessageOriginatingTime, replayDesc.Start);
                Assert.AreEqual(sourceInfo1.LastMessageOriginatingTime, replayDesc.End);

                // verify that all the messages on the stream were read
                Assert.AreEqual(sourceInfo1.MessageCount, output.Count);

                // verify the times of the first and last messages
                Assert.AreEqual(sourceInfo1.FirstMessageCreationTime, output[0].CreationTime);
                Assert.AreEqual(sourceInfo1.LastMessageCreationTime, output[output.Count - 1].CreationTime);
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
            var writeStore = PsiStore.Create(pipelineWrite, name, relative);
            var seq = Generators.Sequence(pipelineWrite, 0, i => i + 1, count, TimeSpan.FromTicks(1));
            seq.Write("seq", writeStore);
            seq.Do((m, e) => before[m] = e);

            using (var pipelineRead = Pipeline.Create("read"))
            {
                var reader = PsiStore.Open(pipelineRead, name, relative);
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

        //[TestMethod]
        //[TestCategory("Stress")]
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
                var writeStore = PsiStore.Create(p, name, null);
                var seq = Generators.Sequence(p, 0, i => i + 1, 1, TimeSpan.FromTicks(1));
                seq.Write("seq", writeStore);
                var sel = seq.Select((m, e) => m);
                p.Run();

                IStreamMetadata meta;
                Assert.IsTrue(PsiStore.TryGetStreamMetadata(seq, out meta));
                Assert.IsNotNull(meta);
                Assert.AreEqual(meta.Id, seq.Out.Id);
                Assert.AreEqual(meta.StoreName, name);
                Assert.IsFalse(PsiStore.TryGetStreamMetadata(sel, out meta));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void TypeRegistrationTest()
        {
            // create a store and write a stream of V1 type
            var name = nameof(this.TypeRegistrationTest);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                Generators.Return(p, new TestTypeV1 { IntProperty = 1, StringProperty = "string" }).Write("data", writeStore);
                p.Run();
            }

            // now open the store and read the stream as V2 type
            TestTypeV2 value = null;
            using (var p = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p, name, this.path);

                // explicitly register compatible V2 type handler for the V1 stream type
                var meta = readStore.GetMetadata("data");
                readStore.Serializers.Register<TestTypeV2>(meta.TypeName);

                // read the V1 stream as a stream of type V2
                readStore.OpenStream<TestTypeV2>("data").Do(x => value = x);
                p.Run();
            }

            Assert.AreEqual(1, value.IntProperty);
            Assert.AreEqual("string", value.StringProperty);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(SerializationException))]
        public void IncompatibleTypeTest()
        {
            // create a store and write a stream of V1 type
            var name = nameof(this.IncompatibleTypeTest);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                Generators.Return(p, new TestTypeV1 { IntProperty = 1, StringProperty = "string" }).Write("data", writeStore);
                p.Run();
            }

            // now open the store and read the stream as V3 type
            TestTypeV3 value = null;
            using (var p = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p, name, this.path);

                // attempt to read the V1 stream as a stream of type V3 (incompatible)
                readStore.OpenStream<TestTypeV3>("data").Do(x => value = x); // should throw
                p.Run();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void DataContractTypeTest()
        {
            // create a store and write a stream of V1 type
            var name = nameof(this.DataContractTypeTest);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                Generators.Return(p, new DataContractTypeV1 { IntProperty = 1, StringProperty = "string" }).Write("data", writeStore);
                p.Run();
            }

            // now open the store and read the stream as V2 type
            DataContractTypeV2 value = null;
            using (var p = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p, name, this.path);

                // read the V1 stream as a stream of type V2 (different versions of the same data contract)
                readStore.OpenStream<DataContractTypeV2>("data").Do(x => value = x);
                p.Run();
            }

            Assert.AreEqual(1, value.IntProperty);
            Assert.AreEqual("string", value.StringProperty);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RealTimePlayback()
        {
            var count = 10;
            var spacing = TimeSpan.FromMilliseconds(50);
            var name = nameof(this.RealTimePlayback);
            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, spacing);
                seq.Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we get something
            int replayCount = 0;
            bool spaced = true;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                var playbackInterval = readStore.MessageOriginatingTimeInterval;
                var seq2 = readStore.OpenStream<int>("seq");
                var verifier = seq2.Do(
                    (s, e) =>
                    {
                        var now = Time.GetCurrentTime();
                        var realTimeDelta = (now - p2.Clock.RealTimeOrigin).Ticks;
                        var messageDelta = (e.OriginatingTime - p2.Clock.Origin).Ticks;

                        // In realtime playback, the messages always arrive according to their originating times.
                        // If we were replaying without realtime clock enforcement, the messages would eventually
                        // outrun the realtime clock, and the messageDeltas would exceed the realTimeDeltas. The
                        // check below verifies that this never happens, implying that the messages arrive spaced
                        // apart by at least their originating time intervals.
                        spaced = spaced && (realTimeDelta - messageDelta) >= 0;
                        replayCount++;
                    });
                p2.Run(playbackInterval, enforceReplayClock: true);
            }

            Assert.IsTrue(spaced);
            Assert.AreEqual(count, replayCount);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CopyStream()
        {
            var count = 100;
            var before = new Envelope[count + 1];
            var after = new Envelope[count + 1];
            var name = nameof(this.CopyStream);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq", writeStore);
                seq.Select(i => i.ToString()).Write("seqString", writeStore);
                seq.Do((m, e) => before[m] = e);
                p.Run();
            }

            // copy to a second store
            using (var p = Pipeline.Create("write2"))
            {
                var readStore = PsiStore.Open(p, name, this.path);
                var writeStore = PsiStore.Create(p, name, this.path);
                readStore.CopyStream("seq", writeStore);
                readStore.CopyStream("seqString", writeStore);
                p.Run();
            }

            // now read the latest using the simple reader
            bool intStreamCorrect = true;
            bool stringStreamCorrect = true;
            using (var p2 = Pipeline.Create("read2"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
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

        [TestMethod]
        [Timeout(60000)]
        public void CropStore()
        {
            var count = 100;
            var before = new Envelope[count + 1];
            var after = new Envelope[count + 1];
            var name = nameof(this.CropStore);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq", writeStore);
                seq.Select(i => i.ToString()).Write("seqString", writeStore);
                seq.Do((m, e) => before[m] = e);
                p.Run(new ReplayDescriptor(new DateTime(1), false));
            }

            // crop a range to a second store
            PsiStore.Crop((name, this.path), (name, this.path), new TimeInterval(new DateTime(5), new DateTime(count - 5)));

            // now read the cropped store
            bool intStreamCorrect = true;
            bool stringStreamCorrect = true;
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                readStore.OpenStream<int>("seq").Do((s, e) =>
                {
                    after[s] = e;
                    intStreamCorrect &= e.SequenceId == s;
                });
                readStore.OpenStream<string>("seqString").Do((s, e) => { stringStreamCorrect &= e.SequenceId.ToString() == s; });
                p2.Run(ReplayDescriptor.ReplayAll);
            }

            // verify the results in the interval before the cropped range
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(0, after[i].SequenceId);
                Assert.AreEqual(0, after[i].OriginatingTime.Ticks);
            }

            // verify the results in the cropped range
            for (int i = 5; i < (count - 4); i++)
            {
                Assert.AreEqual(before[i].SequenceId, after[i].SequenceId);
                Assert.AreEqual(before[i].OriginatingTime, after[i].OriginatingTime);
            }

            // verify the results in the interval after the cropped range
            for (int i = count - 4; i <= count; i++)
            {
                Assert.AreEqual(0, after[i].SequenceId);
                Assert.AreEqual(0, after[i].OriginatingTime.Ticks);
            }

            Assert.IsTrue(intStreamCorrect);
            Assert.IsTrue(stringStreamCorrect);
        }

        [TestMethod]
        [Timeout(60000)]
        public void RepairInvalidStore()
        {
            var count = 100;
            var valid = new Envelope[count + 1];
            var invalid = new Envelope[count + 1];
            var name = nameof(this.RepairInvalidStore);

            // generate a valid store
            using (var p = Pipeline.Create("write"))
            {
                var validStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq", validStore);
                seq.Select(i => i.ToString()).Write("seqString", validStore);
                seq.Do((m, e) => valid[m] = e);
                p.Run(new ReplayDescriptor(new DateTime(1), false));
            }

            // pipeline terminated normally so store should be valid
            Assert.IsTrue(PsiStore.IsClosed(name, this.path));

            // Now create a new pipeline in which we will simulate an invalid store by taking a
            // snapshot of the store files midway through the execution (use synchronous policy
            // to ensure that the messages actually hit the exporter before the snapshot is taken).
            var p2 = Pipeline.Create("write2", DeliveryPolicy.SynchronousOrThrottle);
            var invalidStore = PsiStore.Create(p2, name, this.path);
            string tempFolder = Path.Combine(this.path, Guid.NewGuid().ToString());
            var seq2 = Generators.Sequence(p2, 1, i => i + 1, count, TimeSpan.FromTicks(1));
            seq2.Do((m, e) =>
            {
                // Halfway through, simulate abrupt termination of the pipeline by copying the store
                // files while the pipeline is running, resulting in a store in an invalid state.
                if (e.OriginatingTime.Ticks == count / 2)
                {
                    // at this point the store should still be open
                    Assert.IsFalse(PsiStore.IsClosed(name, this.path));

                    // We need to temporarily save the invalid store before disposing the pipeline,
                    // since the store will be rendered valid when the pipeline is terminated.
                    Directory.CreateDirectory(tempFolder);

                    // Copy the store files to the temp folder - we will restore them later.
                    // The Live marker file cannot be copied because the writer still holds
                    // an exclusive lock.
                    foreach (var file in Directory.EnumerateFiles(invalidStore.Path))
                    {
                        var fileInfo = new FileInfo(file);
                        if (!fileInfo.Name.EndsWith(".Live"))
                        {
                            File.Copy(file, Path.Combine(tempFolder, fileInfo.Name));
                        }
                    }
                }
            }).Write("seq", invalidStore);
            seq2.Select(i => i.ToString()).Write("seqString", invalidStore);

            // run the pipeline with exception handling enabled
            p2.Run(new ReplayDescriptor(new DateTime(1), false));
            p2.Dispose();

            // after disposing the pipeline, the store becomes valid
            Assert.IsTrue(PsiStore.IsClosed(name, this.path));

            // delete the (now valid) store files
            foreach (var file in Directory.EnumerateFiles(invalidStore.Path))
            {
                TestRunner.SafeFileDelete(file);
            }

            // restore the invalid store files from the temp folder
            foreach (var file in Directory.EnumerateFiles(tempFolder))
            {
                var fileInfo = new FileInfo(file);
                File.Move(file, Path.Combine(invalidStore.Path, fileInfo.Name));
            }

            // the generated store should be invalid prior to repairing
            Assert.IsFalse(PsiStore.IsClosed(name, this.path));

            PsiStore.Repair(name, this.path);

            // now read from the repaired store
            bool intStreamCorrect = true;
            bool stringStreamCorrect = true;
            using (var p3 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p3, name, this.path);
                readStore.OpenStream<int>("seq").Do((s, e) =>
                {
                    invalid[s] = e;
                    intStreamCorrect &= e.SequenceId == s;
                });
                readStore.OpenStream<string>("seqString").Do((s, e) => { stringStreamCorrect &= e.SequenceId.ToString() == s; });
                p3.Run(ReplayDescriptor.ReplayAll);
            }

            // Since we cannot guarantee when the messages written to the "invalid store" actually got
            // flushed from the memory-mapped file, just verify that we got some of the initial data.
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(valid[i].SequenceId, invalid[i].SequenceId);
                Assert.AreEqual(valid[i].OriginatingTime, invalid[i].OriginatingTime);
            }

            // log how much of the store actually got written for diagnostic purposes only
            int lastIndex = invalid.LastOrDefault(e => e.SequenceId != 0).SequenceId;
            Console.WriteLine($"Last sequence id found: {lastIndex}");

            // verify there are no results after the point at which the store was rendered invalid
            for (int j = count / 2; j <= count; j++)
            {
                Assert.AreEqual(0, invalid[j].SequenceId);
                Assert.AreEqual(0, invalid[j].OriginatingTime.Ticks);
            }

            Assert.IsTrue(intStreamCorrect);
            Assert.IsTrue(stringStreamCorrect);
        }

        [TestMethod]
        [TestCategory("Performance")]
        [Timeout(60000)]
        public void StoreWriteReadSpeedTest()
        {
            // here we write large messages to a store. This will quickly overflow the extents, causing
            // MemoryMappedViews to be disposed. This is a potentially blocking call which we now do on a
            // separate thread. Prior to this fix, it would block writing/reading the store with a human-noticeable
            // delay of several seconds.

            var payload = new byte[1024 * 1024 * 10];
            const int numMessages = 100;
            var largestDelay = TimeSpan.Zero;
            using (var p = Pipeline.Create())
            using (var q = Pipeline.Create())
            {
                // write to store and times
                var lastWrite = DateTime.MinValue;
                var g = Generators.Repeat(p, payload, numMessages, TimeSpan.FromMilliseconds(50));
                g.Do(_ => lastWrite = DateTime.UtcNow);
                var s = PsiStore.Create(p, "store", this.path);
                g.Write("g", s);
                p.RunAsync();

                // read back from store (*while* writing) and check wall-clock delay
                var lastRead = DateTime.MaxValue;
                var count = 0;
                var t = PsiStore.Open(q, "store", this.path);
                var h = t.OpenStream<byte[]>("g");
                h.Do(_ =>
                {
                    Console.Write('.');
                    var delay = DateTime.UtcNow - lastRead;
                    if (delay > largestDelay)
                    {
                        largestDelay = delay;
                    }

                    lastRead = DateTime.UtcNow;
                    if (++count == numMessages)
                    {
                        p.Dispose(); // close store; allowing this pipeline to complete reading
                    }
                });
                q.Run();

                Assert.IsTrue(largestDelay < TimeSpan.FromSeconds(1), $"Largest read delay: {largestDelay.TotalSeconds} seconds");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ReadFromMultipleSubpipelines()
        {
            // This test ensures that stores in subpipelines correctly propose their replay intervals,
            // and that this is properly accounted for in the parent pipeline at runtime.

            // first, create multiple test stores to read from
            var count = 100;
            var name0 = nameof(this.ReadFromMultipleSubpipelines) + "_0";
            var name1 = nameof(this.ReadFromMultipleSubpipelines) + "_1";
            var name2 = nameof(this.ReadFromMultipleSubpipelines) + "_2";

            using (var p = Pipeline.Create("write0"))
            {
                var writeStore = PsiStore.Create(p, name0, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq0", writeStore);
                p.Run();
            }

            using (var p = Pipeline.Create("write1"))
            {
                var writeStore = PsiStore.Create(p, name1, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq1", writeStore);
                p.Run();
            }

            using (var p = Pipeline.Create("write2"))
            {
                var writeStore = PsiStore.Create(p, name2, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, count, TimeSpan.FromTicks(1));
                seq.Write("seq2", writeStore);
                p.Run();
            }

            double sum0 = 0;
            double sum1 = 0;
            double sum2 = 0;

            // now open and replay the streams in parent/sub-pipelines and verify
            using (var p = Pipeline.Create("parent"))
            {
                // read store0 in parent pipeline
                var store0 = PsiStore.Open(p, name0, this.path);
                var seq0 = store0.OpenStream<int>("seq0");
                seq0.Do(s => sum0 = sum0 + s);

                // read store1 in sub-pipeline
                var sub1 = Subpipeline.Create(p, "sub1");
                var store1 = PsiStore.Open(sub1, name1, this.path);
                var seq1 = store1.OpenStream<int>("seq1");
                seq1.Do(s => sum1 = sum1 + s);

                // read store2 in sub-pipeline
                var sub2 = Subpipeline.Create(p, "sub2");
                var store2 = PsiStore.Open(sub2, name2, this.path);
                var seq2 = store2.OpenStream<int>("seq2");
                seq1.Do(s => sum2 = sum2 + s);

                p.Run();
            }

            Assert.AreEqual(count * (count + 1) / 2, sum0);
            Assert.AreEqual(count * (count + 1) / 2, sum1);
            Assert.AreEqual(count * (count + 1) / 2, sum2);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WriteWithLosslessDeliveryPolicy()
        {
            int count = 100;
            string name = nameof(this.WriteWithLosslessDeliveryPolicy);
            byte[] data = new byte[1024];

            // initialize the pipeline with a default lossy delivery policy to test for explicit override
            using (var p = Pipeline.Create("write", deliveryPolicy: DeliveryPolicy.LatestMessage))
            {
                var writeStore = PsiStore.Create(p, name, this.path);

                // Generate a stream of messages at a very high frequency such that messages
                // will be generated faster than they can be written to the store.
                var seq = Generators.Repeat(p, data, count, TimeSpan.FromTicks(1));

                // verify that exporter overrides pipeline delivery policy with a lossless delivery policy if none is specified
                seq.Write("seq", writeStore);

                p.Run();
            }

            // now replay the contents and verify we get something
            using (var p2 = Pipeline.Create("read"))
            {
                int readCount = 0;
                var readStore = PsiStore.Open(p2, name, this.path);

                // read back the stream from the store
                var seq = readStore.OpenStream<byte[]>("seq").Do(_ => readCount++);
                p2.Run();

                // verify that no messages were dropped
                Assert.AreEqual(count, readCount);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void WriteWithLossyDeliveryPolicy()
        {
            int count = 100;
            string name = nameof(this.WriteWithLossyDeliveryPolicy);
            byte[] data = new byte[1024];

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);

                // Generate a stream of messages at a very high frequency such that messages
                // will be generated faster than they can be written to the store.
                var seq = Generators.Repeat(p, data, count, TimeSpan.FromTicks(1));

                // write the stream to the store with a delivery policy that drops messages
                seq.Write("seq", writeStore, false, DeliveryPolicy.LatestMessage);

                p.Run();
            }

            // now replay the contents and verify we get something
            using (var p2 = Pipeline.Create("read"))
            {
                int readCount = 0;
                var readStore = PsiStore.Open(p2, name, this.path);

                // read back the stream from the store
                var seq = readStore.OpenStream<byte[]>("seq").Do(_ => readCount++);
                p2.Run();

                // verify that messages were dropped
                Assert.IsTrue(readCount < count);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void StoreClosingTest()
        {
            var name = nameof(this.StoreClosingTest);
            DateTime finalOriginatingTime;

            var data = new List<(int, DateTime)>();
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 100; i++)
            {
                data.Add((i, startTime + TimeSpan.FromMilliseconds(i)));
            }

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);

                // last parameter holds pipeline open until explicitly terminated
                var seq = Generators.Sequence(p, data, new DateTime(0), keepOpen: true);
                seq.Write("seq", writeStore);

                // replay at full speed
                p.RunAsync(startTime, false);

                // wait a while to ensure all messages got written to the store
                Thread.Sleep(100);

                // now terminate the pipeline with an originating time at the midpoint of the data
                p.Stop(startTime.AddMilliseconds(50));

                finalOriginatingTime = p.FinalOriginatingTime;
                Assert.AreEqual(startTime.AddMilliseconds(50), finalOriginatingTime);
            }

            // now replay the contents
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                var seq2 = readStore.OpenStream<int>("seq");

                // capture the last observed value on the stream
                (int value, DateTime time) last = default;
                seq2.Do((m, e) => last = (m, e.OriginatingTime));

                // verify metadata reflects the stream closing time
                Assert.IsTrue(PsiStore.TryGetStreamMetadata(seq2, out var meta));
                Assert.AreEqual(finalOriginatingTime, meta.ClosedTime);

                // replay at full speed
                p2.Run(startTime, false);

                // verify the value and time of the last message
                Assert.AreEqual(50, last.value);
                Assert.AreEqual(finalOriginatingTime, last.time);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void StoreProgressTest()
        {
            var progress = new List<double>();
            var name = nameof(this.StoreProgressTest);

            using (var p = Pipeline.Create("write"))
            {
                var writeStore = PsiStore.Create(p, name, this.path);
                var seq = Generators.Sequence(p, 1, i => i + 1, 100, TimeSpan.FromTicks(1)).Write("seq", writeStore);
                p.Run();
            }

            // now replay the contents and verify we get something
            using (var p2 = Pipeline.Create("read"))
            {
                var readStore = PsiStore.Open(p2, name, this.path);
                var readStream = readStore.OpenStream<int>("seq");

                // increase report frequency for testing purposes
                p2.ProgressReportInterval = TimeSpan.FromMilliseconds(50);

                // read from stream and simulate a range of processing delays
                readStream.Do(_ => Thread.Sleep(1));
                readStream.Do(_ => Thread.Sleep(2));
                readStream.Do(_ => Thread.Sleep(5));
                readStream.Do(_ => Thread.Sleep(10));

                // replay as fast as possible from store
                p2.RunAsync(TimeInterval.Infinite, false, new Progress<double>(x => progress.Add(x)));
                p2.WaitAll();
            }

            // Progress<T>.Report() is invoked on the thread-pool since this is a non-UI app,
            // so wait for a bit to ensure that the last progress report action completes.
            Thread.Sleep(100);

            double lastValue = 0;
            foreach (double value in progress)
            {
                Console.WriteLine($"Progress: {value * 100}%");

                // verify progress increases
                Assert.IsTrue(value >= lastValue);
                lastValue = value;
            }

            // verify final progress is 1.0
            Assert.AreEqual(1.0, lastValue);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PersistingStreamSupplementalMetadata()
        {
            var name = nameof(this.PersistingStreamSupplementalMetadata);

            // create store with supplemental meta
            using (var p = Pipeline.Create("write"))
            {
                var store = PsiStore.Create(p, name, this.path);
                var stream0 = Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
                var stream1 = Generators.Range(p, 0, 10, TimeSpan.FromTicks(1));
                stream0.Write("NoMeta", store, true);
                stream1.Write(("Favorite irrational number", Math.E), "WithMeta", store);
            }

            // read it back with an importer
            using (var q = Pipeline.Create("read"))
            {
                var store = PsiStore.Open(q, name, this.path);
                Assert.IsNull(store.GetMetadata("NoMeta").SupplementalMetadataTypeName);
                Assert.AreEqual(typeof(ValueTuple<string, double>).AssemblyQualifiedName, store.GetMetadata("WithMeta").SupplementalMetadataTypeName);
                var supplemental0 = store.GetSupplementalMetadata<(string, double)>("WithMeta");
                Assert.AreEqual("Favorite irrational number", supplemental0.Item1);
                Assert.AreEqual(Math.E, supplemental0.Item2);
                Assert.ThrowsException<InvalidOperationException>(() => store.GetSupplementalMetadata<int>("NoMeta"));
                Assert.ThrowsException<InvalidCastException>(() => store.GetSupplementalMetadata<int>("WithMeta"));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void PersistingDictionaryKeysSupplementalMetadata()
        {
            // using factors as a quick way to get "keys" that come and go
            IEnumerable<KeyValuePair<int, bool>> Factor(int n)
            {
                IEnumerable<int> Divisors(int n) => Enumerable.Range(1, n).Where(i => n % i == 0);
                bool IsPrime(int n) => Divisors(n).Count() == 2;
                if (n == 1)
                {
                    yield return KeyValuePair.Create(1, false);
                }

                while (n > 1)
                {
                    for (var i = 2; i <= n; i++)
                    {
                        if (n % i == 0)
                        {
                            yield return KeyValuePair.Create(i, IsPrime(i));
                            n /= i;
                        }
                    }
                }
            }

            // persisting dictionary with key metadata
            var name = nameof(this.PersistingDictionaryKeysSupplementalMetadata);
            using (var p = Pipeline.Create())
            {
                var exporter = PsiStore.Create(p, name, this.path);
                Generators
                    .Range(p, 0, 20, TimeSpan.FromMilliseconds(10))
                    .Select(x => new Dictionary<int, bool>(Factor(x).Distinct()))
                    .Write("Dictionary", exporter)
                    .SummarizeDistinctKeysInSupplementalMetadata(exporter);
                p.Run();
            }

            // now play back and get dictionary and key metadata
            using var q = Pipeline.Create();
            var importer = PsiStore.Open(q, name, this.path);
            var distinct = importer.GetSupplementalMetadata<Exporter.DistinctKeysSupplementalMetadata<int>>("Dictionary");
            var dictionary = importer.OpenStream<Dictionary<int, bool>>("Dictionary").Select(d => d.Select(kv => (kv.Key, kv.Value)).ToArray()).ToObservable().ToListObservable();
            q.Run();

            Assert.IsTrue(Enumerable.SequenceEqual(new[] { 1, 2, 3, 4, 5, 7, 11, 13, 17, 19 }, distinct.Keys));

            var dict = dictionary.AsEnumerable().ToArray();
            Assert.AreEqual(20, dict.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(new (int, bool)[0], dict[0]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (1, false), }, dict[1]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), }, dict[2]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (3, true), }, dict[3]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), }, dict[4]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (5, true), }, dict[5]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (3, true) }, dict[6]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (7, true), }, dict[7]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (4, false) }, dict[8]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (3, true), }, dict[9]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (5, true) }, dict[10]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (11, true), }, dict[11]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (3, true) }, dict[12]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (13, true), }, dict[13]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (7, true) }, dict[14]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (3, true), (5, true) }, dict[15]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (4, false) }, dict[16]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (17, true), }, dict[17]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (2, true), (3, true) }, dict[18]));
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { (19, true), }, dict[19]));
        }

        [DataContract(Name = "TestDataContract")]
        private class DataContractTypeV1
        {
            [DataMember]
            public int IntProperty { get; set; }

            [DataMember]
            public string StringProperty { get; set; }
        }

        [DataContract(Name = "TestDataContract")]
        private class DataContractTypeV2
        {
            [DataMember]
            public double DoubleProperty { get; set; }

            [DataMember]
            public string StringProperty { get; set; }

            [DataMember]
            public int IntProperty { get; set; }
        }

        private class TestTypeV1
        {
            public int IntProperty { get; set; }

            public string StringProperty { get; set; }
        }

        private class TestTypeV2
        {
            public string StringProperty { get; set; }

            public int IntProperty { get; set; }
        }

        private class TestTypeV3
        {
            public double DoubleProperty { get; set; }

            public int IntProperty { get; set; }
        }
    }
}
