// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Auto-generated serializer for reference types.
    /// Implementers of ISerializer should instantiate and call this class to do the heavy lifting.
    /// </summary>
    /// <typeparam name="T">The type of objects this serializer knows how to handle.</typeparam>
    internal class ClassSerializer<T> : ISerializer<T>
    {
        // we use delegates (instead of generating a full class) because dynamic delegates (unlike dynamic types)
        // can access the private fields of the target type.
        private SerializeDelegate<T> serializeImpl;
        private DeserializeDelegate<T> deserializeImpl;
        private CloneDelegate<T> cloneImpl;
        private ClearDelegate<T> clearImpl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassSerializer{T}"/> class.
        /// The serializer will handle all public and private fields (including property-backing fields and read-only fields)
        /// of the underlying type.
        /// </summary>
        public ClassSerializer()
        {
        }

        /// <inheritdoc />
        public bool? IsClearRequired => null; // depends on the generated implementation

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            var runtimeSchema = TypeSchema.FromType(typeof(T), this.GetType().AssemblyQualifiedName, serializers.RuntimeInfo.SerializationSystemVersion);
            var members = runtimeSchema.GetCompatibleMemberSet(targetSchema);

            this.deserializeImpl = Generator.GenerateDeserializeMethod<T>(il => Generator.EmitDeserializeFields(typeof(T), serializers, il, members));
            this.serializeImpl = Generator.GenerateSerializeMethod<T>(il => Generator.EmitSerializeFields(typeof(T), serializers, il, members));
            this.cloneImpl = Generator.GenerateCloneMethod<T>(il => Generator.EmitCloneFields(typeof(T), serializers, il));
            this.clearImpl = Generator.GenerateClearMethod<T>(il => Generator.EmitClearFields(typeof(T), serializers, il));

            return targetSchema ?? runtimeSchema;
        }

        /// <summary>
        /// Serializes the given instance to the specified stream.
        /// </summary>
        /// <param name="writer">The stream writer to serialize to.</param>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(BufferWriter writer, T instance, SerializationContext context)
        {
            try
            {
                this.serializeImpl(writer, instance, context);
            }
            catch (NotSupportedException)
            {
                if (instance.GetType().BaseType == typeof(MulticastDelegate))
                {
                    throw new NotSupportedException("Cannot serialize Func/Action/Delegate. A common cause is serializing streams of IEnumerables holding closure references. A solution is to reify with `.ToList()` or similar.");
                }

                throw;
            }
        }

        /// <summary>
        /// Deserializes an instance from the specified stream into the specified target object.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An instance to deserialize into.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            this.deserializeImpl(reader, ref target, context);
        }

        /// <summary>
        /// Deep-clones the given object into an existing allocation.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clone(T instance, ref T target, SerializationContext context)
        {
            try
            {
                this.cloneImpl(instance, ref target, context);
            }
            catch (NotSupportedException)
            {
                if (instance.GetType().BaseType == typeof(MulticastDelegate))
                {
                    throw new NotSupportedException("Cannot clone Func/Action/Delegate. A common cause is posting or cloning IEnumerables holding closure references. A solution is to reify with `.ToList()` or similar before posting/cloning.");
                }

                throw;
            }
        }

        /// <summary>
        /// Provides an opportunity to clear an instance before caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// </summary>
        /// <param name="target">The instance to clear.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(ref T target, SerializationContext context)
        {
            this.clearImpl(ref target, context);
        }

        /// <summary>
        /// Prepares an empty object to clone into. This method is expected to allocate a new empty target object if the provided one is insufficient.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into. Could be null.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
        {
            target ??= (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        /// <summary>
        /// Prepares an empty object to deserialize into. This method is expected to allocate a new empty target object if the provided one is insufficient.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An optional existing instance to deserialize into. Could be null.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
            target ??= (T)FormatterServices.GetUninitializedObject(typeof(T));
        }
    }
}
