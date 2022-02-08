// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a depth image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    public class DepthImageRectangle3D : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the depth image.</param>
        /// <param name="depthImage">The depth image.</param>
        public DepthImageRectangle3D(Rectangle3D rectangle, Shared<DepthImage> depthImage)
        {
            this.Rectangle3D = rectangle;
            this.DepthImage = depthImage.AddRef();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="origin">The origin of the depth image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the depth image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the depth image rectangle.</param>
        /// <param name="left">The left edge of the depth image rectangle (relative to origin along the width axis).</param>
        /// <param name="bottom">The bottom edge of the depth image rectangle (relative to origin along the height axis).</param>
        /// <param name="width">The width of the depth image rectangle.</param>
        /// <param name="height">The height of the depth image rectangle.</param>
        /// <param name="depthImage">The depth image.</param>
        /// <remarks>
        /// The edges of the depth image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public DepthImageRectangle3D(
            Point3D origin,
            UnitVector3D widthAxis,
            UnitVector3D heightAxis,
            double left,
            double bottom,
            double width,
            double height,
            Shared<DepthImage> depthImage)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, left, bottom, width, height), depthImage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="scale">The scale to use when calculating metric corner offsets from depth image pixel width and height.</param>
        /// <param name="origin">The origin of the depth image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the depth image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the depth image rectangle.</param>
        /// <param name="depthImage">The depth image.</param>
        /// <remarks>
        /// The (left, bottom) corner of the depth image rectangle is set to the origin (0, 0), and (width, height) are calculated from multiplying
        /// the depth image pixel width and height respectively by a scaling parameter.
        /// The edges of the depth image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public DepthImageRectangle3D(double scale, Point3D origin, UnitVector3D widthAxis, UnitVector3D heightAxis, Shared<DepthImage> depthImage)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, 0, 0, depthImage.Resource.Width * scale, depthImage.Resource.Height * scale), depthImage)
        {
        }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Rectangle3D Rectangle3D { get; }

        /// <summary>
        /// Gets the depth image.
        /// </summary>
        public Shared<DepthImage> DepthImage { get; }

        /// <summary>
        /// Tries to get the nearest pixel value, first projecting the input point into the plane of the 3D rectangle
        /// to determine image space pixel coordinates.
        /// </summary>
        /// <param name="point">The desired point to project into the depth image rectangle and get a pixel value for.</param>
        /// <param name="pixelValue">Pixel value (output).</param>
        /// <returns>True if the point could be projected within the bounds of the depth image, false otherwise.</returns>
        public bool TryGetPixel(Point3D point, out ushort? pixelValue)
        {
            if (this.TryGetPixelCoordinates(point, out int u, out int v))
            {
                pixelValue = this.DepthImage.Resource.GetPixel(u, v);
                return true;
            }
            else
            {
                pixelValue = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to set the nearest pixel to a given value, first projecting the input point into the plane of the 3D rectangle
        /// to determine image space pixel coordinates.
        /// </summary>
        /// <param name="point">The desired point to project into the depth image rectangle and set a pixel value for.</param>
        /// <param name="pixelValue">Value to set pixel to.</param>
        /// <returns>True if the point could be projected within the bounds of the depth image, false otherwise.</returns>
        public bool TrySetPixel(Point3D point, ushort pixelValue)
        {
            if (this.TryGetPixelCoordinates(point, out int u, out int v))
            {
                this.DepthImage.Resource.SetPixel(u, v, pixelValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.DepthImage?.Dispose();
        }

        /// <summary>
        /// Get the pixel coordinates that map to a given 3d point location.
        /// </summary>
        /// <param name="point">The 3d point in "global" coordinates.</param>
        /// <param name="u">The pixel u coordinate (output).</param>
        /// <param name="v">The pixel v coordinate (output).</param>
        /// <param name="maxPlaneDistance">The maximum allowed distance for projecting the point to the rectangular image plane.</param>
        /// <returns>True if the point could be projected within the bounds of the depth image rectangle, false otherwise.</returns>
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
            u = (int)(widthVectorProjection * this.DepthImage.Resource.Width);
            v = (int)(heightVectorProjection * this.DepthImage.Resource.Height);

            if (u >= 0 && v >= 0 && u < this.DepthImage.Resource.Width && v < this.DepthImage.Resource.Height)
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
