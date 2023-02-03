// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using Microsoft.Psi.Common;

    /// <summary>
    /// Auto-generated serializer for complex value types (that is, structs having one or more non-primitive fields).
    /// Implementers of ISerializer should instantiate and call this class to do the heavy lifting.
    /// </summary>
    /// <typeparam name="T">The value type this serializer knows how to handle.</typeparam>
    internal sealed class StructSerializer<T> : ISerializer<T>
    {
        // we use delegates (instead of generating a full class) because dynamic delegates (unlike dynamic types)
        // can access the private fields of the target type.
        private SerializeDelegate<T> serializeImpl;
        private DeserializeDelegate<T> deserializeImpl;
        private CloneDelegate<T> cloneImpl;
        private ClearDelegate<T> clearImpl;

        /// <inheritdoc />
        public bool? IsClearRequired => null; // depends on the generated implementation

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            var runtimeSchema = TypeSchema.FromType(typeof(T), this.GetType().AssemblyQualifiedName, serializers.RuntimeInfo.SerializationSystemVersion);
            var members = runtimeSchema.GetCompatibleMemberSet(targetSchema);

            this.serializeImpl = Generator.GenerateSerializeMethod<T>(il => Generator.EmitSerializeFields(typeof(T), serializers, il, members));
            this.deserializeImpl = Generator.GenerateDeserializeMethod<T>(il => Generator.EmitDeserializeFields(typeof(T), serializers, il, members));
            this.cloneImpl = Generator.GenerateCloneMethod<T>(il => Generator.EmitCloneFields(typeof(T), serializers, il));
            this.clearImpl = Generator.GenerateClearMethod<T>(il => Generator.EmitClearFields(typeof(T), serializers, il));

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
            this.cloneImpl(instance, ref target, context);
        }

        public void Clear(ref T target, SerializationContext context)
        {
            this.clearImpl(ref target, context);
        }

        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
        {
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
        }
    }
}
