// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents an image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    /// <typeparam name="T">Type of image.</typeparam>
    public class ImageRectangle3D<T> : IDisposable
        where T : class, IImage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D{T}"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the image.</param>
        /// <param name="image">The image.</param>
        public ImageRectangle3D(Rectangle3D rectangle, Shared<T> image)
        {
            this.Rectangle3D = rectangle;
            this.Image = image.AddRef();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D{T}"/> class.
        /// </summary>
        /// <param name="origin">The origin of the image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the image rectangle.</param>
        /// <param name="left">The left edge of the image rectangle (relative to origin along the width axis).</param>
        /// <param name="bottom">The bottom edge of the image rectangle (relative to origin along the height axis).</param>
        /// <param name="width">The width of the image rectangle.</param>
        /// <param name="height">The height of the image rectangle.</param>
        /// <param name="image">The image.</param>
        /// <remarks>
        /// The edges of the image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public ImageRectangle3D(
            Point3D origin,
            UnitVector3D widthAxis,
            UnitVector3D heightAxis,
            double left,
            double bottom,
            double width,
            double height,
            Shared<T> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, left, bottom, width, height), image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D{T}"/> class.
        /// </summary>
        /// <param name="scale">The scale to use when calculating metric corner offsets from image pixel width and height.</param>
        /// <param name="origin">The origin of the image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the image rectangle.</param>
        /// <param name="image">The image.</param>
        /// <remarks>
        /// The (left, bottom) corner of the image rectangle is set to the origin (0, 0), and (width, height) are calculated
        /// from multiplying the image pixel width and height respectively by a scaling parameter.
        /// The edges of the image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public ImageRectangle3D(double scale, Point3D origin, UnitVector3D widthAxis, UnitVector3D heightAxis, Shared<T> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, 0, 0, image.Resource.Width * scale, image.Resource.Height * scale), image)
        {
        }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Rectangle3D Rectangle3D { get; }

        /// <summary>
        /// Gets the image.
        /// </summary>
        public Shared<T> Image { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Image?.Dispose();
        }

        /// <summary>
        /// Get the pixel coordinates that map to a given 3d point location.
        /// </summary>
        /// <param name="point">The 3d point in "global" coordinates.</param>
        /// <param name="u">The pixel u coordinate (output).</param>
        /// <param name="v">The pixel v coordinate (output).</param>
        /// <param name="maxPlaneDistance">The maximum allowed distance for projecting the point to the rectangular image plane.</param>
        /// <returns>True if the point could be projected within the bounds of the image rectangle, false otherwise.</returns>
        public bool TryGetPixelCoordinates(Point3D point, out int u, out int v, double maxPlaneDistance = double.MaxValue)
        {
            // Project the given point to the corresponding plane.
            var planeProjectedPoint = point.ProjectOn(Plane.FromPoints(this.Rectangle3D.TopLeft, this.Rectangle3D.TopRight, this.Rectangle3D.BottomRight));

            // Check if the projected point is too far away from the original point.
            if ((planeProjectedPoint - point).Length > maxPlaneDistance)
            {
                u = v = -1;
                return false;
            }

            // Construct a width axis pointing left-to-right and a height axis pointing top-to-bottom,
            var widthVector = this.Rectangle3D.TopRight - this.Rectangle3D.TopLeft;
            var heightVector = this.Rectangle3D.BottomLeft - this.Rectangle3D.TopLeft;

            // Compute the normalized projection to the width and height vectors of the rectangle
            var cornerToPoint = planeProjectedPoint - this.Rectangle3D.TopLeft;
            var widthVectorProjection = cornerToPoint.DotProduct(widthVector) / widthVector.DotProduct(widthVector);
            var heightVectorProjection = cornerToPoint.DotProduct(heightVector) / heightVector.DotProduct(heightVector);

            // Convert to pixel coordinates
            u = (int)(widthVectorProjection * this.Image.Resource.Width);
            v = (int)(heightVectorProjection * this.Image.Resource.Height);

            if (u >= 0 && v >= 0 && u < this.Image.Resource.Width && v < this.Image.Resource.Height)
            {
                return true;
            }
            else
            {
                u = v = -1;
                return false;
            }
        }
    }
}
