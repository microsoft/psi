// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Implements a helper class for running ONNX models which have simple one-dimensional
    /// floating point input and output vectors.
    /// </summary>
    /// <remarks>This class implements the ability to run a specified ONNX model.
    /// It does so by leveraging the ML.NET framework. The
    /// <see cref="OnnxModelConfiguration"/> object specified at construction
    /// time provides information about where to load the network from, etc.</remarks>
    public class OnnxModel : OnnxModel<float, float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModel"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for the onnx model runner.</param>
        public OnnxModel(OnnxModelConfiguration configuration)
            : base(configuration)
        {
        }
    }
}
