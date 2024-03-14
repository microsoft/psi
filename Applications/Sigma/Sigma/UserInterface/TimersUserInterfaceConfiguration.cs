// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;

    /// <summary>
    /// Represents the configuration for the <see cref="TimerUserInterface"/>.
    /// </summary>
    public class TimersUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether workspaces can be created via UI.
        /// </summary>
        public Color Color { get; set; } = System.Drawing.Color.FromArgb(255, 189, 220, 255).ToStereoKitColor();

        /// <summary>
        /// Gets or sets a value indicating whether workspaces can be created via UI.
        /// </summary>
        public Color OvertimeColor { get; set; } = System.Drawing.Color.Orange.ToStereoKitColor();

        /// <summary>
        /// Gets or sets the editing color.
        /// </summary>
        public Color EditingColor { get; set; } = System.Drawing.Color.Yellow.ToStereoKitColor();

        /// <summary>
        /// Gets or sets the text style for the timer display.
        /// </summary>
        public TextStyle TextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.03f, System.Drawing.Color.FromArgb(255, 189, 220, 255).ToStereoKitColor());

        /// <summary>
        /// Gets or sets the text style for the timer display in overtime mode.
        /// </summary>
        public TextStyle OvertimeTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.03f, System.Drawing.Color.Orange.ToStereoKitColor());

        /// <summary>
        /// Gets or sets the text style for the timer display in editing mode.
        /// </summary>
        public TextStyle EditingTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.03f, System.Drawing.Color.Yellow.ToStereoKitColor());

        /// <summary>
        /// Gets or sets the height of the timer.
        /// </summary>
        public float StickHeight { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the width of the timer.
        /// </summary>
        public float Width { get; set; } = 0.2f;

        /// <summary>
        /// Gets or sets the thickness (m) of editing handle.
        /// </summary>
        public float GraspDistance { get; set; } = 0.05f;
    }
}