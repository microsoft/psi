// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class DynamicDeserializationTests
    {
        private string path = Path.Combine(Environment.CurrentDirectory, nameof(PersistenceTest));

        [TestInitialize]
        public void Setup()
        {
            Directory.CreateDirectory(this.path);
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestRunner.SafeDirectoryDelete(this.path, true);
        }

        [TestMethod]
        [Timeout(60000)]
        public void PrimitiveToDynamicTest()
        {
            // simple primitives
            Assert.AreEqual<int>(123, this.InstanceToDynamic(123));
            Assert.AreEqual<double>(Math.E, this.InstanceToDynamic(Math.E));
            Assert.AreEqual<bool>(true, this.InstanceToDynamic(true));
            Assert.AreEqual<char>('x', this.InstanceToDynamic('x'));
            Assert.AreEqual<string>("Hello", this.InstanceToDynamic("Hello"));

            // collections of primitives
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, this.InstanceToDynamic(new[] { 1, 2, 3 }));
        }

        public class TestObject
        {
            public int A = 123;
            public double B = Math.PI;
            public bool C = true;
            public char D = 'x';
            public string E = "Hello";
            public byte F = 7;
            public DateTime G = new DateTime(1971, 11, 3, 0, 0, 0, DateTimeKind.Utc);
            public short H = 42;
            public long I = 456;
            public sbyte J = -1;
            public float K = 2.71f;
            public ushort L = 41;
            public uint M = 122;
            public ulong N = 455;
        }

        public void AssertTestObject(dynamic dyn)
        {
            Assert.IsTrue(dyn is ExpandoObject);
            Assert.AreEqual<int>(123, dyn.A);
            Assert.AreEqual<double>(Math.PI, dyn.B);
            Assert.AreEqual<bool>(true, dyn.C);
            Assert.AreEqual<char>('x', dyn.D);
            Assert.AreEqual<string>("Hello", dyn.E);
            Assert.AreEqual<byte>(7, dyn.F);
            Assert.AreEqual<DateTime>(new DateTime(1971, 11, 3, 0, 0, 0, DateTimeKind.Utc), dyn.G);
            Assert.AreEqual<short>(42, dyn.H);
            Assert.AreEqual<long>(456, dyn.I);
            Assert.AreEqual<sbyte>(-1, dyn.J);
            Assert.AreEqual<float>(2.71f, dyn.K);
            Assert.AreEqual<ushort>(41, dyn.L);
            Assert.AreEqual<uint>(122, dyn.M);
            Assert.AreEqual<ulong>(455, dyn.N);
        }

        [TestMethod]
        [Timeout(60000)]
        public void ObjectToDynamicTest()
        {
            var obj = new TestObject();
            var dyn = this.InstanceToDynamic(obj);
            this.AssertTestObject(dyn);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CollectionWithDuplicatesToDynamicTest()
        {
            // serialization detects duplicates and flags, deserializer caches and returns existing instances
            var obj = new TestObject();
            var arr = new[] { obj, obj };
            var dyn = this.InstanceToDynamic(arr);
            Assert.IsTrue(dyn is object[]);
            Assert.AreEqual(2, dyn.Length);
            this.AssertTestObject(dyn[0]);
            this.AssertTestObject(dyn[1]);
            Assert.AreSame(dyn[0], dyn[1]); // literally the same object!
            dyn[0].Foo = "Bar";
            Assert.AreEqual("Bar", dyn[1].Foo); // spooky action at a distance (same object!)
        }

        [TestMethod]
        [Timeout(60000)]
        public void ComplexObjectToDynamicTest()
        {
            var obj = new ComplexTestObject();
            var dyn = this.InstanceToDynamic(obj);
            Assert.IsTrue(dyn is ExpandoObject);
            this.AssertTestObject(dyn.NestedA);
            this.AssertTestObject(dyn.NestedB);
            this.AssertTestObject(dyn.Self.NestedA);
            this.AssertTestObject(dyn.Self.Self.NestedA);
            this.AssertTestObject(dyn.Self.Self.Self.NestedA);
            this.AssertTestObject(dyn.Self.Self.Self.Self.NestedA); // I could go on...
            Assert.AreSame(dyn.NestedA, dyn.NestedB); // literally the same object!
            Assert.AreSame(dyn, dyn.Self); // also, literally the same object!
        }

        public class ComplexTestObject
        {
            public ComplexTestObject Self;
            public TestObject NestedA;
            public TestObject NestedB;

            public ComplexTestObject()
            {
                this.Self = this; // cyclic!
                this.NestedA = this.NestedB = new TestObject(); // duplicate!
            }
        }

        private dynamic InstanceToDynamic<T>(T instance)
        {
            // Rube Goldberg machine to convert instance to dynamic by
            // writing to a store (typed) and reading back as dynamic
            using (var p = Pipeline.Create())
            {
                var gen = Generators.Return(p, instance);
                var exporter = PsiStore.Create(p, "Test", this.path);
                exporter.Write(gen.Out, "Data", true);
                p.Run();
            }

            using (var p = Pipeline.Create())
            {
                var importer = PsiStore.Open(p, "Test", this.path);
                var data = importer.OpenDynamicStream("Data");
                var result = data.ToEnumerable();
                p.Run();
                return result.First();
            }
        }
    }
}