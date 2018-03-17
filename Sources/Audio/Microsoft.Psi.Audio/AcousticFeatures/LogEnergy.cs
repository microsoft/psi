// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that computes the Log energy.
    /// </summary>
    public sealed class LogEnergy : ConsumerProducer<float[], float>
    {
        /// <summary>
        /// Constants for log energy computation.
        /// </summary>
        private const float EpsInLog = 1e-40f;

        /// <summary>
        /// Constants for log energy computation.
        /// </summary>
        private const float LogOfEps = -92.1f;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEnergy"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        public LogEnergy(Pipeline pipeline)
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
            this.Out.Post(this.ComputeLogEnergy(data), e.OriginatingTime);
        }

        /// <summary>
        /// Compute the log energy of the supplied frame.
        /// </summary>
        /// <param name="frame">The frame over which to compute the log energy.</param>
        /// <returns>The log energy.</returns>
        private float ComputeLogEnergy(float[] frame)
        {
            float egy = 0.0f;
            for (int i = 0; i < frame.Length; i++)
            {
                egy += frame[i] * frame[i];
            }

            egy /= (float)frame.Length;
            if (egy < EpsInLog)
            {
                egy = LogOfEps;
            }
            else
            {
                egy = (float)Math.Log(egy);
            }

            return egy;
        }
    }
}
