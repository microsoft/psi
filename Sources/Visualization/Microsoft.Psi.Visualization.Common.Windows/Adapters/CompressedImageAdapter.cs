// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.IO;
    using System.IO.Compression;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of compresed images into <see cref="Image"/>s.
    /// </summary>
    [StreamAdapter]
    public class CompressedImageAdapter : StreamAdapter<byte[], Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedImageAdapter"/> class.
        /// </summary>
        public CompressedImageAdapter()
            : base(Adapter)
        {
        }

        private static Shared<Image> Adapter(byte[] value, Envelope env)
        {
            var buffer = new byte[424 * 512 * 2];
            using (var compressedStream = new GZipStream(new MemoryStream(value), CompressionMode.Decompress))
            {
                compressedStream.Read(buffer, 0, buffer.Length);
            }

            var psiImage = ImagePool.GetOrCreate(512, 424, PixelFormat.Gray_16bpp);
            psiImage.Resource.CopyFrom(buffer);
            return psiImage;
        }
    }
}