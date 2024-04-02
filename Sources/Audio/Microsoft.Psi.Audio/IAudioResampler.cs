// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines an interface for audio resampler components.
    /// </summary>
    public interface IAudioResampler : IConsumerProducer<AudioBuffer, AudioBuffer>
    {
    }
}
