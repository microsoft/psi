// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Defines the arguments for audio volume events.
    /// </summary>
    internal class AudioVolumeEventArgs : EventArgs
    {
        private bool muted;
        private float masterVolume;
        private float[] channelVolume;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVolumeEventArgs"/> class.
        /// </summary>
        /// <param name="muted">A flag indicating whether the volume is muted.</param>
        /// <param name="masterVolume">The master volume level.</param>
        /// <param name="channelVolume">An array of channel volume levels.</param>
        internal AudioVolumeEventArgs(bool muted, float masterVolume, float[] channelVolume)
        {
            this.muted = muted;
            this.masterVolume = masterVolume;
            this.channelVolume = channelVolume;
        }

        /// <summary>
        /// Gets a value indicating whether the volume is muted.
        /// </summary>
        public bool Muted
        {
            get
            {
                return this.muted;
            }
        }

        /// <summary>
        /// Gets the master volume level.
        /// </summary>
        public float MasterVolume
        {
            get
            {
                return this.masterVolume;
            }
        }

        /// <summary>
        /// Gets an array of channel volume levels.
        /// </summary>
        public float[] ChannelVolume
        {
            get
            {
                return this.channelVolume;
            }
        }
    }
}
