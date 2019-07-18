// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Defines type of compression to use when serializing out an Image.
    /// </summary>
    public enum CompressionMethod
    {
        /// <summary>
        /// Use JPEG compression
        /// </summary>
        JPEG,

        /// <summary>
        /// Use PNG compression
        /// </summary>
        PNG,

        /// <summary>
        /// Use no compression
        /// </summary>
        None,
    }

    /// <summary>
    /// Interface implemented by the system specific assembly.
    /// For instance, Microsoft.Psi.Imaging.Windows will define
    /// an ImageCompressor that implements this interfaces.
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
        /// <param name="instance">Image instance to serialize.</param>
        /// <param name="context">Serialization context.</param>
        void Serialize(BufferWriter writer, Image instance, SerializationContext context);

        /// <summary>
        /// Deserialize compressor.
        /// </summary>
        /// <param name="reader">Reader from which to deserialize.</param>
        /// <param name="target">Target image to which to deserialize.</param>
        /// <param name="context">Serialization context.</param>
        void Deserialize(BufferReader reader, ref Image target, SerializationContext context);
    }
}
