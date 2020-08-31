// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents an object that adapts messages from one type to another.
    /// </summary>
    public interface IStreamAdapter : IDisposable
    {
        /// <summary>
        /// Gets the shared allocation pool.
        /// </summary>
        IPool Pool { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        Type SourceType { get; }
    }
}
