// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Defines a class that can decode an image.
    /// </summary>
    public interface IImageFromStreamDecoder
    {
        /// <summary>
        /// Decodes an encoded image from a stream into a specified image.
        /// </summary>
        /// <param name="stream">Stream containing the encoded image.</param>
        /// <param name="image">The image to decode into.</param>
        void DecodeFromStream(Stream stream, Image image);

        /// <summary>
        /// Gets the pixel format of an encoded image from a stream.
        /// </summary>
        /// <param name="stream">Stream containing the encoded image.</param>
        /// <returns>The pixel format.</returns>
        PixelFormat GetPixelFormat(Stream stream);
    }
}
