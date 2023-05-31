// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from shared encoded depth image to shared depth image.
    /// </summary>
    [StreamAdapter]
    public class EncodedDepthImageToDepthImageAdapter : StreamAdapter<Shared<EncodedDepthImage>, Shared<DepthImage>>
    {
        /// <inheritdoc/>
        public override Shared<DepthImage> GetAdaptedValue(Shared<EncodedDepthImage> source, Envelope envelope)
        {
            Shared<DepthImage> sharedDepthImage = null;

            if (source != null && source.Resource != null)
            {
                sharedDepthImage = DepthImagePool.GetOrCreate(
                    source.Resource.Width,
                    source.Resource.Height,
                    source.Resource.DepthValueSemantics,
                    source.Resource.DepthValueToMetersScaleFactor);
                var decoder = new DepthImageFromStreamDecoder();
                decoder.DecodeFromStream(source.Resource.ToStream(), sharedDepthImage.Resource);
            }

            return sharedDepthImage;
        }

        /// <inheritdoc/>
        public override void Dispose(Shared<DepthImage> destination) =>
            destination?.Dispose();
    }
}
