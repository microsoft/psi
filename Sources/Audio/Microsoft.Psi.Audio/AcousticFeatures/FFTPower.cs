// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that computes the FFT power.
    /// </summary>
    public sealed class FFTPower : ConsumerProducer<float[], float[]>
    {
        private float[] fftPower;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFTPower"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        public FFTPower(Pipeline pipeline)
            : base(pipeline)
        {
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="data">A buffer containing the input data.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(float[] data, Envelope e)
        {
            this.ComputeFFTPower(data, ref this.fftPower);
            this.Out.Post(this.fftPower, e.OriginatingTime);
        }

        /// <summary>
        /// Compute the FFT power.
        /// </summary>
        /// <param name="fft">The input data.</param>
        /// <param name="output">
        /// An array which will hold the output values. If this array is not initialized
        /// or is not of size fftSize on input, it will be allocated and assigned.
        /// </param>
        private void ComputeFFTPower(float[] fft, ref float[] output)
        {
            int halfFftSize = fft.Length >> 1;
            if ((output == null) || (output.Length != halfFftSize))
            {
                output = new float[halfFftSize];
            }

            int i, j;

            for (int cnt = 0; cnt < halfFftSize; cnt++)
            {
                i = cnt + (cnt & ~3);
                j = i + 4;
                output[cnt] = (fft[i] * fft[i]) + (fft[j] * fft[j]);
            }
        }
    }
}
