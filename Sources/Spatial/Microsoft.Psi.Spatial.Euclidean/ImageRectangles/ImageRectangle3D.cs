// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents an image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    public class ImageRectangle3D : ImageRectangle3D<Image>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the image.</param>
        /// <param name="image">The image.</param>
        public ImageRectangle3D(Rectangle3D rectangle, Shared<Image> image)
            : base(rectangle, image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D"/> class.
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
            Shared<Image> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, left, bottom, width, height), image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageRectangle3D"/> class.
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
        public ImageRectangle3D(double scale, Point3D origin, UnitVector3D widthAxis, UnitVector3D heightAxis, Shared<Image> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, 0, 0, image.Resource.Width * scale, image.Resource.Height * scale), image)
        {
        }

        /// <summary>
        /// Tries to get the nearest pixel value, first projecting the input point into the plane of the 3D rectangle
        /// to determine image space pixel coordinates.
        /// </summary>
        /// <param name="point">The desired point to project into the image rectangle and get a pixel value for.</param>
        /// <param name="r">Red channel's value (output).</param>
        /// <param name="g">Green channel's value (output).</param>
        /// <param name="b">Blue channel's value (output).</param>
        /// <param name="a">Alpha channel's value (output).</param>
        /// <returns>True if the point could be projected within the bounds of the image, false otherwise.</returns>
        public bool TryGetPixel(Point3D point, out int r, out int g, out int b, out int a)
        {
            if (this.TryGetPixelCoordinates(point, out int u, out int v))
            {
                (r, g, b, a) = this.Image.Resource.GetPixel(u, v);
                return true;
            }
            else
            {
                r = g = b = a = -1;
                return false;
            }
        }

        /// <summary>
        /// Tries to set the nearest pixel to a given value, first projecting the input point into the plane of the 3D rectangle
        /// to determine image space pixel coordinates.
        /// </summary>
        /// <param name="point">The desired point to project into the image rectangle and set a pixel value for.</param>
        /// <param name="r">Red channel's value.</param>
        /// <param name="g">Green channel's value.</param>
        /// <param name="b">Blue channel's value.</param>
        /// <param name="a">Alpha channel's value.</param>
        /// <returns>True if the point could be projected within the bounds of the image, false otherwise.</returns>
        public bool TrySetPixel(Point3D point, int r, int g, int b, int a)
        {
            if (this.TryGetPixelCoordinates(point, out int u, out int v))
            {
                this.Image.Resource.SetPixel(u, v, r, g, b, a);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to set the nearest pixel to a given gray value, first projecting the input point into the plane of the 3D rectangle
        /// to determine image space pixel coordinates.
        /// </summary>
        /// <param name="point">The desired point to project into the image rectangle and set a pixel value for.</param>
        /// <param name="gray">Gray value to set pixel to.</param>
        /// <returns>True if the point could be projected within the bounds of the image, false otherwise.</returns>
        public bool TrySetPixel(Point3D point, int gray)
        {
            if (this.TryGetPixelCoordinates(point, out int u, out int v))
            {
                this.Image.Resource.SetPixel(u, v, gray);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
