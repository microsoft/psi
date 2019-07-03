// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a singleton object that manages a set of shared object allocation pools based on object type.
    /// </summary>
    public class PoolManager
    {
        /// <summary>
        /// The singleton instance of the <see cref="PoolManager"/>.
        /// </summary>
        public static readonly PoolManager Instance = new PoolManager();

        private readonly Dictionary<Type, Func<IPool>> sharedToPoolMap;

        private PoolManager()
        {
            this.sharedToPoolMap = new Dictionary<Type, Func<IPool>>
            {
                { typeof(Shared<Image>), () => new Pool<Image>(() => new Image(0, 0, PixelFormat.Undefined)) },
                { typeof(Shared<EncodedImage>), () => new Pool<EncodedImage>(() => new EncodedImage()) },
            };
        }

        /// <summary>
        /// Gets (or creates) a shared object allocation pool for an indicated type.
        /// </summary>
        /// <typeparam name="T">The type of objects the pool will allocate.</typeparam>
        /// <returns>A shared object allocation pool of the indicated type.</returns>
        public IPool GetPool<T>()
        {
            IPool pool = null;
            Func<IPool> poolCtor = null;
            if (this.sharedToPoolMap.TryGetValue(typeof(T), out poolCtor))
            {
                pool = poolCtor();
            }

            return pool;
        }
    }
}
