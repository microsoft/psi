// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Generates efficient code to serialize and deserialize instances of an array.
    /// </summary>
    /// <typeparam name="T">The type of objects this serializer knows how to handle.</typeparam>
    internal sealed class ArraySerializer<T> : ISerializer<T[]>
    {
        private const int LatestSchemaVersion = 2;

        private SerializationHandler<T> elementHandler;

        /// <inheritdoc />
        public bool? IsClearRequired => true;

        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            var type = typeof(T[]);
            this.elementHandler = serializers.GetHandler<T>(); // register element type
            var name = TypeSchema.GetContractName(type, serializers.RuntimeInfo.SerializationSystemVersion);
            var elementsMember = new TypeMemberSchema("Elements", typeof(T).AssemblyQualifiedName, true);
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

        public void Serialize(BufferWriter writer, T[] instance, SerializationContext context)
        {
            writer.Write(instance.Length);
            foreach (T element in instance)
            {
                this.elementHandler.Serialize(writer, element, context);
            }
        }

        public void PrepareDeserializationTarget(BufferReader reader, ref T[] target, SerializationContext context)
        {
            var size = reader.ReadInt32();
            this.PrepareTarget(ref target, size, context);
        }

        public void Deserialize(BufferReader reader, ref T[] target, SerializationContext context)
        {
            for (int i = 0; i < target.Length; i++)
            {
                this.elementHandler.Deserialize(reader, ref target[i], context);
            }
        }

        public void PrepareCloningTarget(T[] instance, ref T[] target, SerializationContext context)
        {
            this.PrepareTarget(ref target, instance.Length, context);
        }

        public void Clone(T[] instance, ref T[] target, SerializationContext context)
        {
            for (int i = 0; i < instance.Length; i++)
            {
                this.elementHandler.Clone(instance[i], ref target[i], context);
            }
        }

        public void Clear(ref T[] target, SerializationContext context)
        {
            for (int i = 0; i < target.Length; i++)
            {
                this.elementHandler.Clear(ref target[i], context);
            }
        }

        private void PrepareTarget(ref T[] target, int size, SerializationContext context)
        {
            if (target != null && target.Length > size && (!this.elementHandler.IsClearRequired.HasValue || this.elementHandler.IsClearRequired.Value))
            {
                // use a separate context to clear the unused objects, so that we don't corrupt the current context
                var clearContext = new SerializationContext(context.Serializers);

                // only clear the extra items that we won't use during cloning or deserialization (those get cleared by cloning/deserialization).
                for (int i = size; i < target.Length; i++)
                {
                    this.elementHandler.Clear(ref target[i], clearContext);
                }
            }

            Array.Resize(ref target, size);
        }
    }
}
