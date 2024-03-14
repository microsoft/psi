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
    /// Implements a StereoKit user interface element for displaying a complex step.
    /// </summary>
    internal class ComplexStepPanel : StepPanel
    {
        private readonly float padding;
        private readonly TextStyle instructionsTextStyle;

        private readonly LabelCircle labelCircle;
        private readonly Paragraph instructionsParagraph = default;

        private readonly TextStyle physicalObjectTextStyle;
        private readonly TextStyle highlightedPhysicalObjectTextStyle;
        private readonly Dictionary<int, Paragraph> physicalObjectsParagraphs = new ();
        private readonly Dictionary<int, DoStepPanel> subStepPanels = new ();

        private readonly Color labelCircleColor;
        private readonly Color selectionColor;
        private readonly Dictionary<float, Mesh> selectionMeshes = new ();
        private Material selectionMeshMaterial;

        private string label = string.Empty;
        private string instructions;
        private int? selectedSubStepIndex = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexStepPanel"/> class.
        /// </summary>
        /// <param name="width">The width of the panel.</param>
        /// <param name="padding">The padding for the panel.</param>
        /// <param name="labelCircleColor">The label circle color.</param>
        /// <param name="instructionsTextStyle">The instructions text style.</param>
        /// <param name="physicalObjectTextStyle">The physical object text style.</param>
        /// <param name="highlightedPhysicalObjectTextStyle">The highlighted physical object text style.</param>
        /// <param name="selectionColor">The selection color.</param>
        /// <param name="name">An optional name for the complex step panel.</param>
        public ComplexStepPanel(
            float width,
            float padding,
            Color labelCircleColor,
            TextStyle instructionsTextStyle,
            TextStyle physicalObjectTextStyle,
            TextStyle highlightedPhysicalObjectTextStyle,
            Color selectionColor,
            string name = nameof(ComplexStepPanel))
            : base(name)
        {
            this.Width = width;
            this.padding = padding;
            this.instructionsTextStyle = instructionsTextStyle;
            this.physicalObjectTextStyle = physicalObjectTextStyle;
            this.highlightedPhysicalObjectTextStyle = highlightedPhysicalObjectTextStyle;
            this.labelCircleColor = labelCircleColor;
            this.selectionColor = selectionColor;

            this.labelCircle = new LabelCircle(padding, labelCircleColor, instructionsTextStyle, $"{this.Name}.LabelCircle");
            this.instructionsParagraph = new Paragraph($"{this.Name}.InstructionsParagraph");
        }

        /// <summary>
        /// Updates the state of the complex step panel.
        /// </summary>
        /// <param name="label">The label for the complex step.</param>
        /// <param name="instructions">The instructions for the complex step.</param>
        /// <param name="selectedSubStepIndex">The selected substep index.</param>
        /// <param name="objectsChecklist">The objects checklist.</param>
        /// <param name="subSteps">The substeps for the complex step.</param>
        public void Update(
            string label,
            string instructions,
            int? selectedSubStepIndex,
            List<(string Name, bool Check, bool Highlight)> objectsChecklist,
            List<(string Label, string Description)> subSteps)
        {
            // Get the corresponding step, step index and instructions
            this.label = label;
            this.instructions = instructions;

            this.labelCircle.Update(this.label);

            this.instructionsParagraph.Update(
                this.instructions,
                this.instructionsTextStyle,
                this.Width - this.padding * 2 - this.labelCircle.Radius * 2,
                bottomMargin: 0.002f,
                rightMargin: this.padding,
                leftMargin: this.padding);

            var offsetV = this.instructionsParagraph.Height;

            if (objectsChecklist.Any())
            {
                this.physicalObjectsParagraphs.Update(
                    Enumerable.Range(0, objectsChecklist.Count),
                    createKey: k => new ($"{this.Name}.ObjectParagraph[{k}]"));

                offsetV += this.padding;

                for (int i = 0; i < objectsChecklist.Count; i++)
                {
                    this.physicalObjectsParagraphs[i].Update(
                        objectsChecklist[i].Name,
                        objectsChecklist[i].Highlight ? this.highlightedPhysicalObjectTextStyle : this.physicalObjectTextStyle,
                        this.Width - this.padding * 2 - this.labelCircle.Radius * 2,
                        topMargin: 0.002f,
                        bottomMargin: 0.002f,
                        rightMargin: this.padding,
                        leftMargin: this.padding);
                    offsetV += this.physicalObjectsParagraphs[i].Height;
                }

                this.subStepPanels.Clear();
            }
            else if (subSteps.Any())
            {
                this.selectedSubStepIndex = selectedSubStepIndex;

                // Update the collection of sub-step panels
                this.subStepPanels.Update(
                    Enumerable.Range(0, subSteps.Count),
                    createKey: i => new DoStepPanel(this.Width - this.padding * 2 - this.labelCircle.Radius * 2, this.padding, this.labelCircleColor, this.instructionsTextStyle, $"{this.Name}.Step[{i}]"));

                // Now update the step panels
                if (this.subStepPanels.Count > 0)
                {
                    offsetV += this.padding;
                }

                for (int i = 0; i < this.subStepPanels.Count; i++)
                {
                    this.subStepPanels[i].Update(subSteps[i].Label, subSteps[i].Description);
                    offsetV += this.subStepPanels[i].Height;
                }

                this.physicalObjectsParagraphs.Clear();
            }
            else
            {
                this.physicalObjectsParagraphs.Clear();
                this.subStepPanels.Clear();
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

            if (this.physicalObjectsParagraphs.Count > 0)
            {
                offsetV += this.padding;
            }

            for (int i = 0; i < this.physicalObjectsParagraphs.Count; i++)
            {
                this.physicalObjectsParagraphs[i].Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2 + this.padding, offsetV));
                offsetV += this.physicalObjectsParagraphs[i].Height;
            }

            // Finally, draw the steps. Draw at least one step and draw as long as we're not beyond the panel
            var hasSelection = false;
            var selectionOffsetV = 0f;
            var selectionHeight = 0f;

            if (this.subStepPanels.Count > 0)
            {
                offsetV += this.padding;
            }

            for (int i = 0; i < this.subStepPanels.Count; ++i)
            {
                // Draw the step
                this.subStepPanels[i].Render(renderer, pose.ApplyUV(this.padding + this.labelCircle.Radius * 2 + this.padding, offsetV));
                offsetV += this.subStepPanels[i].Height;

                // If this is the selected step
                if (this.selectedSubStepIndex.HasValue && this.selectedSubStepIndex.Value == i)
                {
                    hasSelection = true;
                    selectionOffsetV = offsetV - this.subStepPanels[i].Height;
                    selectionHeight = this.subStepPanels[i].Height;
                }
            }

            // Now if we have a selection to render
            if (hasSelection)
            {
                var selectionMeshPose = new CoordinateSystem(new Point3D(0, 0, -selectionOffsetV), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).TransformBy(pose);
                var selectionMesh = this.GetOrCreateSelectionMesh(renderer, selectionHeight);
                renderer.RenderMesh(selectionMeshPose, selectionMesh, this.selectionMeshMaterial);
            }

            return this.GetUserInterfaceState(pose);
        }

        private Mesh GetOrCreateSelectionMesh(Renderer renderer, float height)
        {
            if (!this.selectionMeshes.ContainsKey(height))
            {
                // Create the selection mesh
                var selectionMeshCorners = new List<Point3D>()
                {
                    new Point3D(-0.002f, 0, 0),
                    new Point3D(-0.002f, 0, -height),
                    new Point3D(-0.002f, this.Width, -height),
                    new Point3D(-0.002f, this.Width, 0),
                };
                var indices = new List<uint>() { 0, 1, 2, 0, 2, 3 };
                var selectionMesh = renderer.CreateMesh(selectionMeshCorners, indices);
                this.selectionMeshes.Add(height, selectionMesh);
            }

            this.selectionMeshMaterial ??= renderer.GetOrCreateMaterial(this.selectionColor);

            return this.selectionMeshes[height];
        }
    }
}
