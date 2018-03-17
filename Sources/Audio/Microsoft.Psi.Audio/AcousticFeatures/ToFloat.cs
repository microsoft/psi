// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that converts raw audio data to floating point samples.
    /// </summary>
    public sealed class ToFloat : ConsumerProducer<byte[], float[]>
    {
        private ushort bytesPerSample;
        private float[] buffer;
        private Func<byte[], int, float> convertSample;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToFloat"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="format">The format of the input audio.</param>
        public ToFloat(Pipeline pipeline, WaveFormat format)
            : base(pipeline)
        {
            this.bytesPerSample = format.BlockAlign;
            switch (format.BitsPerSample)
            {
                case 8: this.convertSample = (a, i) => a[i]; break;
                case 16: this.convertSample = (a, i) => BitConverter.ToInt16(a, i); break;
                case 24: this.convertSample = (a, i) => BitConverter.ToInt32(new byte[] { a[i], a[i + 1], a[i + 2], (byte)((a[i + 2] & 0x80) == 0 ? 0 : 0xFF) }, 0); break;
                case 32: this.convertSample = (a, i) => BitConverter.ToInt32(a, i); break;
                default: throw new FormatException("Valid sample sizes are 8, 16, 24 or 32 bits");
            }
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="data">A buffer containing the input data.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(byte[] data, Envelope e)
        {
            int frameSize = data.Length / this.bytesPerSample;

            if ((this.buffer == null) || (this.buffer.Length != frameSize))
            {
                this.buffer = new float[frameSize];
            }

            // Create the window frame
            for (int i = 0, j = 0; i < frameSize; ++i, j += this.bytesPerSample)
            {
                // NOTE: only first channel is taken (no averaging over multiple channels).
                this.buffer[i] = this.convertSample(data, j);
            }

            this.Out.Post(this.buffer, e.OriginatingTime);
        }
    }
}
