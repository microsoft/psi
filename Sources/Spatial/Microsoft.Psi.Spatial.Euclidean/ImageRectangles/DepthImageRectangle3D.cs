// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents a depth image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    public class DepthImageRectangle3D : ImageRectangle3D<DepthImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the depth image.</param>
        /// <param name="depthImage">The depth image.</param>
        public DepthImageRectangle3D(Rectangle3D rectangle, Shared<DepthImage> depthImage)
            : base(rectangle, depthImage)
        {
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
                pixelValue = this.Image.Resource.GetPixel(u, v);
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
                this.Image.Resource.SetPixel(u, v, pixelValue);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
