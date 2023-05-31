// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the configuration for the <see cref="OnnxModel"/> class.
    /// </summary>
    /// <remarks>The configuration contains model filename, the name of the
    /// input and output vectors in that ONNX model, as well as the input vector
    /// size.</remarks>
    public class OnnxModelConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModelConfiguration"/> class.
        /// </summary>
        public OnnxModelConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the model file name.
        /// </summary>
        public string ModelFileName { get; set; }

        /// <summary>
        /// Gets or sets the size of the input vector.
        /// </summary>
        public int InputVectorSize { get; set; }

        /// <summary>
        /// Gets or sets the name of the input vector.
        /// </summary>
        public string InputVectorName { get; set; }

        /// <summary>
        /// Gets or sets the name of the output vector.
        /// </summary>
        public string OutputVectorName { get; set; }

        /// <summary>
        /// Gets or sets an optional shape dictionary describing the shape of input vector.
        /// </summary>
        public IDictionary<string, int[]> ShapeDictionary { get; set; }

        /// <summary>
        /// Gets or sets the GPU device ID to run execution on, or null to run on CPU.
        /// </summary>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public int? GpuDeviceId { get; set; }
    }
}
