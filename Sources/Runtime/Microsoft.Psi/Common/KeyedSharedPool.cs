// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedSharedPool{T, TKey}"/> class.
    /// </summary>
    /// <typeparam name="T">Type of shared resource.</typeparam>
    /// <typeparam name="TKey">Type of shared resource key.</typeparam>
    public class KeyedSharedPool<T, TKey> : IDisposable
        where T : class
    {
        private static ConcurrentDictionary<TKey, SharedPool<T>> sharedPools = new ConcurrentDictionary<TKey, SharedPool<T>>();

        /// <summary>
        /// Get pooled instance or create when necessary.
        /// </summary>
        /// <param name="key">Shared resource key</param>
        /// <param name="constructor">Constructor function called as necessary to create new instances</param>
        /// <returns>Shared resource</returns>
        public static Shared<T> GetOrCreate(TKey key, Func<T> constructor)
        {
            return GetSharedPool(key).GetOrCreate(constructor);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var sharedPool in sharedPools.Values)
            {
                sharedPool.Dispose();
            }
        }

        private static SharedPool<T> GetSharedPool(TKey key)
        {
            return sharedPools.GetOrAdd(key, new SharedPool<T>(10));
        }
    }
}
