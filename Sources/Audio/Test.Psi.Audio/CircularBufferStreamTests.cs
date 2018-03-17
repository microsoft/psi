// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Audio;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CircularBufferStreamTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void CircularBufferStream_TestRead()
        {
            CircularBufferStream cbs = new CircularBufferStream(3);
            byte[] data = new byte[] { 0, 1, 2 };
            cbs.Write(data, 0, data.Length);
            Assert.AreEqual(3, cbs.BytesAvailable);
            CollectionAssert.AreEqual(data, cbs.Read());
            Assert.AreEqual(0, cbs.BytesAvailable);

            // Write more data than the buffer capacity
            data = new byte[] { 0, 1, 2, 3 };
            cbs.Write(data, 0, data.Length);

            // first element should be overwritten - buffer should contain { 1, 2, 3 }
            Assert.AreEqual(3, cbs.BytesAvailable);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, cbs.Read());

            Assert.AreEqual(0, cbs.BytesAvailable);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CircularBufferStream_TestReadBytes()
        {
            CircularBufferStream cbs = new CircularBufferStream(3);
            byte[] data = new byte[] { 0, 1, 2 };
            cbs.Write(data, 0, data.Length);

            byte[] outData = new byte[5] { 88, 88, 88, 88, 88 };
            Assert.AreEqual(3, cbs.Read(outData, 1, 3));
            CollectionAssert.AreEqual(new byte[] { 88, 0, 1, 2, 88 }, outData);
            Assert.AreEqual(0, cbs.BytesAvailable);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CircularBufferStream_TestReadPointer()
        {
            CircularBufferStream cbs = new CircularBufferStream(3);
            byte[] data = new byte[] { 0, 1, 2 };
            cbs.Write(data, 0, data.Length);

            byte[] outData = new byte[5] { 88, 88, 88, 88, 88 };
            IntPtr outDataPtr = Marshal.AllocHGlobal(outData.Length);
            Marshal.Copy(outData, 0, outDataPtr, outData.Length);
            Assert.AreEqual(3, cbs.Read(outDataPtr, outData.Length, 3));
            Marshal.Copy(outDataPtr, outData, 0, outData.Length);
            Marshal.FreeHGlobal(outDataPtr);
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 88, 88 }, outData);
            Assert.AreEqual(0, cbs.BytesAvailable);
        }
    }
}
