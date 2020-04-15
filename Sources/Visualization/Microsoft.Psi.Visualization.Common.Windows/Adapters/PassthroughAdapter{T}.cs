// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a stream adapter that passes its input through to its output unchanged.
    /// </summary>
    /// <typeparam name="T">The type of data the adapter can adapt.</typeparam>
    public class PassthroughAdapter<T> : StreamAdapter<T, T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PassthroughAdapter{T}"/> class.
        /// </summary>
        public PassthroughAdapter()
            : base(Adapter)
        {
        }

        private static T Adapter(T data, Envelope env)
        {
            if (data is IDisposable disposableData)
            {
                return data.DeepClone<T>();
            }

            return data;
        }
    }
}
