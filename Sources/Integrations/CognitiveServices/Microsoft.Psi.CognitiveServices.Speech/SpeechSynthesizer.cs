// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Synthesizer = Microsoft.CognitiveServices.Speech.SpeechSynthesizer;

    /// <summary>
    /// Component that synthesizes speech from text strings into audio buffers.
    /// </summary>
    public class SpeechSynthesizer : ConsumerProducer<string, AudioBuffer>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly SpeechSynthesizerConfiguration configuration;
        private readonly WaveFormat format = WaveFormat.Create16kHz1Channel16BitPcm();
        private readonly List<(ulong AudioOffset, string Text)> textProgress = new ();

        private Synthesizer synthesizer;
        private DateTime currentStartTime;
        private string text;
        private DateTime textOriginatingTime;
        private AudioBuffer currentAudioBuffer;
        private CancellationTokenSource cts;
        private Task lastSpeakTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechSynthesizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        public SpeechSynthesizer(Pipeline pipeline, SpeechSynthesizerConfiguration configuration, string name = nameof(SpeechSynthesizer))
            : base(pipeline, name)
        {
            this.configuration = configuration;
            this.synthesizer = new Synthesizer(SpeechConfig.FromSubscription(this.configuration.SubscriptionKey, this.configuration.Region), null);

            this.synthesizer.SynthesisStarted += this.OnSynthesisStarted;
            this.synthesizer.Synthesizing += this.OnSynthesizing;
            this.synthesizer.SynthesisCompleted += this.OnSynthesisCompleted;
            this.synthesizer.WordBoundary += this.OnWordBoundary;
            this.synthesizer.VisemeReceived += this.OnVisemeReceived;

            this.WordBoundaryEvent = pipeline.CreateEmitter<int>(this, nameof(this.WordBoundaryEvent));
            this.VisemeReceivedEvent = pipeline.CreateEmitter<(int, string)>(this, nameof(this.VisemeReceivedEvent));
            this.SynthesisStartedEvent = pipeline.CreateEmitter<bool>(this, nameof(this.SynthesisStartedEvent));
            this.SynthesizingEvent = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.SynthesizingEvent));
            this.SynthesisCompletedEvent = pipeline.CreateEmitter<bool>(this, nameof(this.SynthesisCompletedEvent));
            this.FullResult = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.FullResult));
            this.SynthesisProgress = pipeline.CreateEmitter<SpeechSynthesisProgress>(this, nameof(this.SynthesisProgress));

            // Pre-open the connection to the cloud synthesizer immediately when the pipeline starts running,
            // which should cut down a bit on latency.
            pipeline.PipelineRun += (_, _) =>
            {
                using var connection = Connection.FromSpeechSynthesizer(this.synthesizer);
                connection.Open(true);
            };

            this.pipeline = pipeline;
        }

        /// <summary>
        /// Gets the emitter of word-boundary-reached events.
        /// The message payload captures the current word's text offset in the input text, in characters.
        /// </summary>
        public Emitter<int> WordBoundaryEvent { get; }

        /// <summary>
        /// Gets the emitter for viseme-received events.
        /// The message payload captures the viseme Id and the animation type of the viseme event (could be **svg** or other animation).
        /// </summary>
        public Emitter<(int Id, string Animation)> VisemeReceivedEvent { get; }

        /// <summary>
        /// Gets the emitter for synthesis progress events.
        /// </summary>
        public Emitter<SpeechSynthesisProgress> SynthesisProgress { get; }

        /// <summary>
        /// Gets the emitter for synthesis-started events.
        /// </summary>
        public Emitter<bool> SynthesisStartedEvent { get; }

        /// <summary>
        /// Gets the emitter for synthesizing events, containing an AudioBuffer of bytes that have been synthesized since the last event.
        /// </summary>
        public Emitter<AudioBuffer> SynthesizingEvent { get; }

        /// <summary>
        /// Gets the emitter for synthesis-completed events.
        /// </summary>
        public Emitter<bool> SynthesisCompletedEvent { get; }

        /// <summary>
        /// Gets the emitter of full synthesis results, rather than the incrementally streaming buffered result available in the Out emitter.
        /// This emitter posts one final result for each input text, and originating times are carried through to capture total latency.
        /// </summary>
        public Emitter<AudioBuffer> FullResult { get; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.synthesizer.StopSpeakingAsync();
            this.synthesizer.SynthesisStarted -= this.OnSynthesisStarted;
            this.synthesizer.Synthesizing -= this.OnSynthesizing;
            this.synthesizer.SynthesisCompleted -= this.OnSynthesisCompleted;
            this.synthesizer.WordBoundary -= this.OnWordBoundary;
            this.synthesizer.VisemeReceived -= this.OnVisemeReceived;
            notifyCompleted();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.synthesizer?.Dispose();
            this.synthesizer = null;

            this.cts?.Dispose();
            this.cts = null;
        }

        /// <inheritdoc/>
        protected override void Receive(string text, Envelope envelope)
        {
            if (text != null)
            {
                if (this.lastSpeakTask == null || this.lastSpeakTask.IsCompleted)
                {
                    // This is the first text we've received while not speaking, so start speaking immediately.
                    this.cts = new CancellationTokenSource();
                    this.lastSpeakTask = this.SpeakAsync(text, envelope.OriginatingTime, this.cts.Token);
                }
                else
                {
                    // We're already speaking, so queue the next text to be spoken after the previous one finishes.
                    // We do this by chaining a continuation task to the last one in the chain. Note that since
                    // SpeakAsync is an async method, we need to call Unwrap() to get the actual task representing
                    // the asynchronous SpeakAsync operation.
                    this.lastSpeakTask = this.lastSpeakTask.ContinueWith(
                        _ => this.SpeakAsync(text, envelope.OriginatingTime, this.cts.Token),
                        TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
                }
            }
            else
            {
                // If we receive a null text, it means we should stop speaking. Check
                // this.text first to see if we're currently speaking, and if so, stop.
                if (this.text != null)
                {
                    this.StopSpeaking(envelope.OriginatingTime);
                    this.lastSpeakTask = null;
                }
            }
        }

        private async Task SpeakAsync(string text, DateTime originatingTime, CancellationToken cancellationToken)
        {
            // Check for cancellation before starting to speak.
            cancellationToken.ThrowIfCancellationRequested();

            this.text = text;
            this.textProgress.Clear();
            this.textOriginatingTime = originatingTime;

            // The following logic for incrementally streaming the output audio was inspired by this sample:
            // https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_synthesis_server_scenario_sample.cs
            var result = await this.synthesizer.StartSpeakingTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioStarted)
            {
                using var audioDataStream = AudioDataStream.FromResult(result);
                var buffer = new byte[this.configuration.AudioPacketSize];
                uint totalSize = 0;
                uint filledSize = 0;

                // Known Issue: If input text strings are provide too close together, this could fail because the current time
                // might be earlier than the last posted time.
                this.currentStartTime = this.pipeline.GetCurrentTime();
                originatingTime = this.currentStartTime;

                var firstAudioOutput = true;
                var synthesisProgress = default(SpeechSynthesisProgress);
                var synthesisProgressText = string.Empty;

                // read audio block in a loop here
                // if it is the end of the audio stream, it will return 0
                while ((filledSize = audioDataStream.ReadData(buffer)) > 0)
                {
                    // If there is no previous synthesis progress, then post the start
                    if (firstAudioOutput)
                    {
                        this.SynthesisProgress.Post(SpeechSynthesisProgress.SynthesisStarted(), this.GetCompliantOriginatingTime(originatingTime, this.SynthesisProgress));
                        firstAudioOutput = false;
                    }

                    // If we had a synthesis progress already computed, output it. This is done here,
                    // rather than immediately after the computation of the synthesis progress to ensure
                    // that we are in a condition where there is actually more data and the synthesis
                    // progress computed does not actually correspond to a completion of synthesis (in
                    // the later case we will output a completed type of synthesis progress event.
                    if (synthesisProgress != null)
                    {
                        this.SynthesisProgress.Post(synthesisProgress, this.GetCompliantOriginatingTime(originatingTime, this.SynthesisProgress));
                        synthesisProgress = null;
                    }

                    // Check for cancellation after posting the previous synthesis progress
                    cancellationToken.ThrowIfCancellationRequested();

                    if (this.currentAudioBuffer.Data == null || this.currentAudioBuffer.Data.Length != (int)filledSize)
                    {
                        this.currentAudioBuffer = new AudioBuffer((int)filledSize, this.format);
                    }

                    Array.Copy(buffer, this.currentAudioBuffer.Data, filledSize);

                    // Compute an originating time based on the position of the audio.
                    var audioOffset = TimeSpan.FromSeconds((double)totalSize / this.format.AvgBytesPerSec);
                    originatingTime = this.currentStartTime + audioOffset;

                    // If the originatingTime is in the future, then wait until it is time to post the audio buffer.
                    var waitTime = originatingTime - this.pipeline.GetCurrentTime();
                    if (waitTime > TimeSpan.Zero)
                    {
                        await Task.Delay(waitTime, cancellationToken);
                    }

                    // Check for cancellation after waiting and before posting the next audio buffer
                    cancellationToken.ThrowIfCancellationRequested();

                    // Post the audio buffer output
                    this.Out.Post(this.currentAudioBuffer, this.GetCompliantOriginatingTime(originatingTime, this.Out));

                    // Now compute whether we have made more progress since the previous synthesis progress
                    var textSpokenSoFar = this.GetTextSpokenSoFar(audioOffset);
                    if (textSpokenSoFar != synthesisProgressText)
                    {
                        synthesisProgress = SpeechSynthesisProgress.SynthesisInProgress(textSpokenSoFar);
                        synthesisProgressText = textSpokenSoFar;
                    }

                    totalSize += filledSize;
                }

                this.SynthesisProgress.Post(SpeechSynthesisProgress.SynthesisCompleted(text), this.GetCompliantOriginatingTime(originatingTime, this.SynthesisProgress));
            }
        }

        private void StopSpeaking(DateTime originatingTime)
        {
            // Cancel using the cancellation token and stop the synthesizer
            this.cts.Cancel();
            this.synthesizer.StopSpeakingAsync();

            try
            {
                // Wait for the task chain to complete
                this.lastSpeakTask.Wait();
            }
            catch (AggregateException ae)
            {
                // Throw if the task threw any exception other than cancellation
                foreach (var e in ae.InnerExceptions)
                {
                    ae.Handle(e => e is TaskCanceledException);
                }

                // Post cancellation notification
                this.SynthesisProgress.Post(SpeechSynthesisProgress.SynthesisCancelled(this.text), this.GetCompliantOriginatingTime(originatingTime, this.SynthesisProgress));
            }

            this.text = null;
        }

        private void OnWordBoundary(object sender, SpeechSynthesisWordBoundaryEventArgs args)
        {
            var originatingTime = this.currentStartTime.AddTicks((long)args.AudioOffset);
            lock (this.textProgress)
            {
                this.textProgress.Add((args.AudioOffset, this.text.Substring(0, (int)args.TextOffset)));
            }

            this.WordBoundaryEvent.Post((int)args.TextOffset, this.GetCompliantOriginatingTime(originatingTime, this.WordBoundaryEvent));
        }

        private string GetTextSpokenSoFar(TimeSpan audioOffset)
        {
            var textSpokenSoFar = string.Empty;
            lock (this.textProgress)
            {
                for (int i = 0; i < this.textProgress.Count; i++)
                {
                    if ((ulong)audioOffset.Ticks > this.textProgress[i].AudioOffset)
                    {
                        textSpokenSoFar = this.textProgress[i].Text;
                    }
                }
            }

            return textSpokenSoFar;
        }

        private void OnVisemeReceived(object sender, SpeechSynthesisVisemeEventArgs args)
        {
            // One odd quirk of the synthesizer is that it often fires two viseme events with the same timestamp, where one of the
            // visemes is 0 (which maps to silence). For now, we just ignore all viseme results of value 0, but we may want to
            // revisit this in a way that is more robust.
            if (args.VisemeId == 0)
            {
                return;
            }

            var originatingTime = this.currentStartTime.AddTicks((long)args.AudioOffset);

            this.VisemeReceivedEvent.Post(
                ((int)args.VisemeId, args.Animation),
                this.GetCompliantOriginatingTime(originatingTime, this.VisemeReceivedEvent));
        }

        private void OnSynthesisStarted(object sender, SpeechSynthesisEventArgs args)
        {
            var originatingTime = this.pipeline.GetCurrentTime();
            this.SynthesisStartedEvent.Post(true, this.GetCompliantOriginatingTime(originatingTime, this.SynthesisStartedEvent));
        }

        private void OnSynthesisCompleted(object sender, SpeechSynthesisEventArgs args)
        {
            var originatingTime = this.pipeline.GetCurrentTime();
            this.SynthesisCompletedEvent.Post(true, this.GetCompliantOriginatingTime(originatingTime, this.SynthesisCompletedEvent));

            // Also post the full result with the originating time of the input text string that initiated the synthesis,
            // capturing the full latency.
            this.FullResult.Post(new AudioBuffer(args.Result.AudioData, this.format), this.textOriginatingTime);
        }

        private void OnSynthesizing(object sender, SpeechSynthesisEventArgs args)
        {
            this.SynthesizingEvent.Post(new AudioBuffer(args.Result.AudioData, this.format), this.pipeline.GetCurrentTime());
        }

        private DateTime GetCompliantOriginatingTime(DateTime originatingTime, IEmitter emitter)
            => originatingTime <= emitter.LastEnvelope.OriginatingTime ? emitter.LastEnvelope.OriginatingTime.AddTicks(1) : originatingTime;
    }
}