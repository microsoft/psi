// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Robotics.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Remoting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Validates that chunk encoding/decoding works properly.
    /// </summary>
    [TestClass]
    public class UdpChunkingTest
    {
        /// <summary>
        /// Maximum datagram size (just under 64K).
        /// </summary>
        private const int MaxDatagramSize = 0xFFFE;

        /// <summary>
        /// Small data test - fits within single datagram.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpSmallDataTest()
        {
            var sender = new DataChunker(MaxDatagramSize);
            var data2KB = RandomData(0xFFF);
            var chunks = GetChunks(1, data2KB, sender);
            VerifyReceive(data2KB, chunks, 1, true, 1, 0);
        }

        /// <summary>
        /// Medium data test - split into two chunks.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpMediumDataTest()
        {
            var sender = new DataChunker(MaxDatagramSize);
            var data64KB = RandomData(0xFFFF);
            var chunks = GetChunks(1, data64KB, sender);
            VerifyReceive(data64KB, chunks, 2, true, 1, 0);
        }

        /// <summary>
        /// Medium data reversed test - split into two chunks, received in reverse order.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpMediumDataReversedTest()
        {
            var data64KB = RandomData(0xFFFF);
            var chunks = GetChunks(1, data64KB, new DataChunker(MaxDatagramSize));
            chunks.Reverse();
            VerifyReceive(data64KB, chunks, 2, true, 1, 0);
        }

        /// <summary>
        /// Medium data scrambled test - split into two chunks, received totally out of order.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpMediumDataScrambledTest()
        {
            var data64KB = RandomData(0xFFFF);
            var chunks = ScrambleChunks(GetChunks(1, data64KB, new DataChunker(MaxDatagramSize)));
            VerifyReceive(data64KB, chunks, 2, true, 1, 0);
        }

        /// <summary>
        /// Large data test - split into many chunks.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpLargeDataTest()
        {
            var data1MB = RandomData(0xFFFFF);
            var chunks = GetChunks(2, data1MB, new DataChunker(MaxDatagramSize));
            VerifyReceive(data1MB, chunks, 17, true, 1, 0);
        }

        /// <summary>
        /// Large data reversed test - split into many chunks, received in reverse order.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpLargeDataReversedTest()
        {
            var data1MB = RandomData(0xFFFFF);
            var chunks = GetChunks(2, data1MB, new DataChunker(MaxDatagramSize));
            chunks.Reverse();
            VerifyReceive(data1MB, chunks, 17, true, 1, 0);
        }

        /// <summary>
        /// Large data scrambled test - split into many chunks, received totally out of order.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpLargeDataScrambledTest()
        {
            var data1MB = RandomData(0xFFFFF);
            var chunks = ScrambleChunks(GetChunks(2, data1MB, new DataChunker(MaxDatagramSize)));
            VerifyReceive(data1MB, chunks, 17, true, 1, 0);
        }

        /// <summary>
        /// Data dropped test - two sets of chunks, first with dropped datagrams.
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpDroppedDatagramsTest()
        {
            var data1MB = RandomData(0xFFFFF);
            var data2KB = RandomData(0xFFF);
            var sender = new DataChunker(MaxDatagramSize);
            var chunks = GetChunks(2, data1MB, sender).ToList();
            chunks.RemoveAt(7); // drop!
            var concat = chunks.Concat(GetChunks(3, data2KB, sender)); // then send a different set, abandons and completes instead
            VerifyReceive(data2KB, concat, 17, true, 2, 1); // receives 2 chunks + 1 abandoned set
        }

        /// <summary>
        /// Data interleaving test - two large sets of chunks interleaved (both fail).
        /// </summary>
        [TestMethod]
        [Timeout(60000)]
        public void UdpInterleavedDatagramsTest()
        {
            var sender = new DataChunker(MaxDatagramSize);
            var chunksA = GetChunks(1, RandomData(0xFFFFF), sender).ToArray();
            var chunksB = GetChunks(2, RandomData(0xFFFFF), sender).ToArray();
            var interleaved = chunksA.Zip(chunksB, (a, b) => new byte[][] { a, b }).SelectMany(_ => _).ToArray();
            VerifyReceive(null, interleaved, 34, false, 34, 33); // 34 starts/restarts, 33 abandoned, last set incomplete
        }

        /// <summary>
        /// Run through a scenario and validate results.
        /// </summary>
        /// <param name="rawData">Raw data for this scenario.</param>
        /// <param name="chunks">Chunks for this scenario.</param>
        /// <param name="numChunks">Number of expected chunks.</param>
        /// <param name="complete">Whether to expect completion.</param>
        /// <param name="numChunkSets">Number of expected sets of chunks.</param>
        /// <param name="numAbandoned">Number of expected abandoned datagrams.</param>
        private static void VerifyReceive(byte[] rawData, IEnumerable<byte[]> chunks, int numChunks, bool complete, int numChunkSets, int numAbandoned)
        {
            int chunksets = 0;
            int abandoned = 0;
            var receiver = new DataUnchunker(MaxDatagramSize, id => { chunksets++; }, id => { abandoned++; });
            Assert.AreEqual<int>(numChunks, chunks.Count());
            Assert.AreEqual<bool>(complete, ReceiveChunks(chunks, receiver));
            Assert.AreEqual<int>(numChunkSets, chunksets);
            Assert.AreEqual<int>(numAbandoned, abandoned);
            if (rawData != null)
            {
                VerifyData(rawData, receiver.Payload, receiver.Length);
            }
        }

        /// <summary>
        /// Produce random test data to be used as UDP payload.
        /// </summary>
        /// <param name="length">Number of bytes to produce.</param>
        /// <returns>Random bytes.</returns>
        private static byte[] RandomData(int length)
        {
            var rand = new Random();
            var bytes = new byte[length];
            rand.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// Scramble set of chunks to simulate out-of-order receive.
        /// </summary>
        /// <param name="chunks">Chunks to scramble.</param>
        /// <returns>Scrambled chunks.</returns>
        private static IEnumerable<byte[]> ScrambleChunks(IEnumerable<byte[]> chunks)
        {
            var scrambled = chunks.ToArray();
            var rand = new Random();
            for (var i = 0; i < scrambled.Length; i++)
            {
                var r = rand.Next(scrambled.Length);
                var t = scrambled[i];
                scrambled[i] = scrambled[r];
                scrambled[r] = t;
            }

            return scrambled;
        }

        /// <summary>
        /// Verify send and received data match.
        /// </summary>
        /// <param name="sent">Sent data payload.</param>
        /// <param name="received">Received data payload.</param>
        /// <param name="length">Payload length.</param>
        private static void VerifyData(byte[] sent, byte[] received, int length)
        {
            Assert.AreEqual<int>(length, sent.Length, "Data length mismatch.");
            for (var i = 0; i < length; i++)
            {
                Assert.AreEqual<byte>(sent[i], received[i], "Data mismatch.");
            }
        }

        /// <summary>
        /// Convert payload data chunked into a list of datagrams.
        /// </summary>
        /// <param name="id">Message ID.</param>
        /// <param name="data">Payload bytes.</param>
        /// <param name="sender">UdpChunkSender instance.</param>
        /// <returns>List of datagrams.</returns>
        private static IEnumerable<byte[]> GetChunks(int id, byte[] data, DataChunker sender)
        {
            return sender.GetChunks(id, data, data.Length).Select(chunk => chunk.Item1.Take(chunk.Item2).ToArray());
        }

        /// <summary>
        /// Receive set of chunks.
        /// </summary>
        /// <param name="chunks">Received chunks.</param>
        /// <param name="receiver">UdpChunkReceiver instance.</param>
        /// <returns>Flag indicating whether last chunk signaled completion.</returns>
        private static bool ReceiveChunks(IEnumerable<byte[]> chunks, DataUnchunker receiver)
        {
            var completions = chunks.Select(c => receiver.Receive(c)).ToList();
            Assert.IsTrue(completions.Count(_ => _) <= 1, "Multiple completions.");
            return completions.Last();
        }
    }
}