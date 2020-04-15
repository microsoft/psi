// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of any type into objects.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class ObjectAdapter<T> : StreamAdapter<T, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectAdapter{T}"/> class.
        /// </summary>
        public ObjectAdapter()
            : base(Adapter)
        {
        }

        private static object Adapter(T value, Envelope env)
        {
            return value;
        }
    }
}
