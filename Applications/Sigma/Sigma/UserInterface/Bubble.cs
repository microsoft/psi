// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a bubble with text.
    /// </summary>
    public class Bubble : Rectangle3DUserInterface
    {
        private const float DotMeshRadius = 0.0015f;

        private readonly bool leftOrientation;
        private readonly float maxWidth;
        private readonly float padding;
        private readonly Color backgroundColor;
        private readonly Color lineColor;
        private readonly float lineWidth;
        private readonly TextStyle textStyle;
        private readonly Dictionary<int, Paragraph> paragraphs = new ();
        private readonly DateTime startTime = DateTime.Now;

        // state information
        private string[] text;
        private bool isThinkingIndicator = false;

        // rendering resources
        private List<Point3D> bubbleVertices;
        private Mesh bubbleMesh;
        private Material bubbleMeshMaterial;

        private Mesh dotMesh;
        private Material dotMeshMaterial;
        private Material dotMeshMaterialFaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bubble"/> class.
        /// </summary>
        /// <param name="leftOrientation">True if the bubble if left-oriented, or false if the bubble is right-oriented.</param>
        /// <param name="maxWidth">The max width of the bubble.</param>
        /// <param name="padding">The padding.</param>
        /// <param name="backgroundColor">The bubble background color.</param>
        /// <param name="lineColor">The bubble line color.</param>
        /// <param name="lineWidth">The bubble line width.</param>
        /// <param name="textStyle">The bubble text style.</param>
        /// <param name="name">An optional name for the bubble.</param>
        public Bubble(
            bool leftOrientation,
            float maxWidth,
            float padding,
            Color backgroundColor,
            Color lineColor,
            float lineWidth,
            TextStyle textStyle,
            string name = nameof(Bubble))
            : base(name)
        {
            this.leftOrientation = leftOrientation;
            this.maxWidth = maxWidth;
            this.padding = padding;
            this.backgroundColor = backgroundColor;
            this.lineColor = lineColor;
            this.lineWidth = lineWidth;
            this.textStyle = textStyle;
        }

        /// <summary>
        /// Updates the bubble with a set of text paragraphs to display.
        /// </summary>
        /// <param name="text">The set of text paragraphs to display.</param>
        /// <remarks>
        /// The method also recomputes the Width and Height of the bubble.
        /// </remarks>
        public void Update(string[] text)
        {
            if (text == null)
            {
                throw new ArgumentException($"{nameof(text)} parameter cannot be null.");
            }

            // Clear the is thinking indicator
            this.isThinkingIndicator = false;

            // Check if we need to perform updates
            if (Operators.EnumerableEquals(this.text, text))
            {
                return;
            }
            else
            {
                this.text = text;
            }

            // Force an update of the mesh during Render()
            this.bubbleMesh = null;

            // Update the paragraph renderers and compute the height
            this.paragraphs.Update(Enumerable.Range(0, text.Length), i => new Paragraph($"{this.Name}.Paragraph[{i}]"));

            this.Height = this.padding;
            this.Width = 0;
            for (int i = 0; i < this.text.Length; i++)
            {
                this.paragraphs[i].Update(
                    this.text[i],
                    this.textStyle,
                    this.maxWidth,
                    leftMargin: this.padding,
                    rightMargin: this.padding);

                this.Height += this.paragraphs[i].Height + this.padding;
                if (this.paragraphs[i].Width > this.Width)
                {
                    this.Width = this.paragraphs[i].Width;
                }
            }
        }

        /// <summary>
        /// Updates the bubble to display the thinking indicator (three blinking dots).
        /// </summary>
        public void UpdateIsThinkingIndicator()
        {
            if (this.isThinkingIndicator)
            {
                return;
            }

            this.text = new string[] { };
            this.isThinkingIndicator = true;

            // Force an update of the mesh during Render()
            this.bubbleMesh = null;

            // Update the paragraph renderers to contains a single paragraph to compute the height
            this.paragraphs.Update(Enumerable.Range(0, 1), i => new Paragraph($"{this.Name}.Paragraph[{i}]"));
            this.paragraphs[0].Update(
                "X",
                this.textStyle,
                this.maxWidth,
                leftMargin: this.padding,
                rightMargin: this.padding);

            this.Height = this.padding + this.paragraphs[0].Height + this.padding;
            this.Width = this.padding + 8 * DotMeshRadius + this.padding;
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // Construct (if necessary) the bubble mesh and render it
            this.bubbleMesh ??= this.CreateBubbleMesh(renderer);
            renderer.RenderMesh(pose, this.bubbleMesh, this.bubbleMeshMaterial);
            renderer.RenderPolygon(pose, this.bubbleVertices, this.lineWidth, this.lineColor);

            // Construct the dot mesh
            this.dotMesh ??= this.CreateDotMesh(renderer);
            this.dotMeshMaterial ??= renderer.GetOrCreateMaterial(System.Drawing.Color.White.ToStereoKitColor());
            this.dotMeshMaterialFaded ??= renderer.GetOrCreateMaterial(System.Drawing.Color.Gray.ToStereoKitColor());

            // If we are in "IsThinking" mode
            if (this.isThinkingIndicator)
            {
                // Render the dot meshes
                var secondsElapsed = (int)((DateTime.Now - this.startTime).TotalSeconds * 4);
                for (int i = 0; i < 3; i++)
                {
                    var bubblePose = CoordinateSystem.Translation(new Vector3D(0, this.padding + (3 * i + 1) * DotMeshRadius, -(this.padding + this.paragraphs[0].Height / 2)));
                    var material = secondsElapsed % 3 == i ? this.dotMeshMaterial : this.dotMeshMaterialFaded;
                    renderer.RenderMesh(bubblePose.TransformBy(pose), this.dotMesh, material);
                }
            }
            else
            {
                // O/w render the text paragraphs
                var offsetV = this.padding;
                for (int i = 0; i < this.paragraphs.Count; i++)
                {
                    this.paragraphs[i].Render(renderer, pose.ApplyUV(0, offsetV));
                    offsetV += this.paragraphs[i].Height + this.padding;
                }
            }

            return this.GetUserInterfaceState(pose);
        }

        /// <summary>
        /// Creates the dot mesh.
        /// </summary>
        /// <param name="renderer">The renderer used to manage visual resources.</param>
        /// <returns>The bubble mesh.</returns>
        private Mesh CreateDotMesh(Renderer renderer)
        {
            var thetaDiv = 20;
            var dotVertices = new List<Point3D>() { new Point3D(0.003, 0, 0) };
            var indices = new List<uint>();
            for (uint i = 0; i < thetaDiv; i++)
            {
                var angle = i * Math.PI * 2 / thetaDiv;
                dotVertices.Add(new Point3D(0.003, DotMeshRadius * Math.Cos(angle), DotMeshRadius * Math.Sin(angle)));
                if (i < thetaDiv - 1)
                {
                    indices.Add(0);
                    indices.Add(i + 1);
                    indices.Add(i + 2);
                }
                else
                {
                    indices.Add(0);
                    indices.Add(i + 1);
                    indices.Add(1);
                }
            }

            return renderer.CreateMesh(dotVertices, indices);
        }

        /// <summary>
        /// Creates the bubble mesh.
        /// </summary>
        /// <param name="renderer">The renderer used to manage visual resources.</param>
        /// <returns>The bubble mesh.</returns>
        private Mesh CreateBubbleMesh(Renderer renderer)
        {
            // Create the bubble mesh material
            this.bubbleMeshMaterial = renderer.GetOrCreateMaterial(this.backgroundColor);

            // Create the bubble mesh.
            if (this.leftOrientation)
            {
                // The mesh is displayed in the YZ plane of the coordinate system passed into the Render
                // method (the X axis points towards the user, who is looking at the bubble)
                //
                //   z
                //   ^
                //   |
                //   ---->y
                //
                //    Left
                //    0--6
                //    |  |
                //    1  |
                //   /   |
                //  2    |
                //   \   |
                //    3  |
                //    |  |
                //    4--5
                this.bubbleVertices =
                [
                    new Point3D(-0.003, 0, 0),                           // 0
                    new Point3D(-0.003, 0, -(this.Height - 0.010)),      // 1
                    new Point3D(-0.003, -0.005, -(this.Height - 0.007)), // 2
                    new Point3D(-0.003, 0, -(this.Height - 0.004f)),     // 3
                    new Point3D(-0.003, 0, -this.Height),                // 4
                    new Point3D(-0.003, this.Width, -this.Height),       // 5
                    new Point3D(-0.003, this.Width, 0),                  // 6
                ];

                var indices = new List<uint>()
                {
                    0, 1, 6,
                    1, 2, 3,
                    3, 4, 5,
                    1, 3, 5,
                    1, 5, 6,
                };

                return renderer.CreateMesh(this.bubbleVertices, indices);
            }
            else
            {
                // The mesh is displayed in the YZ plane of the coordinate system passed into the Render
                // method (the X axis points towards the user, who is looking at the bubble)
                //
                //   z
                //   ^
                //   |
                //   ---->y
                //
                //    Left
                //    0--6
                //    |  |
                //    |  5
                //    |   \
                //    |    4
                //    |   /
                //    |  3
                //    |  |
                //    1--2
                this.bubbleVertices =
                [
                    new Point3D(-0.003, 0, 0),                                        // 0
                    new Point3D(-0.003, 0, -this.Height),                             // 1
                    new Point3D(-0.003, this.Width, -this.Height),                    // 2
                    new Point3D(-0.003, this.Width, -(this.Height - 0.004)),          // 3
                    new Point3D(-0.003, this.Width + 0.005, -(this.Height - 0.007)),  // 4
                    new Point3D(-0.003, this.Width, -(this.Height - 0.010)),          // 5
                    new Point3D(-0.003, this.Width, 0),                               // 6
                ];

                var indices = new List<uint>()
                {
                    0, 1, 6,
                    1, 2, 3,
                    3, 4, 5,
                    1, 3, 5,
                    1, 5, 6,
                };

                return renderer.CreateMesh(this.bubbleVertices, indices);
            }
        }
    }
}
