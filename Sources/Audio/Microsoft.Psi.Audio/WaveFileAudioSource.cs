// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that streams audio from a WAVE file.
    /// </summary>
    public sealed class WaveFileAudioSource : Generator<AudioBuffer>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileAudioSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">The path name of the Wave file.</param>
        /// <param name="audioStartTime">Indicates a time to use for the start of the audio source. If null, the current time will be used.</param>
        /// <param name="targetLatencyMs">
        /// The size of each data buffer to post, determined by the amount of audio data it can hold.
        /// </param>
        public WaveFileAudioSource(Pipeline pipeline, string filename, DateTime? audioStartTime = null, int targetLatencyMs = 20)
            : base(pipeline, EnumerateWaveFile(pipeline, filename, audioStartTime, targetLatencyMs), audioStartTime)
        {
        }

        private static IEnumerator<ValueTuple<AudioBuffer, DateTime>> EnumerateWaveFile(
            Pipeline pipeline,
            string filename,
            DateTime? audioStartTime = null,
            int targetLatencyMs = 20)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryReader br = new BinaryReader(stream);
                WaveFormat format = WaveFileHelper.ReadWaveFileHeader(br);

                // Compute originating times based on audio chunk start time + duration
                var startTime = audioStartTime ?? pipeline.StartTime;

                // Buffer size based on target latency
                int bufferSize = (int)(format.AvgBytesPerSec * targetLatencyMs / 1000);

                // Get total length in bytes of audio data
                long bytesRemaining = (int)WaveFileHelper.ReadWaveDataLength(br);

                byte[] buffer = null;
                while (bytesRemaining > 0)
                {
                    int nextBytesToRead = (int)Math.Min(bufferSize, bytesRemaining);

                    // Re-allocate buffer if necessary
                    if ((buffer == null) || (buffer.Length != nextBytesToRead))
                    {
                        buffer = new byte[nextBytesToRead];
                    }

                    // Read next audio chunk
                    int bytesRead = br.Read(buffer, 0, (int)nextBytesToRead);
                    if (bytesRead == 0)
                    {
                        // Break on end of file
                        break;
                    }

                    // Bytes remaining
                    bytesRemaining -= bytesRead;

                    // Truncate buffer if necessary
                    if (bytesRead < nextBytesToRead)
                    {
                        byte[] buffer2 = new byte[bytesRead];
                        Array.Copy(buffer, 0, buffer2, 0, bytesRead);
                        buffer = buffer2;
                    }

                    // Add duration to get originating time
                    DateTime originatingTime = startTime.AddSeconds((double)bytesRead / (double)format.AvgBytesPerSec);

                    // Update for next audio chunk
                    startTime = originatingTime;

                    yield return ValueTuple.Create(new AudioBuffer(buffer, format), originatingTime);
                }
            }
        }
    }
}
