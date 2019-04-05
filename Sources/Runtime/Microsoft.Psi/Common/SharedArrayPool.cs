// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Provides a pool of shared arrays.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    public static class SharedArrayPool<T>
    {
        private static readonly KeyedSharedPool<T[], int> Instance = new KeyedSharedPool<T[], int>(size => new T[size]);

        /// <summary>
        /// Gets or creates a shared array of the specified size.
        /// </summary>
        /// <param name="size">The size of the array.</param>
        /// <returns>A shared array of the requested size.</returns>
        public static Shared<T[]> GetOrCreate(int size)
        {
            return Instance.GetOrCreate(size);
        }
    }
}
