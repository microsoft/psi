// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Provides a pool of shared objects.
    /// Use this class in conjunction with <see cref="Shared{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the objects managed by this pool.</typeparam>
    public class SharedPool<T> : IDisposable
        where T : class
    {
        private readonly Func<T> allocator;
        private readonly KnownSerializers serializers;
        private readonly object availableLock = new ();
        private List<T> pool;
        private Queue<T> available;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedPool{T}"/> class.
        /// </summary>
        /// <param name="allocator">The allocation function for constructing a new object.</param>
        /// <param name="initialSize">The initial size of the pool. The size will be adjusted up as needed, but never down.</param>
        /// <param name="knownSerializers">An optional set of known serializers. Only required if the pool holds objects that are deserialized from an older store.</param>
        public SharedPool(Func<T> allocator, int initialSize = 10, KnownSerializers knownSerializers = null)
        {
            this.allocator = allocator;
            this.available = new Queue<T>(initialSize);
            this.pool = new List<T>();
            this.serializers = knownSerializers;
        }

        /// <summary>
        /// Gets the number of objects available, i.e., that are not live, in the pool.
        /// </summary>
        public int AvailableCount
        {
            get
            {
                lock (this.availableLock)
                {
                    return this.available != null ? this.available.Count : 0;
                }
            }
        }

        /// <summary>
        /// Gets the total number of objects managed by this pool.
        /// </summary>
        public int TotalCount
        {
            get
            {
                lock (this.pool)
                {
                    return this.pool.Count;
                }
            }
        }

        /// <summary>
        /// Resets the shared pool.
        /// </summary>
        /// <param name="clearLiveObjects">Indicates whether to clear any live objects.</param>
        /// <remarks>
        /// If the clearLiveObjects flag is false, an exception is thrown if a reset is attempted while the pool
        /// still contains live objects.
        /// </remarks>
        public void Reset(bool clearLiveObjects = false)
        {
            lock (this.availableLock)
            {
                lock (this.pool)
                {
                    // If no object is still alive, then reset the pool
                    if (clearLiveObjects || (this.available.Count == this.pool.Count))
                    {
                        // Dispose all the objects in the pool
                        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                        {
                            foreach (var entry in this.available)
                            {
                                ((IDisposable)entry).Dispose();
                            }
                        }

                        // Re-initialize
                        this.available = new ();
                        this.pool = new ();
                    }
                    else
                    {
                        throw new InvalidOperationException("Cannot reset a shared pool that contains live objects.");
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve an unused object from the pool.
        /// </summary>
        /// <param name="recyclable">An unused object from the pool, if there is one.</param>
        /// <returns>True if an unused object was available, false otherwise.</returns>
        public bool TryGet(out T recyclable)
        {
            lock (this.availableLock)
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
        /// <returns>True if an unused object was available, false otherwise.</returns>
        public bool TryGet(out Shared<T> recyclable)
        {
            recyclable = null;
            bool success = this.TryGet(out T recycled);
            if (success)
            {
                recyclable = new Shared<T>(recycled, this);
            }

            return success;
        }

        /// <summary>
        /// Attempts to retrieve an unused object from the pool if one is available, otherwise creates and returns a new instance.
        /// </summary>
        /// <returns>An unused object, wrapped in a ref-counted <see cref="Shared{T}"/> instance.</returns>
        public Shared<T> GetOrCreate()
        {
            if (!this.TryGet(out T recycled))
            {
                recycled = this.allocator();
                lock (this.pool)
                {
                    this.pool.Add(recycled);
                }
            }

            return new Shared<T>(recycled, this);
        }

        /// <summary>
        /// Releases all unused objects in the pool.
        /// </summary>
        public void Dispose()
        {
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                lock (this.availableLock)
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
        /// <param name="recyclable">The object to return to the pool.</param>
        internal void Recycle(T recyclable)
        {
            var context = new SerializationContext(this.serializers);
            Serializer.Clear(ref recyclable, context);
            lock (this.availableLock)
            {
                if (this.available != null)
                {
                    this.available.Enqueue(recyclable);
                }
                else
                {
                    // dispose the recycled object if it is disposable
                    if (recyclable is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
