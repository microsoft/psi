// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Defines type of compression to use when serializing out a <see cref="DepthImage"/>.
    /// </summary>
    public enum CompressionMethod
    {
        /// <summary>
        /// Use JPEG compression.
        /// </summary>
        Jpeg,

        /// <summary>
        /// Use PNG compression.
        /// </summary>
        Png,

        /// <summary>
        /// Use no compression.
        /// </summary>
        None,
    }

    /// <summary>
    /// Defines a interface for image compressors.
    /// </summary>
    public interface IImageCompressor
    {
        /// <summary>
        /// Gets or sets the compression method used by compressor.
        /// </summary>
        CompressionMethod CompressionMethod { get; set; }

        /// <summary>
        /// Serialize compressor.
        /// </summary>
        /// <param name="writer">Writer to which to serialize.</param>
        /// <param name="image">Image instance to serialize.</param>
        /// <param name="context">Serialization context.</param>
        void Serialize(BufferWriter writer, Image image, SerializationContext context);

        /// <summary>
        /// Deserialize compressor.
        /// </summary>
        /// <param name="reader">Reader from which to deserialize.</param>
        /// <param name="image">Target image to which to deserialize.</param>
        /// <param name="context">Serialization context.</param>
        void Deserialize(BufferReader reader, ref Image image, SerializationContext context);
    }
}
