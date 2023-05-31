// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Represents an encoded depth image positioned in a 2D rectangle embedded in 3D space.
    /// </summary>
    public class EncodedDepthImageRectangle3D : ImageRectangle3D<EncodedDepthImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="rectangle">The rectangle in 3D space to contain the encoded depth image.</param>
        /// <param name="depthImage">The encoded depth image.</param>
        public EncodedDepthImageRectangle3D(Rectangle3D rectangle, Shared<EncodedDepthImage> depthImage)
            : base(rectangle, depthImage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="origin">The origin of the encoded depth image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the encoded depth image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the encoded depth image rectangle.</param>
        /// <param name="left">The left edge of the encoded depth image rectangle (relative to origin along the width axis).</param>
        /// <param name="bottom">The bottom edge of the encoded depth image rectangle (relative to origin along the height axis).</param>
        /// <param name="width">The width of the encoded depth image rectangle.</param>
        /// <param name="height">The height of the encoded depth image rectangle.</param>
        /// <param name="depthImage">The encoded depth image.</param>
        /// <remarks>
        /// The edges of the encoded depth image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public EncodedDepthImageRectangle3D(
            Point3D origin,
            UnitVector3D widthAxis,
            UnitVector3D heightAxis,
            double left,
            double bottom,
            double width,
            double height,
            Shared<EncodedDepthImage> depthImage)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, left, bottom, width, height), depthImage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedDepthImageRectangle3D"/> class.
        /// </summary>
        /// <param name="scale">The scale to use when calculating metric corner offsets from depth image pixel width and height.</param>
        /// <param name="origin">The origin of the encoded depth image rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the encoded depth image rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the encoded depth image rectangle.</param>
        /// <param name="depthImage">The encoded depth image.</param>
        /// <remarks>
        /// The (left, bottom) corner of the encoded depth image rectangle is set to the origin (0, 0), and (width, height) are calculated from multiplying
        /// the encoded depth image pixel width and height respectively by a scaling parameter.
        /// The edges of the encoded depth image rectangle are aligned to the specified width and height axes.
        /// </remarks>
        public EncodedDepthImageRectangle3D(double scale, Point3D origin, UnitVector3D widthAxis, UnitVector3D heightAxis, Shared<EncodedDepthImage> depthImage)
            : this(new Rectangle3D(origin, widthAxis, heightAxis, 0, 0, depthImage.Resource.Width * scale, depthImage.Resource.Height * scale), depthImage)
        {
        }
    }
}
