// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that performs an FFT on a stream of sample buffers.
    /// </summary>
    public sealed class FFT : ConsumerProducer<float[], float[]>
    {
        private FastFourierTransform fft;
        private float[] fftOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFT"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="fftSize">The FFT size.</param>
        /// <param name="inputSize">The window size.</param>
        public FFT(Pipeline pipeline, int fftSize, int inputSize)
            : base(pipeline)
        {
            this.fft = new FastFourierTransform(fftSize, inputSize);
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="data">A buffer containing the input data.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(float[] data, Envelope e)
        {
            this.fft.ComputeFFT(data, ref this.fftOutput);
            this.Out.Post(this.fftOutput, e.OriginatingTime);
        }
    }
}
