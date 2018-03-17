// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace ArmControlROSSample
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    public class UArmComponent : IStartable
    {
        private readonly UArm arm;

        private readonly Pipeline pipeline;

        public UArmComponent(Pipeline pipeline, UArm arm)
        {
            this.pipeline = pipeline;
            this.arm = arm;
            this.Beep = pipeline.CreateReceiver<Tuple<float, float>>(this, (b, _) => this.arm.Beep(b.Item1, b.Item2), nameof(this.Beep));
            this.Pump = pipeline.CreateReceiver<bool>(this, (p, _) => this.arm.Pump(p), nameof(this.Pump));
            this.AbsolutePosition = pipeline.CreateReceiver<Tuple<float, float, float>>(this, (p, _) => this.arm.AbsolutePosition(p.Item1, p.Item2, p.Item3), nameof(this.AbsolutePosition));
            this.RelativePosition = pipeline.CreateReceiver<Tuple<float, float, float>>(this, (p, _) => this.arm.RelativePosition(p.Item1, p.Item2, p.Item3), nameof(this.RelativePosition));
            this.PositionChanged = pipeline.CreateEmitter<Tuple<float, float, float>>(this, nameof(this.PositionChanged));
        }

        public Receiver<Tuple<float, float>> Beep { get; private set; }

        public Receiver<bool> Pump { get; private set; }

        public Receiver<Tuple<float, float, float>> AbsolutePosition { get; private set; }

        public Receiver<Tuple<float, float, float>> RelativePosition { get; private set; }

        public Emitter<Tuple<float, float, float>> PositionChanged { get; private set; }

        public void Start(Action onCompleted, ReplayDescriptor descriptor)
        {
            this.arm.Connect();
            this.arm.PositionChanged += this.OnPositionChanged;
        }

        public void Stop()
        {
            this.arm.Disconnect();
            this.arm.PositionChanged -= this.OnPositionChanged;
        }

        private void OnPositionChanged(object sender, Tuple<float, float, float> position)
        {
            this.PositionChanged.Post(position, this.pipeline.GetCurrentTime());
        }
    }
}