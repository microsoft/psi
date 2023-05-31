// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Diagnostics
{
    /// <summary>
    /// Pipeline element kind.
    /// </summary>
    public enum PipelineElementKind
    {
        /// <summary>
        /// Represents a source component.
        /// </summary>
        Source,

        /// <summary>
        /// Represents a purely reactive component.
        /// </summary>
        Reactive,

        /// <summary>
        /// Represents a Connector component.
        /// </summary>
        Connector,

        /// <summary>
        /// Represents a Subpipeline component.
        /// </summary>
        Subpipeline,
    }
}