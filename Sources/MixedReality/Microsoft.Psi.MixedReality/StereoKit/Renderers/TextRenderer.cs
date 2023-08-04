// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that visually renders text.
    /// </summary>
    public class TextRenderer : StereoKitRenderer
    {
        private readonly Model billboardFillModel;
        private readonly Material billboardFillMaterial;
        private readonly (Vec3 startPoint, Vec3 endPoint)[] billboardBorderLines;
        private readonly TextRendererConfiguration configuration;

        private Matrix pose;
        private TextStyle textStyle;
        private global::StereoKit.Color billboardBorderColor;
        private Matrix billboardFillTransform;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Configuration to use.</param>
        /// <param name="name">An optional name for the component.</param>
        public TextRenderer(Pipeline pipeline, TextRendererConfiguration configuration = null, string name = nameof(TextRenderer))
            : base(pipeline, name)
        {
            this.configuration = configuration ?? new TextRendererConfiguration();
            this.pose = this.configuration.Pose.ToStereoKitMatrix();

            this.Pose = pipeline.CreateReceiver<CoordinateSystem>(this, this.ReceivePose, nameof(this.Pose));
            this.Visible = pipeline.CreateReceiver<bool>(this, v => this.configuration.Visible = v, nameof(this.Visible));
            this.BillboardSize = pipeline.CreateReceiver<Vec2>(this, this.ReceiveBillboardSize, nameof(this.BillboardSize));
            this.Text = pipeline.CreateReceiver<string>(this, t => this.configuration.Text = t, nameof(this.Text));
            this.TextColor = pipeline.CreateReceiver<Color>(this, this.ReceiveTextColor, nameof(this.TextColor));

            if (this.configuration.DrawBillboardFill)
            {
                // Initialize the billboard fill model and material
                this.billboardFillMaterial = Material.Default.Copy();
                this.billboardFillMaterial[MatParamName.ColorTint] = this.configuration.BillboardFillColor.ToStereoKitColor();
                this.billboardFillMaterial.FaceCull = Cull.None;
                this.billboardFillModel = Model.FromMesh(Mesh.Quad, this.billboardFillMaterial);

                this.BillboardFillColor = pipeline.CreateReceiver<Color>(this, this.ReceiveBillboardFillColor, nameof(this.BillboardFillColor));
            }

            if (this.configuration.DrawBillboardBorder)
            {
                this.billboardBorderLines = new (Vec3, Vec3)[4];
                this.billboardBorderColor = this.configuration.BillboardBorderColor.ToStereoKitColor();
                this.BillboardBorderColor = pipeline.CreateReceiver<Color>(this, this.ReceiveBillboardBorderColor, nameof(this.BillboardBorderColor));
            }

            this.UpdateTextStyle();
            this.UpdateBillboardSize();
        }

        /// <summary>
        /// Gets receiver for visibility.
        /// </summary>
        public Receiver<bool> Visible { get; private set; }

        /// <summary>
        /// Gets receiver for text pose (in \psi basis).
        /// </summary>
        public Receiver<CoordinateSystem> Pose { get; private set; }

        /// <summary>
        /// Gets the receiver for the text content.
        /// </summary>
        public Receiver<string> Text { get; }

        /// <summary>
        /// Gets the receiver for the text color.
        /// </summary>
        public Receiver<Color> TextColor { get; }

        /// <summary>
        /// Gets the receiver for billboard fill color.
        /// </summary>
        public Receiver<Color> BillboardFillColor { get; }

        /// <summary>
        /// Gets the receiver for billboard border color.
        /// </summary>
        public Receiver<Color> BillboardBorderColor { get; }

        /// <summary>
        /// Gets the receiver for the billboard size (width and height).
        /// </summary>
        public Receiver<Vec2> BillboardSize { get; }

        /// <inheritdoc />
        protected override void Render()
        {
            if (this.configuration.Visible)
            {
                Hierarchy.Push(this.pose);

                if (this.configuration.DrawBillboardFill)
                {
                    this.billboardFillModel.Draw(this.billboardFillTransform);
                }

                if (this.configuration.DrawBillboardBorder)
                {
                    foreach (var (lineStart, lineEnd) in this.billboardBorderLines)
                    {
                        Lines.Add(lineStart, lineEnd, this.billboardBorderColor, this.configuration.BillboardBorderThickness);
                    }
                }

                if (!string.IsNullOrEmpty(this.configuration.Text))
                {
                    // Render the text
                    global::StereoKit.Text.Add(
                            this.configuration.Text,
                            Matrix.Identity,
                            this.configuration.DesiredTextSize ?? global::StereoKit.Text.Size(this.configuration.Text),
                            this.configuration.TextFit,
                            this.textStyle,
                            this.configuration.TextPosition,
                            this.configuration.TextAlign,
                            offZ: this.configuration.DrawBillboardFill ? -this.configuration.TextOffsetFromBillboard : 0);
                }

                Hierarchy.Pop();
            }
        }

        private void ReceiveBillboardBorderColor(Color color)
        {
            this.configuration.BillboardBorderColor = color;
            this.billboardBorderColor = this.configuration.BillboardBorderColor.ToStereoKitColor();
        }

        private void ReceiveBillboardFillColor(Color color)
        {
            this.configuration.BillboardFillColor = color;
            this.billboardFillMaterial[MatParamName.ColorTint] = this.configuration.BillboardFillColor.ToStereoKitColor();
            this.billboardFillModel.Visuals[0].Material = this.billboardFillMaterial;
        }

        private void ReceiveTextColor(Color color)
        {
            this.configuration.TextColor = color;
            this.UpdateTextStyle();
        }

        private void UpdateTextStyle()
        {
            var colorGamma = this.configuration.TextColor.ToStereoKitColor().ToGamma();
            this.textStyle = global::StereoKit.Text.MakeStyle(this.configuration.TextFont, TextStyle.Default.CharHeight, colorGamma);
        }

        private void ReceiveBillboardSize(Vec2 size)
        {
            this.configuration.BillboardSize = size;
            this.UpdateBillboardSize();
        }

        private void UpdateBillboardSize()
        {
            if (this.configuration.DrawBillboardFill)
            {
                this.billboardFillTransform = Matrix.S(new Vec3(this.configuration.BillboardSize.x, this.configuration.BillboardSize.y, 1));
            }

            if (this.configuration.DrawBillboardBorder)
            {
                // Initialize the billboard border
                var halfWidth = 0.5f * this.configuration.BillboardSize.x;
                var halfHeight = 0.5f * this.configuration.BillboardSize.y;
                var halfThickness = 0.5f * this.configuration.BillboardBorderThickness;

                // bottom edge
                var p1 = new Vec3(-halfWidth - this.configuration.BillboardBorderThickness, -halfHeight - halfThickness, 0);
                var p2 = new Vec3(halfWidth + this.configuration.BillboardBorderThickness, -halfHeight - halfThickness, 0);

                // right edge
                var p3 = new Vec3(halfWidth + halfThickness, -halfHeight - this.configuration.BillboardBorderThickness, 0);
                var p4 = new Vec3(halfWidth + halfThickness, halfHeight + this.configuration.BillboardBorderThickness, 0);

                // top edge
                var p5 = new Vec3(halfWidth + this.configuration.BillboardBorderThickness, halfHeight + halfThickness, 0);
                var p6 = new Vec3(-halfWidth - this.configuration.BillboardBorderThickness, halfHeight + halfThickness, 0);

                // left edge
                var p7 = new Vec3(-halfWidth - halfThickness, halfHeight + this.configuration.BillboardBorderThickness, 0);
                var p8 = new Vec3(-halfWidth - halfThickness, -halfHeight - this.configuration.BillboardBorderThickness, 0);

                this.billboardBorderLines[0] = (p1, p2); // bottom edge
                this.billboardBorderLines[1] = (p3, p4); // right edge
                this.billboardBorderLines[2] = (p5, p6); // top edge
                this.billboardBorderLines[3] = (p7, p8); // left edge
            }
        }

        private void ReceivePose(CoordinateSystem pose)
        {
            this.configuration.Pose = pose;
            this.pose = pose.ToStereoKitMatrix();
        }
    }
}