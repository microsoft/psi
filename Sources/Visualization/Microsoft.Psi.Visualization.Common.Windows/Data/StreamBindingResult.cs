// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    /// <summary>
    /// An enumeration of possible results from a stream binding update operation.
    /// </summary>
    public enum StreamBindingResult
    {
        /// <summary>
        /// The stream continues to be bound to the same source.
        /// </summary>
        BindingUnchanged,

        /// <summary>
        /// The stream has been bound to a new source.
        /// </summary>
        BoundToNewSource,

        /// <summary>
        /// No source was found for the stream to bind to.
        /// </summary>
        NoSourceToBindTo,
    }
}
