// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;

    /// <summary>
    /// Represents the configuration for the <see cref="TaskPanelUserInterface{TTask}"/>.
    /// </summary>
    public class TaskPanelUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether workspaces can be created via UI.
        /// </summary>
        public Color Color { get; set; } = System.Drawing.Color.FromArgb(128, 0, 96, 168).ToStereoKitColor();

        /// <summary>
        /// Gets or sets a value indicating whether workspaces can be created via UI.
        /// </summary>
        public Color AccentColor { get; set; } = System.Drawing.Color.FromArgb(255, 189, 220, 255).ToStereoKitColor();

        /// <summary>
        /// Gets or sets a value indicating whether workspaces can be created via UI.
        /// </summary>
        public Color SelectionColor { get; set; } = System.Drawing.Color.FromArgb(128, 9, 150, 255).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the minimal distance for grasping the handles.
        /// </summary>
        public float Width { get; set; } = 0.20f;

        /// <summary>
        /// Gets or sets the thickness (m) of editing handle.
        /// </summary>
        public float Height { get; set; } = 0.18f;

        /// <summary>
        /// Gets or sets the text style for the task name.
        /// </summary>
        public TextStyle TaskNameTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.007f, Color.White);

        /// <summary>
        /// Gets or sets the text style for the task list.
        /// </summary>
        public TextStyle TaskListTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, Color.White);

        /// <summary>
        /// Gets or sets the text style for the step instructions.
        /// </summary>
        public TextStyle StepInstructionsTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, Color.White);

        /// <summary>
        /// Gets or sets the object color for the gather step.
        /// </summary>
        public Color GatherStepObjectColor { get; set; } = System.Drawing.Color.FromArgb(255, 200, 200, 200).ToStereoKitColor();

        /// <summary>
        /// Gets or sets the object text style for the gather step.
        /// </summary>
        public TextStyle GatherStepObjectTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, System.Drawing.Color.FromArgb(255, 200, 200, 200).ToStereoKitColor());

        /// <summary>
        /// Gets or sets the highlight object color for the gather step.
        /// </summary>
        public Color GatherStepHighlightObjectColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the highlight object text style for the gather step.
        /// </summary>
        public TextStyle GatherStepHighlightObjectTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, Color.White);

        /// <summary>
        /// Gets or sets the text style for the complex step object.
        /// </summary>
        public TextStyle ComplexStepObjectTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, System.Drawing.Color.FromArgb(255, 200, 200, 200).ToStereoKitColor());

        /// <summary>
        /// Gets or sets the text style for a taught complex step object.
        /// </summary>
        public TextStyle ComplexStepTaughtObjectTextStyle { get; set; } = Text.MakeStyle(Font.Default, 0.005f, Color.White);

        /// <summary>
        /// Gets or sets the padding.
        /// </summary>
        public float Padding { get; set; } = 0.005f;
    }
}