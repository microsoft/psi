// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Source component that surfaces eye tracking information on a stream.
    /// </summary>
    /// <remarks>Applications using this component must enable the Gaze Input capability in Package.appxmanifest.</remarks>
    public class EyesSensor : StereoKitComponent, IProducer<Ray3D>, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        private bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="EyesSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Optional interval at which to poll eye tracking information (default 1/60th second).</param>
        /// <param name="name">An optional name for the component.</param>
        public EyesSensor(Pipeline pipeline, TimeSpan interval = default, string name = nameof(EyesSensor))
            : base(pipeline, name)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.FromTicks(1) : interval; // minimum interval of one-tick
            this.Out = pipeline.CreateEmitter<Ray3D>(this, nameof(this.Out));
            this.EyesTracked = pipeline.CreateEmitter<bool>(this, nameof(this.EyesTracked));
        }

        /// <summary>
        /// Gets the stream of tracked eyes pose as a <see cref="Ray3D"/>.
        /// </summary>
        public Emitter<Ray3D> Out { get; private set; }

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
            if (this.active)
            {
                var currentSampleTime = this.pipeline.ConvertTimeFromOpenXr(Backend.OpenXR.EyesSampleTime);
                var elapsedTime = currentSampleTime - this.Out.LastEnvelope.OriginatingTime;

                if (elapsedTime.Ticks > 0 && elapsedTime >= this.interval)
                {
                    this.Out.Post(PsiInput.Eyes, currentSampleTime);
                    this.EyesTracked.Post(Input.EyesTracked.IsActive(), currentSampleTime);
                }
            }
        }
    }
}
