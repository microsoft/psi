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
    public class MeshRenderer : StereoKitRenderer
    {
        private Matrix pose = Matrix.Identity;
        private Matrix scale = Matrix.Identity;

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
            : base(pipeline, name)
        {
            this.Mesh = mesh;
            this.ReceivePose(pose);
            this.ReceiveScale(scale);

            // initialize the material
            this.Material = Default.Material.Copy();
            this.Material.FaceCull = Cull.None;
            this.ReceiveColor(color);
            this.ReceiveWireframe(wireframe);
            this.IsVisible = visible;

            this.Pose = pipeline.CreateReceiver<CoordinateSystem>(this, this.ReceivePose, nameof(this.Pose));
            this.Scale = pipeline.CreateReceiver<Vector3D>(this, this.ReceiveScale, nameof(this.Scale));
            this.Visible = pipeline.CreateReceiver<bool>(this, this.ReceiveVisible, nameof(this.Visible));
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
        /// Gets receiver for geometry pose (in \psi basis).
        /// </summary>
        public Receiver<CoordinateSystem> Pose { get; private set; }

        /// <summary>
        /// Gets receiver for geometry scale (in \psi basis).
        /// </summary>
        public Receiver<Vector3D> Scale { get; private set; }

        /// <summary>
        /// Gets receiver for visibility.
        /// </summary>
        public Receiver<bool> Visible { get; private set; }

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
        protected Mesh Mesh { get; set; } = null;

        /// <summary>
        /// Gets the overall render transform (scale * pose).
        /// </summary>
        protected Matrix RenderTransform { get; private set; } = Matrix.Identity;

        /// <summary>
        /// Gets or sets the transform for drawing the mesh (relative to the overall render transform).
        /// </summary>
        protected Matrix MeshTransform { get; set; } = Matrix.Identity;

        /// <summary>
        /// Gets the material used for rendering the mesh.
        /// </summary>
        protected Material Material { get; }

        /// <summary>
        /// Gets a value indicating whether the renderer should be currently visibile or not.
        /// </summary>
        protected bool IsVisible { get; private set; }

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
        protected override void Render()
        {
            if (this.IsVisible && this.Mesh is not null)
            {
                Hierarchy.Push(this.RenderTransform);
                this.Mesh.Draw(this.Material, this.MeshTransform);
                Hierarchy.Pop();
            }
        }

        /// <summary>
        /// Update visibility.
        /// </summary>
        /// <param name="visible">Desired visibility.</param>
        /// <param name="envelope">Message envelope.</param>
        protected virtual void ReceiveVisible(bool visible, Envelope envelope)
        {
            this.IsVisible = visible;
        }

        private void ReceivePose(CoordinateSystem pose)
        {
            this.pose = pose is null ? Matrix.Identity : pose.ToStereoKitMatrix();
            this.RenderTransform = this.scale * this.pose;
        }

        private void ReceiveScale(Vector3D scale)
        {
            this.scale = Matrix.S(new Vec3((float)scale.Y, (float)scale.Z, (float)scale.X));
            this.RenderTransform = this.scale * this.pose;
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
