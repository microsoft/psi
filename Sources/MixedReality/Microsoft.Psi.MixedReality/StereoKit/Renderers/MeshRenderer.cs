// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System.IO;
    using System.Reflection;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that visually renders a single <see cref="global::StereoKit.Mesh"/>.
    /// </summary>
    public class MeshRenderer : ModelRenderer
    {
        private Mesh mesh = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="mesh">The mesh to render.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="scale">Geometry scale.</param>
        /// <param name="color">Material color.</param>
        /// <param name="wireframe">Whether to render mesh as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public MeshRenderer(Pipeline pipeline, Mesh mesh, CoordinateSystem pose, Vector3D scale, Color color, bool wireframe = false, bool visible = true, string name = nameof(MeshRenderer))
            : base(pipeline, null, pose, scale, visible, name)
        {
            // initialize the material
            this.Material.FaceCull = Cull.None;
            this.ReceiveColor(color);
            this.ReceiveWireframe(wireframe);

            // initialize the mesh
            this.Mesh = mesh;

            this.Color = pipeline.CreateReceiver<Color>(this, this.ReceiveColor, nameof(this.Color));
            this.Wireframe = pipeline.CreateReceiver<bool>(this, this.ReceiveWireframe, nameof(this.Wireframe));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeshRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="mesh">The mesh to render.</param>
        /// <param name="color">Material color.</param>
        /// <param name="wireframe">Whether to render mesh as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public MeshRenderer(Pipeline pipeline, Mesh mesh, Color color, bool wireframe = false, bool visible = true, string name = nameof(MeshRenderer))
            : this(pipeline, mesh, null, new Vector3D(1, 1, 1), color, wireframe, visible, name)
        {
        }

        /// <summary>
        /// Gets receiver for material color.
        /// </summary>
        public Receiver<Color> Color { get; private set; }

        /// <summary>
        /// Gets receiver for wireframe indicator.
        /// </summary>
        public Receiver<bool> Wireframe { get; private set; }

        /// <summary>
        /// Gets or sets the mesh to be rendered.
        /// </summary>
        protected Mesh Mesh
        {
            get => this.mesh;
            set
            {
                this.mesh = value;

                if (this.mesh is not null)
                {
                    this.Model = Model.FromMesh(this.mesh, this.Material);
                }
            }
        }

        /// <summary>
        /// Gets or sets the material used for rendering the mesh.
        /// </summary>
        protected Material Material { get; set; } = Default.Material.Copy();

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

        private void ReceiveColor(Color color)
        {
            this.Material[MatParamName.ColorTint] = color.ToStereoKitColor();
        }

        private void ReceiveWireframe(bool wireframe)
        {
            this.Material.Wireframe = wireframe;
        }
    }
}
