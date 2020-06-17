// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Provides a class for computing a Fast Fourier Transform.
    /// </summary>
    internal sealed class FastFourierTransform
    {
        private readonly int fftSize;         // FFT size
        private readonly int windowSize;      // Size of the data window.  FFTSize - WindowSize = ZeroPadSize
        private readonly int fftPow2;         // FFT size in form of POW of 2

        private readonly float[] alignedWriFactors;  // SinCos(theta) array - 16 byte aligned
        private readonly short[] revMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastFourierTransform"/> class.
        /// </summary>
        /// <param name="fftSize">The FFT size.</param>
        /// <param name="windowSize">The window size.</param>
        public FastFourierTransform(int fftSize, int windowSize)
        {
            this.fftSize = fftSize;
            this.windowSize = windowSize;
            this.fftPow2 = 1;
            int size = 2;
            while (size < fftSize)
            {
                size <<= 1;
                this.fftPow2++;
            }

            this.alignedWriFactors = new float[this.fftSize * 2];
            this.revMap = new short[this.fftSize / 2];
            this.alignedWriFactors[0] = 1.0f;
            this.alignedWriFactors[1] = -1.0f;
            this.alignedWriFactors[2] = 1.0f;
            this.alignedWriFactors[3] = -1.0f;
            this.alignedWriFactors[this.fftSize] = 0.5f;
            this.alignedWriFactors[this.fftSize + 1] = 0.5f;
            this.alignedWriFactors[this.fftSize + 2] = 0.5f;
            this.alignedWriFactors[this.fftSize + 3] = 0.5f;

            int i, j, k, limit;
            for (i = 2, k = 4, limit = 8; i < this.fftPow2; i++, limit *= 2)
            {
                float theta = (float)(2 * Math.PI / limit);
                for (j = 0; j < limit / 2; j++)
                {
                    this.alignedWriFactors[k] = (float)Math.Cos(theta * j);
                    this.alignedWriFactors[this.fftSize + k] = (float)Math.Sin(theta * j);
                    k++;
                }
            }

            for (i = 0, j = 0; i < ((this.fftSize / 2) - 1); i++)
            {
                this.revMap[i] = (short)((j * 2 < this.windowSize) ? 2 * j : -1);
                k = this.fftSize >> 2;
                while (j >= k)
                {
                    j -= k;
                    k >>= 1;
                }

                j += k;
            }

            this.revMap[i] = (short)((i * 2 < this.windowSize) ? 2 * i : -1);
        }

        /// <summary>
        /// Computes an FFT.
        /// </summary>
        /// <param name="input">The input data on which to apply the FFT.</param>
        /// <param name="output">
        /// An array which will hold the output values. If this array is not initialized
        /// or is not of size fftSize on input, it will be allocated and assigned.
        /// </param>
        public void ComputeFFT(float[] input, ref float[] output)
        {
            if ((output == null) || (output.Length != this.fftSize))
            {
                output = new float[this.fftSize];
            }

            float[] x = output;
            float[] y = input;
            int i, j, ii, jj, n, m;

            n = this.fftSize;
            m = this.fftSize >> 1;

            float zeroVal = 0;
            for (ii = i = 0; ii < m; ii += 4, i += 8)
            {
                j = this.revMap[ii];
                if (j < 0)
                {
                    x[i] = zeroVal;
                    x[i + 4] = zeroVal;
                }
                else
                {
                    x[i] = y[j];
                    x[i + 4] = y[j + 1];
                }

                j = this.revMap[ii + 1];
                if (j < 0)
                {
                    x[i + 1] = zeroVal;
                    x[i + 5] = zeroVal;
                }
                else
                {
                    x[i + 1] = y[j];
                    x[i + 5] = y[j + 1];
                }

                j = this.revMap[ii + 2];
                if (j < 0)
                {
                    x[i + 2] = zeroVal;
                    x[i + 6] = zeroVal;
                }
                else
                {
                    x[i + 2] = y[j];
                    x[i + 6] = y[j + 1];
                }

                j = this.revMap[ii + 3];
                if (j < 0)
                {
                    x[i + 3] = zeroVal;
                    x[i + 7] = zeroVal;
                }
                else
                {
                    x[i + 3] = y[j];
                    x[i + 7] = y[j + 1];
                }
            }

            float wi, wr;
            float xre, xri;

            // Length two and four simple (and done together)
            for (i = 0; i < n; i += 8)
            {
                // Length two
                xre = x[i + 1];
                xri = x[i + 5];
                x[i + 1] = x[i] - xre;
                x[i + 5] = x[i + 4] - xri;
                x[i] = x[i] + xre;
                x[i + 4] = x[i + 4] + xri;

                xre = x[i + 3];
                xri = x[i + 7];
                x[i + 3] = x[i + 2] - xre;
                x[i + 7] = x[i + 6] - xri;
                x[i + 2] = x[i + 2] + xre;
                x[i + 6] = x[i + 6] + xri;

                // Length four
                xre = x[i + 2];
                xri = x[i + 6];
                x[i + 2] = x[i] - xre;
                x[i + 6] = x[i + 4] - xri;
                x[i] = x[i] + xre;
                x[i + 4] = x[i + 4] + xri;

                xre = -x[i + 7];
                xri = x[i + 3];
                x[i + 3] = x[i + 1] - xre;
                x[i + 7] = x[i + 5] - xri;
                x[i + 1] = x[i + 1] + xre;
                x[i + 5] = x[i + 5] + xri;
            }

            int kk = 4;
            int incr = 8, limit = 4;
            float[] wriFactors = this.alignedWriFactors;
            while (incr < n)
            {
                for (jj = 0; jj < n; jj += incr)
                {
                    for (i = jj, jj = j = jj + incr; i < jj; i += 8, j += 8, kk += 4)
                    {
                        wr = wriFactors[kk];
                        wi = wriFactors[kk + n];

                        xre = (wr * x[j]) - (wi * x[j + 4]);
                        xri = (wr * x[j + 4]) + (wi * x[j]);
                        x[j] = x[i] - xre;
                        x[j + 4] = x[i + 4] - xri;
                        x[i] = x[i] + xre;
                        x[i + 4] = x[i + 4] + xri;

                        wr = wriFactors[kk + 1];
                        wi = wriFactors[kk + n + 1];
                        xre = (wr * x[j + 1]) - (wi * x[j + 5]);
                        xri = (wr * x[j + 5]) + (wi * x[j + 1]);
                        x[j + 1] = x[i + 1] - xre;
                        x[j + 5] = x[i + 5] - xri;
                        x[i + 1] = x[i + 1] + xre;
                        x[i + 5] = x[i + 5] + xri;

                        wr = wriFactors[kk + 2];
                        wi = wriFactors[kk + n + 2];
                        xre = (wr * x[j + 2]) - (wi * x[j + 6]);
                        xri = (wr * x[j + 6]) + (wi * x[j + 2]);
                        x[j + 2] = x[i + 2] - xre;
                        x[j + 6] = x[i + 6] - xri;
                        x[i + 2] = x[i + 2] + xre;
                        x[i + 6] = x[i + 6] + xri;

                        wr = wriFactors[kk + 3];
                        wi = wriFactors[kk + n + 3];
                        xre = (wr * x[j + 3]) - (wi * x[j + 7]);
                        xri = (wr * x[j + 7]) + (wi * x[j + 3]);
                        x[j + 3] = x[i + 3] - xre;
                        x[j + 7] = x[i + 7] - xri;
                        x[i + 3] = x[i + 3] + xre;
                        x[i + 7] = x[i + 7] + xri;
                    }

                    kk -= limit;
                }

                kk += limit;
                limit = incr;
                incr += incr;
            }

            float xr1, xi1, xr2, xi2;
            float wrr2, wri2, wir2, wii2;

            kk = m;
            i = 0;
            j = n;

            xr1 = x[0];
            x[0] = xr1 + x[4];
            x[4] = 0;

            wr = wriFactors[kk + 1];
            wi = wriFactors[kk + 1 + n];

            xr1 = (x[i + 1] + x[j - 5]) / 2f;
            xi1 = (x[i + 5] - x[j - 1]) / 2f;
            xr2 = (x[i + 5] + x[j - 1]) / 2f;
            xi2 = (x[j - 5] - x[i + 1]) / 2f;

            wrr2 = wr * xr2;
            wri2 = wr * xi2;
            wir2 = wi * xr2;
            wii2 = wi * xi2;

            x[i + 1] = xr1 + wrr2 - wii2;
            x[i + 5] = xi1 + wri2 + wir2;
            x[j - 5] = xr1 - wrr2 + wii2;
            x[j - 1] = wri2 + wir2 - xi1;

            wr = wriFactors[kk + 2];
            wi = wriFactors[kk + 2 + n];

            xr1 = (x[i + 2] + x[j - 6]) / 2f;
            xi1 = (x[i + 6] - x[j - 2]) / 2f;
            xr2 = (x[i + 6] + x[j - 2]) / 2f;
            xi2 = (x[j - 6] - x[i + 2]) / 2f;

            wrr2 = wr * xr2;
            wri2 = wr * xi2;
            wir2 = wi * xr2;
            wii2 = wi * xi2;

            x[i + 2] = xr1 + wrr2 - wii2;
            x[i + 6] = xi1 + wri2 + wir2;
            x[j - 6] = xr1 - wrr2 + wii2;
            x[j - 2] = wri2 + wir2 - xi1;

            wr = wriFactors[kk + 3];
            wi = wriFactors[kk + 3 + n];
            kk += 4;

            xr1 = (x[i + 3] + x[j - 7]) / 2f;
            xi1 = (x[i + 7] - x[j - 3]) / 2f;
            xr2 = (x[i + 7] + x[j - 3]) / 2f;
            xi2 = (x[j - 7] - x[i + 3]) / 2f;

            wrr2 = wr * xr2;
            wri2 = wr * xi2;
            wir2 = wi * xr2;
            wii2 = wi * xi2;

            x[i + 3] = xr1 + wrr2 - wii2;
            x[i + 7] = xi1 + wri2 + wir2;
            x[j - 7] = xr1 - wrr2 + wii2;
            x[j - 3] = wri2 + wir2 - xi1;

            for (i += 8; i < m; i += 8)
            {
                wr = wriFactors[kk];
                wi = wriFactors[kk + n];

                xr1 = (x[i] + x[j - 8]) / 2f;
                xi1 = (x[i + 4] - x[j - 4]) / 2f;
                xr2 = (x[i + 4] + x[j - 4]) / 2f;
                xi2 = (x[j - 8] - x[i]) / 2f;

                wrr2 = wr * xr2;
                wri2 = wr * xi2;
                wir2 = wi * xr2;
                wii2 = wi * xi2;

                x[i] = xr1 + wrr2 - wii2;
                x[i + 4] = xi1 + wri2 + wir2;
                x[j - 8] = xr1 - wrr2 + wii2;
                x[j - 4] = wri2 + wir2 - xi1;

                j -= 8;

                wr = wriFactors[kk + 1];
                wi = wriFactors[kk + 1 + n];

                xr1 = (x[i + 1] + x[j - 5]) / 2f;
                xi1 = (x[i + 5] - x[j - 1]) / 2f;
                xr2 = (x[i + 5] + x[j - 1]) / 2f;
                xi2 = (x[j - 5] - x[i + 1]) / 2f;

                wrr2 = wr * xr2;
                wri2 = wr * xi2;
                wir2 = wi * xr2;
                wii2 = wi * xi2;

                x[i + 1] = xr1 + wrr2 - wii2;
                x[i + 5] = xi1 + wri2 + wir2;
                x[j - 5] = xr1 - wrr2 + wii2;
                x[j - 1] = wri2 + wir2 - xi1;

                wr = wriFactors[kk + 2];
                wi = wriFactors[kk + 2 + n];

                xr1 = (x[i + 2] + x[j - 6]) / 2f;
                xi1 = (x[i + 6] - x[j - 2]) / 2f;
                xr2 = (x[i + 6] + x[j - 2]) / 2f;
                xi2 = (x[j - 6] - x[i + 2]) / 2f;

                wrr2 = wr * xr2;
                wri2 = wr * xi2;
                wir2 = wi * xr2;
                wii2 = wi * xi2;

                x[i + 2] = xr1 + wrr2 - wii2;
                x[i + 6] = xi1 + wri2 + wir2;
                x[j - 6] = xr1 - wrr2 + wii2;
                x[j - 2] = wri2 + wir2 - xi1;

                wr = wriFactors[kk + 3];
                wi = wriFactors[kk + 3 + n];
                kk += 4;

                xr1 = (x[i + 3] + x[j - 7]) / 2f;
                xi1 = (x[i + 7] - x[j - 3]) / 2f;
                xr2 = (x[i + 7] + x[j - 3]) / 2f;
                xi2 = (x[j - 7] - x[i + 3]) / 2f;

                wrr2 = wr * xr2;
                wri2 = wr * xi2;
                wir2 = wi * xr2;
                wii2 = wi * xi2;

                x[i + 3] = xr1 + wrr2 - wii2;
                x[i + 7] = xi1 + wri2 + wir2;
                x[j - 7] = xr1 - wrr2 + wii2;
                x[j - 3] = wri2 + wir2 - xi1;
            }
        }
    }
}