// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public unsafe class MemoryAccessTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void BufferEx_ReadWrite()
        {
            var bytes = new byte[256];

            Assert.AreEqual(BufferEx.SizeOf<int>(), sizeof(int));
            Assert.AreEqual(BufferEx.SizeOf<RGB>(), sizeof(RGB));

            uint value = 0x8A00AF01;
            RGB rgb = new RGB { R = 16, B = 32, G = 64 };
            BufferEx.Write(value, bytes, 0);
            BufferEx.Write(rgb, bytes, 4);
            var newValue = BufferEx.Read<uint>(bytes, 0);
            var newRGB = BufferEx.Read<RGB>(bytes, 4);
            Assert.AreEqual(value, newValue);
            Assert.AreEqual(rgb, newRGB);
        }

        [TestMethod]
        [Timeout(60000)]
        public void BufferEx_BulkCopy()
        {
            var bytes = new byte[256];
            var rgbs = Enumerable.Range(0, 16).Select(i => new RGB { R = i, B = i * 10, G = i * 100 }).ToArray();

            BufferEx.Copy(rgbs, 0, bytes, 0, rgbs.Length);
            var rgbs2 = new RGB[rgbs.Length];

            BufferEx.Copy(bytes, 0, rgbs2, 0, rgbs.Length);

            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(rgbs[i], rgbs2[i]);
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnmanagedArray_BasicAccess()
        {
            int size = 256;
            var testArray = new UnmanagedArray<int>(size, true);
            var baselineArray = Enumerable.Range(0, size).ToArray();

            // test creation
            Assert.AreEqual(0, testArray.Sum());

            // test copy
            testArray.Copy(baselineArray);
            Assert.AreEqual(size * (size - 1) / 2, testArray.Sum());
            Assert.IsTrue(this.AreIdentical(testArray, baselineArray));

            // test set one value
            int index = 11;
            testArray[index] = 1024;
            Assert.AreEqual(1024, testArray[index]);
            Assert.IsTrue(this.AreIdentical(testArray.Skip(index + 1), baselineArray.Skip(index + 1)));
            Assert.IsFalse(this.AreIdentical(testArray, baselineArray));

            // test get one value
            baselineArray[index] = testArray[index];
            Assert.IsTrue(this.AreIdentical(testArray, baselineArray));

            // test clear
            testArray.Clear();
            Assert.AreEqual(0, testArray.Sum());
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnmanagedArray_Resize()
        {
            int size = 256;
            var testArray = new UnmanagedArray<int>(size, true);
            var baselineArray = Enumerable.Range(0, size).ToArray();
            testArray.Copy(baselineArray);
            Assert.IsTrue(this.AreIdentical(testArray, baselineArray));
            Assert.AreEqual(baselineArray.Sum(), testArray.Sum());

            testArray.Resize(size + 10, true);
            Assert.AreEqual(baselineArray.Sum(), testArray.Sum());
            Assert.IsTrue(this.AreIdentical(testArray.Take(size), baselineArray));
        }

        [TestMethod]
        [Timeout(60000)]
        public void UnmanagedArray_GetByRef()
        {
            var array = new UnmanagedArray<uint>(256);
            ref uint u = ref array.GetRef(0);
            u = 0x8A00AF01;
            Assert.AreEqual(0x8A00AF01, array[0]);
        }

        private bool AreIdentical<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            return first.Zip(second, (i, j) => i.Equals(j)).All(b => b);
        }

        private struct RGB
        {
            public int R;
            public int G;
            public int B;
        }
    }
}