// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using Microsoft.Psi.Common;

    internal delegate void SerializeDelegate<T>(BufferWriter writer, T instance, SerializationContext context);

    internal delegate void DeserializeDelegate<T>(BufferReader reader, ref T target, SerializationContext context);

    internal delegate void CloneDelegate<T>(T instance, ref T target, SerializationContext context);

    internal delegate void ClearDelegate<T>(ref T target, SerializationContext context);

    /// <summary>
    /// The contract for efficient serialization and deserialization of instances of the given type.
    /// </summary>
    /// <remarks>Implementers should delegate as much as possible to the default serializers via the static Serializer class.</remarks>
    /// <typeparam name="T">The type of objects the serializer knows how to handle.</typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Gets a value indicating whether cached instances must be cleared (null if statically unknown).
        /// </summary>
        bool? IsClearRequired { get; }

        /// <summary>
        /// Initializes the serializer with the type schema and target object schema to use.
        /// </summary>
        /// <param name="serializers">The set of serialization handlers.</param>
        /// <param name="targetSchema">
        /// When the serializer is used to deserialize existing data,
        /// this parameter provides the schema that was persisted with the data.
        /// This is in effect the desired schema the serializer should use.
        /// </param>
        /// <returns>The schema this serializer is committed to using (this can be either targetSchema, typeSchema or a custom schema).</returns>
        /// <remarks>
        /// The serializer must read and write data according to targetSchema.
        /// A serializer that wants to delegate some of the functionality to its base class can create a modified schema to pass to
        /// its parent.
        /// Note that targetSchema is a partial schema, without any MemberInfo information.
        /// To obtain MemberInfo information, generate a schema from the runtime type
        /// using <see cref="TypeSchema.FromType(Type, string, int, int)"/>.
        /// </remarks>
        TypeSchema Initialize(KnownSerializers serializers, TypeSchema targetSchema);

        /// <summary>
        /// Serializes the given instance to the specified stream.
        /// </summary>
        /// <param name="writer">The stream writer to serialize to.</param>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        void Serialize(BufferWriter writer, T instance, SerializationContext context);

        /// <summary>
        /// Deserializes an instance from the specified stream into the specified target object.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An instance to deserialize into.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        void Deserialize(BufferReader reader, ref T target, SerializationContext context);

        /// <summary>
        /// Deep-clones the given object into an existing allocation.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        void Clone(T instance, ref T target, SerializationContext context);

        /// <summary>
        /// Prepares an empty object to deserialize into. This method is expected to allocate a new empty target object if the provided one is insufficient.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An optional existing instance to deserialize into. Could be null.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        void PrepareDeserializationTarget(BufferReader reader, ref T target, SerializationContext context);

        /// <summary>
        /// Prepares an empty object to clone into. This method is expected to allocate a new empty target object if the provided one is insufficient.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into. Could be null.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        void PrepareCloningTarget(T instance, ref T target, SerializationContext context);

        /// <summary>
        /// An opportunity to clear an instance before caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// </summary>
        /// <param name="target">The instance to clear.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        void Clear(ref T target, SerializationContext context);
    }
}