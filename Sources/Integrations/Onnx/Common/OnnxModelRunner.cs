// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using Microsoft.Psi;

    /// <summary>
    /// Component that runs an ONNX model.
    /// </summary>
    /// <remarks>
    /// This class implements a \psi component that runs a simple ONNX model.
    /// It expects an input stream containing a vector of floats, and produces
    /// an output stream containing a vector of floats. This component is
    /// constructed by specifying an <see cref="OnnxModelConfiguration"/>
    /// object that describes where to load the model from, and runs the
    /// bare-bones model.
    /// </remarks>
    public class OnnxModelRunner : OnnxModelRunner<float[], float[]>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModelRunner"/> class, based on a given configuration.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <remarks>The configuration parameter specifies the model filename, the
        /// name of the input and output vectors in that ONNX model, as well as
        /// the input vector size.</remarks>
        public OnnxModelRunner(
            Pipeline pipeline,
            OnnxModelConfiguration configuration,
            string name = nameof(OnnxModelRunner))
            : base (pipeline, configuration, i => i, o => o, name)
        {
        }
    }
}
