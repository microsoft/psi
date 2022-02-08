// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Adapter for encoded image rectangles.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageRectangle3DAdapter : StreamAdapter<EncodedImageRectangle3D, ImageRectangle3D>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override ImageRectangle3D GetAdaptedValue(EncodedImageRectangle3D source, Envelope envelope)
        {
            if (source != null)
            {
                var encodedImage = this.imageAdapter.GetAdaptedValue(source.Image, envelope);
                if (encodedImage != null)
                {
                    return new ImageRectangle3D(source.Rectangle3D, encodedImage);
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Dispose(ImageRectangle3D destination)
        {
            if (destination != null)
            {
                this.imageAdapter.Dispose(destination.Image);
            }
        }
    }

    /// <summary>
    /// Adapter for encoded depth image rectangles.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageRectangle3DAdapter : StreamAdapter<EncodedDepthImageRectangle3D, DepthImageRectangle3D>
    {
        private readonly EncodedDepthImageToDepthImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override DepthImageRectangle3D GetAdaptedValue(EncodedDepthImageRectangle3D source, Envelope envelope)
        {
            if (source != null)
            {
                var encodedDepthImage = this.imageAdapter.GetAdaptedValue(source.DepthImage, envelope);
                if (encodedDepthImage != null)
                {
                    return new DepthImageRectangle3D(source.Rectangle3D, encodedDepthImage);
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Dispose(DepthImageRectangle3D destination)
        {
            if (destination != null)
            {
                this.imageAdapter.Dispose(destination.DepthImage);
            }
        }
    }

    /// <summary>
    /// Adapter for list of nullable <see cref="EncodedDepthImageRectangle3D"/> to list of nullable <see cref="DepthImageRectangle3D"/>.
    /// </summary>
    [StreamAdapter]
    public class NullableEncodedDepthImageRectangle3DListAdapter : StreamAdapter<List<EncodedDepthImageRectangle3D>, List<DepthImageRectangle3D>>
    {
        private readonly EncodedDepthImageToDepthImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override List<DepthImageRectangle3D> GetAdaptedValue(List<EncodedDepthImageRectangle3D> source, Envelope envelope)
        {
            if (source != null)
            {
                List<DepthImageRectangle3D> outputList = new ();
                foreach (var inputRectangle in source)
                {
                    if (inputRectangle != null)
                    {
                        var encodedDepthImage = this.imageAdapter.GetAdaptedValue(inputRectangle.DepthImage, envelope);
                        if (encodedDepthImage != null)
                        {
                            outputList.Add(new DepthImageRectangle3D(inputRectangle.Rectangle3D, encodedDepthImage));
                        }
                    }
                }

                return outputList;
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Dispose(List<DepthImageRectangle3D> destination)
        {
            foreach (var imgRect in destination)
            {
                if (imgRect != null)
                {
                    this.imageAdapter.Dispose(imgRect.DepthImage);
                }
            }
        }

        /// <summary>
        /// Adapter for <see cref="Rectangle3D"/> to nullable <see cref="Rectangle3D"/>.
        /// </summary>
        [StreamAdapter]
        public class Rectangle3DToNullableAdapter : StreamAdapter<Rectangle3D, Rectangle3D?>
        {
            /// <inheritdoc/>
            public override Rectangle3D? GetAdaptedValue(Rectangle3D source, Envelope envelope)
                => source;
        }

        /// <summary>
        /// Adapter for list of <see cref="Rectangle3D"/> to list of nullable <see cref="Rectangle3D"/>.
        /// </summary>
        [StreamAdapter]
        public class Rectangle3DListToNullableAdapter : StreamAdapter<List<Rectangle3D>, List<Rectangle3D?>>
        {
            /// <inheritdoc/>
            public override List<Rectangle3D?> GetAdaptedValue(List<Rectangle3D> source, Envelope envelope)
                => source?.Select(p => p as Rectangle3D?).ToList();
        }

        /// <summary>
        /// Adapter for <see cref="Box3D"/> to nullable <see cref="Box3D"/>.
        /// </summary>
        [StreamAdapter]
        public class Box3DToNullableAdapter : StreamAdapter<Box3D, Box3D?>
        {
            /// <inheritdoc/>
            public override Box3D? GetAdaptedValue(Box3D source, Envelope envelope)
                => source;
        }

        /// <summary>
        /// Adapter for list of <see cref="Box3D"/> to list of nullable <see cref="Box3D"/>.
        /// </summary>
        [StreamAdapter]
        public class Box3DListToNullableAdapter : StreamAdapter<List<Box3D>, List<Box3D?>>
        {
            /// <inheritdoc/>
            public override List<Box3D?> GetAdaptedValue(List<Box3D> source, Envelope envelope)
                => source?.Select(p => p as Box3D?).ToList();
        }
    }
}