// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;

    /// <summary>
    /// Represents a buffer of audio and its associated format information.
    /// </summary>
    /// <remarks>
    /// Audio in \\psi is represented as a structure consisting of an array of bytes containing a
    /// chunk of audio data and a <see cref="WaveFormat"/> object which describes the encoding
    /// format of the audio data. While pairing each chunk of audio data with its corresponding
    /// <see cref="WaveFormat"/> object incurs some amount of overhead, it simplifies processing
    /// of audio data by stream components and operators in \\psi by providing all the relevant
    /// audio information in a single unit.
    /// </remarks>
    public struct AudioBuffer
    {
        private WaveFormat format;
        private byte[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBuffer"/> structure.
        /// </summary>
        /// <param name="data">An array of bytes containing the audio data.</param>
        /// <param name="format">The audio format.</param>
        public AudioBuffer(byte[] data, WaveFormat format)
        {
            this.data = data;
            this.format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBuffer"/> structure of a pre-allocated fixed length containing
        /// no data initially. This overload is used primarily for making a copy of an existing <see cref="AudioBuffer"/>.
        /// </summary>
        /// <param name="length">The size in bytes of the audio data.</param>
        /// <param name="format">The audio format.</param>
        public AudioBuffer(int length, WaveFormat format)
            : this(new byte[length], format)
        {
        }

        /// <summary>
        /// Gets the audio format.
        /// </summary>
        public WaveFormat Format
        {
            get { return this.format; }
        }

        /// <summary>
        /// Gets the byte array containing the audio data.
        /// </summary>
        public byte[] Data
        {
            get { return this.data; }
        }

        /// <summary>
        /// Gets the length in bytes of the audio data.
        /// </summary>
        public int Length
        {
            get { return (this.data == null) ? 0 : this.data.Length; }
        }

        /// <summary>
        /// Gets the duration of the audio.
        /// </summary>
        public TimeSpan Duration
        {
            get { return (this.data == null) ? TimeSpan.Zero : TimeSpan.FromTicks(10000000L * this.data.Length / this.format.AvgBytesPerSec); }
        }

        /// <summary>
        /// Gets a value indicating whether or not this <see cref="AudioBuffer"/> contains valid data.
        /// </summary>
        public bool HasValidData
        {
            get { return (this.format != null) && (this.data != null); }
        }
    }
}
