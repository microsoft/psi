// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CustomSerializationTester
    {
        [TestMethod]
        [Timeout(2000)]
        public void PolymorphicSerializer()
        {
            // verify that the default serializer works for polymorphic types
            var c = new TypeWithPolymorphicField();
            c.Name = "foo";
            c.Enumerable = new List<int>();
            BufferWriter bw = new BufferWriter(100);
            Serializer.Serialize(bw, c, new SerializationContext());

            TypeWithPolymorphicField c2 = null;
            var br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref c2, new SerializationContext());
            Assert.AreEqual(c.Name, c2.Name);
        }

        [TestMethod]
        [Timeout(2000)]
        public void CustomSerializer()
        {
            var c = new TypeWithPolymorphicField();
            c.Name = "foo";
            c.Enumerable = new List<int>();
            BufferWriter bw = new BufferWriter(100);
            var ks = new KnownSerializers();
            ks.Register<TypeWithPolymorphicField, TestCustomSerializer>("some alternate name"); // include an alternate name just to exercise that code path
            var sc = new SerializationContext(ks);

            Serializer.Serialize(bw, c, sc);
            sc.Reset();

            TypeWithPolymorphicField c2 = null;
            var br = new BufferReader(bw.Buffer);
            Serializer.Deserialize(br, ref c2, sc);
            Assert.AreEqual(c.Name, c2.Name);
        }

        public class TypeWithPolymorphicField
        {
            public IEnumerable<int> Enumerable;
            public string Name;
        }

        // serializer that skips one property
        public class TestCustomSerializer : ISerializer<TypeWithPolymorphicField>
        {
            public int Version => throw new NotSupportedException();

            public bool? IsClearRequired => false;

            public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
            {
                return null;
            }

            public void Clear(ref TypeWithPolymorphicField target, SerializationContext context)
            {
            }

            public void Clone(TypeWithPolymorphicField instance, ref TypeWithPolymorphicField target, SerializationContext context)
            {
                target.Name = instance.Name;
            }

            public void Deserialize(BufferReader reader, ref TypeWithPolymorphicField target, SerializationContext context)
            {
                target.Name = reader.ReadString();
            }

            public void PrepareCloningTarget(TypeWithPolymorphicField instance, ref TypeWithPolymorphicField target, SerializationContext context)
            {
                target = target ?? new TypeWithPolymorphicField();
            }

            public void PrepareDeserializationTarget(BufferReader reader, ref TypeWithPolymorphicField target, SerializationContext context)
            {
                target = target ?? new TypeWithPolymorphicField();
            }

            public void Serialize(BufferWriter writer, TypeWithPolymorphicField instance, SerializationContext context)
            {
                writer.Write(instance.Name);
            }
        }
    }
}
