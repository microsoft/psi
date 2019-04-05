// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that reframes a stream of audio buffers to fixed size chunks.
    /// </summary>
    public sealed class Reframe : ConsumerProducer<AudioBuffer, AudioBuffer>
    {
        private int frameSizeInBytes;
        private byte[] frameBuffer;
        private int frameBytesRemaining;
        private TimeSpan frameDuration;
        private DateTime lastOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Reframe"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="frameSizeInBytes">The output frame size in bytes.</param>
        public Reframe(Pipeline pipeline, int frameSizeInBytes)
            : base(pipeline)
        {
            if (frameSizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameSizeInBytes), "Please specify a positive output frame size.");
            }

            this.frameSizeInBytes = frameSizeInBytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Reframe"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="frameDuration">The output frame duration.</param>
        public Reframe(Pipeline pipeline, TimeSpan frameDuration)
            : base(pipeline)
        {
            if (frameDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(frameDuration), "Please specify a positive output frame duration.");
            }

            this.frameDuration = frameDuration;
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="audio">A buffer containing the input audio.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(AudioBuffer audio, Envelope e)
        {
            // initialize the output frame buffer on the first audio message received as
            // we may need information in the audio format to determine the buffer size
            if (this.frameBuffer == null)
            {
                // this component is constructed by specifying either an output frame size or duration
                if (this.frameSizeInBytes == 0)
                {
                    // initialize the output frame size based on a specified duration
                    this.frameSizeInBytes = (int)(this.frameDuration.TotalSeconds * audio.Format.AvgBytesPerSec);
                }
                else
                {
                    // initialize the output frame duration based on a specified size in bytes (for completeness
                    // - we don't actually use the value of this.frameDuration for the reframe computation)
                    this.frameDuration = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * this.frameSizeInBytes / audio.Format.AvgBytesPerSec);
                }

                this.frameBuffer = new byte[this.frameSizeInBytes];
                this.frameBytesRemaining = this.frameSizeInBytes;
            }

            int messageBytesRemaining = audio.Length;
            while (messageBytesRemaining > 0)
            {
                int bytesToCopy = Math.Min(this.frameBytesRemaining, messageBytesRemaining);
                Array.Copy(audio.Data, audio.Length - messageBytesRemaining, this.frameBuffer, this.frameBuffer.Length - this.frameBytesRemaining, bytesToCopy);
                messageBytesRemaining -= bytesToCopy;
                this.frameBytesRemaining -= bytesToCopy;
                if (this.frameBytesRemaining == 0)
                {
                    // Compute the originating time of the frame
                    DateTime originatingTime = e.OriginatingTime.AddTicks(
                        -(long)(TimeSpan.TicksPerSecond * messageBytesRemaining / audio.Format.AvgBytesPerSec));

                    // Fixup potential out of order timestamps where successive audio buffer timestamps
                    // drastically overlap. This could be indicative of a system time adjustment having
                    // occurred between captured audio buffers.
                    if (originatingTime <= this.lastOriginatingTime)
                    {
                        originatingTime = this.lastOriginatingTime.AddTicks(1); // add tick to avoid time collision
                    }

                    this.lastOriginatingTime = originatingTime;

                    // Post the completed frame
                    byte[] frame = this.frameBuffer;
                    this.Out.Post(new AudioBuffer(frame, audio.Format), originatingTime);

                    // Reset the frame
                    this.frameBytesRemaining = this.frameSizeInBytes;
                }
            }
        }
    }
}
