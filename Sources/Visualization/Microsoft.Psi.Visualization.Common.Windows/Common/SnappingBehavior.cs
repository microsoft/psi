// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    /// <summary>
    /// Defines various timeline snapping behaviors.
    /// </summary>
    public enum SnappingBehavior
    {
        /// <summary>
        /// Snap to nearest message.
        /// </summary>
        Nearest,

        /// <summary>
        /// Snap to nearest previous message.
        /// </summary>
        Previous,

        /// <summary>
        /// Snap to nearest next message.
        /// </summary>
        Next,
    }
}
