// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Provides a base class for authoring backwards compatible custom serializers (for reading).
    /// </summary>
    /// <typeparam name="T">The type of objects handled by the custom serializer.</typeparam>
    public abstract class BackCompatSerializer<T> : ISerializer<T>
    {
        private readonly int latestSchemaVersion;
        private readonly ISerializer<T> latestVersionSerializer;
        private int targetSchemaVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackCompatSerializer{T}"/> class.
        /// </summary>
        /// <param name="latestSchemaVersion">The latest (current) schema version.</param>
        /// <param name="latestVersionSerializer">The latest version (current) serializer.</param>
        public BackCompatSerializer(
            int latestSchemaVersion,
            ISerializer<T> latestVersionSerializer)
        {
            this.latestSchemaVersion = latestSchemaVersion;
            this.latestVersionSerializer = latestVersionSerializer;
        }

        /// <inheritdoc />
        public bool? IsClearRequired => this.latestVersionSerializer.IsClearRequired;

        /// <inheritdoc />
        public TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema)
        {
            // Capture the target schema version
            this.targetSchemaVersion = targetSchema != null ? targetSchema.Version : this.latestSchemaVersion;

            // Create the latest version schema
            var latestVersionSchema = TypeSchema.FromType(
                typeof(T),
                this.latestVersionSerializer.GetType().AssemblyQualifiedName,
                this.latestSchemaVersion,
                serializers.RuntimeInfo.SerializationSystemVersion);

            // Initialize back-compat handlers
            if (this.targetSchemaVersion != this.latestSchemaVersion)
            {
                this.InitializeBackCompatSerializationHandlers(this.targetSchemaVersion, serializers, targetSchema);
            }

            // Initialize the latest version serializer
            this.latestVersionSerializer.Initialize(serializers, latestVersionSchema);

            return targetSchema ?? latestVersionSchema;
        }

        /// <summary>
        /// Abstract base method for initializing back-compat serialization handlers.
        /// </summary>
        /// <param name="schemaVersion">The schema version to deserialize.</param>
        /// <param name="serializers">The set of serialization handlers.</param>
        /// <param name="targetSchema">When the serializer is used to deserialize existing data, this parameter provides the schema that was persisted with the data.</param>
        public abstract void InitializeBackCompatSerializationHandlers(int schemaVersion, KnownSerializers serializers, TypeSchema targetSchema);

        /// <inheritdoc />
        public void Serialize(BufferWriter writer, T instance, SerializationContext context)
        {
            if (this.targetSchemaVersion != this.latestSchemaVersion)
            {
                throw new NotSupportedException($"The back compat serializer does not support {nameof(this.Serialize)} when using a previous schema version.");
            }

            this.latestVersionSerializer.Serialize(writer, instance, context);
        }

        /// <inheritdoc />
        public void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context)
        {
            this.latestVersionSerializer.PrepareDeserializationTarget(reader, ref target, context);
        }

        /// <inheritdoc />
        public void Deserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            if (this.targetSchemaVersion == this.latestSchemaVersion)
            {
                this.latestVersionSerializer.Deserialize(reader, ref target, context);
            }
            else
            {
                this.BackCompatDeserialize(this.targetSchemaVersion, reader, ref target, context);
            }
        }

        /// <summary>
        /// Abstract method for back-compatible deserialization.
        /// </summary>
        /// <param name="schemaVersion">The schema version to deserialize.</param>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An instance to deserialize into.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        public abstract void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref T target, SerializationContext context);

        /// <inheritdoc />
        public void PrepareCloningTarget(T instance, ref T target, SerializationContext context)
            => this.latestVersionSerializer.PrepareCloningTarget(instance, ref target, context);

        /// <inheritdoc />
        public void Clone(T instance, ref T target, SerializationContext context)
            => this.latestVersionSerializer.Clone(instance, ref target, context);

        /// <inheritdoc />
        public void Clear(ref T target, SerializationContext context)
            => this.latestVersionSerializer.Clear(ref target, context);
    }
}
