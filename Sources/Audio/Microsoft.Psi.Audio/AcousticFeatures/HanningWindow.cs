// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Provides a class for applying a Hanning window.
    /// </summary>
    internal sealed class HanningWindow
    {
        private float[] kernel;
        private float[] output;

        /// <summary>
        /// Initializes a new instance of the <see cref="HanningWindow"/> class.
        /// </summary>
        /// <param name="kernelLength">The Hanning window length.</param>
        public HanningWindow(int kernelLength)
        {
            this.kernel = new float[kernelLength];
            this.output = new float[kernelLength];
            this.ComputeHanningKernel(kernelLength);
        }

        /// <summary>
        /// Applies the Hanning window over the data.
        /// </summary>
        /// <param name="data">
        /// The data to apply the Hanning window to. This must be of the same size as the kernel.
        /// </param>
        /// <returns>The computed hannign window over the data.</returns>
        public float[] Apply(float[] data)
        {
            if (data.Length != this.output.Length)
            {
                throw new ArgumentException("Data must be of the same size as the kernel.");
            }

            for (int i = 0; i < data.Length; ++i)
            {
                this.output[i] = data[i] * this.kernel[i];
            }

            return this.output;
        }

        /// <summary>
        /// Computes the Hanning kernel.
        /// </summary>
        /// <param name="length">The desired length of the kernel.</param>
        private void ComputeHanningKernel(int length)
        {
            double x;
            for (int i = 0; i < length; i++)
            {
                x = (2.0 * Math.PI * (double)i) / (double)length;
                this.kernel[i] = (float)(0.5 * (1.0 - Math.Cos(x)));
            }
        }
    }
}