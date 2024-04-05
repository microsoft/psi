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
    /// Implements a StereoKit user interface for displaying a label circle.
    /// </summary>
    internal class LabelCircle : Rectangle3DUserInterface
    {
        private readonly float padding;
        private readonly Color color;
        private readonly TextStyle textStyle;

        private readonly float radius;

        private string label = string.Empty;
        private Mesh labelCircleMesh;
        private Material labelCircleMeshMaterial;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabelCircle"/> class.
        /// </summary>
        /// <param name="padding">The padding for the label circle.</param>
        /// <param name="color">The color for the label circle.</param>
        /// <param name="textStyle">The textStyle for the label circle.</param>
        /// <param name="name">An optional name for the label circle.</param>
        public LabelCircle(float padding, Color color, TextStyle textStyle, string name = nameof(LabelCircle))
        {
            this.padding = padding;
            this.color = color;
            this.textStyle = textStyle;

            // Make sure we have all the necessary resources
            var labelSizeVec2 = Text.Size("99", this.textStyle);
            this.radius = Math.Max(labelSizeVec2.x, labelSizeVec2.y) * 1.44f / 2;
        }

        /// <summary>
        /// Gets the radius of the label circle.
        /// </summary>
        public float Radius => this.radius;

        /// <summary>
        /// Gets the color of the label circle.
        /// </summary>
        public Color Color => this.color;

        /// <summary>
        /// Updates the label circle.
        /// </summary>
        /// <param name="label">The label.</param>
        public void Update(string label)
        {
            // If nothing has changed
            if (this.label == label)
            {
                // Simply return
                return;
            }
            else
            {
                this.label = label;
            }

            this.Width = this.Height = 2 * (this.padding + this.radius);
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // Render the label
            var labelPose = pose.ApplyUV(this.padding + this.radius, this.padding + this.radius);
            renderer.RenderText(labelPose, this.label, this.textStyle, TextAlign.Center);

            // Render the circle
            this.labelCircleMesh ??= this.CreateLabelCircleMesh(renderer);
            renderer.RenderMesh(labelPose, this.labelCircleMesh, this.labelCircleMeshMaterial);

            return this.GetUserInterfaceState(pose);
        }

        private Mesh CreateLabelCircleMesh(Renderer renderer)
        {
            // Create the circle mesh for task numbers
            this.labelCircleMesh = renderer.GetOrCreateCircleMesh(this.radius, this.radius - 0.0005f);
            this.labelCircleMeshMaterial = renderer.GetOrCreateMaterial(this.color);

            return this.labelCircleMesh;
        }
    }
}
