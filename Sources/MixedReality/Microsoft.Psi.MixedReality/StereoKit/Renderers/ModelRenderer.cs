// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Component that visually renders a <see cref="global::StereoKit.Model"/>.
    /// </summary>
    public class ModelRenderer : StereoKitRenderer
    {
        private Matrix pose = Matrix.Identity;
        private Matrix scale = Matrix.Identity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="model">The model to render.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="scale">Geometry scale.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public ModelRenderer(Pipeline pipeline, Model model, CoordinateSystem pose = null, Vector3D scale = default, bool visible = true, string name = nameof(ModelRenderer))
            : base(pipeline, name)
        {
            this.Model = model;
            this.IsVisible = visible;

            if (scale == default)
            {
                scale = new Vector3D(1, 1, 1);
            }

            this.ReceivePose(pose);
            this.ReceiveScale(scale);

            this.Pose = pipeline.CreateReceiver<CoordinateSystem>(this, this.ReceivePose, nameof(this.Pose));
            this.Scale = pipeline.CreateReceiver<Vector3D>(this, this.ReceiveScale, nameof(this.Scale));
            this.Visible = pipeline.CreateReceiver<bool>(this, this.ReceiveVisible, nameof(this.Visible));
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
        /// Gets or sets the model to be rendered.
        /// </summary>
        protected Model Model { get; set; } = null;

        /// <summary>
        /// Gets the overall render transform (scale * pose).
        /// </summary>
        protected Matrix RenderTransform { get; private set; } = Matrix.Identity;

        /// <summary>
        /// Gets or sets the transform for drawing the model (relative to the overall render transform).
        /// </summary>
        protected Matrix ModelTransform { get; set; } = Matrix.Identity;

        /// <summary>
        /// Gets a value indicating whether the renderer should be currently visibile or not.
        /// </summary>
        protected bool IsVisible { get; private set; }

        /// <inheritdoc />
        protected override void Render()
        {
            if (this.IsVisible && this.Model is not null)
            {
                Hierarchy.Push(this.RenderTransform);
                this.Model.Draw(this.ModelTransform);
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
    }
}
