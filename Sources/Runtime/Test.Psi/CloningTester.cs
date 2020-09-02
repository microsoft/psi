// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CloningTester
    {
        // Note that these tests generally use an explicit SerializationContext rather than the
        // default static context to ensure that the tests (which may run in the same process)
        // start with a clean context and do not encounter leftover state from a previous test.

        [TestMethod]
        [Timeout(60000)]
        public void RegisterFlags()
        {
            var serializers = new KnownSerializers();

            // register different flags for multiple types
            serializers.Register<ClassWithIntPtr>(CloningFlags.CloneIntPtrFields);
            serializers.Register<ClassWithPointer>(CloningFlags.ClonePointerFields);
            serializers.Register<ClassWithNonSerialized>(CloningFlags.SkipNonSerializedFields);
            serializers.Register<StructWithAll>(
                CloningFlags.ClonePointerFields | CloningFlags.CloneIntPtrFields | CloningFlags.SkipNonSerializedFields);

            Assert.AreEqual(CloningFlags.CloneIntPtrFields, serializers.GetCloningFlags(typeof(ClassWithIntPtr)));
            Assert.AreEqual(CloningFlags.ClonePointerFields, serializers.GetCloningFlags(typeof(ClassWithPointer)));
            Assert.AreEqual(CloningFlags.SkipNonSerializedFields, serializers.GetCloningFlags(typeof(ClassWithNonSerialized)));
            Assert.AreEqual(
                CloningFlags.ClonePointerFields | CloningFlags.CloneIntPtrFields | CloningFlags.SkipNonSerializedFields,
                serializers.GetCloningFlags(typeof(StructWithAll)));

            serializers = new KnownSerializers(); // new context

            // registering with no flags is equivalent to None
            serializers.Register<ClassWithPointer>();
            serializers.Register<ClassWithPointer>(CloningFlags.None);
            Assert.AreEqual(CloningFlags.None, serializers.GetCloningFlags(typeof(ClassWithPointer)));

            serializers = new KnownSerializers(); // new context

            // cannot re-register with different flags
            serializers.Register<ClassWithPointer>(CloningFlags.ClonePointerFields);
            Assert.ThrowsException<SerializationException>(
                () => serializers.Register<ClassWithPointer>(CloningFlags.SkipNonSerializedFields));
            Assert.AreEqual(CloningFlags.ClonePointerFields, serializers.GetCloningFlags(typeof(ClassWithPointer)));

            serializers = new KnownSerializers(); // new context

            // once a handler has been created, cannot register flags
            var handler = serializers.GetHandler<ClassWithPointer>();
            Assert.ThrowsException<SerializationException>(
                () => serializers.Register<ClassWithPointer>(CloningFlags.ClonePointerFields));
        }

        [TestMethod]
        [Timeout(60000)]
        public void CloneDelegateSimple()
        {
            Func<int, int> fn = (int x) => x + 1;

            try
            {
                var fn2 = default(Func<int, int>);
                Serializer.Clone(fn, ref fn2, new SerializationContext());
                Assert.Fail("Should have thrown while attempting to clone Func");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone Func"));
            }

            // register to allow IntPtr cloning in Func<int, int> and repeat - should now succeed
            var serializers = new KnownSerializers();
            serializers.Register<Func<int, int>>(CloningFlags.CloneIntPtrFields);
            var fn3 = default(Func<int, int>);
            Serializer.Clone(fn, ref fn3, new SerializationContext(serializers));
            var res = fn3(10);
            Assert.AreEqual(11, res);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CloneDelegateClosure()
        {
            var v = new int[] { 1, 2, 3 };

            Func<int, int[]> fn = (int x) => v.Select(a => a + x).ToArray();

            try
            {
                var fn2 = default(Func<int, int[]>);
                Serializer.Clone(fn, ref fn2, new SerializationContext());
                Assert.Fail("Should have thrown while attempting to clone Func");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone Func"));
            }

            // register to allow IntPtr cloning in Func<int, int> and repeat - should now succeed
            var serializers = new KnownSerializers();
            serializers.Register<Func<int, int[]>>(CloningFlags.CloneIntPtrFields);
            var fn3 = default(Func<int, int[]>);
            Serializer.Clone(fn, ref fn3, new SerializationContext(serializers));
            var res = fn3(10);
            Assert.AreEqual(11, res[0]);
            Assert.AreEqual(12, res[1]);
            Assert.AreEqual(13, res[2]);
        }

        [TestMethod]
        [Timeout(60000)]
        public void CloneNonSerializedField()
        {
            var cls = new ClassWithNonSerialized { Value = 1, Secret = 0x12345678 };

            var cls2 = default(ClassWithNonSerialized);
            Serializer.Clone(cls, ref cls2, new SerializationContext());
            Assert.AreEqual(1, cls2.Value);
            Assert.AreEqual(0x12345678, cls2.Secret);

            // register to disable cloning of NonSerialized fields - field should be skipped
            var serializers = new KnownSerializers();
            serializers.Register<ClassWithNonSerialized>(CloningFlags.SkipNonSerializedFields);
            var cls3 = default(ClassWithNonSerialized);
            Serializer.Clone(cls, ref cls3, new SerializationContext(serializers));
            Assert.AreEqual(1, cls3.Value);
            Assert.AreEqual(default, cls3.Secret); // skipped NonSerialized field
        }

        [TestMethod]
        [Timeout(60000)]
        public unsafe void ClonePointerField()
        {
            int v1 = 456;
            byte v2 = 123;
            var cls = new ClassWithPointer { Pointer1 = &v1, Pointer2 = &v2 };

            try
            {
                var cls2 = default(ClassWithPointer);
                Serializer.Clone(cls, ref cls2, new SerializationContext());
                Assert.Fail("Should have thrown while attempting to clone class containing pointer fields");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone field:Pointer"));
            }

            // register to allow cloning of pointer fields - should now succeed
            var serializers = new KnownSerializers();
            serializers.Register<ClassWithPointer>(CloningFlags.ClonePointerFields);
            var cls3 = default(ClassWithPointer);
            Serializer.Clone(cls, ref cls3, new SerializationContext(serializers));
            Assert.AreEqual(456, *cls3.Pointer1);
            Assert.AreEqual(123, *cls3.Pointer2);

            // verify that cloned pointers point to the locations as original
            *cls.Pointer1 = 543;
            *cls.Pointer2 = 210;
            Assert.AreEqual(543, *cls3.Pointer1);
            Assert.AreEqual(210, *cls3.Pointer2);
        }

        [TestMethod]
        [Timeout(60000)]
        public unsafe void CloneIntPtrField()
        {
            int v1 = 456;
            byte v2 = 123;
            var cls = new ClassWithIntPtr { IntPtr1 = new UIntPtr(&v1), IntPtr2 = new IntPtr(&v2) };

            try
            {
                var cls2 = default(ClassWithIntPtr);
                Serializer.Clone(cls, ref cls2, new SerializationContext());
                Assert.Fail("Should have thrown while attempting to clone class containing IntPtr fields");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone field:IntPtr"));
            }

            // register to allow cloning of IntPtr fields - should now succeed
            var serializers = new KnownSerializers();
            serializers.Register<ClassWithIntPtr>(CloningFlags.CloneIntPtrFields);
            var cls3 = default(ClassWithIntPtr);
            Serializer.Clone(cls, ref cls3, new SerializationContext(serializers));
            Assert.AreEqual(456, *(int*)cls3.IntPtr1);
            Assert.AreEqual(123, *(byte*)cls3.IntPtr2);

            // verify that cloned pointers point to the locations as original
            *(int*)cls.IntPtr1 = 543;
            *(byte*)cls.IntPtr2 = 210;
            Assert.AreEqual(543, *(int*)cls3.IntPtr1);
            Assert.AreEqual(210, *(byte*)cls3.IntPtr2);
        }

        [TestMethod]
        [Timeout(60000)]
        public unsafe void CloneAllFlaggedFields()
        {
            int v1 = 123;
            int v2 = 456;
            var str = new StructWithAll { Value = 11, Secret = "password", IntPtr = new IntPtr(&v1), Pointer = &v2 };

            // set all flags - NonSerialized fields are skipped by default
            var serializers = new KnownSerializers();
            CloningFlags cloningFlags = ~CloningFlags.None;
            serializers.Register<StructWithAll>(cloningFlags);
            var str2 = default(StructWithAll);
            Serializer.Clone(str, ref str2, new SerializationContext(serializers));
            Assert.AreEqual(11, str2.Value);
            Assert.AreEqual(default, str2.Secret); // skipped NonSerialized field
            Assert.AreEqual(default, str2.IntPtr); // skipped NonSerialized field
            Assert.AreEqual(456, *str2.Pointer);

            // clear flag that skips NonSerialized fields - all fields should now be cloned
            cloningFlags &= ~CloningFlags.SkipNonSerializedFields;
            serializers = new KnownSerializers();
            serializers.Register<StructWithAll>(cloningFlags);
            var str3 = default(StructWithAll);
            Serializer.Clone(str, ref str3, new SerializationContext(serializers));
            Assert.AreEqual(11, str3.Value);
            Assert.AreEqual("password", str3.Secret);
            Assert.AreEqual(123, *(int*)str3.IntPtr);
            Assert.AreEqual(456, *str3.Pointer);

            // clear flag that allows cloning of IntPtr fields - should now throw
            cloningFlags &= ~CloningFlags.CloneIntPtrFields;
            serializers = new KnownSerializers();
            serializers.Register<StructWithAll>(cloningFlags);
            try
            {
                var str4 = default(StructWithAll);
                Serializer.Clone(str, ref str4, new SerializationContext(serializers));
                Assert.Fail("Should have thrown while attempting to clone IntPtr field");
            }
            catch (NotSupportedException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Cannot clone field:IntPtr"));
            }
        }

        private class ClassWithNonSerialized
        {
            public int Value;

            [NonSerialized]
            public int Secret;
        }

        private class ClassWithIntPtr
        {
            public UIntPtr IntPtr1;
            public IntPtr IntPtr2;
        }

        private unsafe class ClassWithPointer
        {
            public int* Pointer1;
            public byte* Pointer2;
        }

        private unsafe struct StructWithAll
        {
            public int Value;

            [NonSerialized]
            public string Secret;

            public int* Pointer;

            // testing NonSerialized field that is also non-clonable by default
            [NonSerialized]
            public IntPtr IntPtr;
        }
    }
}
