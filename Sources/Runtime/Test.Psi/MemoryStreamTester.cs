// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MemoryStreamTester
    {
        private byte[] testValues = new byte[] { 52, 0, 127, 253, 7, 14, 98, 219, 9, 77, 184 };

        [TestMethod]
        [Timeout(60000)]
        public void MemoryStream_Expandable()
        {
            // write the values to an expandable memory stream
            var memoryStream = new MemoryStream();
            memoryStream.Write(this.testValues, 0, this.testValues.Length);

            this.TestMemoryStreamSerialization(memoryStream);
            this.TestMemoryStreamCloning(memoryStream);
        }

        [TestMethod]
        [Timeout(60000)]
        public void MemoryStream_NonResizeable()
        {
            // create a non-resizeable MemoryStream
            var memoryStream = new MemoryStream(this.testValues);

            this.TestMemoryStreamSerialization(memoryStream);
            this.TestMemoryStreamCloning(memoryStream);
        }

        [TestMethod]
        [Timeout(60000)]
        public void MemoryStream_NonWriteable()
        {
            // create a non-writeable MemoryStream
            var memoryStream = new MemoryStream(this.testValues, false);

            this.TestMemoryStreamSerialization(memoryStream);
            this.TestMemoryStreamCloning(memoryStream);
        }

        [TestMethod]
        [Timeout(60000)]
        public void MemoryStream_FromRegion()
        {
            // create a non-resizable MemoryStream on a specified region of the array
            var memoryStream = new MemoryStream(this.testValues, 2, 8);

            this.TestMemoryStreamSerialization(memoryStream);
            this.TestMemoryStreamCloning(memoryStream);
        }

        private void TestMemoryStreamSerialization(MemoryStream source)
        {
            var expectedValues = source.ToArray();

            // serialize the source
            var bw = new BufferWriter(0);
            Serializer.Serialize(bw, source, new SerializationContext());

            // verify serialized length matches the source data length (+ refHeader + dataLength)
            Assert.AreEqual(source.Length + 8, bw.Position);

            // deserialize into default target
            var br = new BufferReader(bw.Buffer);
            var target = default(MemoryStream);
            Serializer.Deserialize(br, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);
            Assert.AreEqual(source.Length, target.Capacity);

            // deserialize into target with expandable capacity
            br = new BufferReader(bw.Buffer);
            target = new MemoryStream();
            Serializer.Deserialize(br, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);

            // deserialize into target with existing data
            br = new BufferReader(bw.Buffer);
            target = new MemoryStream();
            target.Write(new byte[] { 1, 2, 3, 4, 5 });
            Serializer.Deserialize(br, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);

            // deserialize into target with fixed capacity >> length
            br = new BufferReader(bw.Buffer);
            target = new MemoryStream(new byte[1000]);
            Serializer.Deserialize(br, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);
            Assert.AreEqual(1000, target.Capacity); // original capacity preserved

            // deserialize into target with fixed and insufficient capacity
            br = new BufferReader(bw.Buffer);
            target = new MemoryStream(new byte[1]);
            try
            {
                Serializer.Deserialize(br, ref target, new SerializationContext());
                Assert.Fail("Expected exception was not thrown");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(NotSupportedException));
            }

            // deserialize into read-only target
            br = new BufferReader(bw.Buffer);
            target = new MemoryStream(new byte[source.Length], writable: false);
            try
            {
                Serializer.Deserialize(br, ref target, new SerializationContext());
                Assert.Fail("Expected exception was not thrown");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(NotSupportedException));
            }
        }

        private void TestMemoryStreamCloning(MemoryStream source)
        {
            var expectedValues = source.ToArray();

            // clone into default target
            var target = default(MemoryStream);
            Serializer.Clone(source, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);
            Assert.AreEqual(source.Length, target.Capacity);

            // clone into target with expandable capacity
            target = new MemoryStream();
            Serializer.Clone(source, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);

            // clone into target with existing data
            target = new MemoryStream();
            target.Write(new byte[] { 1, 2, 3, 4, 5 });
            Serializer.Clone(source, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);

            // clone into target with fixed capacity >> length
            target = new MemoryStream(new byte[1000]);
            Serializer.Clone(source, ref target, new SerializationContext());
            CollectionAssert.AreEqual(expectedValues, target.ToArray());
            Assert.AreEqual(source.Length, target.Length);
            Assert.AreEqual(1000, target.Capacity); // original capacity preserved

            // clone into target with fixed and insufficient capacity
            target = new MemoryStream(new byte[1]);
            try
            {
                Serializer.Clone(source, ref target, new SerializationContext());
                Assert.Fail("Expected exception was not thrown");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(NotSupportedException));
            }

            // clone into read-only target
            target = new MemoryStream(new byte[source.Length], writable: false);
            try
            {
                Serializer.Clone(source, ref target, new SerializationContext());
                Assert.Fail("Expected exception was not thrown");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(NotSupportedException));
            }
        }
    }
}