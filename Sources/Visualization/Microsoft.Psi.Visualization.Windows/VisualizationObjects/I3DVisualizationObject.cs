// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a 3D visualization object.
    /// </summary>
    public interface I3DVisualizationObject
    {
        /// <summary>
        /// Gets the 3D visuals.
        /// </summary>
        Visual3D Visual3D { get; }
    }
}
