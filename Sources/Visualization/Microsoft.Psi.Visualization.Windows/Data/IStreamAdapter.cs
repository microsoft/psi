// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents an object that adapts messages from one type to another.
    /// </summary>
    public interface IStreamAdapter
    {
        /// <summary>
        /// Gets the destination type.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets the allocator for source messages.
        /// </summary>
        Func<dynamic> SourceAllocator { get; }

        /// <summary>
        /// Gets the deallocator for source messages.
        /// </summary>
        Action<dynamic> SourceDeallocator { get; }
    }
}
