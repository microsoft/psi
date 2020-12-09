// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Represents a labeled prediction.
    /// </summary>
    public class LabeledPrediction
    {
        /// <summary>
        /// Gets or sets the class label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the confidence level.
        /// </summary>
        public float Confidence { get; set; }
    }
}
