// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Represents a vector input for an ONNX model.
    /// </summary>
    internal class OnnxInputVector
    {
        /// <summary>
        /// Gets or sets the vector data.
        /// </summary>
        public float[] Vector { get; set; }
    }
}
