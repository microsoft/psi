// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.IO;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Internal wrapper for all serializers of reference types.
    /// Handles the ref envelope, enabling null, shared references and polymorphism.
    /// It implements both a typed and an untyped version of the serialization contract.
    /// The typed contract enables efficient calling (no type lookup and parameter boxing),
    /// while the untyped contract is used for polymorphic fields.
    /// </summary>
    /// <typeparam name="T">The type of objects the handler understands.</typeparam>
    internal sealed class RefHandler<T> : SerializationHandler<T>
    {
        // the inner serializer and serializerEx
        private readonly ISerializer<T> innerSerializer;

        public RefHandler(ISerializer<T> innerSerializer, string contractName, int id)
            : base(contractName, id)
        {
            this.innerSerializer = innerSerializer;
            if (innerSerializer != null && innerSerializer.IsClearRequired.HasValue)
            {
                this.IsClearRequired = innerSerializer.IsClearRequired.Value;
            }

            if (typeof(T).IsValueType)
            {
                throw new InvalidOperationException("Cannot use a class handler with a value type serializer");
            }
        }

        // serialize the ref envelope (null, duplicate reference, circular reference, polymorphism), before serializing the object itself
        public override void Serialize(BufferWriter writer, T instance, SerializationContext context)
        {
            // if null, just write the null prefix and exit
            if (instance == null)
            {
                writer.Write(RefPrefixNull);
                return;
            }

            // is it an object that we already serialized?
            int id;
            if (context.GetOrAddSerializedObjectId(instance, out id))
            {
                // write the reference id and don't serialize it again
                var prefix = RefPrefixExisting | id;
                writer.Write((uint)prefix);
                return;
            }

            // is it a polymorphic object?
            Type instanceType = instance.GetType();
            if (typeof(T) != instanceType)
            {
                // find the correct serialization handler
                var handler = Serializer.GetHandler(instanceType, context);
                var handlerId = ((SerializationHandler)handler).Id;

                // let the context know that this type is required for deserialization
                context.PublishPolymorphicType(handlerId, instanceType);

                // write the type id before serializing the object
                var prefix = RefPrefixTyped | handlerId;
                writer.Write((uint)prefix);

                // delegate serialization to the correct serialization handler
                handler.UntypedSerialize(writer, instance, context);
                return;
            }

            // this is a new object, so invoke the serializer to persist its fields
            writer.Write(RefPrefixNew);
            this.innerSerializer.Serialize(writer, instance, context);
        }

        // deserialize the ref envelope (null, duplicate reference, circular reference, polymorphism), before deserializing the object itself
        public override void Deserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            // read the header
            uint prefix = reader.ReadUInt32();

            switch (prefix & RefPrefixMask)
            {
                // null?
                case RefPrefixNull:
                    {
                        target = default(T);
                        return;
                    }

                // is it an object that we have already seen?
                case RefPrefixExisting:
                    {
                        // find the object in the object cache and return it
                        int id = (int)(prefix & RefPrefixValueMask);
                        target = (T)context.GetDeserializedObject(id);
                        if (target == null)
                        {
                            // a custom serializer was implemented incorrectly
                            throw new InvalidDataException($"The serializer detected an unresolved circular reference to an instance of type {typeof(T)}. The custom serializer for this type needs to implement the ISerializerEx interface.");
                        }

                        return;
                    }

                // is it a polymorphic object?
                case RefPrefixTyped:
                    {
                        // did we use this target before? If so, we can't use it again.
                        if (target != null && context.ContainsDeserializedObject(target))
                        {
                            target = default(T);
                        }

                        // determine the correct handler of the instance to deserialize from the prefix
                        int handlerId = (int)(prefix & RefPrefixValueMask);
                        var handler = context.Serializers.GetUntypedHandler(handlerId, typeof(T));

                        // determine and use the correct serialization handler
                        object objTarget = target;
                        handler.UntypedDeserialize(reader, ref objTarget, context);
                        target = (T)objTarget;
                        return;
                    }

                // not polymorphic and not seen before
                case RefPrefixNew:
                    {
                        // did we use this target before? If so, we can't use it again.
                        if (target != null && context.ContainsDeserializedObject(target))
                        {
                            target = default(T);
                        }

                        // invoke the serializer to read the fields
                        this.InnerDeserialize(reader, ref target, context);
                        return;
                    }
            }
        }

        /// <summary>
        /// Creates a deep clone of the given object.
        /// </summary>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An optional existing instance to clone into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        public override void Clone(T instance, ref T target, SerializationContext context)
        {
            // null?
            if (instance == null)
            {
                target = instance;
                return;
            }

            // is it an object that we have already seen?
            int id;
            if (context.GetOrAddSerializedObjectId(instance, out id))
            {
                // find and reuse the object that we have already cloned in the object cache
                target = (T)context.GetDeserializedObject(id);
                if (target == null)
                {
                    throw new InvalidDataException("The serializer detected a circular reference: " + typeof(T));
                }

                return;
            }

            // did we use this target before? If so, we can't use it again.
            if (target != null && context.ContainsDeserializedObject(target))
            {
                target = default(T);
            }

            // is it a polymorphic object?
            Type instanceType = instance.GetType();
            if (typeof(T) != instanceType)
            {
                // determine and use the correct serialization handler based on the runtime type
                object objTarget = target;
                Serializer.Clone(instanceType, instance, ref objTarget, context);
                target = (T)objTarget;
            }
            else
            {
                // not polymorphic, invoke the serializer to clone the fields
                this.InnerClone(instance, ref target, context);
            }

            return;
        }

        /// <summary>
        /// An opportunity to clear an instance before caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// </summary>
        /// <param name="target">The instance to clear.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        public override void Clear(ref T target, SerializationContext context)
        {
            // null?
            if (target == null)
            {
                return;
            }

            // is it an object that we have already seen?
            int id;
            if (context.GetOrAddSerializedObjectId(target, out id))
            {
                target = (T)context.GetDeserializedObject(id);
                return;
            }

            // is it a polymorphic object?
            Type instanceType = target.GetType();
            if (typeof(T) != instanceType)
            {
                // determine and use the correct serialization handler based on the runtime type
                object objTarget = target;
                Serializer.Clear(instanceType, ref objTarget, context);
                target = (T)objTarget;
            }
            else
            {
                // call the serializer to clear the fields
                this.InnerClear(ref target, context);
            }
        }

        // The untyped interface methods call the inner serializer directly (bypassing the handler methods)
        // to avoid writing and reading a second prefix
        internal override void UntypedSerialize(BufferWriter writer, object instance, SerializationContext context)
            => this.innerSerializer.Serialize(writer, (T)instance, context);

        internal override void UntypedDeserialize(BufferReader reader, ref object target, SerializationContext context)
        {
            T typedTarget = (target is T) ? (T)target : default(T);
            this.InnerDeserialize(reader, ref typedTarget, context);
            target = typedTarget;
        }

        internal override void UntypedClone(object instance, ref object target, SerializationContext context)
        {
            T typedTarget = (target is T) ? (T)target : default(T);
            this.InnerClone((T)instance, ref typedTarget, context);
            target = typedTarget;
        }

        internal override void UntypedClear(ref object target, SerializationContext context)
        {
            T typedTarget = (T)target;
            this.InnerClear(ref typedTarget, context);
            target = typedTarget;
        }

        private void InnerDeserialize(BufferReader reader, ref T target, SerializationContext context)
        {
            // create and register the target, then deserialize the fields
            this.innerSerializer.PrepareDeserializationTarget(reader, ref target, context);
            context.AddDeserializedObject(target);
            this.innerSerializer.Deserialize(reader, ref target, context);
        }

        private void InnerClone(T instance, ref T target, SerializationContext context)
        {
            // create and register the target, then clone the fields
            this.innerSerializer.PrepareCloningTarget(instance, ref target, context);
            context.AddDeserializedObject(target);
            this.innerSerializer.Clone(instance, ref target, context);
        }

        private void InnerClear(ref T target, SerializationContext context)
        {
            context.AddDeserializedObject(target); // must do it first, so that circular dependencies can be detected
            this.innerSerializer.Clear(ref target, context);
        }
    }
}
