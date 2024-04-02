// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Component that merges utterance continuations from consecutive speech recognition results.
    /// </summary>
    public class SpeechRecognitionContinuationDetector
    {
        private readonly Pipeline pipeline;
        private readonly SpeechRecognitionContinuationDetectorConfiguration configuration;
        private readonly List<(IStreamingSpeechRecognitionResult Result, DateTime OriginatingTime)> pendingSpeechRecognitionResults = new ();
        private DateTime lastVoicedActivityOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionContinuationDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        public SpeechRecognitionContinuationDetector(Pipeline pipeline, SpeechRecognitionContinuationDetectorConfiguration configuration)
        {
            this.pipeline = pipeline;
            this.configuration = configuration;

            this.SpeechRecognitionResultsInput = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, this.ReceiveSpeechRecognitionResults, nameof(this.SpeechRecognitionResultsInput));
            this.PartialSpeechRecognitionResultsInput = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, this.ReceivePartialSpeechRecognitionResults, nameof(this.PartialSpeechRecognitionResultsInput));
            this.VoiceActivityDetectorInput = pipeline.CreateReceiver<bool>(this, this.ReceiveVoiceActivityDetector, nameof(this.VoiceActivityDetectorInput));
            this.SpeechRecognitionResultsOutput = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.SpeechRecognitionResultsOutput));
            this.PartialSpeechRecognitionResultsOutput = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.PartialSpeechRecognitionResultsOutput));
        }

        /// <summary>
        /// Gets the receiver for the speech recognition results.
        /// </summary>
        public Receiver<IStreamingSpeechRecognitionResult> SpeechRecognitionResultsInput { get; }

        /// <summary>
        /// Gets the receiver for the partial speech recognition results.
        /// </summary>
        public Receiver<IStreamingSpeechRecognitionResult> PartialSpeechRecognitionResultsInput { get; }

        /// <summary>
        /// Gets the receiver for voice activity detector input.
        /// </summary>
        public Receiver<bool> VoiceActivityDetectorInput { get; }

        /// <summary>
        /// Gets the emitter for the speech recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> SpeechRecognitionResultsOutput { get; }

        /// <summary>
        /// Gets the emitter for the partial speech recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> PartialSpeechRecognitionResultsOutput { get; }

        private void ReceiveSpeechRecognitionResults(IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            this.pendingSpeechRecognitionResults.Add((result.DeepClone(), envelope.OriginatingTime));
        }

        private void ReceivePartialSpeechRecognitionResults(IStreamingSpeechRecognitionResult data, Envelope envelope)
        {
            if (this.pendingSpeechRecognitionResults.Count == 0)
            {
                this.PartialSpeechRecognitionResultsOutput.Post(data, envelope.OriginatingTime);
            }
            else
            {
                this.PartialSpeechRecognitionResultsOutput.Post(
                    new StreamingSpeechRecognitionResult(
                        false,
                        string.Join(this.configuration.Separator, this.pendingSpeechRecognitionResults.Select(r => r.Result.Text)) + this.configuration.Separator + data.Text),
                    envelope.OriginatingTime);
            }
        }

        private void ReceiveVoiceActivityDetector(bool data, Envelope envelope)
        {
            // If we had a previous voiced segment
            if (this.lastVoicedActivityOriginatingTime != DateTime.MinValue)
            {
                // If we're in a silence period
                if (!data)
                {
                    // And the silence period is long enough and we have the corresponding recognition results for the
                    // last voiced activity detection point
                    var currentTime = this.pipeline.GetCurrentTime();
                    if (currentTime - this.lastVoicedActivityOriginatingTime > TimeSpan.FromMilliseconds(this.configuration.MinSilenceTimeSpanMs) &&
                        this.pendingSpeechRecognitionResults.Count > 0 &&
                        this.pendingSpeechRecognitionResults.Last().OriginatingTime == this.lastVoicedActivityOriginatingTime)
                    {
                        var startTime = this.pendingSpeechRecognitionResults[0].OriginatingTime - this.pendingSpeechRecognitionResults[0].Result.Duration.Value;

                        this.SpeechRecognitionResultsOutput.Post(
                            new StreamingSpeechRecognitionResult(
                                false,
                                this.ConstructSpeechRecognitionText(),
                                this.pendingSpeechRecognitionResults.Min(r => r.Result.Confidence),
                                this.ConstructSpeechRecognitionAlternateList(),
                                this.ConstructAudioBuffer(),
                                this.pendingSpeechRecognitionResults.Last().OriginatingTime - startTime),
                            this.lastVoicedActivityOriginatingTime);
                        this.pendingSpeechRecognitionResults.Clear();
                    }
                }
            }

            if (data)
            {
                // If we didn't have a previous voiced segment, and we have a voiced segment now
                this.lastVoicedActivityOriginatingTime = envelope.OriginatingTime;
            }
        }

        private string ConstructSpeechRecognitionText()
            => string.Join(this.configuration.Separator, this.pendingSpeechRecognitionResults.Where(r => !string.IsNullOrEmpty(r.Result?.Text)).Select(r => r.Result.Text));

        private List<SpeechRecognitionAlternate> ConstructSpeechRecognitionAlternateList()
        {
            var alternates = new List<SpeechRecognitionAlternate>();
            this.ConstructSpeechRecognitionAlternatesList(0, string.Empty, alternates);
            return alternates;
        }

        private void ConstructSpeechRecognitionAlternatesList(int index, string prefix, List<SpeechRecognitionAlternate> alternates)
        {
            if (alternates.Count == this.configuration.MaxAlternatesCount)
            {
                return;
            }

            if (index < this.pendingSpeechRecognitionResults.Count - 1)
            {
                foreach (var alternate in this.pendingSpeechRecognitionResults[index].Result.Alternates)
                {
                    var text = string.IsNullOrEmpty(alternate.Text) ? string.Empty : alternate.Text + this.configuration.Separator;
                    this.ConstructSpeechRecognitionAlternatesList(index + 1, prefix + text, alternates);
                }
            }
            else if (index == this.pendingSpeechRecognitionResults.Count - 1)
            {
                foreach (var alternate in this.pendingSpeechRecognitionResults[index].Result.Alternates)
                {
                    alternates.Add(new SpeechRecognitionAlternate(prefix + alternate.Text, null));
                    if (alternates.Count == this.configuration.MaxAlternatesCount)
                    {
                        return;
                    }
                }
            }
        }

        private AudioBuffer ConstructAudioBuffer()
        {
            var audioBuffer = this.pendingSpeechRecognitionResults[0].Result.Audio;
            for (int i = 1; i < this.pendingSpeechRecognitionResults.Count; i++)
            {
                var emptyDuration = this.pendingSpeechRecognitionResults[i].OriginatingTime - this.pendingSpeechRecognitionResults[i].Result.Duration - this.pendingSpeechRecognitionResults[i - 1].OriginatingTime;
                audioBuffer += new AudioBuffer(emptyDuration.Value, audioBuffer.Format) + this.pendingSpeechRecognitionResults[i].Result.Audio;
            }

            return audioBuffer;
        }
    }
}
