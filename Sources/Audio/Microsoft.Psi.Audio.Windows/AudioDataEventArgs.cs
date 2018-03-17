// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Defines the arguments for audio data events.
    /// </summary>
    internal class AudioDataEventArgs : EventArgs
    {
        private long timestamp;
        private IntPtr dataPtr;
        private int dataLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDataEventArgs"/> class.
        /// </summary>
        /// <param name="timestamp">
        /// The timestamp of the first audio sample contained in data.
        /// </param>
        /// <param name="dataPtr">
        /// A pointer to the captured audio samples.
        /// </param>
        /// <param name="dataLength">
        /// The number of bytes of data available.
        /// </param>
        internal AudioDataEventArgs(long timestamp, IntPtr dataPtr, int dataLength)
        {
            this.timestamp = timestamp;
            this.dataPtr = dataPtr;
            this.dataLength = dataLength;
        }

        /// <summary>
        /// Gets or sets the timestamp (in 100-ns ticks since system boot) of the first audio sample contained in <see cref="Psi.Data"/>.
        /// </summary>
        public long Timestamp
        {
            get
            {
                return this.timestamp;
            }

            set
            {
                this.timestamp = value;
            }
        }

        /// <summary>
        /// Gets a pointer to the captured audio samples.
        /// </summary>
        public IntPtr Data
        {
            get
            {
                return this.dataPtr;
            }
        }

        /// <summary>
        /// Gets or sets the number of bytes of data available.
        /// </summary>
        public int Length
        {
            get
            {
                return this.dataLength;
            }

            set
            {
                this.dataLength = value;
            }
        }
    }
}
