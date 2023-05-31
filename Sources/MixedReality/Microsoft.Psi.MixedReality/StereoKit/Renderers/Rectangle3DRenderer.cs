// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using Microsoft.Psi.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that visually renders a <see cref="Rectangle3D"/>.
    /// </summary>
    public class Rectangle3DRenderer : MeshRenderer, IConsumer<Rectangle3D>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="color">Rectangle color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Rectangle3DRenderer(Pipeline pipeline, Color color, bool wireframe = false, bool visible = true, string name = nameof(Rectangle3DRenderer))
            : base(pipeline, null, color, wireframe, visible, name)
        {
            this.In = pipeline.CreateReceiver<Rectangle3D>(this, this.UpdateMesh, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3DRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="rectangle3D">Rectangle to render.</param>
        /// <param name="color">Rectangle color.</param>
        /// <param name="wireframe">Whether to render as wireframe only.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public Rectangle3DRenderer(Pipeline pipeline, Rectangle3D rectangle3D, Color color, bool wireframe = false, bool visible = true, string name = nameof(Rectangle3DRenderer))
            : this(pipeline, color, wireframe, visible, name)
        {
            this.UpdateMesh(rectangle3D);
        }

        /// <summary>
        /// Gets the receiver for the rectangle to render.
        /// </summary>
        public Receiver<Rectangle3D> In { get; private set; }

        private void UpdateMesh(Rectangle3D rectangle3D)
        {
            this.Mesh ??= Mesh.Quad;

            var pose = rectangle3D.GetCenteredCoordinateSystem().ToStereoKitMatrix();
            var scale = Matrix.S(new Vec3((float)rectangle3D.Width, (float)rectangle3D.Height, 1));
            this.MeshTransform = scale * pose;
        }
    }
}
