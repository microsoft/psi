// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that performs an accumulate and shift operation on a stream of audio buffers.
    /// </summary>
    public sealed class FrameShift : ConsumerProducer<byte[], byte[]>
    {
        private int frameSizeInBytes;
        private int frameShiftInBytes;
        private int frameOverlapInBytes;
        private byte[] frameBuffer;
        private double bytesPerSec;
        private int frameBytesRemaining;
        private DateTime lastOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameShift"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="frameSizeInBytes">The frame size in bytes.</param>
        /// <param name="frameShiftInBytes">The number of bytes to shift by.</param>
        /// <param name="bytesPerSec">The sampling frequency in bytes per second.</param>
        public FrameShift(Pipeline pipeline, int frameSizeInBytes, int frameShiftInBytes, double bytesPerSec)
            : base(pipeline)
        {
            this.frameSizeInBytes = frameSizeInBytes;
            this.frameShiftInBytes = frameShiftInBytes;
            this.frameOverlapInBytes = frameSizeInBytes - frameShiftInBytes;
            this.bytesPerSec = bytesPerSec;
            this.frameBuffer = new byte[frameSizeInBytes];
            this.frameBytesRemaining = frameSizeInBytes;
        }

        /// <summary>
        /// Receiver for the input data.
        /// </summary>
        /// <param name="data">A buffer containing the input data.</param>
        /// <param name="e">The message envelope for the input data.</param>
        protected override void Receive(byte[] data, Envelope e)
        {
            int messageBytesRemaining = data.Length;
            while (messageBytesRemaining > 0)
            {
                int bytesToCopy = Math.Min(this.frameBytesRemaining, messageBytesRemaining);
                Array.Copy(data, data.Length - messageBytesRemaining, this.frameBuffer, this.frameBuffer.Length - this.frameBytesRemaining, bytesToCopy);
                messageBytesRemaining -= bytesToCopy;
                this.frameBytesRemaining -= bytesToCopy;
                if (this.frameBytesRemaining == 0)
                {
                    // Compute the originating time of the frame
                    DateTime originatingTime = e.OriginatingTime.AddTicks(
                        -(long)(TimeSpan.TicksPerSecond * (messageBytesRemaining / this.bytesPerSec)));

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
                    this.Out.Post(frame, originatingTime);

                    // Shift the frame
                    Array.Copy(this.frameBuffer, this.frameShiftInBytes, this.frameBuffer, 0, this.frameOverlapInBytes);
                    this.frameBytesRemaining += this.frameShiftInBytes;
                }
            }
        }
    }
}
