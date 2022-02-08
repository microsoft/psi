// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using StereoKit;

    /// <summary>
    /// Source component that surfaces eye tracking information on a stream.
    /// </summary>
    /// <remarks>Applications using this component must enable the Gaze Input capability in Package.appxmanifest.</remarks>
    public class EyesSensor : StereoKitComponent, IProducer<CoordinateSystem>, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        private bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="EyesSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Optional interval at which to poll eye tracking information (default 1/60th second).</param>
        public EyesSensor(Pipeline pipeline, TimeSpan interval = default)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.Zero : interval;
            this.Out = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
            this.EyesTracked = pipeline.CreateEmitter<bool>(this, nameof(this.EyesTracked));
        }

        /// <summary>
        /// Gets the stream of tracked eyes pose.
        /// </summary>
        public Emitter<CoordinateSystem> Out { get; private set; }

        /// <summary>
        /// Gets the stream of whether eyes are currently tracked.
        /// </summary>
        public Emitter<bool> EyesTracked { get; private set; }

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

        /// <inheritdoc />
        public override void Step()
        {
            var currentTime = this.pipeline.GetCurrentTime();
            if (this.active && currentTime - this.Out.LastEnvelope.OriginatingTime >= this.interval)
            {
                var eyes = Input.Eyes;
                var eyesTracked = Input.EyesTracked;
                var originatingTime = this.pipeline.GetCurrentTime();
                this.Out.Post(eyes.ToPsiCoordinateSystem(), originatingTime);
                this.EyesTracked.Post(eyesTracked.IsActive(), originatingTime);
            }
        }
    }
}
