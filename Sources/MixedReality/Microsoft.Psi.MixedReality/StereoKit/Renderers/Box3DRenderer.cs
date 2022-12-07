// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that visually renders a <see cref="Box3D"/>.
    /// </summary>
    public class Box3DRenderer : MeshRenderer, IConsumer<Box3D>
    {
        private readonly bool roundedEdges = false;
        private readonly float roundedEdgeRadius;

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="color">Box color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Box3DRenderer(Pipeline pipeline, Color color, bool wireframe = false, bool visible = true, string name = nameof(Box3DRenderer))
            : base(pipeline, null, color, wireframe, visible, name)
        {
            this.In = pipeline.CreateReceiver<Box3D>(this, this.UpdateMesh, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="box3D">Box to render.</param>
        /// <param name="color">Box color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Box3DRenderer(Pipeline pipeline, Box3D box3D, Color color, bool wireframe = false, bool visible = true, string name = nameof(Box3DRenderer))
            : this(pipeline, color, wireframe, visible, name)
        {
            this.UpdateMesh(box3D);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="color">Box color.</param>
        /// <param name="roundedEdgeRadius">Render with rounded edges, with given radius.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Box3DRenderer(Pipeline pipeline, Color color, float roundedEdgeRadius, bool wireframe = false, bool visible = true, string name = nameof(Box3DRenderer))
            : this(pipeline, color, wireframe, visible, name)
        {
            this.roundedEdges = true;
            this.roundedEdgeRadius = roundedEdgeRadius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="box3D">Box to render.</param>
        /// <param name="color">Box color.</param>
        /// <param name="roundedEdgeRadius">Render with rounded edges, with given radius.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Box3DRenderer(Pipeline pipeline, Box3D box3D, Color color, float roundedEdgeRadius, bool wireframe = false, bool visible = true, string name = nameof(Box3DRenderer))
            : this(pipeline, color, wireframe, visible, name)
        {
            this.roundedEdges = true;
            this.roundedEdgeRadius = roundedEdgeRadius;
            this.UpdateMesh(box3D);
        }

        /// <summary>
        /// Gets the receiver for the box to render.
        /// </summary>
        public Receiver<Box3D> In { get; private set; }

        private void UpdateMesh(Box3D box3D)
        {
            this.Mesh ??= this.roundedEdges ? Mesh.GenerateRoundedCube(Vec3.One, this.roundedEdgeRadius) : Mesh.Cube;

            var scale = Matrix.S(new Vec3((float)box3D.LengthY, (float)box3D.LengthZ, (float)box3D.LengthX));
            var pose = new CoordinateSystem(box3D.Center, box3D.XAxis, box3D.YAxis, box3D.ZAxis);
            this.MeshTransform = scale * pose.ToStereoKitMatrix();
        }
    }
}
