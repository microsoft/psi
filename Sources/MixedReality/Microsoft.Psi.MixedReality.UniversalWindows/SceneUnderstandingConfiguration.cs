// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using Microsoft.MixedReality.SceneUnderstanding;

    /// <summary>
    /// The configuration for the <see cref="SceneUnderstanding"/> component.
    /// </summary>
    public class SceneUnderstandingConfiguration
    {
        /// <summary>
        /// Gets or sets the minimum time interval at which to query for scene understanding.
        /// </summary>
        public TimeSpan MinQueryInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the query radius (meters).
        /// </summary>
        public double QueryRadius { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether to enable computation of placement rectangles.
        /// </summary>
        public bool ComputePlacementRectangles { get; set; } = false;

        /// <summary>
        /// Gets or sets the initial size (in meters) of placement rectangles.
        /// </summary>
        public (double Width, double Height) InitialPlacementRectangleSize { get; set; } = (0, 0);

        /// <summary>
        /// Gets or sets the scene query settings.
        /// </summary>
        public SceneQuerySettings SceneQuerySettings { get; set; } = new SceneQuerySettings()
        {
            EnableSceneObjectMeshes = true,
            EnableSceneObjectQuads = true,
            EnableWorldMesh = true,
            EnableOnlyObservedSceneObjects = false,
            RequestedMeshLevelOfDetail = SceneMeshLevelOfDetail.Unlimited,
        };
    }
}