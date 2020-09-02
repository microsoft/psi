// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that runs an ONNX model.
    /// </summary>
    /// <remarks>
    /// This class implements a \psi component that runs a simple ONNX model.
    /// It expects an input stream containing a vector of floats, and produces
    /// an output stream containing a vector of floats. This component is
    /// constructed by specifying an <see cref="OnnxModelConfiguration"/>
    /// object that describes where to load the model from, and runs the
    /// bare-bones model. In general, input data sent to a neural network
    /// contained in an ONNX model often needs some processing (e.g.,
    /// image pixels need to be arranged in a specific order in the input
    /// tensor, etc.), and similarly the output vector must often be post-
    /// processed to obtain the desired, final results.
    /// </remarks>
    public class OnnxModelRunner : ConsumerProducer<float[], float[]>
    {
        private readonly int inputVectorSize;

        // helper class that actually runs the ONNX model.
        private readonly OnnxModel onnxModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModelRunner"/> class, based on a given configuration.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        /// <remarks>The configuration parameter specifies the model filename, the
        /// name of the input and output vectors in that ONNX model, as well as
        /// the input vector size.</remarks>
        public OnnxModelRunner(Pipeline pipeline, OnnxModelConfiguration configuration)
            : base(pipeline)
        {
            this.inputVectorSize = configuration.InputVectorSize;
            this.onnxModel = new OnnxModel(configuration);
        }

        /// <inheritdoc/>
        protected override void Receive(float[] data, Envelope envelope)
        {
            // check that the incoming data has the expected length
            if (data.Length != this.inputVectorSize)
            {
                throw new InvalidDataException(
                    $"The input vector for the {nameof(OnnxModelRunner)} has size {data.Length}, which does " +
                    $"not match the input vector size ({this.inputVectorSize}) specified in configuration.");
            }

            // run the model and post the results.
            this.Out.Post(this.onnxModel.GetPrediction(data), envelope.OriginatingTime);
        }
    }
}
