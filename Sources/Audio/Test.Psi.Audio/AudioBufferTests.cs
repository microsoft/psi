// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AudioBufferTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_Empty()
        {
            AudioBuffer buffer = new AudioBuffer(0, WaveFormat.Create16kHz1Channel16BitPcm());
            Assert.AreEqual(0, buffer.Length);
            Assert.AreEqual(0, buffer.Data.Length);
            Assert.AreEqual(WaveFormat.Create16kHz1Channel16BitPcm(), buffer.Format);
            Assert.AreEqual(TimeSpan.Zero, buffer.Duration);
            CollectionAssert.AreEqual(new double[0], this.GetSamples(buffer).ToArray());
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_16kHz1Channel16BitPcm1Sample()
        {
            AudioBuffer buffer = new AudioBuffer(BitConverter.GetBytes((short)-12345), WaveFormat.Create16kHz1Channel16BitPcm());
            Assert.AreEqual(-12345, BitConverter.ToInt16(buffer.Data, 0));
            Assert.AreEqual(2, buffer.Length);
            Assert.AreEqual(2, buffer.Data.Length);
            Assert.AreEqual(WaveFormat.Create16kHz1Channel16BitPcm(), buffer.Format);
            Assert.AreEqual(TimeSpan.FromTicks(10000000L / 16000), buffer.Duration);
            CollectionAssert.AreEqual(new double[] { -12345 }, this.GetSamples(buffer).ToArray());
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_16kHz1Channel16BitPcm3Samples()
        {
            short[] rawValues = new short[] { -32768, 32767, 12345 };
            byte[] rawBytes = rawValues.SelectMany(x => BitConverter.GetBytes(x)).ToArray();

            AudioBuffer buffer = new AudioBuffer(rawBytes, WaveFormat.Create16kHz1Channel16BitPcm());
            Assert.AreEqual(-32768, BitConverter.ToInt16(buffer.Data, 0));
            Assert.AreEqual(32767, BitConverter.ToInt16(buffer.Data, 2));
            Assert.AreEqual(12345, BitConverter.ToInt16(buffer.Data, 4));
            Assert.AreEqual(6, buffer.Length);
            Assert.AreEqual(6, buffer.Data.Length);
            Assert.AreEqual(WaveFormat.Create16kHz1Channel16BitPcm(), buffer.Format);
            Assert.AreEqual(TimeSpan.FromTicks(rawValues.Length * (10000000L / 16000)), buffer.Duration);
            CollectionAssert.AreEqual(new double[] { -32768, 32767, 12345 }, this.GetSamples(buffer).ToArray());
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_HasValidData()
        {
            Assert.IsFalse(default(AudioBuffer).HasValidData);
            Assert.IsTrue(new AudioBuffer(new byte[2], WaveFormat.Create16kHz1Channel16BitPcm()).HasValidData);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_Serialize()
        {
            byte[] rawBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
            var wf = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_APTX, 16000, 16, 2, 0, 16000);
            AudioBuffer buffer = new AudioBuffer(rawBytes, wf);

            var writer = new BufferWriter(100);
            Serializer.Serialize(writer, buffer, new SerializationContext());
            AudioBuffer bresult = default(AudioBuffer);
            Serializer.Deserialize(new BufferReader(writer.Buffer), ref bresult, new SerializationContext());

            Assert.AreEqual(6, bresult.Length);
            Assert.AreEqual(6, bresult.Data.Length);
            Assert.AreEqual(wf, bresult.Format);
            CollectionAssert.AreEqual(rawBytes, bresult.Data);
        }

        [TestMethod]
        [Timeout(60000)]
        public void AudioBuffer_Persist()
        {
            byte[] rawBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
            var wf = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_APTX, 16000, 16, 2, 0, 16000);
            AudioBuffer buffer = new AudioBuffer(rawBytes, wf);
            AudioBuffer bresult = default(AudioBuffer);

            var p1 = Pipeline.Create();
            var store = PsiStore.Create(p1, "audio", null);
            Generators.Return(p1, buffer).Write("audio", store);
            p1.RunAsync();

            var p2 = Pipeline.Create();
            var store2 = PsiStore.Open(p2, "audio", null);
            store2.OpenStream<AudioBuffer>("audio").Do(b => bresult = b);
            p2.RunAsync();
            System.Threading.Thread.Sleep(100);
            p1.Dispose();
            p2.Dispose();

            Assert.AreEqual(6, bresult.Length);
            Assert.AreEqual(6, bresult.Data.Length);
            Assert.AreEqual(wf, bresult.Format);
            CollectionAssert.AreEqual(rawBytes, bresult.Data);
        }

        private IEnumerable<double> GetSamples(AudioBuffer buffer, int channel = 0)
        {
            int bitsPerSample = buffer.Format.BitsPerSample;
            int bytesPerSample = bitsPerSample / 8;
            int start = 0 + ((channel % buffer.Format.Channels) * bytesPerSample);
            int end = buffer.Length;
            int step = buffer.Format.BlockAlign;

            // Assumes sample format based on bits per sample: 16 = int, 32 = float
            if (bitsPerSample == 16)
            {
                for (int offset = start; offset < end; offset += step)
                {
                    yield return BitConverter.ToInt16(buffer.Data, offset);
                }
            }
            else if (bitsPerSample == 32)
            {
                for (int offset = start; offset < end; offset += step)
                {
                    yield return BitConverter.ToSingle(buffer.Data, offset);
                }
            }
            else
            {
                throw new NotSupportedException($"{bitsPerSample}-bit samples not yet supported");
            }
        }
    }
}