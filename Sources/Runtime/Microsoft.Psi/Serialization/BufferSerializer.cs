// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Implements efficient code to serialize and deserialize BufferReader instances.
    /// </summary>
    internal sealed class BufferSerializer : ISerializer<BufferReader>
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
        public void Serialize(BufferWriter writer, BufferReader instance, SerializationContext context)
        {
            var length = instance.RemainingLength;
            writer.Write(length);
            if (length > 0)
            {
                writer.WriteEx(instance.Buffer, instance.Position, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareDeserializationTarget(BufferReader reader, ref BufferReader target, SerializationContext context)
        {
            var length = reader.ReadInt32();
            target ??= new BufferReader();
            target.Reset(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deserialize(BufferReader reader, ref BufferReader target, SerializationContext context)
        {
            if (target.RemainingLength > 0)
            {
                reader.Read(target.Buffer, target.RemainingLength);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareCloningTarget(BufferReader instance, ref BufferReader target, SerializationContext context)
        {
            var length = instance.RemainingLength;
            target = target ?? new BufferReader();
            target.Reset(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clone(BufferReader instance, ref BufferReader target, SerializationContext context)
        {
            var length = instance.RemainingLength;
            Buffer.BlockCopy(instance.Buffer, instance.Position, target.Buffer, 0, length);
        }

        public void Clear(ref BufferReader target, SerializationContext context)
        {
            // nothing to clear
        }
    }
}
