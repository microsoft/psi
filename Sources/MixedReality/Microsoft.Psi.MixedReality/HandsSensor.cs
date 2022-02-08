// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using Microsoft.Psi.Components;
    using StereoKit;

    /// <summary>
    /// Source component that produces streams containing information about the tracked hands.
    /// </summary>
    public class HandsSensor : StereoKitComponent, ISourceComponent
    {
        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        private bool active;
        private bool visible = true;
        private bool solid = true;
        private Material material = Default.MaterialHand;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandsSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Optional interval at which to poll hand information (default 1/60th second).</param>
        /// <param name="visible">Optional value indicating whether hands should be rendered (default true).</param>
        /// <param name="solid">Optional value indicating whether hands participate in StereoKit physics (default true).</param>
        /// <param name="material">Optional material used to render the hands (default <see cref="Default.MaterialHand"/>).</param>
        public HandsSensor(Pipeline pipeline, TimeSpan interval = default, bool visible = true, bool solid = false, Material material = null)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.interval = interval == default ? TimeSpan.Zero : interval;
            this.visible = visible;
            this.solid = solid;
            this.material = material ?? Default.MaterialHand;
            this.Left = pipeline.CreateEmitter<Hand>(this, nameof(this.Left));
            this.Right = pipeline.CreateEmitter<Hand>(this, nameof(this.Right));
            this.Visible = pipeline.CreateReceiver<bool>(this, v => this.visible = v, nameof(this.Visible));
            this.Solid = pipeline.CreateReceiver<bool>(this, s => this.solid = s, nameof(this.Solid));
            this.Material = pipeline.CreateReceiver<Material>(this, m => this.material = m, nameof(this.Material));
        }

        /// <summary>
        /// Gets the stream of left hand information.
        /// </summary>
        public Emitter<Hand> Left { get; }

        /// <summary>
        /// Gets the stream of left hand information.
        /// </summary>
        public Emitter<Hand> Right { get; }

        /// <summary>
        /// Gets the receiver of a value indicating whether hands should be rendered.
        /// </summary>
        public Receiver<bool> Visible { get; }

        /// <summary>
        /// Gets the receiver of a value indicating whether hands participate in physics.
        /// </summary>
        public Receiver<bool> Solid { get; }

        /// <summary>
        /// Gets the receiver of the material used to render the hands.
        /// </summary>
        public Receiver<Material> Material { get; }

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
            if (this.active && currentTime - this.Left.LastEnvelope.OriginatingTime >= this.interval)
            {
                var left = Input.Hand(Handed.Left);
                var right = Input.Hand(Handed.Right);
                var originatingTime = this.pipeline.GetCurrentTime();

                left.Visible = this.visible;
                left.Solid = this.solid;
                left.Material = this.material;
                this.Left.Post(Hand.FromStereoKitHand(left), originatingTime);

                right.Visible = this.visible;
                right.Solid = this.solid;
                right.Material = this.material;
                this.Right.Post(Hand.FromStereoKitHand(right), originatingTime);
            }
        }
    }
}
