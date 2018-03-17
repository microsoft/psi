// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    using System.IO;

    /// <summary>
    /// Bitmap encoder interface.
    /// </summary>
    public interface IBitmapEncoder
    {
        /// <summary>
        /// Encode image to stream.
        /// </summary>
        /// <param name="image">Image to encode</param>
        /// <param name="stream">Stream to which to encode</param>
        void Encode(Image image, Stream stream);
    }
}
