// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    using System;

    /// <summary>
    /// Struct represents a 2D rectangle
    /// </summary>
    public struct Rectangle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle"/> struct.
        /// </summary>
        /// <param name="x0">The x0 coordinate.</param>
        /// <param name="y0">The y0 coordinate.</param>
        /// <param name="x1">The x1 coordinate.</param>
        /// <param name="y1">The y1 coordinate.</param>
        public Rectangle(double x0, double y0, double x1, double y1)
        {
            this.X0 = x0;
            this.Y0 = y0;
            this.X1 = x1;
            this.Y1 = y1;
        }

        /// <summary>
        /// Gets or sets the X0 coordinate of the rectangle
        /// </summary>
        public double X0 { get; set; }

        /// <summary>
        /// Gets or sets the Y0 coordinate of the rectangle
        /// </summary>
        public double Y0 { get; set; }

        /// <summary>
        /// Gets or sets the X1 coordinate of the rectangle
        /// </summary>
        public double X1 { get; set; }

        /// <summary>
        /// Gets or sets the Y1 coordinate of the rectangle
        /// </summary>
        public double Y1 { get; set; }

        /// <summary>
        /// Gets the minimum x coordinate
        /// </summary>
        public double XMin => Math.Min(this.X0, this.X1);

        /// <summary>
        /// Gets the maximum x coordinate
        /// </summary>
        public double XMax => Math.Max(this.X0, this.X1);

        /// <summary>
        /// Gets the minimum y coordinate
        /// </summary>
        public double YMin => Math.Min(this.Y0, this.Y1);

        /// <summary>
        /// Gets the maximum y coordinate
        /// </summary>
        public double YMax => Math.Max(this.Y0, this.Y1);

        /// <summary>
        /// Gets the width of the rectangle
        /// </summary>
        public double Width => Math.Abs(this.X1 - this.X0);

        /// <summary>
        /// Gets the height of the rectangle
        /// </summary>
        public double Height => Math.Abs(this.Y1 - this.Y0);

        /// <summary>
        /// Inflates the rectangle by a factor
        /// </summary>
        /// <param name="factor">The factor to inflate the rectangle by</param>
        public void Inflate(double factor)
        {
            if (factor == 0)
            {
                return;
            }

            double widthInflate = this.Width * factor;
            double heightInflate = this.Height * factor;

            if (this.X1 > this.X0)
            {
                this.X1 += widthInflate;
                this.X0 -= widthInflate;
            }
            else
            {
                this.X1 -= widthInflate;
                this.X0 += widthInflate;
            }

            if (this.Y1 > this.Y0)
            {
                this.Y1 += heightInflate;
                this.Y0 -= heightInflate;
            }
            else
            {
                this.Y1 -= heightInflate;
                this.Y0 += heightInflate;
            }
        }
    }
}
