// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Maintains a cache of unused instances that can be use as cloning or deserialization targets.
    /// </summary>
    /// <typeparam name="T">The type of instances that can be cached by this cloner.</typeparam>
    public interface IRecyclingPool<T>
    {
        /// <summary>
        /// Gets the number of allocations that have not yet been returned to the pool.
        /// </summary>
        int OutstandingAllocationCount { get; }

        /// <summary>
        /// Gets the number of available allocations that have been already returned to the pool.
        /// </summary>
        int AvailableAllocationCount { get; }

        /// <summary>
        /// Returns the next available cached object.
        /// </summary>
        /// <returns>An unused cached object that can be reused as a target for cloning or deserialization.</returns>
        T Get();

        /// <summary>
        /// Returns an unused object back to the pool.
        /// The caller must guarantee that the entire object tree (the object and any of the objects it references) are not in use anymore.
        /// </summary>
        /// <param name="freeInstance">The object to return to the pool.</param>
        void Recycle(T freeInstance);
    }
}
