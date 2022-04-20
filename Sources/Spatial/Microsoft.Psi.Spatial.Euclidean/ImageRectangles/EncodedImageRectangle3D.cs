// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents an encoded image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    public class EncodedImageRectangle3D : ImageRectangle3D<EncodedImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageRectangle3D"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the encoded image.</param>
        /// <param name="image">The encoded image.</param>
        public EncodedImageRectangle3D(Rectangle3D rectangle, Shared<EncodedImage> image)
            : base(rectangle, image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageRectangle3D"/> class.
        /// </summary>
        /// <param name="origin">The origin of the encoded image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the encoded image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the encoded image rectangle.</param>
        /// <param name="left">The left edge of the encoded image rectangle (relative to origin along the width axis).</param>
        /// <param name="bottom">The bottom edge of the encoded image rectangle (relative to origin along the height axis).</param>
        /// <param name="width">The width of the encoded image rectangle.</param>
        /// <param name="height">The height of the encoded image rectangle.</param>
        /// <param name="image">The encoded image.</param>
        /// <remarks>
        /// The edges of the encoded image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public EncodedImageRectangle3D(
            Point3D origin,
            UnitVector3D widthAxis,
            UnitVector3D heightAxis,
            double left,
            double bottom,
            double width,
            double height,
            Shared<EncodedImage> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, left, bottom, width, height), image)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageRectangle3D"/> class.
        /// </summary>
        /// <param name="scale">The scale to use when calculating metric corner offsets from image pixel width and height.</param>
        /// <param name="origin">The origin of the encoded image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the encoded image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the encoded image rectangle.</param>
        /// <param name="image">The encoded image.</param>
        /// <remarks>
        /// The (left, bottom) corner of the encoded image rectangle is set to the origin (0, 0), and (width, height) are calculated from multiplying
        /// the encoded image pixel width and height respectively by a scaling parameter.
        /// The edges of the encoded image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public EncodedImageRectangle3D(double scale, Point3D origin, UnitVector3D widthAxis, UnitVector3D heightAxis, Shared<EncodedImage> image)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, 0, 0, image.Resource.Width * scale, image.Resource.Height * scale), image)
        {
        }
    }
}
