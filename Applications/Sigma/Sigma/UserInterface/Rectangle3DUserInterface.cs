// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Defines a base abstract class for planar (rectangle 3D) user interface elements.
    /// </summary>
    public abstract class Rectangle3DUserInterface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3DUserInterface"/> class.
        /// </summary>
        /// <param name="name">An optional name for the element.</param>
        public Rectangle3DUserInterface(string name = nameof(Rectangle3DUserInterface))
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the element.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the width of the element.
        /// </summary>
        public float Width { get; protected set; }

        /// <summary>
        /// Gets or sets the height of the element.
        /// </summary>
        public float Height { get; protected set; }

        /// <summary>
        /// Renders the element.
        /// </summary>
        /// <param name="renderer">The renderer to use.</param>
        /// <param name="pose">The pose for the element.</param>
        /// <returns>The list of user interface element states for the element and its children.</returns>
        public abstract List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose);

        /// <summary>
        /// Gets the state of the planar user interface element.
        /// </summary>
        /// <param name="pose">The pose at which the element is rendered.</param>
        /// <returns>The state of the planar user interface element.</returns>
        protected virtual List<Rectangle3DUserInterfaceState> GetUserInterfaceState(CoordinateSystem pose)
            =>
            [
                new ()
                {
                    Name = this.Name,
                    Rectangle3D = new Rectangle3D(
                        pose.Origin,
                        pose.YAxis.Normalize(),
                        pose.ZAxis.Negate().Normalize(),
                        0,
                        0,
                        this.Width,
                        this.Height),
                },
            ];
    }
}
