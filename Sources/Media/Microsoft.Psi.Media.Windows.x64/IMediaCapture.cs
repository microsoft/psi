// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Defines the interface for a media capture device.
    /// </summary>
    public interface IMediaCapture
    {
        /// <summary>
        /// Gets the emitter for the audio stream.
        /// </summary>
        Emitter<AudioBuffer> Audio { get; }

        /// <summary>
        /// Gets the emitter for the video stream.
        /// </summary>
        Emitter<Shared<Image>> Video { get; }
    }
}
