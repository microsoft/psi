// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Collections.Generic;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Represents a scene understanding object collection.
    /// </summary>
    public class SceneObjectCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObjectCollection"/> class.
        /// </summary>
        /// <param name="background">Background object.</param>
        /// <param name="ceiling">Ceiling object.</param>
        /// <param name="floor">Floor object.</param>
        /// <param name="inferred">Inferred object.</param>
        /// <param name="platform">Platform object.</param>
        /// <param name="unknown">Unknown object.</param>
        /// <param name="wall">Wall object.</param>
        /// <param name="world">World object.</param>
        public SceneObjectCollection(
            SceneObject background,
            SceneObject ceiling,
            SceneObject floor,
            SceneObject inferred,
            SceneObject platform,
            SceneObject unknown,
            SceneObject wall,
            SceneObject world)
        {
            this.Background = background;
            this.Ceiling = ceiling;
            this.Floor = floor;
            this.Inferred = inferred;
            this.Platform = platform;
            this.Unknown = unknown;
            this.Wall = wall;
            this.World = world;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneObjectCollection"/> class.
        /// </summary>
        public SceneObjectCollection()
            : this(SceneObject.Empty, SceneObject.Empty, SceneObject.Empty, SceneObject.Empty, SceneObject.Empty, SceneObject.Empty, SceneObject.Empty, SceneObject.Empty)
        {
        }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Background { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Ceiling { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Floor { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Inferred { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Platform { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Unknown { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject Wall { get; set; }

        /// <summary>
        /// Gets or sets the background scene object.
        /// </summary>
        public SceneObject World { get; set; }

        /// <summary>
        /// Represents a scene object.
        /// </summary>
        public class SceneObject
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SceneObject"/> class.
            /// </summary>
            /// <param name="meshes">Meshes.</param>
            /// <param name="colliderMeshes">Collider meshes.</param>
            /// <param name="rectangles">Rectangles.</param>
            /// <param name="placementRectangles">Centermost placement rectangles.</param>
            public SceneObject(List<Mesh3D> meshes, List<Mesh3D> colliderMeshes, List<Rectangle3D> rectangles, List<Rectangle3D?> placementRectangles)
            {
                this.Meshes = meshes;
                this.ColliderMeshes = colliderMeshes;
                this.Rectangles = rectangles;
                this.PlacementRectangles = placementRectangles;
            }

            /// <summary>
            /// Gets empty singleton instance.
            /// </summary>
            public static SceneObject Empty { get; } = new SceneObject(new List<Mesh3D>(), new List<Mesh3D>(), new List<Rectangle3D>(), new List<Rectangle3D?>());

            /// <summary>
            /// Gets the meshes.
            /// </summary>
            public List<Mesh3D> Meshes { get; private set; }

            /// <summary>
            /// Gets the collider meshes.
            /// </summary>
            public List<Mesh3D> ColliderMeshes { get; private set; }

            /// <summary>
            /// Gets the quad rectangles.
            /// </summary>
            public List<Rectangle3D> Rectangles { get; private set; }

            /// <summary>
            /// Gets the centermost placement rectangles.
            /// </summary>
            public List<Rectangle3D?> PlacementRectangles { get; private set; }
        }
    }
}
