// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Drawing;
    using Microsoft.Psi.MixedReality.StereoKit;

    /// <summary>
    /// Represents the configuration for the bubble dialog.
    /// </summary>
    public class BubbleDialogUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets the number of history bubbles.
        /// </summary>
        public int HistoryBubbleCount { get; set; } = 2;

        /// <summary>
        /// Gets or sets the width of the dialog.
        /// </summary>
        public float Width { get; set; } = 0.20f;

        /// <summary>
        /// Gets or sets the padding.
        /// </summary>
        public float Padding { get; set; } = 0.005f;

        /// <summary>
        /// Gets or sets the vertical spacing between bubbles.
        /// </summary>
        public float BubbleVerticalSpacing { get; set; } = 0.005f;

        /// <summary>
        /// Gets or sets the maximum width of a bubble.
        /// </summary>
        public float BubbleMaxWidth { get; set; } = 0.15f;

        /// <summary>
        /// Gets or sets the line width of a bubble.
        /// </summary>
        public float BubbleLineWidth { get; set; } = 0.001f;

        /// <summary>
        /// Gets or sets the color of the system bubble.
        /// </summary>
        public StereoKit.Color SystemBubbleColor { get; set; } = Color.FromArgb(128, 84, 80, 80).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the color of the system bubble line.
        /// </summary>
        public StereoKit.Color SystemBubbleLineColor { get; set; } = StereoKit.Color.White;

        /// <summary>
        /// Gets or sets the system bubble text style.
        /// </summary>
        public StereoKit.TextStyle SystemBubbleTextStyle { get; set; } = StereoKit.Text.MakeStyle(StereoKit.Font.Default, 0.005f, StereoKit.Color.White);

        /// <summary>
        /// Gets or sets the color of the user response bubble.
        /// </summary>
        public StereoKit.Color UserResponseBubbleColor { get; set; } = Color.FromArgb(128, 84, 80, 80).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the color of the user response bubble line.
        /// </summary>
        public StereoKit.Color UserResponseBubbleLineColor { get; set; } = StereoKit.Color.White;

        /// <summary>
        /// Gets or sets the user response bubble text style.
        /// </summary>
        public StereoKit.TextStyle UserResponseBubbleTextStyle { get; set; } = StereoKit.Text.MakeStyle(StereoKit.Font.Default, 0.005f, StereoKit.Color.White);

        /// <summary>
        /// Gets or sets the color of the user response set bubble.
        /// </summary>
        public StereoKit.Color UserResponseSetBubbleColor { get; set; } = Color.FromArgb(128, 44, 40, 40).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the color of the user response set bubble line.
        /// </summary>
        public StereoKit.Color UserResponseSetBubbleLineColor { get; set; } = Color.FromArgb(255, 150, 150, 150).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the user response set bubble text style.
        /// </summary>
        public StereoKit.TextStyle UserResponseSetBubbleTextStyle { get; set; } = StereoKit.Text.MakeStyle(StereoKit.Font.Default, 0.005f, Color.FromArgb(255, 150, 150, 150).ToStereoKitColor());
    }
}
