// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Speech.AudioFormat;
    using System.Speech.Recognition;
    using System.Speech.Recognition.SrgsGrammar;
    using System.Threading;
    using System.Xml;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Language;

    /// <summary>
    /// Component that performs speech recognition using the desktop speech recognition engine from `System.Speech`.
    /// </summary>
    /// <remarks>
    /// This component performs continuous recognition on an audio stream. Recognition results are of type
    /// <see cref="StreamingSpeechRecognitionResult"/> which implements the <see cref="IStreamingSpeechRecognitionResult"/> interface.
    /// This pattern allows for results from speech recognition components based on different underlying technologies to conform to a
    /// common interface for consumption by downstream components. Messages on the Out stream contain final results, while messages on
    /// the PartialRecognitionResults stream contain partial results. Partial results contain partial hypotheses while speech is in
    /// progress and are useful for displaying hypothesized text as feedback to the user. The final result is emitted once the recognizer
    /// has determined that speech has ended, and will contain the top hypothesis for the utterance.
    ///
    /// The originating times of speech recognition events emitted by this component are estimates. These are estimated in a couple of ways
    /// from the results that the underlying speech recognition engine returns. In the case of a final recognition result, we use the audio
    /// position offset of the recognized audio as reported by the recognition engine to compute an estimate of the originating time. For
    /// partial hypotheses, we use the engine's current offset into the audio stream to estimate the originating time.
    /// </remarks>
    public sealed class SystemSpeechRecognizer : ConsumerProducer<AudioBuffer, IStreamingSpeechRecognitionResult>, ISourceComponent, IDisposable
    {
        /// <summary>
        /// Stream for buffering audio samples to send to the speech recognition engine.
        /// </summary>
        private readonly BufferedAudioStream inputAudioStream;

        // A mapping from emitter to originating time consistency-check group. All emitters in any given
        // group enforce monotonically increasing originating times across the group when posting.
        private readonly Dictionary<IEmitter, EmitterGroup> originatingTimeConsistencyCheckGroup = new Dictionary<IEmitter, EmitterGroup>();

        // The last originating time that was recorded for each output stream group.
        private readonly Dictionary<EmitterGroup, DateTime> lastPostedOriginatingTimes = new Dictionary<EmitterGroup, DateTime>();

        /// <summary>
        /// The System.Speech speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// The implied stream start time.
        /// </summary>
        private DateTime streamStartTime;

        /// <summary>
        /// Event to signal that the recognizer has been stopped.
        /// </summary>
        private ManualResetEvent recognizeComplete;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public SystemSpeechRecognizer(Pipeline pipeline, SystemSpeechRecognizerConfiguration configuration)
            : base(pipeline)
        {
            this.Configuration = configuration ?? new SystemSpeechRecognizerConfiguration();

            // create receiver of grammar updates
            this.ReceiveGrammars = pipeline.CreateReceiver<IEnumerable<string>>(this, this.SetGrammars, nameof(this.ReceiveGrammars), true);

            // assign the default Out emitter to the RecognitionResults group
            this.originatingTimeConsistencyCheckGroup.Add(this.Out, EmitterGroup.RecognitionResults);

            // create the additional output streams
            this.PartialRecognitionResults = this.CreateEmitterInGroup<IStreamingSpeechRecognitionResult>(pipeline, nameof(this.PartialRecognitionResults), EmitterGroup.RecognitionResults);
            this.IntentData = this.CreateEmitterInGroup<IntentData>(pipeline, nameof(this.IntentData), EmitterGroup.IntentData);

            // create output streams for speech event args
            this.SpeechDetected = this.CreateEmitterInGroup<SpeechDetectedEventArgs>(pipeline, nameof(this.SpeechDetected), EmitterGroup.SpeechEvents);
            this.SpeechHypothesized = this.CreateEmitterInGroup<SpeechHypothesizedEventArgs>(pipeline, nameof(this.SpeechHypothesized), EmitterGroup.SpeechEvents);
            this.SpeechRecognized = this.CreateEmitterInGroup<SpeechRecognizedEventArgs>(pipeline, nameof(this.SpeechRecognized), EmitterGroup.SpeechEvents);
            this.SpeechRecognitionRejected = this.CreateEmitterInGroup<SpeechRecognitionRejectedEventArgs>(pipeline, nameof(this.SpeechRecognitionRejected), EmitterGroup.SpeechEvents);
            this.RecognizeCompleted = this.CreateEmitterInGroup<RecognizeCompletedEventArgs>(pipeline, nameof(this.RecognizeCompleted), EmitterGroup.SpeechEvents);
            this.EmulateRecognizeCompleted = this.CreateEmitterInGroup<EmulateRecognizeCompletedEventArgs>(pipeline, nameof(this.EmulateRecognizeCompleted), EmitterGroup.SpeechEvents);

            // create output streams for audio state event args
            this.AudioSignalProblemOccurred = this.CreateEmitterInGroup<AudioSignalProblemOccurredEventArgs>(pipeline, nameof(this.AudioSignalProblemOccurred), EmitterGroup.AudioEvents);
            this.AudioStateChanged = this.CreateEmitterInGroup<AudioStateChangedEventArgs>(pipeline, nameof(this.AudioStateChanged), EmitterGroup.AudioEvents);
            this.AudioLevelUpdated = this.CreateEmitterInGroup<AudioLevelUpdatedEventArgs>(pipeline, nameof(this.AudioLevelUpdated), EmitterGroup.AudioEvents);

            // create output stream for the grammar event args
            this.LoadGrammarCompleted = this.CreateEmitterInGroup<LoadGrammarCompletedEventArgs>(pipeline, nameof(this.LoadGrammarCompleted), EmitterGroup.StateUpdateEvents);
            this.RecognizerUpdateReached = this.CreateEmitterInGroup<RecognizerUpdateReachedEventArgs>(pipeline, nameof(this.RecognizerUpdateReached), EmitterGroup.StateUpdateEvents);

            // create table of last stream group originating times
            this.lastPostedOriginatingTimes = new Dictionary<EmitterGroup, DateTime>();

            // Create a BufferedAudioStream with an internal buffer large enough
            // to accommodate the specified number of milliseconds of audio data.
            this.inputAudioStream = new BufferedAudioStream(
                this.Configuration.InputFormat.AvgBytesPerSec * this.Configuration.BufferLengthInMs / 1000);

            this.recognizeComplete = new ManualResetEvent(false);

            // create the recognition engine
            this.speechRecognitionEngine = this.CreateSpeechRecognitionEngine();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public SystemSpeechRecognizer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new SystemSpeechRecognizerConfiguration() : new ConfigurationHelper<SystemSpeechRecognizerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// An enumerable for emitter groups.
        /// </summary>
        private enum EmitterGroup
        {
            RecognitionResults,
            IntentData,
            SpeechEvents,
            AudioEvents,
            StateUpdateEvents,
        }

        /// <summary>
        /// Gets the receiver for new grammars.
        /// </summary>
        public Receiver<IEnumerable<string>> ReceiveGrammars { get; }

        /// <summary>
        /// Gets the output stream of partial recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> PartialRecognitionResults { get; }

        /// <summary>
        /// Gets the output stream of intents and entities.
        /// </summary>
        public Emitter<IntentData> IntentData { get; }

        /// <summary>
        /// Gets the output stream of speech detected events.
        /// </summary>
        public Emitter<SpeechDetectedEventArgs> SpeechDetected { get; }

        /// <summary>
        /// Gets the output stream of speech hypothesized events.
        /// </summary>
        public Emitter<SpeechHypothesizedEventArgs> SpeechHypothesized { get; }

        /// <summary>
        /// Gets the output stream of speech recognized events.
        /// </summary>
        public Emitter<SpeechRecognizedEventArgs> SpeechRecognized { get; }

        /// <summary>
        /// Gets the output stream of speech recognition rejected events.
        /// </summary>
        public Emitter<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected { get; }

        /// <summary>
        /// Gets the output stream of audio problem events.
        /// </summary>
        public Emitter<AudioSignalProblemOccurredEventArgs> AudioSignalProblemOccurred { get; }

        /// <summary>
        /// Gets the output stream of audio state change events.
        /// </summary>
        public Emitter<AudioStateChangedEventArgs> AudioStateChanged { get; }

        /// <summary>
        /// Gets the output stream of recognize completed events.
        /// </summary>
        public Emitter<RecognizeCompletedEventArgs> RecognizeCompleted { get; }

        /// <summary>
        /// Gets the output stream of audio level updated events.
        /// </summary>
        public Emitter<AudioLevelUpdatedEventArgs> AudioLevelUpdated { get; }

        /// <summary>
        /// Gets the output stream of emulate recognize completed completed events.
        /// </summary>
        public Emitter<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted { get; }

        /// <summary>
        /// Gets the output stream of load grammar completed events.
        /// </summary>
        public Emitter<LoadGrammarCompletedEventArgs> LoadGrammarCompleted { get; }

        /// <summary>
        /// Gets the output stream of recognizer update reached events.
        /// </summary>
        public Emitter<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached { get; }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private SystemSpeechRecognizerConfiguration Configuration { get; }

        /// <summary>
        /// Replace grammars with given.
        /// </summary>
        /// <param name="srgsXmlGrammars">A collection of XML-format speech grammars that conform to the SRGS 1.0 specification.</param>
        public void SetGrammars(Message<IEnumerable<string>> srgsXmlGrammars)
        {
            this.speechRecognitionEngine.RequestRecognizerUpdate(srgsXmlGrammars.Data.Select(g =>
            {
                using (var xmlReader = XmlReader.Create(new StringReader(g)))
                {
                    return new Grammar(new SrgsDocument(xmlReader));
                }
            }));
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (this.speechRecognitionEngine != null)
            {
                this.speechRecognitionEngine.Dispose();
                this.speechRecognitionEngine = null;
            }

            // Free any other managed objects here.
            this.recognizeComplete.Dispose();
            this.recognizeComplete = null;
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // start the speech recognition engine
            this.speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            // Unregister handlers so they won't fire while disposing.
            this.speechRecognitionEngine.SpeechDetected -= this.OnSpeechDetected;
            this.speechRecognitionEngine.SpeechHypothesized -= this.OnSpeechHypothesized;
            this.speechRecognitionEngine.SpeechRecognized -= this.OnSpeechRecognized;
            this.speechRecognitionEngine.SpeechRecognitionRejected -= this.OnSpeechRecognitionRejected;
            this.speechRecognitionEngine.AudioSignalProblemOccurred -= this.OnAudioSignalProblemOccurred;
            this.speechRecognitionEngine.AudioStateChanged -= this.OnAudioStateChanged;
            this.speechRecognitionEngine.RecognizeCompleted -= this.OnRecognizeCompleted;
            this.speechRecognitionEngine.RecognizerUpdateReached -= this.OnRecognizerUpdateReached;
            this.speechRecognitionEngine.AudioLevelUpdated -= this.OnAudioLevelUpdated;
            this.speechRecognitionEngine.EmulateRecognizeCompleted -= this.OnEmulateRecognizeCompleted;
            this.speechRecognitionEngine.LoadGrammarCompleted -= this.OnLoadGrammarCompleted;

            // Close the audio stream first so that RecognizeAyncCancel
            // will finalize the current recognition operation.
            this.inputAudioStream.Close();

            // Cancel any in-progress recognition and wait for completion
            // (OnRecognizeCompleted) before disposing of recognition engine.
            this.speechRecognitionEngine.RecognizeAsyncCancel();
            this.recognizeComplete.WaitOne(333);

            notifyCompleted();
        }

        /// <summary>
        /// Receiver for audio data.
        /// </summary>
        /// <param name="audio">A buffer containing the next chunk of audio data.</param>
        /// <param name="e">The message envelope for the audio data.</param>
        protected override void Receive(AudioBuffer audio, Envelope e)
        {
            this.inputAudioStream.Write(audio.Data, 0, audio.Length);

            // Compute the implied start time based on the latest originating time, less the total number of bytes
            // written less overruns. This will be the originating time at the beginning of the audio input stream
            // seen by the recognizer, and can thus be used to compute the originating time of any recognizer event
            // from its reported AudioPosition.
            this.streamStartTime = e.OriginatingTime -
                TimeSpan.FromSeconds((double)(this.inputAudioStream.BytesWritten - this.inputAudioStream.BytesOverrun) /
                    this.Configuration.InputFormat.AvgBytesPerSec);
        }

        /// <summary>
        /// Creates a new speech recognition engine.
        /// </summary>
        /// <returns>A new speech recognition engine object.</returns>
        private SpeechRecognitionEngine CreateSpeechRecognitionEngine()
        {
            // Create the recognizer
            var recognizer = SystemSpeech.CreateSpeechRecognitionEngine(this.Configuration.Language, this.Configuration.Grammars);

            // Attach event handlers for speech recognition events
            recognizer.SpeechDetected += this.OnSpeechDetected;
            recognizer.SpeechHypothesized += this.OnSpeechHypothesized;
            recognizer.SpeechRecognized += this.OnSpeechRecognized;
            recognizer.SpeechRecognitionRejected += this.OnSpeechRecognitionRejected;
            recognizer.AudioSignalProblemOccurred += this.OnAudioSignalProblemOccurred;
            recognizer.AudioStateChanged += this.OnAudioStateChanged;
            recognizer.RecognizeCompleted += this.OnRecognizeCompleted;
            recognizer.RecognizerUpdateReached += this.OnRecognizerUpdateReached;
            recognizer.AudioLevelUpdated += this.OnAudioLevelUpdated;
            recognizer.EmulateRecognizeCompleted += this.OnEmulateRecognizeCompleted;
            recognizer.LoadGrammarCompleted += this.OnLoadGrammarCompleted;

            // Create the format info from the configuration input format
            SpeechAudioFormatInfo formatInfo = new SpeechAudioFormatInfo(
                (EncodingFormat)this.Configuration.InputFormat.FormatTag,
                (int)this.Configuration.InputFormat.SamplesPerSec,
                this.Configuration.InputFormat.BitsPerSample,
                this.Configuration.InputFormat.Channels,
                (int)this.Configuration.InputFormat.AvgBytesPerSec,
                this.Configuration.InputFormat.BlockAlign,
                (this.Configuration.InputFormat is WaveFormatEx) ? ((WaveFormatEx)this.Configuration.InputFormat).ExtraInfo : null);

            // Specify the input stream and audio format
            recognizer.SetInputToAudioStream(this.inputAudioStream, formatInfo);

            recognizer.InitialSilenceTimeout = TimeSpan.FromMilliseconds(this.Configuration.InitialSilenceTimeoutMs);
            recognizer.BabbleTimeout = TimeSpan.FromMilliseconds(this.Configuration.BabbleTimeoutMs);
            recognizer.EndSilenceTimeout = TimeSpan.FromMilliseconds(this.Configuration.EndSilenceTimeoutMs);
            recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(this.Configuration.EndSilenceTimeoutAmbiguousMs);

            return recognizer;
        }

        /// <summary>
        /// Called when the engine finalizes the recognition operation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.RecognizeCompleted, e, originatingTime);

            this.recognizeComplete.Set();
        }

        /// <summary>
        /// Called when the final recognition result received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Convention for intervals is to use the end time as the originating time so we add the duration as well.
            DateTime originatingTime = this.streamStartTime + e.Result.Audio.AudioPosition + e.Result.Audio.Duration;

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechRecognized, e, originatingTime);

            if (e.Result.Alternates.Count > 0)
            {
                var result = this.BuildSpeechRecognitionResult(e.Result);
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, result, originatingTime);

                if (e.Result.Semantics != null)
                {
                    var intents = SystemSpeech.BuildIntentData(e.Result.Semantics);
                    this.PostWithOriginatingTimeConsistencyCheck(this.IntentData, intents, originatingTime);
                }
            }
        }

        /// <summary>
        /// Called whenever a partial recognition result is available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // SpeechHypothesized events don't come with an audio position, so we will have to
            // calculate it based on the current position of the stream reader.
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechHypothesized, e, originatingTime);

            var result = this.BuildPartialSpeechRecognitionResult(e.Result);
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialRecognitionResults, result, originatingTime);
        }

        /// <summary>
        /// Called when speech is detected.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechDetected, e, originatingTime);
        }

        /// <summary>
        /// Called when the engine is unable to match speech input to any of its enabled grammars.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            // Convention for intervals is to use the end time as the originating time so we add the duration as well.
            DateTime originatingTime = this.streamStartTime + e.Result.Audio.AudioPosition + e.Result.Audio.Duration;

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechRecognitionRejected, e, originatingTime);

            if (e.Result.Alternates.Count > 0)
            {
                var result = this.BuildSpeechRecognitionResult(e.Result);
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, result, originatingTime);
            }
        }

        /// <summary>
        /// Requested by `SetGrammars()` - now ready to update.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args (`UserToken` expected to contain grammars).</param>
        private void OnRecognizerUpdateReached(object sender, RecognizerUpdateReachedEventArgs e)
        {
            Console.WriteLine("Updating grammars");
            if (e.UserToken is IEnumerable<Grammar> grammars)
            {
                this.speechRecognitionEngine.UnloadAllGrammars();
                foreach (var g in grammars)
                {
                    this.speechRecognitionEngine.LoadGrammar(g);
                }
            }

            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.RecognizerUpdateReached, e, originatingTime);
        }

        private void OnAudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.AudioSignalProblemOccurred, e, originatingTime);
        }

        private void OnAudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.AudioStateChanged, e, originatingTime);
        }

        private void OnAudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.AudioLevelUpdated, e, originatingTime);
        }

        private void OnEmulateRecognizeCompleted(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.EmulateRecognizeCompleted, e, originatingTime);
        }

        private void OnLoadGrammarCompleted(object sender, LoadGrammarCompletedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.LoadGrammarCompleted, e, originatingTime);
        }

        /// <summary>
        /// Creates an emitter and assigns it to the specified originating time consistency check group such
        /// that all streams in any given group will contain post messages with strictly monotonically increasing
        /// originating times.
        /// </summary>
        /// <typeparam name="T">The type of the output stream.</typeparam>
        /// <param name="pipeline">The pipeline in which this component was created.</param>
        /// <param name="name">The name of the stream.</param>
        /// <param name="consistencyCheckGroup">The group in which to create the stream.</param>
        /// <returns>The newly created emitter for the stream.</returns>
        private Emitter<T> CreateEmitterInGroup<T>(Pipeline pipeline, string name, EmitterGroup consistencyCheckGroup)
        {
            var emitter = pipeline.CreateEmitter<T>(this, name);
            this.originatingTimeConsistencyCheckGroup[emitter] = consistencyCheckGroup;
            return emitter;
        }

        /// <summary>
        /// Posts a message to a stream while ensuring the consistency of the supplied originating time
        /// such that it cannot be before the originating time of the last posted message on the stream.
        /// If so, it will be adjusted accordingly.
        /// </summary>
        /// <remarks>
        /// Use this method when posting messages to output streams where the computed originating times of
        /// the messages are not guaranteed to be monotonically increasing, which is a requirement of Psi.
        /// </remarks>
        /// <typeparam name="T">The type of the output stream.</typeparam>
        /// <param name="stream">The stream on which to post.</param>
        /// <param name="data">The data to post.</param>
        /// <param name="originatingTime">The originating time of the data.</param>
        private void PostWithOriginatingTimeConsistencyCheck<T>(Emitter<T> stream, T data, DateTime originatingTime)
        {
            var group = this.originatingTimeConsistencyCheckGroup[stream];

            // Get the last posted originating time on this stream
            this.lastPostedOriginatingTimes.TryGetValue(group, out DateTime lastPostedOriginatingTime);

            // Enforce monotonically increasing originating time
            if (originatingTime <= lastPostedOriginatingTime)
            {
                originatingTime = lastPostedOriginatingTime.AddTicks(1);
            }

            // Post the message and update the originating time for this stream
            stream.Post(data, originatingTime);
            this.lastPostedOriginatingTimes[group] = originatingTime;
        }

        /// <summary>
        /// Builds a SpeechRecognitionResult object from a RecognitionResult returned by the recognizer.
        /// </summary>
        /// <param name="result">The RecognitionResult object.</param>
        /// <returns>A SpeechRecognitionResult object containing the recognition results.</returns>
        private StreamingSpeechRecognitionResult BuildSpeechRecognitionResult(RecognitionResult result)
        {
            // Allocate a buffer large enough to hold the raw audio bytes. Round up to account for precision errors.
            byte[] audioBytes = new byte[(int)Math.Ceiling(result.Audio.Duration.TotalSeconds * result.Audio.Format.AverageBytesPerSecond)];

            // Write recognized audio in the RecognitionResult to a MemoryStream and create an AudioBuffer of the entire utterance
            using (MemoryStream ms = new MemoryStream(audioBytes))
            {
                result.Audio.WriteToAudioStream(ms);
            }

            return new StreamingSpeechRecognitionResult(
                true,
                result.Text,
                result.Confidence,
                result.Alternates.Select(p => new SpeechRecognitionAlternate(p.Text, p.Confidence)),
                new AudioBuffer(audioBytes, this.Configuration.InputFormat),
                result.Audio?.Duration);
        }

        /// <summary>
        /// Builds a SpeechRecognitionResult object from a partial RecognitionResult returned by the recognizer.
        /// </summary>
        /// <param name="result">The RecognitionResult object.</param>
        /// <returns>A SpeechRecognitionResult object containing the partial recognition result.</returns>
        private StreamingSpeechRecognitionResult BuildPartialSpeechRecognitionResult(RecognitionResult result)
        {
            return new StreamingSpeechRecognitionResult(false, result.Text, result.Confidence, null, null, result.Audio?.Duration);
        }
    }
}
