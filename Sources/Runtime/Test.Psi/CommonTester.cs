// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CommonTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void TimestampedTest()
        {
            Envelope e1 = new Envelope() { OriginatingTime = Time.GetCurrentTime(), SequenceId = 15, SourceId = 23, CreationTime = Time.GetCurrentTime() };
            Envelope e2 = new Envelope() { OriginatingTime = Time.GetCurrentTime(), SequenceId = 10, SourceId = 13, CreationTime = Time.GetCurrentTime() };
            Assert.IsTrue(new Message<int>(5, e1) == new Message<int>(5, e1));
            Assert.IsTrue(new Message<int>(5, e1) != new Message<int>(5, e2));
            Assert.IsTrue(new Message<int>(5, e1) != new Message<int>(6, e1));
            Assert.IsTrue(new Message<object>(null, e1) == new Message<object>(null, e1));
            Assert.IsTrue(new Message<string>("test", e1) == new Message<string>("test", e1));
            Assert.IsTrue(new Message<string>("test", e1) != new Message<string>("test", e2));
            Assert.IsTrue(new Message<string>("test", e1) != new Message<string>("test1", e1));
            Assert.IsTrue(new Message<string>("test", e1) != new Message<string>(null, e1));
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferTest()
        {
            BufferWriter bufW = new BufferWriter(2); // force reallocations
            byte b = 5;
            byte[] br = new byte[] { 1, 2, 3 };
            char c = 'A';
            char[] cr = new char[] { 'A', 'B', 'C' };
            string str = "a string";
            double d = 1.1;
            double[] dr = new double[] { 1.1, 2.2, 3.3 };
            float f = 0.1f;
            float[] fr = new float[] { 0.1f, 0.01f, 0.001f };
            int i = 0x0FF00FFF;
            int[] ir = new[] { 0x0FF00FF0, 0x0FF00FF1, 0x0FF00FF2 };
            short s = 0x0FFF;
            short[] sr = new short[] { 0x0FF0, 0x0FF1, 0x0FF2 };
            long l = 0x00FFFF0000EEEE0F;
            long[] lr = new long[] { 0x00FFFF0000EEEE00, 0x00FFFF0000EEEE01, 0x00FFFF0000EEEE02 };
            ushort us = 0xFFFF;
            uint ui = 0xFFFFFFFF;
            ulong ul = 0xFFFFFFFFFFFFFFFF;
            DateTime dt = DateTime.UtcNow;
            var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            bufW.Write(b);
            bufW.Write(br);
            bufW.Write(c);
            bufW.Write(cr);
            bufW.Write(str);
            bufW.Write(d);
            bufW.Write(dr);
            bufW.Write(f);
            bufW.Write(fr);
            bufW.Write(i);
            bufW.Write(ir);
            bufW.Write(s);
            bufW.Write(sr);
            bufW.Write(l);
            bufW.Write(lr);
            bufW.Write(us);
            bufW.Write(ui);
            bufW.Write(ul);
            bufW.Write(dt);
            bufW.CopyFromStream(stream, 2);
            bufW.CopyFromStream(stream, 3);
            int position = bufW.Position;
            bufW.CopyFromStream(stream, 2); // attempt to read past end of stream
            Assert.AreEqual(position, bufW.Position);

            BufferReader bufR = new BufferReader(bufW.Buffer);
            Assert.AreEqual(b, bufR.ReadByte());
            var br_r = new byte[br.Length];
            bufR.Read(br_r, br.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(br, br_r));
            Assert.AreEqual(c, bufR.ReadChar());
            var cr_r = new char[cr.Length];
            bufR.Read(cr_r, cr.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(cr, cr_r));
            Assert.AreEqual(str, bufR.ReadString());
            Assert.AreEqual(d, bufR.ReadDouble());
            var dr_r = new double[dr.Length];
            bufR.Read(dr_r, dr.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(dr, dr_r));
            Assert.AreEqual(f, bufR.ReadSingle());
            var fr_r = new float[fr.Length];
            bufR.Read(fr_r, fr.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(fr, fr_r));
            Assert.AreEqual(i, bufR.ReadInt32());
            var ir_r = new int[ir.Length];
            bufR.Read(ir_r, ir.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(ir, ir_r));
            Assert.AreEqual(s, bufR.ReadInt16());
            var sr_r = new short[sr.Length];
            bufR.Read(sr_r, sr.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(sr, sr_r));
            Assert.AreEqual(l, bufR.ReadInt64());
            var lr_r = new long[lr.Length];
            bufR.Read(lr_r, lr.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(lr, lr_r));
            Assert.AreEqual(us, bufR.ReadUInt16());
            Assert.AreEqual(ui, bufR.ReadUInt32());
            Assert.AreEqual(ul, bufR.ReadUInt64());
            Assert.AreEqual(dt, bufR.ReadDateTime());
            var stream_r = new MemoryStream();
            bufR.CopyToStream(stream_r, (int)stream.Length);
            CollectionAssert.AreEqual(stream.ToArray(), stream_r.ToArray());
        }
    }
}
