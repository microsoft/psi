// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class DatasetTests
    {
        public static readonly string StorePath = Path.Combine(Environment.CurrentDirectory, nameof(DatasetTests));

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(StorePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestRunner.SafeDirectoryDelete(StorePath, true);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DatasetAddSession()
        {
            var dataset = new Dataset();
            Assert.AreEqual(0, dataset.Sessions.Count);
            Assert.IsTrue(dataset.OriginatingTimeInterval.IsEmpty);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session
            var session0 = dataset.AddSessionFromExistingStore("Session_0", "PsiStore", StorePath);
            Assert.AreEqual(1, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);

            // verify originating time interval
            Assert.AreEqual(session0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(session0.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // add a second session with a different name
            var session1 = dataset.AddSessionFromExistingStore("Session_1", "NewStore", StorePath);
            Assert.AreEqual(2, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);
            Assert.AreEqual("Session_1", dataset.Sessions[1].Name);

            // verify new originating time interval
            Assert.AreEqual(session0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(session1.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DatasetAddSessionDuplicateName()
        {
            var dataset = new Dataset();
            Assert.AreEqual(0, dataset.Sessions.Count);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session
            var session0 = dataset.AddSessionFromExistingStore("Session_0", "PsiStore", StorePath);
            Assert.AreEqual(1, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // add a second session with a duplicate name
            var session1 = dataset.AddSessionFromExistingStore("Session_0", "NewStore", StorePath); // should throw
        }

        [TestMethod]
        [Timeout(60000)]
        public void DatasetAppend()
        {
            var dataset = new Dataset();
            Assert.AreEqual(0, dataset.Sessions.Count);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session
            var session0 = dataset.AddSessionFromExistingStore("Session_0", "PsiStore", StorePath);
            Assert.AreEqual(1, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);

            // verify originating time interval
            Assert.AreEqual(session0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(session0.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // create a second dataset from the new store and append it to the first
            var dataset1 = new Dataset();
            var session1 = dataset1.AddSessionFromExistingStore("Session_1", "NewStore", StorePath);

            dataset.Append(dataset1);
            Assert.AreEqual(2, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);
            Assert.AreEqual("Session_1", dataset.Sessions[1].Name);

            // verify new originating time interval
            Assert.AreEqual(session0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(session1.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DatasetAppendDuplicateName()
        {
            var dataset = new Dataset();
            Assert.AreEqual(0, dataset.Sessions.Count);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session
            var session0 = dataset.AddSessionFromExistingStore("Session_0", "PsiStore", StorePath);
            Assert.AreEqual(1, dataset.Sessions.Count);
            Assert.AreEqual("Session_0", dataset.Sessions[0].Name);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // create a second dataset with a duplicate session name and append it to the first
            var dataset1 = new Dataset();
            var session1 = dataset1.AddSessionFromExistingStore("Session_0", "NewStore", StorePath);

            dataset.Append(dataset1); // should throw
        }

        [TestMethod]
        [Timeout(60000)]
        public void SessionAddPartition()
        {
            var dataset = new Dataset();
            var session = dataset.CreateSession();
            Assert.AreEqual(0, session.Partitions.Count);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a partition
            var partition0 = session.AddStorePartition("PsiStore", StorePath, "Partition_0");
            Assert.AreEqual(1, session.Partitions.Count);
            Assert.AreEqual("Partition_0", session.Partitions[0].Name);

            // verify new originating time intervals (session and dataset)
            Assert.AreEqual(partition0.OriginatingTimeInterval.Left, session.OriginatingTimeInterval.Left);
            Assert.AreEqual(partition0.OriginatingTimeInterval.Right, session.OriginatingTimeInterval.Right);
            Assert.AreEqual(partition0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(partition0.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // add a second partition with a different name
            var partition1 = session.AddStorePartition("NewStore", StorePath, "Partition_1");
            Assert.AreEqual(2, session.Partitions.Count);
            Assert.AreEqual("Partition_0", session.Partitions[0].Name);
            Assert.AreEqual("Partition_1", session.Partitions[1].Name);

            // verify new originating time intervals (session and dataset)
            Assert.AreEqual(partition0.OriginatingTimeInterval.Left, session.OriginatingTimeInterval.Left);
            Assert.AreEqual(partition1.OriginatingTimeInterval.Right, session.OriginatingTimeInterval.Right);
            Assert.AreEqual(partition0.OriginatingTimeInterval.Left, dataset.OriginatingTimeInterval.Left);
            Assert.AreEqual(partition1.OriginatingTimeInterval.Right, dataset.OriginatingTimeInterval.Right);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SessionAddPartitionDuplicateName()
        {
            var dataset = new Dataset();
            var session = dataset.CreateSession();
            Assert.AreEqual(0, session.Partitions.Count);

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a partition
            var partition0 = session.AddStorePartition("PsiStore", StorePath, "Partition_0");
            Assert.AreEqual(1, session.Partitions.Count);
            Assert.AreEqual("Partition_0", session.Partitions[0].Name);

            // generate a new store with a different originating time interval than the first
            GenerateTestStore("NewStore", StorePath);

            // add a second partition with a duplicate name
            var partition1 = session.AddStorePartition("NewStore", StorePath, "Partition_0"); // should throw
        }

        [TestMethod]
        [Timeout(60000)]
        public void DatasetAbsolutePaths()
        {
            var dataset = new Dataset();

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session and assume it loaded correctly if it has a partition containing a stream
            dataset.AddSessionFromExistingStore("Session_0", "PsiStore", Path.GetFullPath(StorePath));
            Assert.IsTrue(dataset.Sessions[0].Partitions[0].AvailableStreams.Count() > 0);

            // save dataset with absolute store paths
            string datasetFile = Path.Combine(StorePath, "dataset.pds");
            dataset.Save(datasetFile, false);

            // create Temp sub-folder
            var tempFolder = Path.Combine(StorePath, "Temp");
            Directory.CreateDirectory(tempFolder);

            // move the dataset file only
            string newDatasetFile = Path.Combine(tempFolder, "dataset.pds");
            File.Move(datasetFile, newDatasetFile);

            // reload the saved dataset and verify that the store paths are still valid
            var newDataset = Dataset.Load(newDatasetFile);
            Assert.IsTrue(newDataset.Sessions[0].Partitions[0].AvailableStreams.Count() > 0);
        }

        [TestMethod]
        [Timeout(60000)]
        public void DatasetRelativePaths()
        {
            var dataset = new Dataset();

            // generate a test store
            GenerateTestStore("PsiStore", StorePath);

            // add a session and assume it loaded correctly if it has a partition containing a stream
            dataset.AddSessionFromExistingStore("Session_0", "PsiStore", Path.GetFullPath(StorePath));
            Assert.IsTrue(dataset.Sessions[0].Partitions[0].AvailableStreams.Count() > 0);

            // save dataset with relative store paths
            string datasetFile = Path.Combine(StorePath, "dataset.pds");
            dataset.Save(datasetFile, true);

            // create Temp sub-folder
            var tempFolder = Path.Combine(StorePath, "Temp");
            Directory.CreateDirectory(tempFolder);

            // move both the dataset file and data stores into the sub-folder
            Directory.Move(Path.Combine(StorePath, "PsiStore.0000"), Path.Combine(tempFolder, "PsiStore.0000"));
            string newDatasetFile = Path.Combine(tempFolder, "dataset.pds");
            File.Move(datasetFile, newDatasetFile);

            // reload the saved dataset and verify that store paths are still valid
            var newDataset = Dataset.Load(newDatasetFile);
            Assert.IsTrue(newDataset.Sessions[0].Partitions[0].AvailableStreams.Count() > 0);
        }

        [TestMethod]
        [Timeout(60000)]
        public async Task SessionCreateDerivedPartition()
        {
            var dataset = new Dataset();
            var session = dataset.CreateSession();

            // generate a test store
            GenerateTestStore("OriginalStore", StorePath);

            // add a partition
            var partition0 = session.AddStorePartition("OriginalStore", StorePath, "Partition_0");
            Assert.AreEqual(1, session.Partitions.Count);
            Assert.AreEqual("Partition_0", session.Partitions[0].Name);

            int multiplier = 7;

            // create a derived partition which contains the values from the original stream multiplied by a multiplier
            await session.CreateDerivedPartitionAsync(
                (pipeline, importer, exporter, parameter) =>
                {
                    var inputStream = importer.OpenStream<int>("Root");
                    var derivedStream = inputStream.Select(x => x * parameter).Write("DerivedStream", exporter);
                },
                multiplier,
                "Partition_1",
                false,
                "DerivedStore",
                StorePath);

            // should have created a new store partition
            Assert.AreEqual(2, session.Partitions.Count);
            Assert.IsTrue(Store.Exists("OriginalStore", StorePath));
            Assert.IsTrue(Store.Exists("DerivedStore", StorePath));

            // verify partition metadata
            var originalPartition = session.Partitions[0] as StorePartition;
            Assert.AreEqual("Partition_0", originalPartition.Name);
            Assert.AreEqual("OriginalStore", originalPartition.StoreName);
            Assert.AreEqual(StorePath, originalPartition.StorePath);

            var derivedPartition = session.Partitions[1] as StorePartition;
            Assert.AreEqual("Partition_1", derivedPartition.Name);
            Assert.AreEqual("DerivedStore", derivedPartition.StoreName);
            Assert.AreEqual(StorePath, derivedPartition.StorePath);

            // collections to capture stream values
            var originalValues = new List<int>();
            var originalTimes = new List<DateTime>();
            var derivedValues = new List<int>();
            var derivedTimes = new List<DateTime>();

            // read stream values from the partitions
            using (var pipeline = Pipeline.Create())
            {
                var originalPartitionImporter = Store.Open(pipeline, originalPartition.StoreName, originalPartition.StorePath);
                originalPartitionImporter.OpenStream<int>("Root").Do(
                    (i, e) =>
                    {
                        originalValues.Add(i);
                        originalTimes.Add(e.OriginatingTime);
                    });

                var derivedPartitionImporter = Store.Open(pipeline, derivedPartition.StoreName, derivedPartition.StorePath);
                derivedPartitionImporter.OpenStream<int>("DerivedStream").Do(
                    (i, e) =>
                    {
                        derivedValues.Add(i);
                        derivedTimes.Add(e.OriginatingTime);
                    });

                pipeline.Run();
            }

            // verify that we read the data
            Assert.AreEqual(10, originalValues.Count);
            Assert.AreEqual(10, originalTimes.Count);
            Assert.AreEqual(10, derivedValues.Count);
            Assert.AreEqual(10, derivedTimes.Count);

            // verify values from both streams are what we expect
            CollectionAssert.AreEqual(originalValues.Select(x => x * multiplier).ToList(), derivedValues);
            CollectionAssert.AreEqual(originalTimes, derivedTimes);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task SessionCreateDerivedPartitionCancellation()
        {
            var dataset = new Dataset();
            var session = dataset.CreateSession();

            // generate a test store
            GenerateTestStore("OriginalStore", StorePath);

            // add a partition
            var partition0 = session.AddStorePartition("OriginalStore", StorePath, "Partition_0");
            Assert.AreEqual(1, session.Partitions.Count);
            Assert.AreEqual("Partition_0", session.Partitions[0].Name);

            int multiplier = 7;

            try
            {
                // create a cancellation token source that will automatically request cancellation after 500 ms
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

                // create a derived partition which contains the values from the original stream multiplied by a multiplier
                await session.CreateDerivedPartitionAsync(
                    (pipeline, importer, exporter, parameter) =>
                    {
                        var inputStream = importer.OpenStream<int>("Root");
                        var derivedStream = inputStream.Sample(TimeSpan.FromMinutes(1), RelativeTimeInterval.Infinite).Select(x => x * parameter).Write("DerivedStream", exporter);

                        // add a dummy source and propose a long time interval so that the operation will block (and eventually be cancelled)
                        var generator = Generators.Repeat(pipeline, 0, int.MaxValue, TimeSpan.FromMilliseconds(1000));
                        var replayTimeInterval = TimeInterval.LeftBounded(importer.OriginatingTimeInterval.Left);
                        pipeline.ProposeReplayTime(replayTimeInterval);
                    },
                    multiplier,
                    "Partition_1",
                    false,
                    "DerivedStore",
                    StorePath,
                    null,
                    null,
                    cts.Token);
            }
            catch (OperationCanceledException)
            {
                // should NOT have created a new partition (but original partition should be intact)
                Assert.AreEqual(1, session.Partitions.Count);
                Assert.IsTrue(Store.Exists("OriginalStore", StorePath));
                Assert.IsFalse(Store.Exists("DerivedStore", StorePath));

                // verify original partition metadata
                var originalPartition = session.Partitions[0] as StorePartition;
                Assert.AreEqual("Partition_0", originalPartition.Name);
                Assert.AreEqual("OriginalStore", originalPartition.StoreName);
                Assert.AreEqual(StorePath, originalPartition.StorePath);

                throw;
            }
        }

        private static void GenerateTestStore(string storeName, string storePath)
        {
            using (var p = Pipeline.Create())
            {
                var store = Store.Create(p, storeName, storePath);
                var root = Generators.Sequence(p, 0, i => i + 1, 10, TimeSpan.FromTicks(1)).Write("Root", store);
                p.Run();
            }
        }
    }
}
