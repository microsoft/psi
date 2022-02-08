// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Component that visually renders a list of 3D rectangles.
    /// </summary>
    public class Rectangle3DListStereoKitRenderer : ModelBasedStereoKitRenderer, IConsumer<List<Rectangle3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3DListStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="color">Material color.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="wireframe">Whether to render as model as wireframe only.</param>
        public Rectangle3DListStereoKitRenderer(Pipeline pipeline, CoordinateSystem pose, System.Drawing.Color color, bool visible = true, bool wireframe = false)
            : base(pipeline, pose, new Vector3D(1, 1, 1), color, visible, wireframe)
        {
            this.In = pipeline.CreateReceiver<List<Rectangle3D>>(this, this.UpdateRectangles, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3DListStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="visible">Initial visibility.</param>
        public Rectangle3DListStereoKitRenderer(Pipeline pipeline, bool visible = true)
            : this(pipeline, new CoordinateSystem(), System.Drawing.Color.White, visible)
        {
        }

        /// <summary>
        /// Gets the receiver for rectangles.
        /// </summary>
        public Receiver<List<Rectangle3D>> In { get; private set; }

        /// <inheritdoc />
        public override bool Initialize()
        {
            base.Initialize();
            this.Material.FaceCull = Cull.None;
            return true;
        }

        private static Vertex[] ConvertToStereoKitVertices(Rectangle3D rect)
        {
            // Convert rectangle points and normal into StereoKit vertices. We only need to change basis,
            // and do not need to change from world to StereoKit coordinates, because that is already done
            // by the parent Model that these mesh vertices will be attached to.
            var normal = (rect.BottomRight - rect.BottomLeft).CrossProduct(rect.TopLeft - rect.BottomLeft).Normalize();
            var stereoKitNormal = normal.ToPoint3D().ToVec3(false);

            return new Vertex[]
            {
                new Vertex(rect.TopLeft.ToVec3(false), stereoKitNormal),
                new Vertex(rect.TopRight.ToVec3(false), stereoKitNormal),
                new Vertex(rect.BottomLeft.ToVec3(false), stereoKitNormal),
                new Vertex(rect.BottomRight.ToVec3(false), stereoKitNormal),
            };
        }

        private void UpdateRectangles(List<Rectangle3D> rectangles)
        {
            static Mesh ToQuadMesh(Rectangle3D rect)
            {
                var mesh = new Mesh();
                mesh.SetVerts(ConvertToStereoKitVertices(rect));
                mesh.SetInds(new uint[] { 3, 1, 0, 2, 3, 0 }); // two triangles from corner vertices
                return mesh;
            }

            var model = new Model();
            foreach (var rect in rectangles)
            {
                model.AddNode(null, Matrix.Identity, ToQuadMesh(rect), this.Material);
            }

            this.Model = model;
        }
    }
}
