// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Represents the configuration for the <see cref="MaskRCNNModelRunner"/> class.
    /// </summary>
    public class MaskRCNNModelConfiguration
    {
        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// Gets or sets the model file name (see remarks on <see cref="MaskRCNNModelRunner"/>).
        /// </summary>
        public string ModelFileName { get; set; }

        /// <summary>
        /// Gets or sets the classes file name (see remarks on <see cref="MaskRCNNModelRunner"/>).
        /// </summary>
        public string ClassesFileName { get; set; }

        /// <summary>
        /// Gets or sets the confidence threshold to use in filtering results.
        /// </summary>
        public float ConfidenceThreshold { get; set; } = 0.3f;

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
