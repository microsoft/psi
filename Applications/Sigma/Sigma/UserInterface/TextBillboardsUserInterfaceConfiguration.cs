// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;

    /// <summary>
    /// Represents the configuration for the <see cref="TextBillboardUserInterface"/>.
    /// </summary>
    public class TextBillboardsUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets the text style for the text display.
        /// </summary>
        public TextStyle TextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.03f, System.Drawing.Color.FromArgb(255, 189, 220, 255).ToStereoKitColor());

        /// <summary>
        /// Gets or sets the width of the text display.
        /// </summary>
        public float Width { get; set; } = 0.2f;
    }
}