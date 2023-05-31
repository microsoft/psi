// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Filters
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that implements the 1-Euro filter:
    /// See https://cristal.univ-lille.fr/~casiez/1euro/ for the original paper.
    /// </summary>
    public class OneEuroFilter : ConsumerProducer<double, double>
    {
        private readonly double cutoffSlope;
        private readonly double minCutoffFrequency;
        private readonly double derivateCutoffFrequency;

        private DateTime previousTime = default;
        private double previousValue;
        private double previousDerivate;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneEuroFilter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="cutoffSlope">Cutoff slope (beta).</param>
        /// <param name="minCutoffFrequency">Minimum cutoff frequency.</param>
        /// <param name="derivateCutoffFrequency">Cutoff frequency for derivate.</param>
        public OneEuroFilter(Pipeline pipeline, double cutoffSlope = 0, double minCutoffFrequency = 1.0, double derivateCutoffFrequency = 1.0)
            : base(pipeline)
        {
            this.minCutoffFrequency = minCutoffFrequency;
            this.cutoffSlope = cutoffSlope;
            this.derivateCutoffFrequency = derivateCutoffFrequency;
        }

        /// <inheritdoc/>
        protected override void Receive(double value, Envelope envelope)
        {
            if (this.previousTime == default)
            {
                this.previousTime = envelope.OriginatingTime;
                this.previousValue = value;
                this.previousDerivate = 0;
            }
            else
            {
                // Compute the current data rate from time since last sample was seen.
                var rate = 1.0 / (envelope.OriginatingTime - this.previousTime).TotalSeconds;
                this.previousTime = envelope.OriginatingTime;

                var dx = (value - this.previousValue) * rate;
                this.previousDerivate = this.LowPassFilter(dx, this.previousDerivate, this.ComputeAlpha(rate, this.derivateCutoffFrequency));

                var cutoff = this.minCutoffFrequency + (this.cutoffSlope * Math.Abs(this.previousDerivate));
                this.previousValue = this.LowPassFilter(value, this.previousValue, this.ComputeAlpha(rate, cutoff));
            }

            this.Out.Post(this.previousValue, envelope.OriginatingTime);
        }

        private double LowPassFilter(double x, double prevX, double alpha)
        {
            return (alpha * x) + ((1.0 - alpha) * prevX);
        }

        private double ComputeAlpha(double rate, double cutoffFrequency)
        {
            var tau = 1.0 / (2.0 * Math.PI * cutoffFrequency);
            var te = 1.0 / rate;
            return 1.0 / (1.0 + (tau / te));
        }
    }
}
