// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that runs an ONNX model.
    /// </summary>
    /// <typeparam name="TIn">The type of input messages.</typeparam>
    /// <typeparam name="TOut">The type of output messages.</typeparam>
    /// <remarks>
    /// This class implements a \psi component that runs an ONNX model.
    /// The component is constructed via an <see cref="OnnxModelConfiguration"/>
    /// object that describes where to load the model from. In general, input
    /// data sent to a neural network contained in an ONNX model often needs
    /// some processing (e.g., image pixels need to be arranged in a specific
    /// order in the input tensor, etc.), and similarly the output vector must
    /// often be post-processed to obtain the desired, final results. This
    /// component allows the user to provide this input and output processing
    /// functions in the constructor.
    /// </remarks>
    public class OnnxModelRunner<TIn, TOut> : ConsumerProducer<TIn, TOut>, IDisposable
    {
        private readonly int inputVectorSize;
        private readonly Func<TIn, float[]> inputConstructor;
        private readonly Func<float[], TOut> outputConstructor;

        // helper class that actually runs the ONNX model.
        private OnnxModel onnxModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModelRunner{TIn, TOut}"/> class, based on a given configuration.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        /// <param name="inputConstructor">A function that constructs the network input (float array) from the component input.</param>
        /// <param name="outputConstuctor">A function that constructs the component output from the network output (float array).</param>
        /// <param name="name">An optional name for the component.</param>
        /// <remarks>The configuration parameter specifies the model filename, the name of the input and output vectors in
        /// that ONNX model, as well as the input vector size.</remarks>
        public OnnxModelRunner(
            Pipeline pipeline,
            OnnxModelConfiguration configuration,
            Func<TIn, float[]> inputConstructor,
            Func<float[], TOut> outputConstuctor,
            string name = nameof(OnnxModelRunner<TIn, TOut>))
            : base(pipeline, name)
        {
            this.inputVectorSize = configuration.InputVectorSize;
            this.inputConstructor = inputConstructor;
            this.outputConstructor = outputConstuctor;
            this.onnxModel = new OnnxModel(configuration);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.onnxModel?.Dispose();
            this.onnxModel = null;
        }

        /// <inheritdoc/>
        protected override void Receive(TIn input, Envelope envelope)
        {
            // construct the input vector
            var inputVector = this.inputConstructor(input);

            // check that the incoming data has the expected length
            if (inputVector.Length != this.inputVectorSize)
            {
                throw new InvalidDataException(
                    $"The input vector for the {nameof(OnnxModelRunner<TIn, TOut>)} has size {inputVector.Length}, which does " +
                    $"not match the input vector size ({this.inputVectorSize}) specified in configuration.");
            }

            // run the model, construct the output, and post the results
            var outputVector = this.onnxModel.GetPrediction(inputVector);
            var output = this.outputConstructor(outputVector);
            this.Out.Post(output, envelope.OriginatingTime);
        }
    }
}
