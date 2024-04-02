// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a step panel.
    /// </summary>
    public class DoStepPanel : StepPanel
    {
        private readonly float padding;
        private readonly TextStyle instructionsTextStyle;
        private readonly LabelCircle labelCircle;
        private readonly Paragraph instructionsParagraph = default;

        private string label = string.Empty;
        private string instructions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoStepPanel"/> class.
        /// </summary>
        /// <param name="width">The width of the step panel.</param>
        /// <param name="padding">The padding for the step panel.</param>
        /// <param name="labelCircleColor">The label circle color.</param>
        /// <param name="instructionsTextStyle">The instructions text style.</param>
        /// <param name="name">An optional name for the step panel.</param>
        public DoStepPanel(float width, float padding, Color labelCircleColor, TextStyle instructionsTextStyle, string name = nameof(DoStepPanel))
            : base(name)
        {
            this.Width = width;
            this.padding = padding;
            this.instructionsTextStyle = instructionsTextStyle;

            this.labelCircle = new LabelCircle(padding, labelCircleColor, instructionsTextStyle, $"{this.Name}.LabelCircle");
            this.instructionsParagraph = new Paragraph($"{this.Name}.InstructionsParagraph");
        }

        /// <summary>
        /// Updates the step panel state.
        /// </summary>
        /// <param name="label">The label for the step.</param>
        /// <param name="instructions">The instructions for the step.</param>
        public void Update(string label, string instructions)
        {
            // If nothing has changed
            if (this.label == label &&
                this.instructions == instructions)
            {
                // Simply return
                return;
            }
            else
            {
                this.label = label;
                this.instructions = instructions;
            }

            // Update the label
            this.labelCircle.Update(label);

            // Update the step instructions paragraph
            this.instructionsParagraph.Update(
                this.instructions,
                this.instructionsTextStyle,
                this.Width - this.padding - this.labelCircle.Radius * 2,
                bottomMargin: 0.002f,
                rightMargin: this.padding,
                leftMargin: this.padding);

            // Compute the height
            this.Height = (float)Math.Max(2 * this.padding + this.labelCircle.Radius - this.instructionsTextStyle.CharHeight / 2 + this.instructionsParagraph.Height, this.labelCircle.Height);
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // render the label circle
            this.labelCircle.Render(renderer, pose);

            // Render the step instructions
            if (!string.IsNullOrEmpty(this.instructions))
            {
                this.instructionsParagraph.Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2, this.padding + this.labelCircle.Radius - this.instructionsTextStyle.CharHeight / 2));
            }

            return this.GetUserInterfaceState(pose);
        }
    }
}
