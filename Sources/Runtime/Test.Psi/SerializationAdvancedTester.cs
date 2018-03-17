// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SerializationAdvancedTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void SerializeDelegateSimple()
        {
            Func<int, int> fn = (int x) => x + 1;

            var fn2 = fn.DeepClone();

            var res = fn2(10);
            Assert.AreEqual(11, res);
        }

        [TestMethod]
        [Timeout(60000)]
        public void SerializeDelegateClosure()
        {
            var v = new int[] { 1, 2, 3 };

            Func<int, int[]> fn = (int x) => v.Select(a => a + x).ToArray();

            var fn2 = fn.DeepClone();

            var res = fn2(10);
            Assert.AreEqual(11, res[0]);
            Assert.AreEqual(12, res[1]);
            Assert.AreEqual(13, res[2]);
        }

        private struct AStruct
        {
#pragma warning disable 0649 // Field 'SerializationAdvancedTester.AStruct.Value' is never assigned to, and will always have its default value 0
            public long Value;
#pragma warning restore 0649

            public double Data { get; set; }
        }

        private struct AClass
        {
#pragma warning disable 0649 // Field 'SerializationAdvancedTester.AClass.Value' is never assigned to, and will always have its default value 0
            public long Value;
#pragma warning restore 0649

            public double Data { get; set; }
        }
    }
}
