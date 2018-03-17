// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;

    public class KeyedSharedPool<T, TKey> : IDisposable
        where T : class
    {
        private static ConcurrentDictionary<TKey, SharedPool<T>> sharedPools = new ConcurrentDictionary<TKey, SharedPool<T>>();

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
