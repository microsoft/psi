// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Test.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi;
    using Microsoft.Psi.Data.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class SimpleObject
    {
        public int[] ArrayValue { get; set; }

        public bool BoolValue { get; set; }

        public DateTime DateTimeValue { get; set; }

        public double DoubleValue { get; set; }

        public int IntValue { get; set; }

        public List<int> ListValue { get; set; }

        public string StringValue { get; set; }

        public TimeSpan TimeSpanValue { get; set; }
    }

    [TestClass]
    public class JsonTests
    {
        public static readonly string RootPath;
        public static readonly string InputPath;
        public static readonly string OutputPath;

        public static readonly string StoreName;
        public static readonly string TypeName;
        public static readonly string PartitionName;
        public static readonly string PartitionPath;
        public static readonly DateTime FirstTime;
        public static readonly DateTime LastTime;

        public static readonly SimpleObject Data;

        static JsonTests()
        {
            RootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            InputPath = Path.Combine(RootPath, "Input");
            OutputPath = Path.Combine(RootPath, "Output");
            StoreName = "JsonStore";

            TypeName = "Test.Psi.Data.SimpleObject";
            PartitionName = "JsonStore";
            PartitionPath = "Input";
            FirstTime = DateTime.Parse("2017-11-01T09:15:30.12345Z", null, DateTimeStyles.AdjustToUniversal);
            LastTime = DateTime.Parse("2017-11-01T09:15:34.12345Z", null, DateTimeStyles.AdjustToUniversal);
            Data = new SimpleObject()
            {
                ArrayValue = new int[] { 0, 1, 2, 3 },
                BoolValue = true,
                DateTimeValue = DateTime.Parse("2017-11-30T12:59:41.896745Z", null, DateTimeStyles.AdjustToUniversal),
                DoubleValue = 0.123456,
                IntValue = 123456,
                ListValue = new List<int>(new int[] { 4, 5, 6, 7 }),
                StringValue = "abc",
                TimeSpanValue = TimeSpan.Parse("1:2:3.456789"),
            };
        }

        [TestMethod]
        [Timeout(60000)]
        public void JsonSimpleReaderTest()
        {
            List<Message<SimpleObject>> stream1 = new List<Message<SimpleObject>>();
            List<Message<SimpleObject>> stream2 = new List<Message<SimpleObject>>();
            IStreamMetadata metadata1 = null;
            IStreamMetadata metadata2 = null;

            using (var reader = new JsonSimpleReader())
            {
                reader.OpenStore(StoreName, InputPath);

                metadata1 = reader.AvailableStreams.First((m) => m.Name == "Stream1");
                ValidateMetadata(metadata1, "Stream1", 1, TypeName, PartitionName, PartitionPath, FirstTime, LastTime, FirstTime, LastTime, 388, 0, 2);

                metadata2 = reader.AvailableStreams.First((m) => m.Name == "Stream2");
                ValidateMetadata(metadata2, "Stream2", 2, TypeName, PartitionName, PartitionPath, FirstTime, LastTime, FirstTime, LastTime, 388, 0, 2);

                reader.OpenStream<SimpleObject>("Stream1", (d, e) => stream1.Add(new Message<SimpleObject>(d, e.OriginatingTime, e.Time, e.SourceId, e.SequenceId)));
                reader.OpenStream<SimpleObject>("Stream2", (d, e) => stream2.Add(new Message<SimpleObject>(d, e.OriginatingTime, e.Time, e.SourceId, e.SequenceId)));
                reader.ReadAll(ReplayDescriptor.ReplayAll);
            }

            Assert.AreEqual(stream1.Count, 2);
            Assert.AreEqual(stream2.Count, 2);

            ValidateMessage(stream1[0], 1, 0, FirstTime, FirstTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream1[1], 1, 1, LastTime, LastTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream2[0], 2, 0, FirstTime, FirstTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream2[1], 2, 1, LastTime, LastTime, (data) => ValidateSimpleObject(data, Data));
        }

        [TestMethod]
        [Timeout(60000)]
        public void JsonSimpleWriterTest()
        {
            using (var writer = new JsonSimpleWriter())
            {
                writer.CreateStore(StoreName, OutputPath, false);

                IStreamMetadata metadata1 = new JsonStreamMetadata("Stream1", 1, TypeName, PartitionName, OutputPath);
                IStreamMetadata metadata2 = new JsonStreamMetadata("Stream2", 2, TypeName, PartitionName, OutputPath);

                List<Message<SimpleObject>> stream1 = new List<Message<SimpleObject>>();
                stream1.Add(new Message<SimpleObject>(Data, FirstTime, FirstTime, 1, 0));
                stream1.Add(new Message<SimpleObject>(Data, LastTime, LastTime, 1, 1));

                List<Message<SimpleObject>> stream2 = new List<Message<SimpleObject>>();
                stream2.Add(new Message<SimpleObject>(Data, FirstTime, FirstTime, 2, 0));
                stream2.Add(new Message<SimpleObject>(Data, LastTime, LastTime, 2, 1));

                writer.CreateStream(metadata1, stream1);
                writer.CreateStream(metadata2, stream2);
                writer.WriteAll(ReplayDescriptor.ReplayAll);
            }

            var escapedOutputPath = OutputPath.Replace(@"\", @"\\");
            string expectedCatalog = "[{\"Name\":\"Stream1\",\"Id\":1,\"TypeName\":\"Test.Psi.Data.SimpleObject\",\"PartitionName\":\"JsonStore\",\"PartitionPath\":\"" + escapedOutputPath + "\",\"FirstMessageTime\":\"2017-11-01T09:15:30.12345Z\",\"LastMessageTime\":\"2017-11-01T09:15:34.12345Z\",\"FirstMessageOriginatingTime\":\"2017-11-01T09:15:30.12345Z\",\"LastMessageOriginatingTime\":\"2017-11-01T09:15:34.12345Z\",\"AverageMessageSize\":303,\"AverageLatency\":0,\"MessageCount\":2},{\"Name\":\"Stream2\",\"Id\":2,\"TypeName\":\"Test.Psi.Data.SimpleObject\",\"PartitionName\":\"JsonStore\",\"PartitionPath\":\"" + escapedOutputPath + "\",\"FirstMessageTime\":\"2017-11-01T09:15:30.12345Z\",\"LastMessageTime\":\"2017-11-01T09:15:34.12345Z\",\"FirstMessageOriginatingTime\":\"2017-11-01T09:15:30.12345Z\",\"LastMessageOriginatingTime\":\"2017-11-01T09:15:34.12345Z\",\"AverageMessageSize\":303,\"AverageLatency\":0,\"MessageCount\":2}]";
            string expectedData = "[{\"Envelope\":{\"SourceId\":1,\"SequenceId\":0,\"OriginatingTime\":\"2017-11-01T09:15:30.12345Z\",\"Time\":\"2017-11-01T09:15:30.12345Z\"},\"Data\":{\"ArrayValue\":[0,1,2,3],\"BoolValue\":true,\"DateTimeValue\":\"2017-11-30T12:59:41.896745Z\",\"DoubleValue\":0.123456,\"IntValue\":123456,\"ListValue\":[4,5,6,7],\"StringValue\":\"abc\",\"TimeSpanValue\":\"01:02:03.4567890\"}},{\"Envelope\":{\"SourceId\":2,\"SequenceId\":0,\"OriginatingTime\":\"2017-11-01T09:15:30.12345Z\",\"Time\":\"2017-11-01T09:15:30.12345Z\"},\"Data\":{\"ArrayValue\":[0,1,2,3],\"BoolValue\":true,\"DateTimeValue\":\"2017-11-30T12:59:41.896745Z\",\"DoubleValue\":0.123456,\"IntValue\":123456,\"ListValue\":[4,5,6,7],\"StringValue\":\"abc\",\"TimeSpanValue\":\"01:02:03.4567890\"}},{\"Envelope\":{\"SourceId\":1,\"SequenceId\":1,\"OriginatingTime\":\"2017-11-01T09:15:34.12345Z\",\"Time\":\"2017-11-01T09:15:34.12345Z\"},\"Data\":{\"ArrayValue\":[0,1,2,3],\"BoolValue\":true,\"DateTimeValue\":\"2017-11-30T12:59:41.896745Z\",\"DoubleValue\":0.123456,\"IntValue\":123456,\"ListValue\":[4,5,6,7],\"StringValue\":\"abc\",\"TimeSpanValue\":\"01:02:03.4567890\"}},{\"Envelope\":{\"SourceId\":2,\"SequenceId\":1,\"OriginatingTime\":\"2017-11-01T09:15:34.12345Z\",\"Time\":\"2017-11-01T09:15:34.12345Z\"},\"Data\":{\"ArrayValue\":[0,1,2,3],\"BoolValue\":true,\"DateTimeValue\":\"2017-11-30T12:59:41.896745Z\",\"DoubleValue\":0.123456,\"IntValue\":123456,\"ListValue\":[4,5,6,7],\"StringValue\":\"abc\",\"TimeSpanValue\":\"01:02:03.4567890\"}}]";
            string actualCatalog = string.Empty;
            string actualData = string.Empty;

            string catalogPath = Path.Combine(OutputPath, StoreName + ".Catalog.json");
            using (var file = File.OpenText(catalogPath))
            {
                actualCatalog = file.ReadToEnd();
            }

            string dataPath = Path.Combine(OutputPath, StoreName + ".Data.json");
            using (var file = File.OpenText(dataPath))
            {
                actualData = file.ReadToEnd();
            }

            File.Delete(catalogPath);
            File.Delete(dataPath);

            Assert.AreEqual(expectedCatalog, actualCatalog);
            Assert.AreEqual(expectedData, actualData);
        }

        [TestMethod]
        [Timeout(60000)]
        public void JsonGeneratorTest()
        {
            List<Message<SimpleObject>> stream1 = new List<Message<SimpleObject>>();
            List<Message<SimpleObject>> stream2 = new List<Message<SimpleObject>>();
            IStreamMetadata metadata1 = null;
            IStreamMetadata metadata2 = null;

            using (var p = Pipeline.Create("JsonGeneratorTest"))
            {
                var generator = JsonStore.Open(p, StoreName, InputPath);

                generator.OpenStream<SimpleObject>("Stream1").Do((d, e) => stream1.Add(new Message<SimpleObject>(d, e.OriginatingTime, e.Time, e.SourceId, e.SequenceId)));
                generator.OpenStream<SimpleObject>("Stream2").Do((d, e) => stream2.Add(new Message<SimpleObject>(d, e.OriginatingTime, e.Time, e.SourceId, e.SequenceId)));

                metadata1 = generator.GetMetadata("Stream1");
                ValidateMetadata(metadata1, "Stream1", 1, TypeName, PartitionName, PartitionPath, FirstTime, LastTime, FirstTime, LastTime, 388, 0, 2);

                metadata2 = generator.GetMetadata("Stream2");
                ValidateMetadata(metadata2, "Stream2", 2, TypeName, PartitionName, PartitionPath, FirstTime, LastTime, FirstTime, LastTime, 388, 0, 2);

                p.Run();
            }

            Assert.AreEqual(stream1.Count, 2);
            Assert.AreEqual(stream2.Count, 2);

            ValidateMessage(stream1[0], FirstTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream1[1], LastTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream2[0], FirstTime, (data) => ValidateSimpleObject(data, Data));
            ValidateMessage(stream2[1], LastTime, (data) => ValidateSimpleObject(data, Data));
        }

        private static void ValidateMetadata(
            IStreamMetadata metadata,
            string name,
            int id,
            string typeName,
            string partitionName,
            string partitionPath,
            DateTime firstMessageTime,
            DateTime lastMessageTime,
            DateTime firstMessageOriginatingTime,
            DateTime lastMessageOriginatingTime,
            int averageMessageSize,
            int averageMessageLatency,
            int messageCount)
        {
            Assert.AreEqual(metadata.Name, name);
            Assert.AreEqual(metadata.Id, id);
            Assert.AreEqual(metadata.TypeName, typeName);
            Assert.AreEqual(metadata.PartitionName, partitionName);
            Assert.AreEqual(metadata.PartitionPath, partitionPath);
            Assert.AreEqual(metadata.FirstMessageTime, firstMessageTime);
            Assert.AreEqual(metadata.LastMessageTime, lastMessageTime);
            Assert.AreEqual(metadata.FirstMessageOriginatingTime, firstMessageOriginatingTime);
            Assert.AreEqual(metadata.LastMessageOriginatingTime, lastMessageOriginatingTime);
            Assert.AreEqual(metadata.AverageMessageSize, averageMessageSize);
            Assert.AreEqual(metadata.AverageLatency, averageMessageLatency);
            Assert.AreEqual(metadata.MessageCount, messageCount);
        }

        private static void ValidateMessage<T>(Message<T> message, int sourceId, int sequenceId, DateTime originatingTime, DateTime time, Action<T> validateData)
        {
            Assert.AreEqual(message.SourceId, sourceId);
            Assert.AreEqual(message.SequenceId, sequenceId);
            Assert.AreEqual(message.Time, time);
            ValidateMessage(message, originatingTime, validateData);
        }

        private static void ValidateMessage<T>(Message<T> message, DateTime originatingTime, Action<T> validateData)
        {
            Assert.AreEqual(message.OriginatingTime, originatingTime);
            validateData(message.Data);
        }

        private static void ValidateSimpleObject(SimpleObject data1, SimpleObject data2)
        {
            Assert.IsTrue((data1.ArrayValue == null && data2.ArrayValue == null) || (data1.ArrayValue != null && data1.ArrayValue.SequenceEqual(data2.ArrayValue)));
            Assert.AreEqual(data1.BoolValue, data2.BoolValue);
            Assert.AreEqual(data1.DateTimeValue, data2.DateTimeValue);
            Assert.AreEqual(data1.DoubleValue, data2.DoubleValue);
            Assert.AreEqual(data1.IntValue, data2.IntValue);
            Assert.IsTrue((data1.ListValue == null && data2.ListValue == null) || (data1.ListValue != null && data1.ListValue.SequenceEqual(data2.ListValue)));
            Assert.AreEqual(data1.StringValue, data2.StringValue);
            Assert.AreEqual(data1.TimeSpanValue, data2.TimeSpanValue);
        }
    }
}
