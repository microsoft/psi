// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from shared encoded depth image to shared image.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToImageAdapter : StreamAdapter<Shared<EncodedDepthImage>, Shared<Image>>
    {
        /// <inheritdoc/>
        public override Shared<Image> GetAdaptedValue(Shared<EncodedDepthImage> source, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if ((source != null) && (source.Resource != null))
            {
                using var sharedDepthImage = DepthImagePool.GetOrCreate(
                    source.Resource.Width,
                    source.Resource.Height,
                    source.Resource.DepthValueSemantics,
                    source.Resource.DepthValueToMetersScaleFactor);
                sharedImage = ImagePool.GetOrCreate(source.Resource.Width, source.Resource.Height, PixelFormat.Gray_16bpp);
                var decoder = new DepthImageFromStreamDecoder();
                decoder.DecodeFromStream(source.Resource.ToStream(), sharedDepthImage.Resource);
                sharedDepthImage.Resource.CopyTo(sharedImage.Resource);
            }

            return sharedImage;
        }

        /// <inheritdoc/>
        public override void Dispose(Shared<Image> destination) =>
            destination?.Dispose();
    }
}
