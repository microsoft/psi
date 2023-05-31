// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that represents a movable UI handle.
    /// </summary>
    public class Handle : StereoKitRenderer, IProducer<CoordinateSystem>, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly string id;
        private readonly Bounds bounds;
        private readonly bool show;

        private bool active;
        private Pose pose;

        /// <summary>
        /// Initializes a new instance of the <see cref="Handle"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialPose">Initial handle pose.</param>
        /// <param name="bounds">Handle bounds.</param>
        /// <param name="showHandle">Whether to show the handle.</param>
        public Handle(Pipeline pipeline, CoordinateSystem initialPose, Vector3D bounds, bool showHandle = false)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.id = Guid.NewGuid().ToString();
            this.pose = initialPose.TransformBy(StereoKitTransforms.WorldToStereoKit).ToStereoKitPose();
            this.bounds = new Bounds(new Vec3((float)bounds.Y, (float)bounds.Z, (float)bounds.X)); // psi -> SK coordinates
            this.show = showHandle;
            this.Out = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the stream of the handle's pose.
        /// </summary>
        public Emitter<CoordinateSystem> Out { get; private set; }

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
                base.Step();
                this.Out.Post(this.pose.ToCoordinateSystem(), this.pipeline.GetCurrentTime());
            }
        }

        /// <inheritdoc />
        protected override void Render()
        {
            UI.Handle(this.id, ref this.pose, this.bounds, this.show);
        }
    }
}
