// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Defines a class that can decode a depth image.
    /// </summary>
    public interface IDepthImageFromStreamDecoder
    {
        /// <summary>
        /// Decodes an encoded depth image from a stream into a given depth image.
        /// </summary>
        /// <param name="stream">Stream containing the encoded depth image.</param>
        /// <param name="depthImage">The depth image to decode into.</param>
        void DecodeFromStream(Stream stream, DepthImage depthImage);
    }
}
