// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines a voice activity detector component.
    /// </summary>
    public interface IVoiceActivityDetector : IConsumerProducer<AudioBuffer, bool>
    {
    }
}
