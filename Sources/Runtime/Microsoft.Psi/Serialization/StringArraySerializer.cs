// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Version of array optimized for arrays of strings.
    /// </summary>
    internal sealed class StringArraySerializer : ISerializer<string[]>
    {
        private const int LatestSchemaVersion = 2;

        /// <inheritdoc />
        public bool? IsClearRequired => false;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            serializers.GetHandler<string>(); // register element type
            var type = typeof(string[]);
            var name = TypeSchema.GetContractName(type, serializers.RuntimeInfo.SerializationSystemVersion);
            var elementsMember = new TypeMemberSchema("Elements", typeof(string).AssemblyQualifiedName, true);
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

        public void Serialize(BufferWriter writer, string[] instance, SerializationContext context)
        {
            writer.Write(instance.Length);
            foreach (var item in instance)
            {
                writer.Write(item);
            }
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref string[] target, SerializationContext context)
        {
            var size = reader.ReadInt32();
            Array.Resize(ref target, size);
        }

        public void Deserialize(BufferReader reader, ref string[] target, SerializationContext context)
        {
            for (int i = 0; i < target.Length; i++)
            {
                target[i] = reader.ReadString();
            }
        }

        public void PrepareCloningTarget(string[] instance, ref string[] target, SerializationContext context)
        {
            Array.Resize(ref target, instance.Length);
        }

        public void Clone(string[] instance, ref string[] target, SerializationContext context)
        {
            Array.Copy(instance, target, instance.Length);
        }

        public void Clear(ref string[] target, SerializationContext context)
        {
        }
    }
}
