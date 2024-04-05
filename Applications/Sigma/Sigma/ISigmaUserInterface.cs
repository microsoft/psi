// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Defines the Sigma user interface component.
    /// </summary>
    public interface ISigmaUserInterface
    {
        /// <summary>
        /// Gets the receiver for the user state input.
        /// </summary>
        public Receiver<UserState> UserStateInput { get; }

        /// <summary>
        /// Gets the receiver for the speech synthesis command.
        /// </summary>
        public Emitter<string> SpeechSynthesisCommand { get; }

        /// <summary>
        /// Gets the receiver for the Sigma position.
        /// </summary>
        public Emitter<Point3D> Position { get; }
    }
}
