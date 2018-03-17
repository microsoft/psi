// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that computes the Spectral entropy.
    /// </summary>
    public sealed class SpectralEntropy : ConsumerProducer<float[], float>
    {
        private int start;
        private int end;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectralEntropy"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="start">The starting frequency of the band.</param>
        /// <param name="end">The ending frequency of the band.</param>
        public SpectralEntropy(Pipeline pipeline, int start, int end)
            : base(pipeline)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="data">A buffer containing the input data.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(float[] data, Envelope e)
        {
            this.Out.Post(this.ComputeSpectralEntropy(data, this.start, this.end), e.OriginatingTime);
        }

        /// <summary>
        /// Computes the spectral entropy within a frequency band.
        /// </summary>
        /// <param name="power">The power spectrum.</param>
        /// <param name="start">The starting frequency of the band.</param>
        /// <param name="end">The ending frequency of the band.</param>
        /// <returns>The spectral entropy in the frequency band.</returns>
        private float ComputeSpectralEntropy(float[] power, int start, int end)
        {
            float entropy = 0.0f;
            float prob;
            float energy = 0.0f;

            for (int i = start; i <= end; i++)
            {
                energy += power[i];
            }

            for (int i = start; i <= end; i++)
            {
                prob = power[i] / energy;
                entropy -= prob * (float)Math.Log10(prob);
            }

            int entropyNumBands = end - start + 1;
            entropy /= (float)Math.Log10(entropyNumBands);

            return entropy;
        }
    }
}
