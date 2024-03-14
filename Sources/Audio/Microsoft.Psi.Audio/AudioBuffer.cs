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
        private readonly WaveFormat format;
        private readonly byte[] data;

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
        /// Initializes a new instance of the <see cref="AudioBuffer"/> struct.
        /// </summary>
        /// <param name="timeSpan">The time span of the audio data.</param>
        /// <param name="format">The format of the audio data.</param>
        public AudioBuffer(TimeSpan timeSpan, WaveFormat format)
        {
            // Ensure that the length of the audio data is a multiple of the block size
            int length = (int)Math.Ceiling(timeSpan.TotalSeconds * format.SamplesPerSec) * format.BlockAlign;
            this.data = new byte[length];
            this.format = format;
        }

        /// <summary>
        /// Gets the audio format.
        /// </summary>
        public readonly WaveFormat Format => this.format;

        /// <summary>
        /// Gets the byte array containing the audio data.
        /// </summary>
        public readonly byte[] Data => this.data;

        /// <summary>
        /// Gets the length in bytes of the audio data.
        /// </summary>
        public readonly int Length => (this.data == null) ? 0 : this.data.Length;

        /// <summary>
        /// Gets the duration of the audio.
        /// </summary>
        public readonly TimeSpan Duration
            => (this.data == null) ? TimeSpan.Zero : TimeSpan.FromTicks(10000000L * this.data.Length / this.format.AvgBytesPerSec);

        /// <summary>
        /// Gets a value indicating whether or not this <see cref="AudioBuffer"/> contains valid data.
        /// </summary>
        public readonly bool HasValidData => (this.format != null) && (this.data != null);

        /// <summary>
        /// Implements the addition operator for combining two <see cref="AudioBuffer"/> objects.
        /// </summary>
        /// <param name="a">The first <see cref="AudioBuffer"/> to combine.</param>
        /// <param name="b">The second <see cref="AudioBuffer"/> to combine.</param>
        /// <returns>The audio buffer resulting from the concatenation of the two input audio buffers.</returns>
        /// <exception cref="ArgumentException">AudioBuffer formats must match.</exception>
        public static AudioBuffer operator +(AudioBuffer a, AudioBuffer b)
        {
            if (!WaveFormat.Equals(a.format, b.format))
            {
                throw new ArgumentException("AudioBuffer formats must match");
            }

            byte[] newData = new byte[a.data.Length + b.data.Length];
            Array.Copy(a.data, newData, a.data.Length);
            Array.Copy(b.data, 0, newData, a.data.Length, b.data.Length);
            return new AudioBuffer(newData, a.format);
        }
    }
}
