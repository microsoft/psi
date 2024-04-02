// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a text that orients towards the user.
    /// </summary>
    public class TextBillboardUserInterface : Rectangle3DUserInterface
    {
        private readonly Paragraph textParagraph = default;
        private readonly TextBillboardsUserInterfaceConfiguration configuration;
        private Point3D location;
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBillboardUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The text display configuration.</param>
        /// <param name="location">The initial location for the text.</param>
        /// <param name="text">The initial text to display.</param>
        /// <param name="name">An optional name for the text display.</param>
        public TextBillboardUserInterface(TextBillboardsUserInterfaceConfiguration configuration, Point3D location, string text, string name = nameof(TextBillboardUserInterface))
            : base(name)
        {
            this.configuration = configuration;
            this.location = location;
            this.text = text;

            this.textParagraph = new Paragraph($"{this.Name}.TextParagraph");
        }

        /// <summary>
        /// Updates the state of the text display.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="location">The location.</param>
        public void Update(string text, Point3D location)
        {
            this.location = location;
            this.text = text;
            this.textParagraph.Update(
                this.text,
                this.configuration.TextStyle,
                this.configuration.Width,
                centered: true);

            this.Width = this.configuration.Width;
            this.Height = this.textParagraph.Height;
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // Render the paragraph
            var paragraphPose =
                renderer.GetHorizontalHeadOrientedCoordinateSystem(this.location).ApplyUV(-this.configuration.Width / 2, -this.textParagraph.Height);

            this.textParagraph.Render(renderer, paragraphPose);

            return this.GetUserInterfaceState(paragraphPose);
        }
    }
}
