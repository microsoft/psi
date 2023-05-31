// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Defines type of compression to use when serializing out a <see cref="DepthImage"/>.
    /// </summary>
    public enum DepthCompressionMethod
    {
        /// <summary>
        /// Use PNG compression.
        /// </summary>
        Png,

        /// <summary>
        /// Use TIFF compression.
        /// </summary>
        Tiff,

        /// <summary>
        /// Use no compression.
        /// </summary>
        None,
    }

    /// <summary>
    /// Defines a interface for depth image compressors.
    /// </summary>
    public interface IDepthImageCompressor
    {
        /// <summary>
        /// Gets or sets the compression method used by compressor.
        /// </summary>
        DepthCompressionMethod DepthCompressionMethod { get; set; }

        /// <summary>
        /// Serialize compressor.
        /// </summary>
        /// <param name="writer">Writer to which to serialize.</param>
        /// <param name="depthImage">Depth image instance to serialize.</param>
        /// <param name="context">Serialization context.</param>
        void Serialize(BufferWriter writer, DepthImage depthImage, SerializationContext context);

        /// <summary>
        /// Deserialize compressor.
        /// </summary>
        /// <param name="reader">Reader from which to deserialize.</param>
        /// <param name="depthImage">Target depth image to which to deserialize.</param>
        /// <param name="context">Serialization context.</param>
        void Deserialize(BufferReader reader, ref DepthImage depthImage, SerializationContext context);
    }
}
