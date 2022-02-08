// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using MathNet.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Base class for StereoKit model-based rendering components.
    /// </summary>
    public abstract class ModelBasedStereoKitRenderer : StereoKitComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBasedStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="pose">Geometry pose.</param>
        /// <param name="scale">Geometry scale.</param>
        /// <param name="color">Material color.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="wireframe">Whether to render as model as wireframe only.</param>
        public ModelBasedStereoKitRenderer(Pipeline pipeline, CoordinateSystem pose, Vector3D scale, System.Drawing.Color color, bool visible = true, bool wireframe = false)
            : base(pipeline)
        {
            this.Color = color;
            this.Visible = visible;
            this.Wireframe = wireframe;

            // Convert pose and scale to StereoKit basis.
            this.PoseMatrix = pose.ToStereoKitMatrix();
            this.Scale = new Vec3((float)scale.Y, (float)scale.Z, (float)scale.X);

            this.ColorInput = pipeline.CreateReceiver<System.Drawing.Color>(this, this.ReceiveColor, nameof(this.ColorInput));
            this.PoseInput = pipeline.CreateReceiver<CoordinateSystem>(this, this.ReceivePose, nameof(this.PoseInput));
            this.ScaleInput = pipeline.CreateReceiver<Vector3D>(this, this.ReceiveScale, nameof(this.ScaleInput));
            this.VisibleInput = pipeline.CreateReceiver<bool>(this, this.ReceiveVisible, nameof(this.VisibleInput));
            this.WireframeInput = pipeline.CreateReceiver<bool>(this, this.ReceiveWireframe, nameof(this.WireframeInput));
        }

        /// <summary>
        /// Gets receiver for material color.
        /// </summary>
        public Receiver<System.Drawing.Color> ColorInput { get; private set; }

        /// <summary>
        /// Gets receiver for geometry pose (in \psi basis).
        /// </summary>
        public Receiver<CoordinateSystem> PoseInput { get; private set; }

        /// <summary>
        /// Gets receiver for geometry scale (in \psi basis).
        /// </summary>
        public Receiver<Vector3D> ScaleInput { get; private set; }

        /// <summary>
        /// Gets receiver for visibility.
        /// </summary>
        public Receiver<bool> VisibleInput { get; private set; }

        /// <summary>
        /// Gets receiver for wireframe indicator.
        /// </summary>
        public Receiver<bool> WireframeInput { get; private set; }

        /// <summary>
        /// Gets or sets material.
        /// </summary>
        protected Material Material { get; set; }

        /// <summary>
        /// Gets or sets geometry model.
        /// </summary>
        protected Model Model { get; set; }

        /// <summary>
        /// Gets or sets the model transform.
        /// </summary>
        protected Matrix ModelTransform { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        protected System.Drawing.Color Color { get; set; }

        /// <summary>
        /// Gets or sets the pose as a Matrix (in StereoKit basis).
        /// </summary>
        protected Matrix PoseMatrix { get; set; }

        /// <summary>
        /// Gets or sets the scale (in StereoKit basis).
        /// </summary>
        protected Vec3 Scale { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the renderer is visible.
        /// </summary>
        protected bool Visible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to render the model as wireframe-only.
        /// </summary>
        protected bool Wireframe { get; set; }

        /// <inheritdoc />
        public override bool Initialize()
        {
            this.UpdateMaterial();
            this.UpdateModelTransform();

            return true;
        }

        /// <inheritdoc />
        public override void Step()
        {
            if (this.Visible)
            {
                this.Model?.Draw(this.ModelTransform);
            }
        }

        /// <summary>
        /// Updates the material based on the other properties.
        /// </summary>
        protected virtual void UpdateMaterial()
        {
            this.Material ??= Default.Material.Copy();
            this.Material[MatParamName.ColorTint] = this.Color.ToStereoKitColor();
            this.Material.Wireframe = this.Wireframe;

            if (this.Model != null)
            {
                this.Model.Visuals[0].Material = this.Material;
            }
        }

        /// <summary>
        /// Updates the model transform.
        /// </summary>
        protected virtual void UpdateModelTransform()
        {
            this.ModelTransform = Matrix.S(this.Scale) * this.PoseMatrix;
        }

        private void ReceiveColor(System.Drawing.Color color)
        {
            this.Color = color;
            this.UpdateMaterial();
        }

        private void ReceivePose(CoordinateSystem pose)
        {
            this.PoseMatrix = pose.ToStereoKitMatrix();
            this.UpdateModelTransform();
        }

        private void ReceiveScale(Vector3D scale)
        {
            this.Scale = new Vec3((float)scale.Y, (float)scale.Z, (float)scale.X);
            this.UpdateModelTransform();
        }

        private void ReceiveVisible(bool visible)
        {
            this.Visible = visible;
        }

        private void ReceiveWireframe(bool wireframe)
        {
            this.Wireframe = wireframe;
            this.UpdateMaterial();
        }
    }
}
