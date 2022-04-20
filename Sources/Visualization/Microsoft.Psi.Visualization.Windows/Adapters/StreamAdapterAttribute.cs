// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;

    /// <summary>
    /// Represents a stream adapter attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StreamAdapterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapterAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the stream adapter.</param>
        public StreamAdapterAttribute(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapterAttribute"/> class.
        /// </summary>
        public StreamAdapterAttribute()
        {
            this.Name = null;
        }

        /// <summary>
        /// Gets the name of the stream adapter.
        /// </summary>
        public string Name { get; private set; }
    }
}
