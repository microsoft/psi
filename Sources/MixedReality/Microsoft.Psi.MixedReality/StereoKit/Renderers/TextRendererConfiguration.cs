// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Represents the configuration for a <see cref="TextRenderer"/> component.
    /// </summary>
    /// <remarks>This configuration object is used when initializing a new renderer instance, but many of the parameters can
    /// be subsequently changed based on streaming inputs to the component.</remarks>
    public class TextRendererConfiguration
    {
        /// <summary>
        /// Gets or sets the text to render.
        /// </summary>
        public string Text { get; set; } = null;

        /// <summary>
        /// Gets or sets the pose of the text.
        /// </summary>
        public CoordinateSystem Pose { get; set; } = new CoordinateSystem();

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        public Color TextColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the font to use for the text style.
        /// </summary>
        public Font TextFont { get; set; } = Font.Default;

        /// <summary>
        /// Gets or sets how the text's bounding rectangle should be positioned relative to the overall pose.
        /// </summary>
        public TextAlign TextPosition { get; set; } = TextAlign.Center;

        /// <summary>
        /// Gets or sets how the text should be aligned within the text's bounding rectangle.
        /// </summary>
        public TextAlign TextAlign { get; set; } = TextAlign.Center;

        /// <summary>
        /// Gets or sets the desired size (width, height) to draw the text (in meters). If null, the size is auto computed.
        /// </summary>
        public Vec2? DesiredTextSize { get; set; } = null;

        /// <summary>
        /// Gets or sets how the text should behave when one of its size dimensions conflicts with the provided <see cref="DesiredTextSize"/> parameter.
        /// </summary>
        public TextFit TextFit { get; set; } = TextFit.Exact;

        /// <summary>
        /// Gets or sets a value indicating whether to draw a billboard behind the text.
        /// </summary>
        public bool DrawBillboardFill { get; set; } = false;

        /// <summary>
        /// Gets or sets the depth offset from the billboard that text should be drawn (in meters).
        /// </summary>
        /// <remarks> When set high enough, alleviates weird aliasing effects of the text intersecting with the billboard fill (if drawn).
        /// The farther away the user is, the more likely the text will render intersected with the billboard fill.
        /// </remarks>
        public float TextOffsetFromBillboard { get; set; } = 0.01f;

        /// <summary>
        /// Gets or sets the size (width, height) to draw the billboard (in meters).
        /// </summary>
        public Vec2 BillboardSize { get; set; } = Vec2.Zero;

        /// <summary>
        /// Gets or sets the fill color of the billboard.
        /// </summary>
        public Color BillboardFillColor { get; set; } = Color.Black;

        /// <summary>
        /// Gets or sets a value indicating whether to draw a border around the billboard.
        /// </summary>
        public bool DrawBillboardBorder { get; set; } = false;

        /// <summary>
        /// Gets or sets the color of the billboard border.
        /// </summary>
        public Color BillboardBorderColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the thickness of the billboard border (in meters).
        /// </summary>
        public float BillboardBorderThickness { get; set; } = 0.01f;

        /// <summary>
        /// Gets or sets a value indicating whether or not the renderer should be visible.
        /// </summary>
        public bool Visible { get; set; } = true;
    }
}