// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that computes the Zero crossing rate of the input signal.
    /// </summary>
    public sealed class ZeroCrossingRate : ConsumerProducer<float[], float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZeroCrossingRate"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        public ZeroCrossingRate(Pipeline pipeline)
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
            this.Out.Post(this.ComputeZeroCrossingRate(data), e.OriginatingTime);
        }

        /// <summary>
        /// Computes the zero crossing rate of the signal.
        /// </summary>
        /// <param name="frame">A data frame of the signal.</param>
        /// <returns>The zero crossing rate.</returns>
        private float ComputeZeroCrossingRate(float[] frame)
        {
            int counter = 0;
            for (int i = 1; i < frame.Length; i++)
            {
                if (frame[i] * frame[i - 1] < 0)
                {
                    counter++;
                }
            }

            return (float)counter / (float)frame.Length;
        }
    }
}
