// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    /// <summary>
    /// Defines various modes for finding a nearest message to a specified time.
    /// </summary>
    public enum NearestMessageType
    {
        /// <summary>
        /// The nearest message.
        /// </summary>
        Nearest,

        /// <summary>
        /// The nearest previous message.
        /// </summary>
        Previous,

        /// <summary>
        /// The nearest next message.
        /// </summary>
        Next,
    }
}
