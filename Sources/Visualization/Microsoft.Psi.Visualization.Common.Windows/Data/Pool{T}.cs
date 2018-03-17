// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents an allocation pool for a set of shared objects.
    /// </summary>
    /// <typeparam name="T">The type of shared object.</typeparam>
    public class Pool<T> : IPool
        where T : class
    {
        private readonly Func<T> allocator;
        private readonly SharedPool<T> recycler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> class.
        /// </summary>
        /// <param name="allocator">Allocation function.</param>
        public Pool(Func<T> allocator)
        {
            this.allocator = allocator;
            this.recycler = new SharedPool<T>(2);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.recycler.Dispose();
        }

        /// <inheritdoc />
        public object GetOrCreate()
        {
            return this.recycler.GetOrCreate(this.allocator);
        }
    }
}
