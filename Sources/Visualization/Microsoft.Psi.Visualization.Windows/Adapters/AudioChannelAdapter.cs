// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter that selects a single audio channel from a stream of audio buffers.
    /// </summary>
    public class AudioChannelAdapter : StreamAdapter<AudioBuffer, AudioBuffer>
    {
        private readonly int channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioChannelAdapter"/> class.
        /// </summary>
        /// <param name="channel">The selected audio channel.</param>
        public AudioChannelAdapter(int channel)
        {
            this.channel = channel;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioChannelAdapter"/> class.
        /// </summary>
        /// <param name="channel">The selected audio channel.</param>
        /// <remarks>Required because deserialized integer values from JSON layouts are typed long.</remarks>
        public AudioChannelAdapter(long channel)
            : this((int)channel)
        {
        }

        /// <inheritdoc/>
        public override AudioBuffer GetAdaptedValue(AudioBuffer source, Envelope envelope)
            => source.SelectChannel(this.channel);
    }
}
