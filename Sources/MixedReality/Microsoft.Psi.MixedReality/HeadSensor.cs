// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using StereoKit;

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
        public HeadSensor(Pipeline pipeline, TimeSpan interval = default)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.Zero : interval;
            this.Out = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the stream of tracked head pose.
        /// </summary>
        public Emitter<CoordinateSystem> Out { get; private set; }

        /// <inheritdoc />
        public override void Step()
        {
            var currentTime = this.pipeline.GetCurrentTime();
            if (this.active && currentTime - this.Out.LastEnvelope.OriginatingTime >= this.interval)
            {
                var head = Input.Head;
                var originatingTime = this.pipeline.GetCurrentTime();
                this.Out.Post(head.ToPsiCoordinateSystem(), originatingTime);
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
