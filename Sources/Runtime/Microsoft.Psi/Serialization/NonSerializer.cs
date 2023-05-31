// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Serializers for types that can't really be serialized.
    /// </summary>
    /// <typeparam name="T">The type known to not be serializable.</typeparam>
    internal class NonSerializer<T> : ISerializer<T>
    {
        private const int Version = 0;

        /// <inheritdoc />
        public bool? IsClearRequired => false;

        /// <inheritdoc/>
        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            return null;
        }

        /// <inheritdoc/>
        public void Clone(T instance, ref T target, SerializationContext context)
        {
        }

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, T instance, SerializationContext context)
        {
            throw new NotSupportedException($"Serialization is not supported for type: {typeof(T).AssemblyQualifiedName}");
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            throw new NotSupportedException($"Deserialization is not supported for type: {typeof(T).AssemblyQualifiedName}");
        }

        /// <inheritdoc/>
        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
            throw new NotSupportedException($"Deserialization is not supported for type: {typeof(T).AssemblyQualifiedName}");
        }

        /// <inheritdoc/>
        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
        {
            target = instance;
        }

        /// <inheritdoc/>
        public void Clear(ref T target, SerializationContext context)
        {
        }
    }
}
