// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System.Linq;
    using global::StereoKit;
    using Microsoft.Psi.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that visually renders a <see cref="Mesh3D"/>.
    /// </summary>
    public class Mesh3DRenderer : MeshRenderer, IConsumer<Mesh3D>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="color">Mesh color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Mesh3DRenderer(Pipeline pipeline, Color color, bool wireframe = false, bool visible = true, string name = nameof(Mesh3DRenderer))
            : base(pipeline, null, color, wireframe, visible, name)
        {
            this.In = pipeline.CreateReceiver<Mesh3D>(this, this.UpdateMesh, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="mesh3D">Mesh to render.</param>
        /// <param name="color">Mesh color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Mesh3DRenderer(Pipeline pipeline, Mesh3D mesh3D, Color color, bool wireframe = false, bool visible = true, string name = nameof(Mesh3DRenderer))
            : this(pipeline, color, wireframe, visible, name)
        {
            this.UpdateMesh(mesh3D);
        }

        /// <summary>
        /// Gets the receiver for the mesh.
        /// </summary>
        public Receiver<Mesh3D> In { get; private set; }

        private void UpdateMesh(Mesh3D mesh3D)
        {
            this.Mesh ??= new Mesh();
            this.Mesh.SetVerts(mesh3D.Vertices.Select(p => new Vertex(p.ToVec3(), Vec3.One)).ToArray());
            this.Mesh.SetInds(mesh3D.TriangleIndices);
        }
    }
}
