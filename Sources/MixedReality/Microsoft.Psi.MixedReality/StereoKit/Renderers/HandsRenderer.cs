// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Component that controls rendering of the hands in StereoKit.
    /// </summary>
    public class HandsRenderer
    {
        private readonly string name;
        private readonly Material material = Default.MaterialHand;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandsRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="visible">Initial value indicating whether hands should be rendered (default true).</param>
        /// <param name="solid">Initial value indicating whether hands participate in StereoKit physics (default false).</param>
        /// <param name="name">An optional name for the component.</param>
        public HandsRenderer(Pipeline pipeline, bool visible = true, bool solid = false, string name = nameof(HandsRenderer))
        {
            this.name = name;
            this.ReceiveVisible(visible);
            this.ReceiveSolid(solid);

            this.Visible = pipeline.CreateReceiver<bool>(this, this.ReceiveVisible, nameof(this.Visible));
            this.Solid = pipeline.CreateReceiver<bool>(this, this.ReceiveSolid, nameof(this.Solid));
            this.Color = pipeline.CreateReceiver<Color>(this, this.ReceiveColor, nameof(this.Color));
        }

        /// <summary>
        /// Gets the receiver of a value indicating whether hands should be rendered.
        /// </summary>
        public Receiver<bool> Visible { get; }

        /// <summary>
        /// Gets the receiver of a value indicating whether hands participate in the StereoKit physics system.
        /// </summary>
        public Receiver<bool> Solid { get; }

        /// <summary>
        /// Gets the receiver of the color used to render the hands.
        /// </summary>
        public Receiver<Color> Color { get; }

        /// <inheritdoc />
        public override string ToString() => this.name;

        private void ReceiveColor(Color color)
        {
            this.material[MatParamName.ColorTint] = color.ToStereoKitColor();
            Input.HandMaterial(Handed.Max, this.material);
        }

        private void ReceiveVisible(bool visible)
        {
            Input.HandVisible(Handed.Max, visible);
        }

        private void ReceiveSolid(bool solid)
        {
            Input.HandSolid(Handed.Max, solid);
        }
    }
}
