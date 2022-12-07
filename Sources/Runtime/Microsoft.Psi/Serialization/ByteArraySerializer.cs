// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Implements efficient code to serialize and deserialize instances of an array of bytes.
    /// </summary>
    /// <remarks>
    /// byte[] would be covered by SimpleArraySerializer. However, it's useful to have this explicit implementation as a performance baseline instead.
    /// </remarks>
    internal sealed class ByteArraySerializer : ISerializer<byte[]>
    {
        private const int LatestSchemaVersion = 2;

        /// <inheritdoc />
        public bool? IsClearRequired => false;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            serializers.GetHandler<byte>(); // register element type
            var type = typeof(byte[]);
            var name = TypeSchema.GetContractName(type, serializers.RuntimeInfo.SerializationSystemVersion);
            var elementsMember = new TypeMemberSchema("Elements", typeof(byte).AssemblyQualifiedName, true);
            var schema = new TypeSchema(
                type.AssemblyQualifiedName,
                TypeFlags.IsCollection,
                new TypeMemberSchema[] { elementsMember },
                name,
                TypeSchema.GetId(name),
                LatestSchemaVersion,
                this.GetType().AssemblyQualifiedName,
                serializers.RuntimeInfo.SerializationSystemVersion);
            return targetSchema ?? schema;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(BufferWriter writer, byte[] instance, SerializationContext context)
        {
            writer.Write(instance.Length);
            if (instance.Length > 0)
            {
                writer.Write(instance);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareDeserializationTarget(BufferReader reader, ref byte[] target, SerializationContext context)
        {
            var size = reader.ReadInt32();
            Array.Resize(ref target, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deserialize(BufferReader reader, ref byte[] target, SerializationContext context)
        {
            if (target.Length > 0)
            {
                reader.Read(target, target.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareCloningTarget(byte[] instance, ref byte[] target, SerializationContext context)
        {
            Array.Resize(ref target, instance.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clone(byte[] instance, ref byte[] target, SerializationContext context)
        {
            Buffer.BlockCopy(instance, 0, target, 0, instance.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(ref byte[] target, SerializationContext context)
        {
            // nothing to clear
        }
    }
}
