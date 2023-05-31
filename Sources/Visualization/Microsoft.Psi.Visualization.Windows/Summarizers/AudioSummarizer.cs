// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents an audio summarizer that performs interval-based data summarization over a series of audiobuffer values.
    /// </summary>
    [Summarizer]
    public class AudioSummarizer : Summarizer<AudioBuffer, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSummarizer"/> class.
        /// </summary>
        public AudioSummarizer()
            : this(0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSummarizer"/> class.
        /// </summary>
        /// <param name="channel">Channel to summarize.</param>
        public AudioSummarizer(int channel)
            : base((m, t) => Summarizer(m, t, (ushort)channel))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSummarizer"/> class.
        /// </summary>
        /// <param name="channel">Channel to summarize.</param>
        public AudioSummarizer(long channel)
            : base((m, t) => Summarizer(m, t, (ushort)channel))
        {
        }

        /// <summary>
        /// Summarizes an enumerable of audio buffer messages into summarized audio data.
        /// </summary>
        /// <param name="messages">Enumerable of audio buffer messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="channel">The audio channel to summarize.</param>
        /// <returns>List of summarized audio data.</returns>
        public static List<IntervalData<double>> Summarizer(IEnumerable<Message<AudioBuffer>> messages, TimeSpan interval, ushort channel)
        {
            var intervalData = new List<IntervalData<double>>();

            // Inspect the first message and only continue if it exists
            Message<AudioBuffer> first = messages.FirstOrDefault();
            if (first != default(Message<AudioBuffer>))
            {
                // Assume audio format is the same for all items
                WaveFormat audioFormat = first.Data.Format;

                // Get the appropriate sample converter. Assumption is 16-bit samples
                // are integer and 32-bit samples are floating point values.
                Func<byte[], int, double> sampleConverter;
                switch (audioFormat.BitsPerSample)
                {
                    case 16:
                        sampleConverter = (buf, i) => BitConverter.ToInt16(buf, i);
                        break;

                    case 32:
                        sampleConverter = (buf, i) => BitConverter.ToSingle(buf, i);
                        break;

                    default:
                        sampleConverter = (buf, i) => buf[i];
                        break;
                }

                TimeSpan samplingInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / audioFormat.SamplesPerSec);
                int byteOffset = 0 + ((channel % audioFormat.Channels) * audioFormat.BitsPerSample / 8);

                DateTime currentIntervalStartTime = GetIntervalStartTime(first.OriginatingTime, interval);
                double min = double.MaxValue;
                double max = double.MinValue;

                foreach (var message in messages)
                {
                    // Each item holds an AudioBuffer containing multiple audio samples
                    AudioBuffer audioBuffer = message.Data;

                    // Start (originating time) of the first sample in the AudioBuffer
                    DateTime currentTime = message.OriginatingTime - audioBuffer.Duration;

                    // calculate min/max for specified channel over the entire audio buffer
                    for (int offset = byteOffset; offset < audioBuffer.Length; offset += audioFormat.BlockAlign)
                    {
                        DateTime intervalStartTime = GetIntervalStartTime(currentTime, interval);

                        // Check if we are still in the current interval. If not, commit the current
                        // min/max values to the current interval if they have non-default values.
                        if ((intervalStartTime != currentIntervalStartTime) && (min != double.MaxValue) && (max != double.MinValue))
                        {
                            var current = IntervalData.Create(min + ((max - min) / 2), min, max, currentIntervalStartTime, interval);
                            intervalData.Add(current);

                            // Reset min-max for next interval
                            min = double.MaxValue;
                            max = double.MinValue;

                            // Update next interval start time
                            currentIntervalStartTime = intervalStartTime;
                        }

                        // Update min-max range for the current interval
                        double sampleValue = sampleConverter(audioBuffer.Data, offset);
                        if (sampleValue < min)
                        {
                            min = sampleValue;
                        }

                        if (sampleValue > max)
                        {
                            max = sampleValue;
                        }

                        // Increment by the sampling interval
                        currentTime += samplingInterval;
                    }
                }

                // Add the last interval
                intervalData.Add(IntervalData.Create(min + ((max - min) / 2), min, max, currentIntervalStartTime, interval));
            }

            return intervalData;
        }
    }
}
