// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Auto-generated serializer for immutable types (both reference and value type).
    /// </summary>
    /// <typeparam name="T">The type of objects this serializer knows how to handle.</typeparam>
    internal class ImmutableSerializer<T> : ISerializer<T>
    {
        private SerializeDelegate<T> serializeImpl;
        private DeserializeDelegate<T> deserializeImpl;

        public ImmutableSerializer()
        {
        }

        /// <inheritdoc />
        public bool? IsClearRequired => false;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            var runtimeSchema = TypeSchema.FromType(typeof(T), this.GetType().AssemblyQualifiedName, serializers.RuntimeInfo.SerializationSystemVersion);
            var members = runtimeSchema.GetCompatibleMemberSet(targetSchema);

            this.serializeImpl = Generator.GenerateSerializeMethod<T>(il => Generator.EmitSerializeFields(typeof(T), serializers, il, members));
            this.deserializeImpl = Generator.GenerateDeserializeMethod<T>(il => Generator.EmitDeserializeFields(typeof(T), serializers, il, members));

            return targetSchema ?? runtimeSchema;
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
            target = instance;
        }

        public void Clear(ref T target, SerializationContext context)
        {
            // nothing to clear
        }

        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
        {
            // we won't be cloning anything, but we want the object graph in SerializationContext to remember the correct instance.
            target = instance;
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
            // always allocate a new target
            target = default(T);
            if (target == null)
            {
                target = (T)FormatterServices.GetUninitializedObject(typeof(T));
            }
        }
    }
}
