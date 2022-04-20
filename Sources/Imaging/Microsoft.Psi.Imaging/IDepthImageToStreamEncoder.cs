// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Defines a class that can encode a depth image.
    /// </summary>
    public interface IDepthImageToStreamEncoder
    {
        /// <summary>
        /// Gets the description of the depth image stream encoder.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Encodes a depth image into a stream.
        /// </summary>
        /// <param name="depthImage">Depth image to be encoded.</param>
        /// <param name="stream">Stream to encode the depth image into.</param>
        void EncodeToStream(DepthImage depthImage, Stream stream);
    }
}
