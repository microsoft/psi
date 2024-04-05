// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Defines an interface for MPEG-4 writer components.
    /// </summary>
    public interface IMpeg4Writer : IConsumer<Shared<Image>>
    {
        /// <summary>
        /// Gets or sets the input audio stream.
        /// </summary>
        Receiver<AudioBuffer> AudioIn { get; set; }
    }
}
