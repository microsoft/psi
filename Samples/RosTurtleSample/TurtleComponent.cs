// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace TurtleROSSample
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Turtle ROS bridge component.
    /// </summary>
    public class TurtleComponent : ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly Turtle turtle;

        private bool stopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="TurtleComponent"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which component belongs.</param>
        /// <param name="turtle">Turtle ROS bridge instance.</param>
        public TurtleComponent(Pipeline pipeline, Turtle turtle)
        {
            this.pipeline = pipeline;
            this.turtle = turtle;
            this.Velocity = pipeline.CreateReceiver<(float, float)>(this, (c, _) => this.turtle.Velocity(c.Item1, c.Item2), nameof(this.Velocity));
            this.PoseChanged = pipeline.CreateEmitter<(float, float, float)>(this, nameof(this.PoseChanged));
        }

        /// <summary>
        /// Gets velocity receiver.
        /// </summary>
        public Receiver<(float, float)> Velocity { get; private set; }

        /// <summary>
        /// Gets pose changed emitter.
        /// </summary>
        public Emitter<(float, float, float)> PoseChanged { get; private set; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
            this.turtle.Connect();
            this.turtle.PoseChanged += this.OnPoseChanged;
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            try
            {
                this.stopped = true;
                this.turtle.Disconnect();
                this.turtle.PoseChanged -= this.OnPoseChanged;
            }
            finally
            {
                notifyCompleted();
            }
        }

        private void OnPoseChanged(object sender, (float, float, float) pose)
        {
            if (!this.stopped)
            {
                this.PoseChanged.Post(pose, this.pipeline.GetCurrentTime());
            }
        }
    }
}