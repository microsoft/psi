// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Source component that produces streams containing information about the tracked hands (via StereoKit).
    /// </summary>
    public class HandsSensor : StereoKitComponent, ISourceComponent, IProducer<(Hand Left, Hand Right)>
    {
        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        private Emitter<Hand> leftHandEmitter = null;
        private Emitter<Hand> rightHandEmitter = null;
        private bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandsSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Optional interval at which to poll hand information (default to frequency of StereoKit Stepper).</param>
        /// <param name="name">An optional name for the component.</param>
        public HandsSensor(Pipeline pipeline, TimeSpan interval = default, string name = nameof(HandsSensor))
            : base(pipeline, name)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.FromTicks(1) : interval; // minimum interval of one-tick

            this.Out = pipeline.CreateEmitter<(Hand Left, Hand Right)>(this, nameof(this.Out));
        }

        /// <inheritdoc/>
        public Emitter<(Hand Left, Hand Right)> Out { get; }

        /// <summary>
        /// Gets the stream of left hand information.
        /// </summary>
        public Emitter<Hand> Left => this.leftHandEmitter ??= this.Out.Select(
            hands => hands.Left,
            DeliveryPolicy.SynchronousOrThrottle,
            "SelectLeftHand").Out;

        /// <summary>
        /// Gets the stream of right hand information.
        /// </summary>
        public Emitter<Hand> Right => this.rightHandEmitter ??= this.Out.Select(
            hands => hands.Right,
            DeliveryPolicy.SynchronousOrThrottle,
            "SelectRightHand").Out;

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
            // Get the current time from OpenXR
            var currentSampleTime = this.pipeline.GetCurrentTimeFromOpenXr();

            if (this.active && currentSampleTime - this.Out.LastEnvelope.OriginatingTime >= this.interval)
            {
                this.Out.Post((PsiInput.LeftHand, PsiInput.RightHand), currentSampleTime);
            }
        }
    }
}
