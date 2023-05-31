// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    /// <summary>
    /// The type of a stream update.
    /// </summary>
    public enum StreamUpdateType
    {
        /// <summary>
        /// An item is being added.
        /// </summary>
        Add,

        /// <summary>
        /// An item is being replaced.
        /// </summary>
        Replace,

        /// <summary>
        /// An item is being deleted.
        /// </summary>
        Delete,
    }
}
