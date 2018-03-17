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
        private int id;
        private string name;
        private Type type;
        private int sizeOf;
        private bool isImmutableType;

        protected SerializationHandler(Type targetType, string contractName, int id)
        {
            this.id = id;
            this.name = contractName;
            this.type = targetType;
            this.isImmutableType = Generator.IsImmutableType(targetType);
        }

        public int Id => this.id;

        public string Name => this.name;

        public Type TargetType => this.type;

        public bool IsImmutableType => this.isImmutableType;

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
        /// <param name="writer">The stream writer to serialize to</param>
        /// <param name="instance">The instance to serialize</param>
        /// <param name="context">A context object containing accumulated type and object references</param>
        internal abstract void UntypedSerialize(BufferWriter writer, object instance, SerializationContext context);

        /// <summary>
        /// Deserializes an instance from the specified stream. This gets called after the ref prefix (if any) has been read.
        /// </summary>
        /// <param name="reader">The stream reader to deserialize from</param>
        /// <param name="target">An optional existing instance to deserialize into</param>
        /// <param name="context">A context object containing accumulated type and object references</param>
        internal abstract void UntypedDeserialize(BufferReader reader, ref object target, SerializationContext context);

        /// <summary>
        /// Deep clones the given object into an existing allocation.
        /// </summary>
        /// <param name="instance">The instance to clone</param>
        /// <param name="target">An existing instance to clone into</param>
        /// <param name="context">A context object containing accumulated type and object references</param>
        internal abstract void UntypedClone(object instance, ref object target, SerializationContext context);

        /// <summary>
        /// An opportunity to clear the instance before caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// </summary>
        /// <param name="target">The instance to clone</param>
        /// <param name="context">A context object containing accumulated type mappings and object references</param>
        internal abstract void UntypedClear(ref object target, SerializationContext context);
    }

    /// <summary>
    /// Base class for serialization handlers.
    /// Custom serializers should cache the handlers they need for serializing object fields.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    public abstract class SerializationHandler<T> : SerializationHandler
    {
        protected SerializationHandler(string contractName, int id)
            : base(typeof(T), contractName, id)
        {
        }

        public abstract void Serialize(BufferWriter writer, T instance, SerializationContext context);

        public abstract void Deserialize(BufferReader reader, ref T target, SerializationContext context);

        public abstract void Clone(T instance, ref T target, SerializationContext context);

        public abstract void Clear(ref T target, SerializationContext context);
    }
#pragma warning restore SA1402
}
