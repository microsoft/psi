// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.IO;
    using System.Reflection;
    using MathNet.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Component that visually renders a mesh.
    /// </summary>
    public class MeshStereoKitRenderer : ModelBasedStereoKitRenderer
    {
        private readonly Mesh mesh;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="mesh">Geometry mesh.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="scale">Geometry scale.</param>
        /// <param name="color">Material color.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="wireframe">Whether to render as model as wireframe only.</param>
        public MeshStereoKitRenderer(Pipeline pipeline, Mesh mesh, CoordinateSystem pose, Vector3D scale, System.Drawing.Color color, bool visible = true, bool wireframe = false)
            : base(pipeline, pose, scale, color, visible, wireframe)
        {
            this.mesh = mesh;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="mesh">Geometry mesh.</param>
        public MeshStereoKitRenderer(Pipeline pipeline, Mesh mesh)
            : this(pipeline, mesh, new CoordinateSystem(), new Vector3D(1, 1, 1), System.Drawing.Color.White)
        {
        }

        /// <summary>
        /// Get a mesh from an embedded resource asset.
        /// </summary>
        /// <param name="name">Name of resource.</param>
        /// <returns>StereoKit Mesh.</returns>
        public static Mesh CreateMeshFromEmbeddedResource(string name)
        {
            using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(name);
            using var mem = new MemoryStream();
            stream.CopyTo(mem);
            return Model.FromMemory(name, mem.ToArray()).Visuals[0].Mesh;
        }

        /// <inheritdoc />
        public override bool Initialize()
        {
            base.Initialize();
            this.Model = Model.FromMesh(this.mesh, this.Material);
            return true;
        }
    }
}
