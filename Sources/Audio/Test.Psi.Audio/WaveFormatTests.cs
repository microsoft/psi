// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Audio
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Wave format tests.
    /// </summary>
    [TestClass]
    public class WaveFormatTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_CreatePcm()
        {
            // Define "native" WAVEFORMATEX structure for PCM
            byte[] formatBytes = new byte[]
            {
                0x01, 0x00, // FormatTag = 1
                0x02, 0x00, // Channels = 2
                0x44, 0xac, 0x00, 0x00, // SamplesPerSec = 44100
                0x10, 0xb1, 0x02, 0x00, // AvgBytesPerSec = 176400
                0x04, 0x00, // BlockAlign = 4
                0x10, 0x00, // BitsPerSample = 16
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormat.CreatePcm(44100, 16, 2);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_Create16kHz1Channel16BitPcm()
        {
            // Define "native" WAVEFORMATEX structure for PCM
            byte[] formatBytes = new byte[]
            {
                0x01, 0x00, // FormatTag = 1
                0x01, 0x00, // Channels = 1
                0x80, 0x3e, 0x00, 0x00, // SamplesPerSec = 16000
                0x00, 0x7d, 0x00, 0x00, // AvgBytesPerSec = 32000
                0x02, 0x00, // BlockAlign = 2
                0x10, 0x00, // BitsPerSample = 16
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormat.Create16kHz1Channel16BitPcm();

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_CreateIeeeFloat()
        {
            // Define "native" WAVEFORMATEX structure for IEEE float PCM
            byte[] formatBytes = new byte[]
            {
                0x03, 0x00, // FormatTag = 3
                0x02, 0x00, // Channels = 2
                0x44, 0xac, 0x00, 0x00, // SamplesPerSec = 44100
                0x20, 0x62, 0x05, 0x00, // AvgBytesPerSec = 352800
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormat.CreateIeeeFloat(44100, 2);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_Create16kHz1ChannelIeeeFloat()
        {
            // Define "native" WAVEFORMATEX structure for IEEE float PCM
            byte[] formatBytes = new byte[]
            {
                0x03, 0x00, // FormatTag = 3
                0x01, 0x00, // Channels = 1
                0x80, 0x3e, 0x00, 0x00, // SamplesPerSec = 16000
                0x00, 0xfa, 0x00, 0x00, // AvgBytesPerSec = 64800
                0x04, 0x00, // BlockAlign = 4
                0x20, 0x00, // BitsPerSample = 32
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormat.Create16kHz1ChannelIeeeFloat();

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_Create()
        {
            // Define "native" WAVEFORMATEX structure
            byte[] formatBytes = new byte[]
            {
                0x03, 0x00, // FormatTag = 3
                0x02, 0x00, // Channels = 2
                0x80, 0xbb, 0x00, 0x00, // SamplesPerSec = 48000
                0x00, 0xdc, 0x05, 0x00, // AvgBytesPerSec = 384000
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 384000);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_Create()
        {
            // Define "native" WAVEFORMATEX structure
            byte[] formatBytes = new byte[]
            {
                0xfe, 0xff, // FormatTag = WAVE_FORMAT_EXTENSIBLE (0xfeff)
                0x02, 0x00, // Channels = 2
                0x80, 0xbb, 0x00, 0x00, // SamplesPerSec = 48000
                0x00, 0xdc, 0x05, 0x00, // AvgBytesPerSec = 384000
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x16, 0x00, // ExtraSize = 22
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, // ExtraInfo
            };

            // Create equivalent managed WaveFormat object
            byte[] extraInfo = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_Equality()
        {
            // Create WaveFormat object
            WaveFormat format = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 384000);

            // Create an identical WaveFormat object and check for equality
            WaveFormat copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 384000);
            Assert.AreEqual(format, copy);

            // Make multiple copies each with a field modified and verify inequality
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_PCM, 48000, 32, 2, 8, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 16000, 32, 2, 8, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 16, 2, 8, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 1, 8, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 4, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 32000);
            Assert.AreNotEqual(format, copy);

            // Test null and reference equality
            copy = format;
            Assert.AreEqual(format, copy);
            copy = null;
            Assert.AreNotEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_Equality()
        {
            // Create WaveFormatEx object
            byte[] extraInfo = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Create an identical WaveFormat object and check for equality
            byte[] extraInfoCopy = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfoCopy);
            Assert.AreEqual(format, copy);

            // Make multiple copies each with a field modified and verify inequality
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 16000, 32, 2, 8, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 16, 2, 8, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 1, 8, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 4, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 32000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            extraInfoCopy = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);
            extraInfoCopy = new byte[] { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            copy = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfoCopy);
            Assert.AreNotEqual(format, copy);

            // Test null and reference equality
            copy = format;
            Assert.AreEqual(format, copy);
            copy = null;
            Assert.AreNotEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_CreateCopy()
        {
            // Create WaveFormat object
            WaveFormat format = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 384000);

            // Create a copy and check for equality
            WaveFormat copy = WaveFormat.Create(format);
            Assert.AreEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_CreateCopy()
        {
            // Create WaveFormatEx object
            byte[] extraInfo = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Create a copy and check for equality
            WaveFormat copy = WaveFormat.Create(format);
            Assert.IsTrue(copy is WaveFormatEx);
            Assert.AreEqual(format, copy);

            // Create WaveFormatEx object (with no extra bytes)
            format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000);

            // Create a copy and check for equality
            copy = WaveFormat.Create(format);
            Assert.IsTrue(copy is WaveFormatEx);
            Assert.AreEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormat_ReadWrite()
        {
            // Create WaveFormat object
            WaveFormat format = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, 48000, 32, 2, 8, 384000);

            // Write WaveFormat object out to a memory stream
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            format.WriteTo(writer);

            // Verify written bytes for equality
            byte[] streamBytes = stream.ToArray();
            this.MarshalAndVerify(format, streamBytes);

            // Read WaveFormat back from the stream and verify
            stream.Seek(0, SeekOrigin.Begin);
            WaveFormat copy = WaveFormat.FromStream(stream, Marshal.SizeOf(format));
            Assert.AreEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_ReadWrite()
        {
            // Create WaveFormat object
            byte[] extraInfo = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Write WaveFormat object out to a memory stream
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            format.WriteTo(writer);

            // Verify written bytes for equality
            byte[] streamBytes = stream.ToArray();
            this.MarshalAndVerify(format, streamBytes);

            // Read WaveFormat back from the stream and verify
            stream.Seek(0, SeekOrigin.Begin);
            WaveFormat copy = WaveFormat.FromStream(stream, Marshal.SizeOf<WaveFormat>() + extraInfo.Length);
            Assert.AreEqual(format, copy);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_SmallExtraInfo()
        {
            // Define "native" WAVEFORMATEX structure
            byte[] formatBytes = new byte[]
            {
                0xfe, 0xff, // FormatTag = WAVE_FORMAT_EXTENSIBLE (0xfeff)
                0x02, 0x00, // Channels = 2
                0x80, 0xbb, 0x00, 0x00, // SamplesPerSec = 48000
                0x00, 0xdc, 0x05, 0x00, // AvgBytesPerSec = 384000
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x01, 0x00, // ExtraSize = 1
                0x42, // ExtraInfo
            };

            // Create equivalent managed WaveFormat object
            byte[] extraInfo = new byte[1] { 0x42 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_ZeroExtraInfo()
        {
            // Define "native" WAVEFORMATEX structure
            byte[] formatBytes = new byte[]
            {
                0xfe, 0xff, // FormatTag = WAVE_FORMAT_EXTENSIBLE (0xfeff)
                0x02, 0x00, // Channels = 2
                0x80, 0xbb, 0x00, 0x00, // SamplesPerSec = 48000
                0x00, 0xdc, 0x05, 0x00, // AvgBytesPerSec = 384000
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            byte[] extraInfo = new byte[0];
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void WaveFormatEx_NullExtraInfo()
        {
            // Define "native" WAVEFORMATEX structure
            byte[] formatBytes = new byte[]
            {
                0xfe, 0xff, // FormatTag = WAVE_FORMAT_EXTENSIBLE (0xfeff)
                0x02, 0x00, // Channels = 2
                0x80, 0xbb, 0x00, 0x00, // SamplesPerSec = 48000
                0x00, 0xdc, 0x05, 0x00, // AvgBytesPerSec = 384000
                0x08, 0x00, // BlockAlign = 8
                0x20, 0x00, // BitsPerSample = 32
                0x00, 0x00, // ExtraSize = 0
            };

            // Create equivalent managed WaveFormat object
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, null);

            // Verify against expected
            this.MarshalAndVerify(format, formatBytes);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WaveFormat_MarshalIncorrectNonZeroExtraSize()
        {
            // Create WaveFormat object
            WaveFormat format = WaveFormat.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000);

            // Fiddle with the length to make it non-zero
            format.ExtraSize = 16;

            // Write WaveFormat object out to a memory stream (should throw expected exception)
            IntPtr formatPtr = Marshal.AllocHGlobal(WaveFormat.MarshalSizeOf(format));
            WaveFormat.MarshalToPtr(format, formatPtr);
            Marshal.FreeHGlobal(formatPtr);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WaveFormatEx_MarshalIncorrectExtraSize()
        {
            // Create WaveFormat object
            byte[] extraInfo = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, extraInfo);

            // Fiddle with the length to make it incorrect
            format.ExtraSize = (ushort)(extraInfo.Length + 1);

            // Write WaveFormat object out to a memory stream (should throw expected exception)
            IntPtr formatPtr = Marshal.AllocHGlobal(WaveFormat.MarshalSizeOf(format));
            WaveFormat.MarshalToPtr(format, formatPtr);
            Marshal.FreeHGlobal(formatPtr);
        }

        [TestMethod]
        [Timeout(60000)]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WaveFormatEx_MarshalIncorrectNullExtraInfo()
        {
            // Create WaveFormat object
            WaveFormat format = WaveFormatEx.Create(WaveFormatTag.WAVE_FORMAT_EXTENSIBLE, 48000, 32, 2, 8, 384000, null);

            // Fiddle with the length to make it non-zero
            format.ExtraSize = 16;

            // Write WaveFormat object out to a memory stream (should throw expected exception)
            IntPtr formatPtr = Marshal.AllocHGlobal(WaveFormat.MarshalSizeOf(format));
            WaveFormat.MarshalToPtr(format, formatPtr);
            Marshal.FreeHGlobal(formatPtr);
        }

        private void MarshalAndVerify(WaveFormat format, byte[] expectedBytes)
        {
            // Get unmanaged size of format object and verify it
            int marshaledSize = WaveFormat.MarshalSizeOf(format);
            Assert.AreEqual(Marshal.SizeOf<WaveFormat>() + format.ExtraSize, marshaledSize);

            // Marshal managed WaveFormat to native and verify the bytes by copying
            // them back and comparing with our pre-defined "native" structure. Add
            // a canary at the end of the allocated memory to check for buffer overruns.
            IntPtr formatPtr = Marshal.AllocHGlobal(marshaledSize + sizeof(int));
            unchecked
            {
                Marshal.WriteInt32(formatPtr, marshaledSize, (int)0xDEADBEEF);
            }

            WaveFormat.MarshalToPtr(format, formatPtr);

            // Verify no buffer overruns.
            unchecked
            {
                Assert.AreEqual((int)0xDEADBEEF, Marshal.ReadInt32(formatPtr, marshaledSize));
            }

            // Compare marshaled bytes with expected.
            byte[] marshaledBytes = new byte[marshaledSize];
            Marshal.Copy(formatPtr, marshaledBytes, 0, marshaledSize);
            CollectionAssert.AreEqual(expectedBytes, marshaledBytes);

            // Marshal back to managed and verify object is equal to the original
            WaveFormat roundTripCopy = WaveFormat.MarshalFromPtr(formatPtr);
            Assert.AreEqual(format, roundTripCopy);

            // Free marshaled object pointer
            Marshal.FreeHGlobal(formatPtr);
        }
    }
}
