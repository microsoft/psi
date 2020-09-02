// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    /// <summary>
    /// Represents cursor modes.
    /// </summary>
    public enum CursorMode
    {
        /// <summary>
        /// The user can manually place the cursor anywhere on a timeline with the mouse, and can scrub the timeline to any desired position
        /// </summary>
        Manual,

        /// <summary>
        /// The cursor is driven by the playback timer playing back an existing stream
        /// </summary>
        Playback,

        /// <summary>
        /// The cursor is driven by new messages being written to a live store.
        /// </summary>
        Live,
    }
}