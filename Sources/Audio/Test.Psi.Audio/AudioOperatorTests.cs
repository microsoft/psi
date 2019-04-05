// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AudioOperatorTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_ReframeLarger()
        {
            var audioFormat = WaveFormat.Create16kHz1Channel16BitPcm();
            var audioData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            int inputCount = 100;
            int inputSize = 10;
            int outputSize = 16;
            var inputInterval = TimeSpan.FromTicks(inputSize * 10000 / 32);  // 10 bytes @ 32 kBytes/sec
            var outputInterval = TimeSpan.FromTicks(outputSize * 10000 / 32); // 16 bytes @ 32 kBytes/sec

            var output = new List<(AudioBuffer, DateTime)>();
            var startTime = DateTime.MinValue;

            using (var p = Pipeline.Create())
            {
                // input stream of 10-byte audio buffers
                var audio = Generators.Repeat(p, new AudioBuffer(audioData, audioFormat), inputCount, inputInterval);

                // reframe output stream as 16-byte audio buffers
                var reframed = audio.Reframe(outputSize);

                // capture outputs and start time for verification
                reframed.Do((x, e) => output.Add((x.DeepClone(), e.OriginatingTime)));
                audio.First().Do((x, e) => startTime = e.OriginatingTime - inputInterval);

                p.Run();
            }

            // verify no. of reframed output buffers
            Assert.AreEqual(inputCount * inputSize / outputSize, output.Count);

            foreach (var (buffer, dt) in output)
            {
                // verify output audio buffer originating times
                startTime += outputInterval;
                Assert.AreEqual(startTime, dt);

                // verify audio format remains the same
                Assert.AreEqual(audioFormat, buffer.Format);

                // verify that the output audio bytes equal outputSize bytes taken from two consecutive input data buffers
                CollectionAssert.AreEqual(audioData.Concat(audioData.Take(outputSize - inputSize)).ToArray(), buffer.Data);

                // shift the input data to be aligned with the start of the next expected output buffer
                audioData = audioData.Skip(outputSize - inputSize).Concat(audioData.Take(outputSize - inputSize)).ToArray();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_ReframeSmaller()
        {
            var audioFormat = WaveFormat.Create16kHz1Channel16BitPcm();
            var audioData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            int inputCount = 100;
            int inputSize = 10;
            int outputSize = 6;
            var inputInterval = TimeSpan.FromTicks(inputSize * 10000 / 32);  // 10 bytes @ 32 kBytes/sec
            var outputInterval = TimeSpan.FromTicks(outputSize * 10000 / 32); // 6 bytes @ 32 kBytes/sec

            var output = new List<(AudioBuffer, DateTime)>();
            var startTime = DateTime.MinValue;

            using (var p = Pipeline.Create())
            {
                // input stream of 10-byte audio buffers
                var audio = Generators.Repeat(p, new AudioBuffer(audioData, audioFormat), inputCount, inputInterval);

                // reframe output stream as 6-byte audio buffers
                var reframed = audio.Reframe(outputSize);

                // capture outputs and start time for verification
                reframed.Do((x, e) => output.Add((x.DeepClone(), e.OriginatingTime)));
                audio.First().Do((x, e) => startTime = e.OriginatingTime - inputInterval);

                p.Run();
            }

            // verify no. of reframed output buffers
            Assert.AreEqual(inputCount * inputSize / outputSize, output.Count);

            foreach (var (buffer, dt) in output)
            {
                // verify output audio buffer originating times
                startTime += outputInterval;
                Assert.AreEqual(startTime, dt);

                // verify audio format remains the same
                Assert.AreEqual(audioFormat, buffer.Format);

                // verify that the output audio bytes match the first [outputSize] bytes of the input data
                CollectionAssert.AreEqual(audioData.Take(outputSize).ToArray(), buffer.Data);

                // shift the input data to be aligned with the start of the next expected output buffer
                audioData = audioData.Skip(outputSize).Concat(audioData.Take(outputSize)).ToArray();
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_ReframeByDuration()
        {
            var audioFormat = WaveFormat.Create16kHz1Channel16BitPcm();
            var audioData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            int inputCount = 100;
            int inputSize = 10;
            var inputInterval = TimeSpan.FromTicks(inputSize * 10000 / 32); // 10 bytes @ 32 kBytes/sec
            var outputInterval = TimeSpan.FromMilliseconds(10);             // 10 ms
            int outputSize = (int)(outputInterval.TotalSeconds * audioFormat.AvgBytesPerSec);

            var output = new List<(AudioBuffer, DateTime)>();
            var startTime = DateTime.MinValue;

            using (var p = Pipeline.Create())
            {
                // input stream of 10-byte audio buffers
                var audio = Generators.Repeat(p, new AudioBuffer(audioData, audioFormat), inputCount, inputInterval);

                // reframe output stream as 10 ms audio buffers
                var reframed = audio.Reframe(outputInterval);

                // capture outputs and start time for verification
                reframed.Do((x, e) => output.Add((x.DeepClone(), e.OriginatingTime)));
                audio.First().Do((x, e) => startTime = e.OriginatingTime - inputInterval);

                p.Run();
            }

            // verify no. of reframed output buffers
            Assert.AreEqual(inputCount * inputSize / outputSize, output.Count);

            foreach (var (buffer, dt) in output)
            {
                // verify output audio buffer originating times
                startTime += outputInterval;
                Assert.AreEqual(startTime, dt);

                // verify audio format remains the same
                Assert.AreEqual(audioFormat, buffer.Format);

                // verify the output audio bytes by constructing the expected output from a concatenation of the input data
                var expectedOutput = Enumerable.Repeat(audioData, outputSize / inputSize).SelectMany(x => x).Concat(audioData.Take(outputSize % inputSize));
                CollectionAssert.AreEqual(expectedOutput.ToArray(), buffer.Data);

                // shift the input data to account for any partial bytes when constructing the expected output above
                audioData = audioData.Skip(outputSize % inputSize).Concat(audioData.Take(outputSize % inputSize)).ToArray();
            }
        }
    }
}