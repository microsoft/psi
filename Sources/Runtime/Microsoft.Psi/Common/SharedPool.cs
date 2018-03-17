// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Provides a pool of objects that can be reused.
    /// Use this class in conjunction with <see cref="Shared{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of data held by this pool</typeparam>
    public class SharedPool<T> : IDisposable
        where T : class
    {
        private Queue<T> available;
        private List<T> keepAlive;
        private KnownSerializers serializers;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedPool{T}"/> class.
        /// </summary>
        /// <param name="initialSize">The initial size of the pool. The size will be adjusted up as needed, but never down.</param>
        /// <param name="knownSerializers">An optional set of known serializers. Only required if the pool holds objects that are deserialized from an older store</param>
        public SharedPool(int initialSize, KnownSerializers knownSerializers = null)
        {
            this.available = new Queue<T>(initialSize);
            this.keepAlive = new List<T>();
            this.serializers = knownSerializers;
        }

        /// <summary>
        /// Gets the number of objects available in the pool
        /// </summary>
        public int AvailableCount
        {
            get
            {
                lock (this.available)
                {
                    return this.available.Count;
                }
            }
        }

        /// <summary>
        /// Gets the total number of objects managed by this pool
        /// </summary>
        public int TotalCount
        {
            get
            {
                lock (this.keepAlive)
                {
                    return this.keepAlive.Count;
                }
            }
        }

        /// <summary>
        /// Attempts to get an object from the pool.
        /// </summary>
        /// <param name="recyclable">An unused object from the pool, if there is one</param>
        /// <returns>True if an unused object was available, false otherwise</returns>
        public bool TryGet(out T recyclable)
        {
            lock (this.available)
            {
                if (this.available.Count > 0)
                {
                    recyclable = this.available.Dequeue();
                    return true;
                }
            }

            recyclable = null;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve an unused object from the pool.
        /// </summary>
        /// <param name="recyclable">An unused object, wrapped in a ref-counted <see cref="Shared{T}"/> instance.</param>
        /// <returns>True if an unused object was available, false otherwise</returns>
        public bool TryGet(out Shared<T> recyclable)
        {
            T recycled = null;
            recyclable = null;
            bool success = this.TryGet(out recycled);
            if (success)
            {
                recyclable = new Shared<T>(recycled, this);
            }

            return success;
        }

        /// <summary>
        /// Retrieves an unused object from the pool if one is available, otherwise creates and returns a new instance.
        /// </summary>
        /// <param name="constructor">A function that can create the instance if an unused object is not availablke in the pool</param>
        /// <returns>An unused object, wrapped in a ref-counted <see cref="Shared{T}"/> instance.</returns>
        public Shared<T> GetOrCreate(Func<T> constructor)
        {
            T recycled;
            if (!this.TryGet(out recycled))
            {
                recycled = constructor();
                lock (this.keepAlive)
                {
                    this.keepAlive.Add(recycled);
                }
            }

            return new Shared<T>(recycled, this);
        }

        /// <summary>
        /// Releases all unused objects.
        /// </summary>
        public void Dispose()
        {
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                lock (this.available)
                {
                    foreach (var entry in this.available)
                    {
                        ((IDisposable)entry).Dispose();
                    }

                    this.available = null;
                }
            }
        }

        /// <summary>
        /// Returns an object to the pool.
        /// This method is meant for internal use. Use <see cref="Shared{T}.Dispose"/> instead.
        /// </summary>
        /// <param name="recyclable">The obejct to return to the pool</param>
        internal void Recycle(T recyclable)
        {
            var context = new SerializationContext(this.serializers);
            Serializer.Clear(ref recyclable, context);
            lock (this.available)
            {
                this.available.Enqueue(recyclable);
            }
        }
    }
}
