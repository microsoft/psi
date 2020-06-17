// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace ArmControlROSSample
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// UArm component.
    /// </summary>
    public class UArmComponent : ISourceComponent
    {
        private readonly UArm arm;

        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="UArmComponent"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="arm">UArm instance.</param>
        public UArmComponent(Pipeline pipeline, UArm arm)
        {
            this.pipeline = pipeline;
            this.arm = arm;
            this.Beep = pipeline.CreateReceiver<(float, float)>(this, (b, _) => this.arm.Beep(b.Item1, b.Item2), nameof(this.Beep));
            this.Pump = pipeline.CreateReceiver<bool>(this, (p, _) => this.arm.Pump(p), nameof(this.Pump));
            this.AbsolutePosition = pipeline.CreateReceiver<(float, float, float)>(this, (p, _) => this.arm.AbsolutePosition(p.Item1, p.Item2, p.Item3), nameof(this.AbsolutePosition));
            this.RelativePosition = pipeline.CreateReceiver<(float, float, float)>(this, (p, _) => this.arm.RelativePosition(p.Item1, p.Item2, p.Item3), nameof(this.RelativePosition));
            this.PositionChanged = pipeline.CreateEmitter<(float, float, float)>(this, nameof(this.PositionChanged));
        }

        /// <summary>
        /// Gets receiver of beep commands.
        /// </summary>
        public Receiver<(float, float)> Beep { get; private set; }

        /// <summary>
        /// Gets receiver of pump commands.
        /// </summary>
        public Receiver<bool> Pump { get; private set; }

        /// <summary>
        /// Gets receiver of absolute Cartesian positions.
        /// </summary>
        public Receiver<(float, float, float)> AbsolutePosition { get; private set; }

        /// <summary>
        /// Gets receiver of relative Cartesian positions.
        /// </summary>
        public Receiver<(float, float, float)> RelativePosition { get; private set; }

        /// <summary>
        /// Gets emitter of position changes.
        /// </summary>
        public Emitter<(float, float, float)> PositionChanged { get; private set; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.arm.Connect();
            this.arm.PositionChanged += this.OnPositionChanged;
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.arm.Disconnect();
            this.arm.PositionChanged -= this.OnPositionChanged;
            notifyCompleted();
        }

        private void OnPositionChanged(object sender, (float, float, float) position)
        {
            this.PositionChanged.Post(position, this.pipeline.GetCurrentTime());
        }
    }
}
