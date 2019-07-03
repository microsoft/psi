// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// The main entry point into the serialization subsystem. Provides methods to serialize, deserialize and clone objects.
    /// </summary>
    /// <remarks>
    /// For efficiency reasons the Serializer doesn't serialize any type information, and as a result it requires the user to identify the type being deserialized.
    /// In the case of polymorphic fields, the serializer collects type information in the Schema instance passed in to Serialize.
    /// The caller needs to provide this type info back when calling Deserialize.
    /// </remarks>
    public static class Serializer
    {
        /// <summary>
        /// Serializes the given instance to the specified stream.
        /// Call this override from within custom serializers.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="writer">The stream writer to serialize to.</param>
        /// <param name="instance">The instance to serialize.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize<T>(BufferWriter writer, T instance, SerializationContext context)
        {
            var handler = GetHandler<T>(context);
            handler.Serialize(writer, instance, context);
        }

        /// <summary>
        /// Deserializes the given instance from the specified stream.
        /// Call this override from within custom serializers.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="reader">The stream reader to deserialize from.</param>
        /// <param name="target">An optional existing instance to clone into.</param>
        /// <param name="context">A context object containing accumulated type and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize<T>(BufferReader reader, ref T target, SerializationContext context)
        {
            var handler = GetHandler<T>(context);
            handler.Deserialize(reader, ref target, context);
        }

        /// <summary>
        /// Makes a deep clone of the given object graph into the target object graph,
        /// avoiding any allocations, provided that the target object graph has the same shape.
        /// Call this override from within custom serializers.
        /// </summary>
        /// <typeparam name="T">The type of object to clone.</typeparam>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into.</param>
        /// <param name="context">An optional serialization context.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clone<T>(T instance, ref T target, SerializationContext context)
        {
            // check for null here to avoid generating the serializer when not really needed
            if (instance == null)
            {
                target = instance;
                return;
            }

            var handler = GetHandler<T>(context);
            handler.Clone(instance, ref target, context);
        }

        /// <summary>
        /// Makes a deep clone of the given object graph into the target object graph,
        /// avoiding any allocations (provided that the target object tree has the same shape).
        /// </summary>
        /// <typeparam name="T">The type of object to clone.</typeparam>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="target">An existing instance to clone into.</param>
        public static void DeepClone<T>(this T instance, ref T target)
        {
            Serializer.Clone(instance, ref target, new SerializationContext());
        }

        /// <summary>
        /// Creates a deep clone of the given object.
        /// The method will clone into an unused target instance obtained from the specified recycler.
        /// The caller should return the clone to the recycler when done.
        /// </summary>
        /// <typeparam name="T">The type of object to clone.</typeparam>
        /// <param name="instance">The instance to clone.</param>
        /// <param name="recycler">An object recycling cache.</param>
        /// <returns>The deep-clone.</returns>
        public static T DeepClone<T>(this T instance, IRecyclingPool<T> recycler)
        {
            T result = (recycler == null) ? default(T) : recycler.Get();
            Serializer.Clone(instance, ref result, new SerializationContext());
            return result;
        }

        /// <summary>
        /// Creates a deep clone of the given object.
        /// Except for the case of simple value types, this method allocates a new object tree to clone into.
        /// This can become a performance bottleneck when the clone operation needs to be executed many times.
        /// In these cases, the other Clone overrides which avoid allocations perform significantly better.
        /// </summary>
        /// <typeparam name="T">The type of object to clone.</typeparam>
        /// <param name="instance">The instance to clone.</param>
        /// <returns>The deep-clone.</returns>
        public static T DeepClone<T>(this T instance)
        {
            T result = default(T);
            Serializer.Clone(instance, ref result, new SerializationContext());
            return result;
        }

        /// <summary>
        /// Clears the instance in preparation for caching it for future reuse as a cloning or deserialization target.
        /// The method is expected to call Serializer.Clear on all reference-type fields.
        /// This method is for custom serializers.
        /// </summary>
        /// <typeparam name="T">The type of object to clear.</typeparam>
        /// <param name="target">The instance to clear.</param>
        /// <param name="context">A context object containing accumulated type mappings and object references.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(ref T target, SerializationContext context)
        {
            if (target == null)
            {
                return;
            }

            var handler = GetHandler<T>(context);
            handler.Clear(ref target, context);
        }

        /// <summary>
        /// Returns true if the type is immutable (it is a primitive type or all its fields are read-only immutable types).
        /// </summary>
        /// <typeparam name="T">The type to analyze.</typeparam>
        /// <returns>True if the type is immutable.</returns>
        public static bool IsImmutableType<T>()
        {
            return KnownSerializers.Default.GetHandler<T>().IsImmutableType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Serialize(Type type, BufferWriter writer, object instance, SerializationContext context)
        {
            var handler = GetHandler(type, context);
            handler.UntypedSerialize(writer, instance, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Deserialize(Type type, BufferReader reader, ref object target, SerializationContext context)
        {
            var handler = GetHandler(type, context);
            handler.UntypedDeserialize(reader, ref target, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clone(Type type, object instance, ref object target, SerializationContext context)
        {
            var handler = GetHandler(type, context);
            handler.UntypedClone(instance, ref target, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Clear(Type type, ref object target, SerializationContext context)
        {
            var handler = GetHandler(type, context);
            handler.UntypedClear(ref target, context);
        }

        internal static SerializationHandler<T> GetHandler<T>(SerializationContext context)
        {
            return context.Serializers.GetHandler<T>();
        }

        internal static SerializationHandler GetHandler(Type type, SerializationContext context)
        {
            return context.Serializers.GetUntypedHandler(type);
        }

        internal static SerializationHandler GetHandler(int handlerId, Type baseType, SerializationContext context)
        {
            return context.Serializers.GetUntypedHandler(handlerId, baseType);
        }
    }
}
