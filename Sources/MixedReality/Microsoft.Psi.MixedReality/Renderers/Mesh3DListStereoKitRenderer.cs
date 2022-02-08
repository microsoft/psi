// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Component that visually renders a list of meshes.
    /// </summary>
    public class Mesh3DListStereoKitRenderer : ModelBasedStereoKitRenderer, IConsumer<List<Mesh3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3DListStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="scale">Geometry scale.</param>
        /// <param name="color">Material color.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="wireframe">Whether to render as model as wireframe only.</param>
        public Mesh3DListStereoKitRenderer(Pipeline pipeline, CoordinateSystem pose, Vector3D scale, System.Drawing.Color color, bool visible = true, bool wireframe = false)
            : base(pipeline, pose, scale, color, visible, wireframe)
        {
            this.In = pipeline.CreateReceiver<List<Mesh3D>>(this, this.UpdateMeshes, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3DListStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="wireframe">Whether to render as model as wireframe only.</param>
        public Mesh3DListStereoKitRenderer(Pipeline pipeline, bool visible = true, bool wireframe = true)
            : this(pipeline, new CoordinateSystem(), new Vector3D(1, 1, 1), System.Drawing.Color.White, visible, wireframe)
        {
        }

        /// <summary>
        /// Gets the receiver for meshes.
        /// </summary>
        public Receiver<List<Mesh3D>> In { get; private set; }

        /// <inheritdoc />
        public override bool Initialize()
        {
            base.Initialize();
            this.Material.FaceCull = Cull.None;
            return true;
        }

        private void UpdateMeshes(List<Mesh3D> meshes)
        {
            static Mesh ToStereoKitMesh(Mesh3D mesh3d)
            {
                var verts = mesh3d.Vertices.Select(v => new Vertex(new Vec3((float)-v.Y, (float)v.Z, (float)-v.X), Vec3.One)).ToArray(); // TODO: surface normal?
                var mesh = new Mesh();
                mesh.SetInds(mesh3d.TriangleIndices);
                mesh.SetVerts(verts);
                return mesh;
            }

            var model = new Model();
            foreach (var mesh in meshes)
            {
                model.AddNode(null, Matrix.Identity, ToStereoKitMesh(mesh), this.Material);
            }

            this.Model = model;
        }
    }
}
