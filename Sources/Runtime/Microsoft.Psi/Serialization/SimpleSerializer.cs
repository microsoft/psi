// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using Microsoft.Psi.Common;

    /// <summary>
    /// Default class for custom serializers of primitive types.
    /// </summary>
    /// <typeparam name="T">A primitive type (pure value type).</typeparam>
    internal sealed class SimpleSerializer<T> : ISerializer<T>
    {
        private const int Version = 0;
        private SerializeDelegate<T> serializeImpl;
        private DeserializeDelegate<T> deserializeImpl;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            var schema = TypeSchema.FromType(typeof(T), serializers.RuntimeVersion, this.GetType(), Version);
            this.serializeImpl = Generator.GenerateSerializeMethod<T>(il => Generator.EmitPrimitiveSerialize(typeof(T), il));
            this.deserializeImpl = Generator.GenerateDeserializeMethod<T>(il => Generator.EmitPrimitiveDeserialize(typeof(T), il));
            return targetSchema ?? schema;
        }

        public void Serialize(BufferWriter writer, T instance, SerializationContext context)
        {
            this.serializeImpl(writer, instance, context);
        }

        public void Deserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            this.deserializeImpl(reader, ref target, context);
        }

        public void Clone(T instance, ref T target, SerializationContext context)
        {
            // uncomment to verify inlining in release builds
            // Generator.DumpStack();
            target = instance;
        }

        public void Clear(ref T target, SerializationContext context)
        {
        }

        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
        {
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
        }
    }
}
