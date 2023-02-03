// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Source component that produces a stream of head coordinates.
    /// </summary>
    public class HeadSensor : StereoKitComponent, IProducer<CoordinateSystem>, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        private bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeadSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Optional interval at which to poll head information (default 1/60th second).</param>
        /// <param name="name">An optional name for the component.</param>
        public HeadSensor(Pipeline pipeline, TimeSpan interval = default, string name = nameof(HeadSensor))
            : base(pipeline, name)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.FromTicks(1) : interval; // minimum interval of one-tick
            this.Out = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the stream of tracked head pose.
        /// </summary>
        public Emitter<CoordinateSystem> Out { get; private set; }

        /// <inheritdoc />
        public override void Step()
        {
            // Get the current time from OpenXR
            var currentSampleTime = this.pipeline.GetCurrentTimeFromOpenXr();

            if (this.active && currentSampleTime - this.Out.LastEnvelope.OriginatingTime >= this.interval)
            {
                this.Out.Post(PsiInput.Head, currentSampleTime);
            }
        }

        /// <inheritdoc />
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.active = true;
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc />
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.active = false;
            notifyCompleted();
        }
    }
}
