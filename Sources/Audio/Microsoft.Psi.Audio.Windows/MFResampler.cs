// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// The Media Foundation audio resampler class.
    /// </summary>
    internal class MFResampler : IDisposable
    {
        private int bufferLengthInMs;
        private int inputBytesPerSecond;
        private IMFTransform resampler;
        private int inputBufferSize;
        private IMFMediaBuffer inputBuffer;
        private IMFSample inputSample;
        private int outputBufferSize;
        private IMFMediaBuffer outputBuffer;
        private IMFSample outputSample;
        private AudioDataAvailableCallback dataAvailableCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="MFResampler"/> class.
        /// </summary>
        public MFResampler()
        {
        }

        /// <summary>
        /// Disposes the <see cref="MFResampler"/> object.
        /// </summary>
        public void Dispose()
        {
            if (this.resampler != null)
            {
                Marshal.ReleaseComObject(this.resampler);
                this.resampler = null;
            }

            if (this.inputBuffer != null)
            {
                Marshal.ReleaseComObject(this.inputBuffer);
                this.inputBuffer = null;
            }

            if (this.inputSample != null)
            {
                Marshal.ReleaseComObject(this.inputSample);
                this.inputSample = null;
            }

            if (this.outputBuffer != null)
            {
                Marshal.ReleaseComObject(this.outputBuffer);
                this.outputBuffer = null;
            }

            if (this.outputSample != null)
            {
                Marshal.ReleaseComObject(this.outputSample);
                this.outputSample = null;
            }
        }

        /// <summary>
        /// Initialize the resampler.
        /// </summary>
        /// <param name="targetLatencyInMs">
        /// The target maximum number of milliseconds of acceptable lag between
        /// input and resampled output audio samples.
        /// </param>
        /// <param name="inFormat">
        /// The input format of the audio to be resampled.
        /// </param>
        /// <param name="outFormat">
        /// The output format of the resampled audio.
        /// </param>
        /// <param name="callback">
        /// Callback delegate which will receive the resampled data.
        /// </param>
        public void Initialize(int targetLatencyInMs, WaveFormat inFormat, WaveFormat outFormat, AudioDataAvailableCallback callback)
        {
            // Buffer sizes are calculated from the target latency.
            this.bufferLengthInMs = targetLatencyInMs;
            this.inputBytesPerSecond = (int)inFormat.AvgBytesPerSec;
            this.inputBufferSize = (int)(this.bufferLengthInMs * inFormat.AvgBytesPerSec / 1000);
            this.outputBufferSize = (int)(this.bufferLengthInMs * outFormat.AvgBytesPerSec / 1000);

            // Activate native Media Foundation COM objects on a thread-pool thread to ensure that they are in an MTA
            Task.Run(() =>
            {
                DeviceUtil.CreateResamplerBuffer(this.inputBufferSize, out this.inputSample, out this.inputBuffer);
                DeviceUtil.CreateResamplerBuffer(this.outputBufferSize, out this.outputSample, out this.outputBuffer);

                // Create resampler object
                this.resampler = DeviceUtil.CreateResampler(inFormat, outFormat);
            }).Wait();

            // Set the callback function
            this.dataAvailableCallback = callback;
        }

        /// <summary>
        /// Resamples audio data.
        /// </summary>
        /// <param name="dataPtr">
        /// Pointer to a buffer containing the audio data to be resampled.
        /// </param>
        /// <param name="length">
        /// The number of bytes in dataPtr.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp in 100-ns ticks of the first sample in pbData.
        /// </param>
        /// <returns>
        /// The number of bytes in dataPtr that were processed.
        /// </returns>
        public int Resample(IntPtr dataPtr, int length, long timestamp)
        {
            int resampledBytes = 0;

            while (length > 0)
            {
                IntPtr ptrLocked;
                int maxLength;

                this.inputBuffer.Lock(out ptrLocked, out maxLength, out _);

                // Copy the next chunk into the input buffer
                int bytesToWrite = Math.Min(maxLength, length);
                unsafe
                {
                    Buffer.MemoryCopy(dataPtr.ToPointer(), ptrLocked.ToPointer(), maxLength, bytesToWrite);
                }

                // Count the number of bytes processed
                resampledBytes += bytesToWrite;

                // Set the sample timestamp and duration
                long sampleDuration = 10000000L * bytesToWrite / this.inputBytesPerSecond;
                this.inputSample.SetSampleTime(timestamp);
                this.inputSample.SetSampleDuration(sampleDuration);

                // Process and resample the audio data
                this.inputBuffer.SetCurrentLength(bytesToWrite);
                var hr = this.resampler.ProcessInput(0, this.inputSample, 0);

                this.inputBuffer.Unlock();

                if (hr == 0)
                {
                    // Process output from resampler
                    this.ProcessResamplerOutput();
                }

                // Advance the data pointer and timestamp
                dataPtr += bytesToWrite;
                length -= bytesToWrite;
                timestamp += sampleDuration;
            }

            return resampledBytes;
        }

        /// <summary>
        /// Get data output from audio resampler and raises a callback.
        /// </summary>
        /// <returns>The number of bytes of resampled data.</returns>
        private int ProcessResamplerOutput()
        {
            MFTOutputDataBuffer outBuffer;
            int outStatus;
            int lockedLength = 0;

            outBuffer.StreamID = 0;
            outBuffer.Sample = this.outputSample;
            outBuffer.Status = 0;
            outBuffer.Events = null;

            var hr = this.resampler.ProcessOutput(0, 1, ref outBuffer, out outStatus);
            if (hr == 0)
            {
                IntPtr ptrLocked;
                this.outputBuffer.Lock(out ptrLocked, out _, out _);

                lockedLength = this.outputBuffer.GetCurrentLength();

                long sampleTime;
                hr = this.outputSample.GetSampleTime(out sampleTime);
                if (hr < 0)
                {
                    // Use zero to indicate that timestamp was not available
                    sampleTime = 0;
                }

                // Raise the callback to handle the captured samples
                this.dataAvailableCallback?.Invoke(ptrLocked, lockedLength, sampleTime);

                this.outputBuffer.Unlock();
            }

            return lockedLength;
        }
    }
}