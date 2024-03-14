// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for typesetting a paragraph of text within a
    /// specified width and with a specified alignment.
    /// </summary>
    internal class Paragraph : Rectangle3DUserInterface
    {
        private readonly List<(string Word, Point2D Position)> wordPositions = new ();

        private string text;
        private TextStyle textStyle;
        private float maxWidth;
        private float rightMargin;
        private float leftMargin;
        private float topMargin;
        private float bottomMargin;
        private float lineSpacing;
        private bool centered;

        /// <summary>
        /// Initializes a new instance of the <see cref="Paragraph"/> class.
        /// </summary>
        /// <param name="name">An optional name for the paragraph.</param>
        public Paragraph(string name = nameof(Paragraph))
            : base(name)
        {
        }

        /// <summary>
        /// Updates the paragraph renderer with the specified text.
        /// </summary>
        /// <param name="text">The text to render.</param>
        /// <param name="textStyle">The text style to use when rendering.</param>
        /// <param name="maxWidth">The maximal width of the paragraph.</param>
        /// <param name="rightMargin">The right margin.</param>
        /// <param name="leftMargin">The left margin.</param>
        /// <param name="topMargin">The top margin.</param>
        /// <param name="bottomMargin">The bottom margin.</param>
        /// <param name="lineSpacing">A line-spacing factor that specifies the proportion of text height that should be used for line spacing.</param>
        /// <param name="centered">A flag indicating whether the text should be centered.</param>
        public void Update(
            string text,
            TextStyle textStyle,
            float maxWidth,
            float rightMargin = 0,
            float leftMargin = 0,
            float topMargin = 0,
            float bottomMargin = 0,
            float lineSpacing = 0.5f,
            bool centered = false)
        {
            // Render empty string if text is null
            text ??= string.Empty;

            // If nothing has changed
            if (this.text == text &&
                this.textStyle.Equals(textStyle) &&
                this.maxWidth == maxWidth &&
                this.rightMargin == rightMargin &&
                this.leftMargin == leftMargin &&
                this.topMargin == topMargin &&
                this.bottomMargin == bottomMargin &&
                this.lineSpacing == lineSpacing &&
                this.centered == centered)
            {
                // Then don't update
                return;
            }
            else
            {
                // O/w assign the new values
                this.text = text;
                this.textStyle = textStyle;
                this.maxWidth = maxWidth;
                this.rightMargin = rightMargin;
                this.leftMargin = leftMargin;
                this.topMargin = topMargin;
                this.bottomMargin = bottomMargin;
                this.lineSpacing = lineSpacing;
                this.centered = centered;
            }

            this.wordPositions.Clear();
            this.Width = this.leftMargin + this.rightMargin;
            this.Height = this.topMargin;
            var currentX = this.leftMargin;
            var lastLineStartIndex = 0;

            foreach (var word in this.text.Split(' '))
            {
                var wordWithSpace = word + " ";
                var wordSize = Text.Size(word, this.textStyle);
                var wordWithSpaceSize = Text.Size(wordWithSpace, this.textStyle);
                if (currentX + wordSize.x + this.rightMargin > this.maxWidth)
                {
                    // First, if the next needs to be centered, then we need to
                    // adjust the positions of all words on the last line
                    if (this.centered)
                    {
                        // Compute how much we need to horizontally shift each word
                        var centeringOffset = (this.maxWidth - currentX - this.rightMargin) / 2;

                        // Shift the words horizontally by the computed amount
                        for (int i = lastLineStartIndex; i < this.wordPositions.Count; i++)
                        {
                            this.wordPositions[i] = (this.wordPositions[i].Word, new Point2D(this.wordPositions[i].Position.X + centeringOffset, this.wordPositions[i].Position.Y));
                        }

                        // Finally, adjust the last line start index
                        lastLineStartIndex = this.wordPositions.Count;
                    }

                    // We need to wrap the line, so increment the height and reset the currentX
                    this.Height += this.textStyle.CharHeight * (1 + this.lineSpacing);
                    currentX = this.leftMargin;
                }

                // Add the word to the list
                this.wordPositions.Add((wordWithSpace, new Point2D(currentX, this.Height)));

                // Increment the current X position
                currentX += wordWithSpaceSize.x;

                // Adjust the right margin
                if (currentX + this.rightMargin > this.Width)
                {
                    this.Width = currentX + this.rightMargin;
                }
            }

            // If we are centering, we need to shift the words on the last line
            if (this.centered)
            {
                // First, compute how much we need to horizontally shift each word
                var lastLineCenteringOffset = (this.maxWidth - currentX - this.rightMargin) / 2;

                // Shift the words horizontally by the computed amount
                for (int i = lastLineStartIndex; i < this.wordPositions.Count; i++)
                {
                    this.wordPositions[i] = (this.wordPositions[i].Word, new Point2D(this.wordPositions[i].Position.X + lastLineCenteringOffset, this.wordPositions[i].Position.Y));
                }
            }

            // Finally add the text line height (for the last line) and bottom margin to the
            // total output height
            this.Height += this.textStyle.CharHeight + this.bottomMargin;
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            foreach ((var word, var position) in this.wordPositions)
            {
                renderer.RenderText(pose.ApplyUV((float)position.X, (float)position.Y), word, this.textStyle, TextAlign.TopLeft);
            }

            return this.GetUserInterfaceState(pose);
        }
    }
}
