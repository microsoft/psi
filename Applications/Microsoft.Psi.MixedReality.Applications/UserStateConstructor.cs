// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.MixedReality.OpenXR;
    using Microsoft.Psi.MixedReality.WinRT;

    /// <summary>
    /// Component that constructs the user state.
    /// </summary>
    public class UserStateConstructor : ConsumerProducer<int, UserState>
    {
        private readonly UserState userState = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserStateConstructor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public UserStateConstructor(Pipeline pipeline, string name = nameof(UserStateConstructor))
            : base(pipeline, name)
        {
            this.EyesAndHead = pipeline.CreateReceiver<(Eyes, CoordinateSystem)>(this, this.ReceiveEyesAndHead, nameof(this.EyesAndHead));
            this.Hands = pipeline.CreateReceiver<(Hand, Hand)>(this, this.ReceiveHands, nameof(this.Hands));
        }

        /// <summary>
        /// Gets the receiver for the eyes and head information.
        /// </summary>
        public Receiver<(Eyes, CoordinateSystem)> EyesAndHead { get; }

        /// <summary>
        /// Gets the receiver for the hands information.
        /// </summary>
        public Receiver<(Hand, Hand)> Hands { get; }

        /// <inheritdoc/>
        protected override void Receive(int data, Envelope envelope)
        {
            this.Out.Post(this.userState, envelope.OriginatingTime);
        }

        private void ReceiveEyesAndHead((Eyes Eyes, CoordinateSystem Head) eyeAndHead, Envelope envelope)
        {
            this.userState.Eyes = eyeAndHead.Eyes.DeepClone();
            this.userState.Head = eyeAndHead.Head.DeepClone();
        }

        private void ReceiveHands((Hand Left, Hand Right) hands)
        {
            this.userState.HandLeft = hands.Left.DeepClone();
            this.userState.HandRight = hands.Right.DeepClone();
        }
    }
}
