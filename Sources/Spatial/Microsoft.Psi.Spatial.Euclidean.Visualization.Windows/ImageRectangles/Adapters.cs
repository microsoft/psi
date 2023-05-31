// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.Collections.Generic;
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
                var encodedDepthImage = this.imageAdapter.GetAdaptedValue(source.Image, envelope);
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
                this.imageAdapter.Dispose(destination.Image);
            }
        }
    }

    /// <summary>
    /// Adapter for list of <see cref="EncodedImageRectangle3D"/> to list of <see cref="ImageRectangle3D"/>.
    /// </summary>
    [StreamAdapter]
    public class EncodedImageRectangle3DListAdapter : StreamAdapter<List<EncodedImageRectangle3D>, List<ImageRectangle3D>>
    {
        private readonly EncodedImageToImageAdapter imageAdapter = new ();

        /// <inheritdoc/>
        public override List<ImageRectangle3D> GetAdaptedValue(List<EncodedImageRectangle3D> source, Envelope envelope)
        {
            if (source != null)
            {
                List<ImageRectangle3D> outputList = new ();
                foreach (var inputRectangle in source)
                {
                    if (inputRectangle != null)
                    {
                        var encodedImage = this.imageAdapter.GetAdaptedValue(inputRectangle.Image, envelope);
                        if (encodedImage != null)
                        {
                            outputList.Add(new ImageRectangle3D(inputRectangle.Rectangle3D, encodedImage));
                        }
                    }
                }

                return outputList;
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Dispose(List<ImageRectangle3D> destination)
        {
            foreach (var imageRectangle3D in destination)
            {
                if (imageRectangle3D != null)
                {
                    this.imageAdapter.Dispose(imageRectangle3D.Image);
                }
            }
        }
    }

    /// <summary>
    /// Adapter for list of <see cref="EncodedDepthImageRectangle3D"/> to list of <see cref="DepthImageRectangle3D"/>.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageRectangle3DListAdapter : StreamAdapter<List<EncodedDepthImageRectangle3D>, List<DepthImageRectangle3D>>
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
                        var encodedDepthImage = this.imageAdapter.GetAdaptedValue(inputRectangle.Image, envelope);
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
            foreach (var depthImageRectangle3D in destination)
            {
                if (depthImageRectangle3D != null)
                {
                    this.imageAdapter.Dispose(depthImageRectangle3D.Image);
                }
            }
        }
    }
}