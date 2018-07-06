// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
    /// Component that implements a speech recognizer.
    /// </summary>
    /// <remarks>
    /// This component performs continuous recognition on an audio stream. Recognition results are posted as they are available on its
    /// Out stream. Recognition results are of type <see cref="StreamingSpeechRecognitionResult"/> which
    /// implements the <see cref="IStreamingSpeechRecognitionResult"/> interface. This pattern allows for results from speech recognition
    /// components based on different underlying technologies to conform to a common interface for consumption by downstream components.
    /// Messages on the Out stream may contain partial or final results, distinguished by the
    /// <see cref="StreamingSpeechRecognitionResult.IsFinal"/> property. Partial results contain partial hypotheses while speech is in
    /// progress and are useful for displaying hypothesized text as feedback to the user. The final result is emitted once the recognizer
    /// has determined that speech has ended, and will contain the top hypothesis for the utterance.
    ///
    /// The originating times of speech recognition events emitted by this component are estimates. These are estimated in a couple of ways
    /// from the results that the underlying speech recognition engine returns. In the case of a final recognition result, we use the audio
    /// position offset of the recognized audio as reported by the recognition engine to compute an estimate of the originating time. For
    /// partial hypotheses, we use the engine's current offset into the audio stream to estimate the originating time.
    /// </remarks>
    public sealed class SystemSpeechRecognizer : ConsumerProducer<AudioBuffer, IStreamingSpeechRecognitionResult>, IDisposable
    {
        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly SystemSpeechRecognizerConfiguration configuration;

        /// <summary>
        /// The output stream of intents and entities.
        /// </summary>
        private readonly Emitter<IntentData> intentData;

        /// <summary>
        /// The output stream of speech detected events
        /// </summary>
        private readonly Emitter<SpeechDetectedEventArgs> speechDetected;

        /// <summary>
        /// The output stream of speech hypothesized events
        /// </summary>
        private readonly Emitter<SpeechHypothesizedEventArgs> speechHypothesized;

        /// <summary>
        /// The output stream of speech recognized events
        /// </summary>
        private readonly Emitter<SpeechRecognizedEventArgs> speechRecognized;

        /// <summary>
        /// The output stream of speech recognition rejected events
        /// </summary>
        private readonly Emitter<SpeechRecognitionRejectedEventArgs> speechRecognitionRejected;

        /// <summary>
        /// The output stream of audio signal problem occurred events
        /// </summary>
        private readonly Emitter<AudioSignalProblemOccurredEventArgs> audioSignalProblemOccurred;

        /// <summary>
        /// The output stream of audio state changed events
        /// </summary>
        private readonly Emitter<AudioStateChangedEventArgs> audioStateChanged;

        /// <summary>
        /// The output stream of recognize completed events
        /// </summary>
        private readonly Emitter<RecognizeCompletedEventArgs> recognizeCompleted;

        /// <summary>
        /// The output stream of audio level updated events
        /// </summary>
        private readonly Emitter<AudioLevelUpdatedEventArgs> audioLevelUpdated;

        /// <summary>
        /// The output stream of emulate recognize completed events
        /// </summary>
        private readonly Emitter<EmulateRecognizeCompletedEventArgs> emulateRecognizeCompleted;

        /// <summary>
        /// The output stream of load grammar completed events
        /// </summary>
        private readonly Emitter<LoadGrammarCompletedEventArgs> loadGrammarCompleted;

        /// <summary>
        /// The output stream of recognizer update reached events
        /// </summary>
        private readonly Emitter<RecognizerUpdateReachedEventArgs> recognizerUpdateReached;

        /// <summary>
        /// The System.Speech speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// Stream for buffering audio samples to send to the speech recognition engine.
        /// </summary>
        private BufferedAudioStream inputAudioStream;

        /// <summary>
        /// The implied stream start time.
        /// </summary>
        private DateTime streamStartTime;

        /// <summary>
        /// The last originating time that was recorded for each output stream.
        /// </summary>
        private Dictionary<IEmitter, DateTime> lastPostedOriginatingTimes;

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
            pipeline.RegisterPipelineStartHandler(this, this.OnPipelineStart);

            this.configuration = configuration ?? new SystemSpeechRecognizerConfiguration();

            // create receiver of grammar updates
            this.ReceiveGrammars = pipeline.CreateReceiver<IEnumerable<string>>(this, this.SetGrammars, nameof(this.ReceiveGrammars), true);

            // create the additional output streams
            this.intentData = pipeline.CreateEmitter<IntentData>(this, "IntentData");

            // create output streams for all the event args
            this.speechDetected = pipeline.CreateEmitter<SpeechDetectedEventArgs>(this, nameof(this.SpeechDetected));
            this.speechHypothesized = pipeline.CreateEmitter<SpeechHypothesizedEventArgs>(this, nameof(this.SpeechHypothesized));
            this.speechRecognized = pipeline.CreateEmitter<SpeechRecognizedEventArgs>(this, nameof(this.SpeechRecognized));
            this.speechRecognitionRejected = pipeline.CreateEmitter<SpeechRecognitionRejectedEventArgs>(this, nameof(this.SpeechRecognitionRejected));
            this.audioSignalProblemOccurred = pipeline.CreateEmitter<AudioSignalProblemOccurredEventArgs>(this, nameof(this.AudioSignalProblemOccurred));
            this.audioStateChanged = pipeline.CreateEmitter<AudioStateChangedEventArgs>(this, nameof(this.AudioStateChanged));
            this.recognizeCompleted = pipeline.CreateEmitter<RecognizeCompletedEventArgs>(this, nameof(this.RecognizeCompleted));
            this.audioLevelUpdated = pipeline.CreateEmitter<AudioLevelUpdatedEventArgs>(this, nameof(this.AudioLevelUpdated));
            this.emulateRecognizeCompleted = pipeline.CreateEmitter<EmulateRecognizeCompletedEventArgs>(this, nameof(this.EmulateRecognizeCompleted));
            this.loadGrammarCompleted = pipeline.CreateEmitter<LoadGrammarCompletedEventArgs>(this, nameof(this.LoadGrammarCompleted));
            this.recognizerUpdateReached = pipeline.CreateEmitter<RecognizerUpdateReachedEventArgs>(this, nameof(this.RecognizerUpdateReached));

            // create table of last stream originating times
            this.lastPostedOriginatingTimes = new Dictionary<IEmitter, DateTime>();

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
        /// Gets the receiver for new grammars.
        /// </summary>
        public Receiver<IEnumerable<string>> ReceiveGrammars { get; }

        /// <summary>
        /// Gets the output stream of intents and entities.
        /// </summary>
        public Emitter<IntentData> IntentData
        {
            get { return this.intentData; }
        }

        /// <summary>
        /// Gets the output stream of speech detected events
        /// </summary>
        public Emitter<SpeechDetectedEventArgs> SpeechDetected
        {
            get { return this.speechDetected; }
        }

        /// <summary>
        /// Gets the output stream of speech hypothesized events
        /// </summary>
        public Emitter<SpeechHypothesizedEventArgs> SpeechHypothesized
        {
            get { return this.speechHypothesized; }
        }

        /// <summary>
        /// Gets the output stream of speech recognized events
        /// </summary>
        public Emitter<SpeechRecognizedEventArgs> SpeechRecognized
        {
            get { return this.speechRecognized; }
        }

        /// <summary>
        /// Gets the output stream of speech recognition rejected events
        /// </summary>
        public Emitter<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected
        {
            get { return this.speechRecognitionRejected; }
        }

        /// <summary>
        /// Gets the output stream of audio problem events
        /// </summary>
        public Emitter<AudioSignalProblemOccurredEventArgs> AudioSignalProblemOccurred
        {
            get { return this.audioSignalProblemOccurred; }
        }

        /// <summary>
        /// Gets the output stream of audio state change events
        /// </summary>
        public Emitter<AudioStateChangedEventArgs> AudioStateChanged
        {
            get { return this.audioStateChanged; }
        }

        /// <summary>
        /// Gets the output stream of recognize completed events
        /// </summary>
        public Emitter<RecognizeCompletedEventArgs> RecognizeCompleted
        {
            get { return this.recognizeCompleted; }
        }

        /// <summary>
        /// Gets the output stream of audio level updated events
        /// </summary>
        public Emitter<AudioLevelUpdatedEventArgs> AudioLevelUpdated
        {
            get { return this.audioLevelUpdated; }
        }

        /// <summary>
        /// Gets the output stream of emulate recognize completed completed events
        /// </summary>
        public Emitter<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted
        {
            get { return this.emulateRecognizeCompleted; }
        }

        /// <summary>
        /// Gets the output stream of load grammar completed events
        /// </summary>
        public Emitter<LoadGrammarCompletedEventArgs> LoadGrammarCompleted
        {
            get { return this.loadGrammarCompleted; }
        }

        /// <summary>
        /// Gets the output stream of recognizer update reached events
        /// </summary>
        public Emitter<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached
        {
            get { return this.recognizerUpdateReached; }
        }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private SystemSpeechRecognizerConfiguration Configuration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Called once all the subscriptions are established.
        /// </summary>
        public void OnPipelineStart()
        {
            // start the speech recognition engine
            this.speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

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
                this.speechRecognitionEngine.Dispose();
                this.speechRecognitionEngine = null;
            }

            // Free any other managed objects here.
            this.recognizeComplete.Dispose();
            this.recognizeComplete = null;
        }

        /// <summary>
        /// Receiver for audio data.
        /// </summary>
        /// <param name="audio">A buffer containing the next chunk of audio data.</param>
        /// <param name="e">The message envelope for the audio data.</param>
        protected override void Receive(AudioBuffer audio, Envelope e)
        {
            this.inputAudioStream.Write(audio.Data, 0, audio.Length);

            // Compute the implied start time based on the latest originating time,
            // less the total number of bytes written less overruns. This will be
            // the time that the audio stream "thinks" it started, and can thus be
            // used to compute originating time from RecognizedAudio.AudioPosition.
            this.streamStartTime = e.OriginatingTime -
                TimeSpan.FromSeconds((double)(this.inputAudioStream.BytesWritten - this.inputAudioStream.BytesOverrun) /
                    this.Configuration.InputFormat.AvgBytesPerSec);
        }

        /// <summary>
        /// Creates a new speech recognition engine
        /// </summary>
        /// <returns>A new speech recognition engine object.</returns>
        private SpeechRecognitionEngine CreateSpeechRecognitionEngine()
        {
            SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(new CultureInfo(this.Configuration.Language));
            if (this.Configuration.Grammars == null)
            {
                recognizer.LoadGrammar(new DictationGrammar());
            }
            else
            {
                foreach (GrammarInfo grammarInfo in this.Configuration.Grammars)
                {
                    Grammar grammar = new Grammar(grammarInfo.FileName);
                    grammar.Name = grammarInfo.Name;
                    recognizer.LoadGrammar(grammar);
                }
            }

            // Event handlers for speech recognition events
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

            return recognizer;
        }

        /// <summary>
        /// Called when the engine finalizes the recognition operation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            this.recognizeComplete.Set();

            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.recognizeCompleted, e, originatingTime);
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
            this.PostWithOriginatingTimeConsistencyCheck(this.speechRecognized, e, originatingTime);

            if (e.Result.Alternates.Count > 0)
            {
                var result = this.BuildSpeechRecognitionResult(e.Result);
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, result, originatingTime);

                if (e.Result.Semantics != null)
                {
                    IntentData intents = this.BuildIntentData(e.Result.Semantics);
                    this.PostWithOriginatingTimeConsistencyCheck(this.intentData, intents, originatingTime);
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
            this.PostWithOriginatingTimeConsistencyCheck(this.speechHypothesized, e, originatingTime);

            var result = this.BuildPartialSpeechRecognitionResult(e.Result);
            this.PostWithOriginatingTimeConsistencyCheck(this.Out, result, originatingTime);
        }

        /// <summary>
        /// Called when speech is detected.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.speechDetected, e, originatingTime);
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
            this.PostWithOriginatingTimeConsistencyCheck(this.speechRecognitionRejected, e, originatingTime);

            if (e.Result.Alternates.Count > 0)
            {
                var result = this.BuildSpeechRecognitionResult(e.Result);
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, result, originatingTime);
            }
        }

        /// <summary>
        /// Requested by `SetGrammars()` - now ready to update
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args (`UserToken` expected to contain grammars)</param>
        private void OnRecognizerUpdateReached(object sender, RecognizerUpdateReachedEventArgs e)
        {
            Console.WriteLine("Updating grammars");
            var grammars = e.UserToken as IEnumerable<Grammar>;
            if (grammars != null)
            {
                this.speechRecognitionEngine.UnloadAllGrammars();
                foreach (var g in grammars)
                {
                    this.speechRecognitionEngine.LoadGrammar(g);
                }
            }

            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.recognizerUpdateReached, e, originatingTime);
        }

        private void OnAudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.audioSignalProblemOccurred, e, originatingTime);
        }

        private void OnAudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.audioStateChanged, e, originatingTime);
        }

        private void OnAudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.audioLevelUpdated, e, originatingTime);
        }

        private void OnEmulateRecognizeCompleted(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.emulateRecognizeCompleted, e, originatingTime);
        }

        private void OnLoadGrammarCompleted(object sender, LoadGrammarCompletedEventArgs e)
        {
            DateTime originatingTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.loadGrammarCompleted, e, originatingTime);
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
            // Get the last posted originating time on this stream
            DateTime lastPostedOriginatingTime;
            this.lastPostedOriginatingTimes.TryGetValue(stream, out lastPostedOriginatingTime);

            // Enforce monotonically increasing originating time
            if (originatingTime < lastPostedOriginatingTime)
            {
                originatingTime = lastPostedOriginatingTime;
            }

            // Post the message and update the originating time for this stream
            stream.Post(data, originatingTime);
            this.lastPostedOriginatingTimes[stream] = originatingTime;
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

        /// <summary>
        /// Method to construct the IntentData (intents and entities) from
        /// a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>An IntentData object containing the intents and entities.</returns>
        private IntentData BuildIntentData(SemanticValue semanticValue)
        {
            List<Intent> intentList = new List<Intent>();

            if (semanticValue.Value != null)
            {
                intentList.Add(new Intent()
                {
                    Value = semanticValue.Value.ToString(),
                    Score = semanticValue.Confidence
                });
            }

            // Consider top-level semantics to be intents.
            foreach (var entry in semanticValue)
            {
                intentList.Add(new Intent()
                {
                    Value = entry.Key,
                    Score = entry.Value.Confidence
                });
            }

            List<Entity> entityList = this.ExtractEntities(semanticValue);

            return new IntentData()
            {
                Intents = intentList.ToArray(),
                Entities = entityList.ToArray()
            };
        }

        /// <summary>
        /// Method to extract all entities contained within a SemanticValue.
        /// </summary>
        /// <param name="semanticValue">The SemanticValue object.</param>
        /// <returns>The list of extracted entities.</returns>
        private List<Entity> ExtractEntities(SemanticValue semanticValue)
        {
            List<Entity> entityList = new List<Entity>();
            foreach (var entry in semanticValue)
            {
                // We currently only consider leaf nodes (whose underlying
                // value is of type string) as entities.
                if (entry.Value.Value is string)
                {
                    // Extract the entity's type (key), value and confidence score.
                    entityList.Add(new Entity()
                    {
                        Type = entry.Key,
                        Value = (string)entry.Value.Value,
                        Score = entry.Value.Confidence
                    });
                }
                else
                {
                    // Keep looking for leaf nodes.
                    entityList.AddRange(this.ExtractEntities(entry.Value));
                }
            }

            return entityList;
        }
    }
}
