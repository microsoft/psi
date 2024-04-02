// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for the gather step.
    /// </summary>
    public class GatherStepPanel : StepPanel
    {
        private readonly float padding;
        private readonly TextStyle instructionsTextStyle;

        private readonly LabelCircle labelCircle;
        private readonly Paragraph instructionsParagraph = default;

        private readonly Color objectColor;
        private readonly TextStyle objectTextStyle;
        private readonly Color highlightObjectColor;
        private readonly TextStyle highlightObjectTextStyle;
        private readonly Dictionary<int, Paragraph> physicalObjectsParagraphs = new ();
        private readonly Dictionary<int, Checkbox> physicalObjectsCheckboxes = new ();

        private string label = string.Empty;
        private string instructions;

        /// <summary>
        /// Initializes a new instance of the <see cref="GatherStepPanel"/> class.
        /// </summary>
        /// <param name="width">The width of the panel.</param>
        /// <param name="padding">The padding for the panel.</param>
        /// <param name="labelColor">The label color.</param>
        /// <param name="instructionsTextStyle">The text style for the instructions.</param>
        /// <param name="objectColor">The objects color.</param>
        /// <param name="objectTextStyle">The object text style.</param>
        /// <param name="highlightObjectColor">The highlighted object color.</param>
        /// <param name="highlightObjectTextStyle">The highlighted object text style.</param>
        /// <param name="name">An optional name for the gather step panel.</param>
        public GatherStepPanel(
            float width,
            float padding,
            Color labelColor,
            TextStyle instructionsTextStyle,
            Color objectColor,
            TextStyle objectTextStyle,
            Color highlightObjectColor,
            TextStyle highlightObjectTextStyle,
            string name = nameof(GatherStepPanel))
            : base(name)
        {
            this.Width = width;
            this.padding = padding;
            this.instructionsTextStyle = instructionsTextStyle;
            this.objectColor = objectColor;
            this.objectTextStyle = objectTextStyle;
            this.highlightObjectColor = highlightObjectColor;
            this.highlightObjectTextStyle = highlightObjectTextStyle;

            this.labelCircle = new LabelCircle(padding, labelColor, instructionsTextStyle, $"{this.Name}.LabelCircle");
            this.instructionsParagraph = new Paragraph($"{this.Name}.InstructionsParagraph");
        }

        /// <summary>
        /// Updates the state of the gather step panel.
        /// </summary>
        /// <param name="label">The label for the gather step.</param>
        /// <param name="instructions">The instructions for the gather step.</param>
        /// <param name="objectsChecklist">The set of objects to display.</param>
        public void Update(string label, string instructions, List<(string Name, bool Check, bool Highlight)> objectsChecklist)
        {
            this.label = label;
            this.instructions = instructions;

            this.labelCircle.Update(label);

            this.instructionsParagraph.Update(
                this.instructions,
                this.instructionsTextStyle,
                this.Width - this.padding - this.labelCircle.Radius * 2,
                bottomMargin: 0.002f,
                rightMargin: this.padding,
                leftMargin: this.padding);

            var offsetV = this.instructionsParagraph.Height;

            objectsChecklist ??= new ();
            this.physicalObjectsParagraphs.Update(
                Enumerable.Range(0, objectsChecklist.Count),
                createKey: k => new ($"{this.Name}.ObjectsParagraph[{k}]"));

            this.physicalObjectsCheckboxes.Update(
                Enumerable.Range(0, objectsChecklist.Count),
                createKey: k => new (0f, this.instructionsTextStyle.CharHeight, 0.0005f, $"{this.Name}.ObjectCheckbox[{k}]"));

            for (int i = 0; i < objectsChecklist.Count; i++)
            {
                var textStyle = objectsChecklist[i].Highlight ? this.highlightObjectTextStyle : this.objectTextStyle;
                var checkboxColor = objectsChecklist[i].Highlight ? this.highlightObjectColor : this.objectColor;

                this.physicalObjectsParagraphs[i].Update(
                    objectsChecklist[i].Name,
                    textStyle,
                    this.Width - this.padding - this.labelCircle.Radius * 2,
                    topMargin: 0.002f,
                    bottomMargin: 0.002f,
                    rightMargin: this.padding,
                    leftMargin: this.padding);

                this.physicalObjectsCheckboxes[i].Update(
                    boxVisible: true,
                    boxColor: checkboxColor,
                    checkmarkVisible: objectsChecklist[i].Check,
                    checkmarkColor: checkboxColor);

                offsetV += this.physicalObjectsParagraphs[i].Height;
            }

            this.Height = (float)Math.Max(2 * this.padding + this.labelCircle.Radius * 2 - this.instructionsTextStyle.CharHeight + offsetV, this.labelCircle.Height);
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            this.labelCircle.Render(renderer, pose);

            // Start rendering text
            var offsetV = this.padding;

            // Render the step instructions
            if (!string.IsNullOrEmpty(this.instructions))
            {
                offsetV += this.labelCircle.Radius - this.instructionsTextStyle.CharHeight / 2;
                this.instructionsParagraph.Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2, offsetV));
                offsetV += this.instructionsParagraph.Height;
            }

            offsetV += this.padding;

            for (int i = 0; i < this.physicalObjectsParagraphs.Count; i++)
            {
                this.physicalObjectsCheckboxes[i].Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2 + this.padding, offsetV + 0.0025f));
                this.physicalObjectsParagraphs[i].Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2 + this.padding + this.physicalObjectsCheckboxes[i].Width, offsetV));
                offsetV += this.physicalObjectsParagraphs[i].Height;
            }

            return this.GetUserInterfaceState(pose);
        }
    }
}
