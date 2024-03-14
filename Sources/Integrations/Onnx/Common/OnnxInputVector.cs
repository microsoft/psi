// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Represents a vector input for an ONNX model.
    /// </summary>
    /// <typeparam name="T">The input data type of the model.</typeparam>
    internal class OnnxInputVector<T>
    {
        /// <summary>
        /// Gets or sets the vector data.
        /// </summary>
        public T[] Vector { get; set; }
    }
}
