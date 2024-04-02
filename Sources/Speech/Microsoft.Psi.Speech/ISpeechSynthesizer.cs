// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines an interface for speech synthesizer components.
    /// </summary>
    public interface ISpeechSynthesizer : IConsumerProducer<string, AudioBuffer>
    {
    }
}
