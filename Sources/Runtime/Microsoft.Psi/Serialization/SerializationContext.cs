// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Maintains the objects and types seen during serialization, to enable polymorphism,
    /// single-instanced references (multiple references to same object) and circular dependencies.
    /// </summary>
    public class SerializationContext
    {
        private readonly KnownSerializers serializers;
        private Action<int, Type> polymorphicTypePublisher;
        private Dictionary<object, int> serialized;
        private int nextSerializedId;
        private Dictionary<object, int> deserialized;
        private Dictionary<int, object> deserializedById;
        private int nextDeserializedId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationContext"/> class.
        /// This will become internal. Use Serializer.Schema instead.
        /// </summary>
        public SerializationContext()
            : this(KnownSerializers.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationContext"/> class, with the specified serialization overrides.
        /// </summary>
        /// <param name="serializers">The set of custom serializers to use instead of the default ones.</param>
        public SerializationContext(KnownSerializers serializers)
        {
            this.serializers = serializers ?? KnownSerializers.Default;
        }

        internal KnownSerializers Serializers => this.serializers;

        internal Action<int, Type> PolymorphicTypePublisher
        {
            set { this.polymorphicTypePublisher = value; }
        }

        /// <summary>
        /// Clears the object caches used to identify multiple references to the same instance.
        /// You must call this method before reusing the context object to serialize another object graph.
        /// </summary>
        public void Reset()
        {
            this.serialized?.Clear();
            this.deserialized?.Clear();
            this.deserializedById?.Clear();
            this.nextSerializedId = 0;
            this.nextDeserializedId = 0;
        }

        internal void PublishPolymorphicType(int id, Type type)
        {
            this.polymorphicTypePublisher?.Invoke(id, type);
        }

        internal bool GetOrAddSerializedObjectId(object obj, out int id)
        {
            if (obj == null || obj is string)
            {
                id = this.nextSerializedId++;
                return false;
            }

            if (this.serialized == null)
            {
                this.serialized = new Dictionary<object, int>(ReferenceEqualsComparer.Default);
            }

            if (this.serialized.TryGetValue(obj, out id))
            {
                return true;
            }

            // not found
            id = this.nextSerializedId++;
            this.serialized.Add(obj, id);
            return false;
        }

        internal IEnumerable<T> GetSerializedObjects<T>()
            where T : class
        {
            return this.serialized.Keys.Where(o => o is T).Select(o => o as T);
        }

        internal object GetDeserializedObject(int id)
        {
            if (this.deserializedById != null)
            {
                return this.deserializedById[id];
            }

            return null;
        }

        internal void AddDeserializedObject(object obj)
        {
            int id = this.nextDeserializedId++;

            // We need the serialized and deserialized collections to have the same ids when cloning, so we have to count nulls and strings too.
            if (obj == null || obj is string)
            {
                return;
            }

            if (obj is string)
            {
                return;
            }

            if (this.deserialized == null)
            {
                this.deserialized = new Dictionary<object, int>(ReferenceEqualsComparer.Default);
                this.deserializedById = new Dictionary<int, object>();
            }

            this.deserialized.Add(obj, id);
            this.deserializedById.Add(id, obj);
        }

        internal bool ContainsDeserializedObject(object obj)
        {
            if (this.deserialized == null)
            {
                return false;
            }

            if (obj == null || obj is string)
            {
                return false;
            }

            return this.deserialized.ContainsKey(obj);
        }

        internal IEnumerable<T> GetDeserializedObjects<T>()
            where T : class
        {
            return this.deserialized.Keys.Where(o => o is T).Select(o => o as T);
        }

        private class ReferenceEqualsComparer : EqualityComparer<object>
        {
            public static readonly new ReferenceEqualsComparer Default = new ReferenceEqualsComparer();

            public override bool Equals(object obj1, object obj2)
            {
                return object.ReferenceEquals(obj1, obj2);
            }

            public override int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
