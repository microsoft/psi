// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi.Common;

#pragma warning disable SA1402 // File may only contain a single class

    /// <summary>
    /// Base class for ref and struct handlers, providing a type id for each type.
    /// </summary>
    public abstract class SerializationHandler
    {
        /// <summary>
        /// Reference type prefix indicating null.
        /// </summary>
        public const uint RefPrefixNull = 0x00000000;

        /// <summary>
        /// Reference type prefix flag indicating new type.
        /// </summary>
        public const uint RefPrefixNew = 0x80000000;

        /// <summary>
        /// Reference type prefix flag indicating existing type.
        /// </summary>
        public const uint RefPrefixExisting = 0x40000000;

        /// <summary>
        /// Reference type prefix flag indicating typed.
        /// </summary>
        public const uint RefPrefixTyped = 0xC0000000;

        /// <summary>
        /// Reference type prefix mask.
        /// </summary>
        public const uint RefPrefixMask = 0xC0000000;

        /// <summary>
        /// Reference type prefix value mask.
        /// </summary>
        public const uint RefPrefixValueMask = 0x3FFFFFFF;

        private int id;
        private string name;
        private Type type;
        private int sizeOf;
        private bool isImmutableType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationHandler"/> class.
        /// </summary>
        /// <param name="targetType">Target serialization type.</param>
        /// <param name="contractName">Serialization handler contract name.</param>
        /// <param name="id">Serialization handler ID.</param>
        protected SerializationHandler(Type targetType, string contractName, int id)
        {
            this.id = id;
            this.name = contractName;
            this.type = targetType;
            this.isImmutableType = Generator.IsImmutableType(targetType);
        }

        /// <summary>
        /// Gets serialization handler ID.
        /// </summary>
        public int Id => this.id;

        /// <summary>
        /// Gets serialization handler name.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Gets target serialization type.
        /// </summary>
        public Type TargetType => this.type;

        /// <summary>
        /// Gets a value indicating whether type is immutable.
        /// </summary>
        public bool IsImmutableType => this.isImmutableType;

        /// <summary>
        /// Gets or sets a value indicating whether an instance of the target type must
        /// first be cleared before it is reused as a cloning or deserialization target.
        /// </summary>
        public bool? IsClearRequired { get; set; }

        /// <summary>
        /// Returns the size, in bytes, of the value type associated with this handler.
        /// For a reference type, the size returned is the size of a reference value of the corresponding type (4 bytes on 32-bit systems),
        /// not the size of the data stored in objects referred to by the reference value.
        /// </summary>
        /// <returns>The size, in bytes, of the supplied value type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SizeOf()
        {
            if (this.sizeOf == 0)
            {
                this.sizeOf = Generator.SizeOf(this.TargetType);
            }

            return this.sizeOf;
        }

        internal static SerializationHandler<T> Create<T>(ISerializer<T> serializer, string name, int id)
        {
            if (typeof(T).IsValueType)
            {
                return new StructHandler<T>(serializer, name, id);
            }
            else
            {
                return new RefHandler<T>(serializer, name, id);
            }
        }

        /// <summary>
        /// Serializes the given instance to the specified stream.
        /// </summary>
        /// <param name="writer">The stream writer to serialize to.</param>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        internal abstract void UntypedSerialize(BufferWriter writer, object instance, SerializationContext context);

        /// <summary>
        /// Deserializes an instance from the specified stream. This gets called after the ref prefix (if any) has been read.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An optional existing instance to deserialize into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        internal abstract void UntypedDeserialize(BufferReader reader, ref object target, SerializationContext context);

        /// <summary>
        /// Deep clones the given object into an existing allocation.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        internal abstract void UntypedClone(object instance, ref object target, SerializationContext context);

        /// <summary>
        /// An opportunity to clear the instance before caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// </summary>
        /// <param name="target">The instance to clone.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        internal abstract void UntypedClear(ref object target, SerializationContext context);
    }

    /// <summary>
    /// Base class for serialization handlers.
    /// Custom serializers should cache the handlers they need for serializing object fields.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    public abstract class SerializationHandler<T> : SerializationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationHandler{T}"/> class.
        /// </summary>
        /// <param name="contractName">Serialization handler contract name.</param>
        /// <param name="id">Serialization handler ID.</param>
        protected SerializationHandler(string contractName, int id)
            : base(typeof(T), contractName, id)
        {
        }

        /// <summary>
        /// Serialize to buffer.
        /// </summary>
        /// <param name="writer">Buffer to which to serialize.</param>
        /// <param name="instance">Instance to serialize.</param>
        /// <param name="context">Serialization context.</param>
        public abstract void Serialize(BufferWriter writer, T instance, SerializationContext context);

        /// <summary>
        /// Deserialize from buffer.
        /// </summary>
        /// <param name="reader">Buffer from which to deserialize.</param>
        /// <param name="target">Target into which to deserialize.</param>
        /// <param name="context">Serialization context.</param>
        public abstract void Deserialize(BufferReader reader, ref T target, SerializationContext context);

        /// <summary>
        /// Clone instance to target.
        /// </summary>
        /// <param name="instance">Instance to be cloned.</param>
        /// <param name="target">Target into which to clone.</param>
        /// <param name="context">Serialization context.</param>
        public abstract void Clone(T instance, ref T target, SerializationContext context);

        /// <summary>
        /// Clear target value.
        /// </summary>
        /// <param name="target">Target to clear.</param>
        /// <param name="context">Serialization context.</param>
        public abstract void Clear(ref T target, SerializationContext context);
    }
#pragma warning restore SA1402
}
