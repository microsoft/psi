// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a checkbox.
    /// </summary>
    public class Checkbox : Rectangle3DUserInterface
    {
        // construction-time constants
        private readonly float padding;
        private readonly float lineWidth;

        // state information
        private Color boxColor;
        private bool boxVisible;
        private Color checkmarkColor;
        private bool checkmarkVisible;

        /// <summary>
        /// Initializes a new instance of the <see cref="Checkbox"/> class.
        /// </summary>
        /// <param name="padding">The padding.</param>
        /// <param name="width">The width of the checkbox.</param>
        /// <param name="lineWidth">The line width.</param>
        /// <param name="name">An optional name for the checkbox user interface element.</param>
        public Checkbox(float padding, float width, float lineWidth, string name = nameof(Checkbox))
            : base(name)
        {
            this.Width = width;
            this.Height = width;
            this.padding = padding;
            this.lineWidth = lineWidth;
        }

        /// <summary>
        /// Updates the checkbox state.
        /// </summary>
        /// <param name="boxVisible">Whether the box is visible.</param>
        /// <param name="boxColor">The color of the box.</param>
        /// <param name="checkmarkVisible">Whether the checkmark is visible.</param>
        /// <param name="checkmarkColor">The color of the checkmark.</param>
        public void Update(bool boxVisible, Color boxColor, bool checkmarkVisible, Color checkmarkColor)
        {
            this.boxVisible = boxVisible;
            this.boxColor = boxColor;
            this.checkmarkVisible = checkmarkVisible;
            this.checkmarkColor = checkmarkColor;
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // Render the box
            if (this.boxVisible)
            {
                var u1 = new Point3D(0, this.padding, -this.padding).TransformBy(pose);
                var u2 = new Point3D(0, this.padding + this.Width, -this.padding).TransformBy(pose);
                var v1 = new Point3D(0, this.padding, -(this.padding + this.Height)).TransformBy(pose);
                var v2 = new Point3D(0, this.padding + this.Width, -(this.padding + this.Height)).TransformBy(pose);

                renderer.RenderLine(u1, u2, this.lineWidth, this.boxColor);
                renderer.RenderLine(u2, v2, this.lineWidth, this.boxColor);
                renderer.RenderLine(v2, v1, this.lineWidth, this.boxColor);
                renderer.RenderLine(v1, u1, this.lineWidth, this.boxColor);
            }

            // Render the checkmark
            if (this.checkmarkVisible)
            {
                var c1 = new Point3D(0, this.padding + this.Width * 0.25, -(this.padding + this.Height * 0.5)).TransformBy(pose);
                var c2 = new Point3D(0, this.padding + this.Width * 0.5, -(this.padding + this.Height * 0.75)).TransformBy(pose);
                var c3 = new Point3D(0, this.padding + this.Width * 0.75, -(this.padding + this.Height * 0.25)).TransformBy(pose);

                renderer.RenderLine(c1, c2, this.lineWidth, this.checkmarkColor);
                renderer.RenderLine(c2, c3, this.lineWidth, this.checkmarkColor);
            }

            return this.GetUserInterfaceState(pose);
        }
    }
}
