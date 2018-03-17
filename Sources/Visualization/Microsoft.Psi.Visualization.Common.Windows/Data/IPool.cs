// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents an allocation pool for a set of shared objects.
    /// </summary>
    public interface IPool : IDisposable
    {
        /// <summary>
        /// Gets, or creates if none exist, an instance of a shared object.
        /// </summary>
        /// <returns>Instance of a shared object.</returns>
        object GetOrCreate();
    }
}
