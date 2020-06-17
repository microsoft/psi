// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Defines a class that can encode an image.
    /// </summary>
    public interface IImageToStreamEncoder
    {
        /// <summary>
        /// Encodes an image into a stream.
        /// </summary>
        /// <param name="image">Image to be encoded.</param>
        /// <param name="stream">Stream to encode the image into.</param>
        void EncodeToStream(Image image, Stream stream);
    }
}
