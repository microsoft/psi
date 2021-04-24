// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a shared depth image to a shared image.
    /// </summary>
    [StreamAdapter]
    public class DepthImageToImageAdapter : StreamAdapter<Shared<DepthImage>, Shared<Image>>
    {
        /// <inheritdoc/>
        public override Shared<Image> GetAdaptedValue(Shared<DepthImage> source, Envelope envelope)
        {
            Shared<Image> sharedImage = null;

            if ((source != null) && (source.Resource != null))
            {
                sharedImage = ImagePool.GetOrCreate(source.Resource.Width, source.Resource.Height, PixelFormat.Gray_16bpp);
                sharedImage.Resource.CopyFrom(source.Resource);
            }

            return sharedImage;
        }

        /// <inheritdoc/>
        public override void Dispose(Shared<Image> destination) =>
            destination?.Dispose();
    }
}
