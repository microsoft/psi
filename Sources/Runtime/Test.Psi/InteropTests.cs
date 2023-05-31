// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Format;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class InteropTests
    {
        private string path = Path.Combine(Environment.CurrentDirectory, nameof(PersistenceTest));
        private DateTime originatingTime;

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(this.path);
            this.originatingTime = new DateTime(1971, 11, 3, 0, 0, 0, 123, DateTimeKind.Utc).AddTicks(4567);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestRunner.SafeDirectoryDelete(this.path, true);
        }

        private void AssertStringSerialization(dynamic value, string expected, IFormatSerializer serializer, IFormatDeserializer deserializer, bool roundTrip = true)
        {
            var serialized = serializer.SerializeMessage(value, this.originatingTime);
            Assert.AreEqual<string>(expected, Encoding.UTF8.GetString(serialized.Item1, serialized.Item2, serialized.Item3));

            if (roundTrip)
            {
                var deserialized = deserializer.DeserializeMessage(serialized.Item1, serialized.Item2, serialized.Item3);
                Assert.AreEqual(this.originatingTime, deserialized.Item2);

                var roundtrip = serializer.SerializeMessage(deserialized.Item1, this.originatingTime);
                Assert.AreEqual<string>(expected, Encoding.UTF8.GetString(roundtrip.Item1, roundtrip.Item2, roundtrip.Item3));
            }
        }

        private void AssertBinarySerialization(dynamic value, IFormatSerializer serializer, IFormatDeserializer deserializer)
        {
            var serialized = serializer.SerializeMessage(value, this.originatingTime);
            var deserialized = deserializer.DeserializeMessage(serialized.Item1, serialized.Item2, serialized.Item3);
            Assert.AreEqual(this.originatingTime, deserialized.Item2);

            var roundtrip = serializer.SerializeMessage(deserialized.Item1, this.originatingTime);
            Enumerable.SequenceEqual<byte>(serialized.Item1, roundtrip.Item1);
            Assert.AreEqual<int>(serialized.Item2, roundtrip.Item2);
            Assert.AreEqual<int>(serialized.Item3, roundtrip.Item3);
        }

        [TestMethod]
        [Timeout(60000)]
        public void JsonFormatSerializerTest()
        {
            var json = JsonFormat.Instance;
            this.AssertStringSerialization(123, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":123}", json, json);
            this.AssertStringSerialization(true, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":true}", json, json);
            this.AssertStringSerialization(2.71828, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":2.71828}", json, json);
            this.AssertStringSerialization("Howdy", @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":""Howdy""}", json, json);
            this.AssertStringSerialization((object)null, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":null}", json, json);
            this.AssertStringSerialization(new[] { 1, 2, 3 }, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":[1,2,3]}", json, json);

            var structured = new
            {
                ID = 123,
                Confidence = 0.92,
                Face = new
                {
                    X = 213,
                    Y = 107,
                    Width = 42,
                    Height = 61,
                },
            };
            this.AssertStringSerialization(structured, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":{""ID"":123,""Confidence"":0.92,""Face"":{""X"":213,""Y"":107,""Width"":42,""Height"":61}}}", json, json);

            var nestedArray = new
            {
                Intent = "vanilla",
                Description = "Testing",
                ActionList = new[]
                {
                    new
                    {
                        Name = "SayText",
                        Args = new[] { "default", "both" },
                    },
                },
            };
            this.AssertStringSerialization(nestedArray, @"{""originatingTime"":""1971-11-03T00:00:00.1234567Z"",""message"":{""Intent"":""vanilla"",""Description"":""Testing"",""ActionList"":[{""Name"":""SayText"",""Args"":[""default"",""both""]}]}}", json, json);

            // also verify "manually"
            var serialized = json.SerializeMessage(structured, this.originatingTime);
            var deserialized = json.DeserializeMessage(serialized.Item1, serialized.Item2, serialized.Item3);
            var message = deserialized.Item1;
            var timestamp = deserialized.Item2;
            Assert.AreEqual<DateTime>(this.originatingTime, timestamp);
            Assert.AreEqual<int>(123, message.ID);
            Assert.AreEqual<double>(0.92, message.Confidence);
            Assert.AreEqual<int>(213, message.Face.X);
            Assert.AreEqual<int>(107, message.Face.Y);
            Assert.AreEqual<int>(42, message.Face.Width);
            Assert.AreEqual<int>(61, message.Face.Height);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CsvFormatSerializerTest()
        {
            var csv = CsvFormat.Instance;
            this.AssertStringSerialization(123, "_OriginatingTime_,_Value_\r\n1971-11-03T00:00:00.1234567Z,123\r\n", csv, csv);
            this.AssertStringSerialization(true, "_OriginatingTime_,_Value_\r\n1971-11-03T00:00:00.1234567Z,True\r\n", csv, csv);
            this.AssertStringSerialization(2.71828, "_OriginatingTime_,_Value_\r\n1971-11-03T00:00:00.1234567Z,2.71828\r\n", csv, csv);
            this.AssertStringSerialization("Howdy", "_OriginatingTime_,_Value_\r\n1971-11-03T00:00:00.1234567Z,Howdy\r\n", csv, csv);

            // special case
            this.AssertStringSerialization(new double[] { 1, 2, 3 }, "_OriginatingTime_,_Column0_,_Column1_,_Column2_\r\n1971-11-03T00:00:00.1234567Z,1,2,3\r\n", csv, csv);

            var structured = new
            {
                ID = 123,
                Confidence = 0.92,
                Face = new
                {
                    X = 213,
                    Y = 107,
                    Width = 42,
                    Height = 61,
                    Points = new[] { 123, 456 },
                },
            };

            // notice Face is traversed but flattened - no hierarchy allowed
            this.AssertStringSerialization(structured, "_OriginatingTime_,ID,Confidence,X,Y,Width,Height\r\n1971-11-03T00:00:00.1234567Z,123,0.92,213,107,42,61\r\n", csv, csv);

            var structuredAmbiguous = new
            {
                ID = 123,
                Confidence = 0.92,
                Face = new
                {
                    Confidence = 0.89,
                    X = 213,
                    Y = 107,
                    Width = 42,
                    Height = 61,
                },
            };

            // notice Face is traversed but flattened - no hierarchy allowed
            this.AssertStringSerialization(structuredAmbiguous, "_OriginatingTime_,ID,Confidence,Confidence,X,Y,Width,Height\r\n1971-11-03T00:00:00.1234567Z,123,0.92,0.89,213,107,42,61\r\n", csv, csv, false);

            var flat = new
            {
                ID = 123,
                Confidence = 0.92,
                FaceX = 213,
                FaceY = 107,
                FaceWidth = 42,
                FaceHeight = 61,
            };
            this.AssertStringSerialization(flat, "_OriginatingTime_,ID,Confidence,FaceX,FaceY,FaceWidth,FaceHeight\r\n1971-11-03T00:00:00.1234567Z,123,0.92,213,107,42,61\r\n", csv, csv);
        }

        [TestMethod]
        [Timeout(60000)]
        public void MessagePackFormatSerializerTest()
        {
            var msg = MessagePackFormat.Instance;
            this.AssertBinarySerialization(123, msg, msg);
            this.AssertBinarySerialization(true, msg, msg);
            this.AssertBinarySerialization(2.71828, msg, msg);
            this.AssertBinarySerialization("Howdy", msg, msg);
            this.AssertBinarySerialization(new[] { 1, 2, 3 }, msg, msg);

            var structured = new
            {
                ID = 123,
                Confidence = 0.92,
                Face = new
                {
                    X = 213,
                    Y = 107,
                    Width = 42,
                    Height = 61,
                },
            };

            // can't round-trip ExpandoObjects, so verifying "manually"
            var serialized = msg.SerializeMessage(structured, this.originatingTime);
            var deserialized = msg.DeserializeMessage(serialized.Item1, serialized.Item2, serialized.Item3);
            var message = deserialized.Item1;
            var timestamp = deserialized.Item2;
            Assert.AreEqual<DateTime>(this.originatingTime, timestamp);
            Assert.AreEqual<int>(123, message.ID);
            Assert.AreEqual<double>(0.92, message.Confidence);
            Assert.AreEqual<int>(213, message.Face.X);
            Assert.AreEqual<int>(107, message.Face.Y);
            Assert.AreEqual<int>(42, message.Face.Width);
            Assert.AreEqual<int>(61, message.Face.Height);
        }

        private void FilePersistenceFormatTest(string extension, IPersistentFormatSerializer serializer, IPersistentFormatDeserializer deserializer)
        {
            var filePath = $"{this.path}/test.{extension}";

            using (var p = Pipeline.Create())
            {
                var gen = Generators.Range(p, 0, 10, TimeSpan.FromMilliseconds(10));
                var file = new FileWriter<int>(p, filePath, serializer);
                gen.PipeTo(file);
                p.Run();
            }

            using (var q = Pipeline.Create())
            {
                var read = new FileSource<int>(q, filePath, deserializer);
                var results = read.ToObservable().ToListObservable();
                q.Run();
                Assert.IsTrue(Enumerable.SequenceEqual(results.AsEnumerable(), new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void FilePersistenceTest()
        {
            this.FilePersistenceFormatTest("json", JsonFormat.Instance, JsonFormat.Instance);
            this.FilePersistenceFormatTest("csv", CsvFormat.Instance, CsvFormat.Instance);
            this.FilePersistenceFormatTest("msg", MessagePackFormat.Instance, MessagePackFormat.Instance);
        }

        [TestMethod]
        [Timeout(60000)]
        private void NetMQTransportTest()
        {
            const string topic = "test-topic";
            string address = "tcp://localhost:12345";

            // start client
            ListObservable<double> results;
            var complete = false;
            using (var p = Pipeline.Create())
            {
                Console.WriteLine("Starting client...");
                var client = new NetMQSource<double>(p, topic, address, JsonFormat.Instance);
                client.Do(x => complete = x == 9).Do(x => Console.WriteLine($"MSG: {x}"));
                results = client.ToObservable().ToListObservable();
                p.RunAsync();

                Thread.Sleep(1000); // give time to open TCP port

                // start server - run to end of finite generator
                Console.WriteLine("Starting server...");
                using (var q = Pipeline.Create())
                {
                    var gen = Generators.Range(q, 0, 10, TimeSpan.FromMilliseconds(10)).Select(x => (double)x);
                    var server = new NetMQWriter<double>(q, topic, address, JsonFormat.Instance);
                    gen.PipeTo(server);
                    q.Run();
                }

                Console.WriteLine("Waiting...");
                while (!complete)
                {
                    Thread.Sleep(100);
                }
            }

            Assert.IsTrue(Enumerable.SequenceEqual(results.AsEnumerable(), new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }

        [TestMethod]
        [Timeout(60000)]
        private void NetMQTransportMultiTopicTest()
        {
            const string topic0 = "test-topic0";
            const string topic1 = "test-topic1";
            string address = "tcp://localhost:12345";

            // start client
            ListObservable<double> results0;
            ListObservable<int> results1;
            var complete0 = false;
            var complete1 = false;
            using (var p = Pipeline.Create())
            {
                Console.WriteLine("Starting client...");
                var client0 = new NetMQSource<double>(p, topic0, address, JsonFormat.Instance);
                client0.Do(x => complete0 = x == 9).Do(x => Console.WriteLine($"MSG0: {x}"));
                results0 = client0.ToObservable().ToListObservable();
                var client1 = new NetMQSource<int>(p, topic1, address, JsonFormat.Instance);
                client1.Do(x => complete1 = x == 9).Do(x => Console.WriteLine($"MSG1: {x}"));
                results1 = client1.ToObservable().ToListObservable();
                p.RunAsync();

                Thread.Sleep(1000); // give time to open TCP port

                // start server - run to end of finite generator
                Console.WriteLine("Starting server...");
                using (var q = Pipeline.Create())
                {
                    var gen0 = Generators.Range(q, 0, 10, TimeSpan.FromMilliseconds(10)).Select(x => (double)x);
                    var gen1 = Generators.Range(q, 0, 10, TimeSpan.FromMilliseconds(10));
                    var server = new NetMQWriter(q, address, JsonFormat.Instance);
                    var receiver0 = server.AddTopic<double>(topic0);
                    var receiver1 = server.AddTopic<int>(topic1);
                    gen0.PipeTo(receiver0);
                    gen1.PipeTo(receiver1);
                    q.Run();
                }

                Console.WriteLine("Waiting...");
                while (!complete0 || !complete1)
                {
                    Thread.Sleep(100);
                }
            }

            Assert.IsTrue(Enumerable.SequenceEqual(results0.AsEnumerable(), new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.IsTrue(Enumerable.SequenceEqual(results1.AsEnumerable(), new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        }
    }
}
