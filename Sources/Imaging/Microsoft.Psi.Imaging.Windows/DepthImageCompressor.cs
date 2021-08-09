﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Implements a compressor used by the serialization layer for compressing streams
    /// of depth images in a generic fashion. This object should not be called directly,
    /// but instead is used by the <see cref="DepthImage.CustomSerializer"/> class.
    /// </summary>
    public class DepthImageCompressor : IDepthImageCompressor
    {
        private readonly IDepthImageToStreamEncoder encoder = null;
        private readonly IDepthImageFromStreamDecoder decoder = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthImageCompressor"/> class.
        /// </summary>
        /// <param name="depthCompressionMethod">The depth image compression method to be used.</param>
        public DepthImageCompressor(DepthCompressionMethod depthCompressionMethod)
        {
            this.DepthCompressionMethod = depthCompressionMethod;
            switch (this.DepthCompressionMethod)
            {
                case DepthCompressionMethod.Png:
                    this.encoder = new DepthImageToPngStreamEncoder();
                    break;
                case DepthCompressionMethod.Tiff:
                    this.encoder = new DepthImageToTiffStreamEncoder();
                    break;
                case DepthCompressionMethod.None:
                    break;
            }

            this.decoder = new DepthImageFromStreamDecoder();
        }

        /// <inheritdoc/>
        public DepthCompressionMethod DepthCompressionMethod { get; set; } = DepthCompressionMethod.Png;

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, DepthImage depthImage, SerializationContext context)
        {
            if (this.encoder != null)
            {
                using var sharedEncodedDepthImage = EncodedDepthImagePool.GetOrCreate(
                    depthImage.Width, depthImage.Height);
                sharedEncodedDepthImage.Resource.EncodeFrom(depthImage, this.encoder);
                Serializer.Serialize(writer, sharedEncodedDepthImage, context);
            }
            else
            {
                Serializer.Serialize(writer, depthImage, context);
            }
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, ref DepthImage depthImage, SerializationContext context)
        {
            Shared<EncodedDepthImage> sharedEncodedDepthImage = null;
            Serializer.Deserialize(reader, ref sharedEncodedDepthImage, context);
            using var sharedDepthImage = DepthImagePool.GetOrCreate(sharedEncodedDepthImage.Resource.Width, sharedEncodedDepthImage.Resource.Height);
            sharedDepthImage.Resource.DecodeFrom(sharedEncodedDepthImage.Resource, this.decoder);
            depthImage = sharedDepthImage.Resource.DeepClone();
            sharedEncodedDepthImage.Dispose();
        }
    }
}
