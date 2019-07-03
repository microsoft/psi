// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MicrosoftSpeech
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Language;
    using Microsoft.Psi.Speech;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.Recognition.SrgsGrammar;

    /// <summary>
    /// Component that performs speech recognition using the Microsoft Speech Platform SDK.
    /// </summary>
    /// <remarks>
    /// Separate download and installation of the Microsoft Speech Platform runtime and language pack are required in order to use this component.
    /// - Click <a href="http://go.microsoft.com/fwlink/?LinkID=223568">here</a> to download the Microsoft Speech Platform runtime.
    /// - Click <a href="http://go.microsoft.com/fwlink/?LinkID=223569">here</a> to download the Microsoft Speech Platform language pack.
    ///
    /// The originating times of speech recognition events emitted by this component are estimates. These are estimated in a couple of ways
    /// from the results that the underlying speech recognition engine returns. In the case of a final recognition result, we use the audio
    /// position offset of the recognized audio as reported by the recognition engine to compute an estimate of the originating time. For
    /// partial hypotheses, we use the engine's current offset into the audio stream to estimate the originating time.
    /// </remarks>
    public sealed class MicrosoftSpeechRecognizer : ConsumerProducer<AudioBuffer, IStreamingSpeechRecognitionResult>, ISourceComponent, IDisposable
    {
        /// <summary>
        /// Stream for buffering audio samples to send to the speech recognition engine.
        /// </summary>
        private readonly BufferedAudioStream inputAudioStream;

        /// <summary>
        /// The last originating time that was recorded for each output stream.
        /// </summary>
        private readonly Dictionary<IEmitter, DateTime> lastPostedOriginatingTimes;

        /// <summary>
        /// The Microsoft.Speech speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// The implied stream start time.
        /// </summary>
        private DateTime streamStartTime;

        /// <summary>
        /// Event to signal that the recognizer has been stopped.
        /// </summary>
        private ManualResetEvent recognizeCompleteManualReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public MicrosoftSpeechRecognizer(Pipeline pipeline, MicrosoftSpeechRecognizerConfiguration configuration)
            : base(pipeline)
        {
            this.Configuration = configuration;

            // create receiver of grammar updates
            this.ReceiveGrammars = pipeline.CreateReceiver<IEnumerable<string>>(this, this.SetGrammars, nameof(this.ReceiveGrammars), true);

            // create receiver of grammar updates by name
            this.ReceiveGrammarNames = pipeline.CreateReceiver<string[]>(this, this.EnableGrammars, nameof(this.ReceiveGrammarNames), true);

            // create the additional output streams
            this.PartialRecognitionResults = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.PartialRecognitionResults));
            this.IntentData = pipeline.CreateEmitter<IntentData>(this, nameof(this.IntentData));

            // create output streams for all the event args
            this.SpeechDetected = pipeline.CreateEmitter<SpeechDetectedEventArgs>(this, nameof(SpeechDetectedEventArgs));
            this.SpeechHypothesized = pipeline.CreateEmitter<SpeechHypothesizedEventArgs>(this, nameof(SpeechHypothesizedEventArgs));
            this.SpeechRecognized = pipeline.CreateEmitter<SpeechRecognizedEventArgs>(this, nameof(SpeechRecognizedEventArgs));
            this.SpeechRecognitionRejected = pipeline.CreateEmitter<SpeechRecognitionRejectedEventArgs>(this, nameof(SpeechRecognitionRejectedEventArgs));
            this.AudioSignalProblemOccurred = pipeline.CreateEmitter<AudioSignalProblemOccurredEventArgs>(this, nameof(AudioSignalProblemOccurredEventArgs));
            this.AudioStateChanged = pipeline.CreateEmitter<AudioStateChangedEventArgs>(this, nameof(AudioStateChangedEventArgs));
            this.RecognizeCompleted = pipeline.CreateEmitter<RecognizeCompletedEventArgs>(this, nameof(RecognizeCompletedEventArgs));
            this.AudioLevelUpdated = pipeline.CreateEmitter<AudioLevelUpdatedEventArgs>(this, nameof(AudioLevelUpdatedEventArgs));
            this.EmulateRecognizeCompleted = pipeline.CreateEmitter<EmulateRecognizeCompletedEventArgs>(this, nameof(EmulateRecognizeCompletedEventArgs));
            this.LoadGrammarCompleted = pipeline.CreateEmitter<LoadGrammarCompletedEventArgs>(this, nameof(LoadGrammarCompletedEventArgs));
            this.RecognizerUpdateReached = pipeline.CreateEmitter<RecognizerUpdateReachedEventArgs>(this, nameof(RecognizerUpdateReachedEventArgs));

            // create table of last stream originating times
            this.lastPostedOriginatingTimes = new Dictionary<IEmitter, DateTime>();

            // Create a BufferedAudioStream with an internal buffer large enough
            // to accommodate the specified number of milliseconds of audio data.
            this.inputAudioStream = new BufferedAudioStream(
                this.Configuration.InputFormat.AvgBytesPerSec * this.Configuration.BufferLengthInMs / 1000);

            this.recognizeCompleteManualReset = new ManualResetEvent(false);

            // create the recognition engine
            this.speechRecognitionEngine = this.CreateSpeechRecognitionEngine();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The Psi pipeline.</param>
        /// <param name="configurationFilename">The name of the configuration file.</param>
        public MicrosoftSpeechRecognizer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new MicrosoftSpeechRecognizerConfiguration() : new ConfigurationHelper<MicrosoftSpeechRecognizerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the receiver for new grammars.
        /// </summary>
        public Receiver<IEnumerable<string>> ReceiveGrammars { get; }

        /// <summary>
        /// Gets the receiver for new grammars (by name).
        /// </summary>
        public Receiver<string[]> ReceiveGrammarNames { get; }

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
        private MicrosoftSpeechRecognizerConfiguration Configuration { get; }

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
        /// Enable all the grammars indicated by name, disabling all others.
        /// </summary>
        /// <param name="grammarNames">Speech grammars.</param>
        public void EnableGrammars(Message<string[]> grammarNames)
        {
            foreach (var g in this.speechRecognitionEngine.Grammars)
            {
                g.Enabled = grammarNames.Data.Contains(g.Name) ? true : false;
            }
        }

        /// <summary>
        /// Disable all of the grammars loaded on this recognition engine.
        /// </summary>
        public void DisableAllGrammars()
        {
            foreach (var g in this.speechRecognitionEngine.Grammars)
            {
                g.Enabled = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.speechRecognitionEngine != null)
            {
                // NOTE: The following Dispose() method will block execution of disposing the SpeechRecognitionEngine object until
                // either all pending event handlers have returned or until 30 seconds have elapsed, whichever occurs first.
                // Be aware that if you call Dispose() in an event handler, that this will create a circular wait for the full 30 seconds,
                // during which execution of the method will be blocked. Typically, calling Dispose() in an event handler is not advised.
                // (From): https://msdn.microsoft.com/en-us/library/office/dd146960(v=office.14).aspx
                this.speechRecognitionEngine.Dispose();
                this.speechRecognitionEngine = null;
            }

            // Free any other managed objects here.
            this.recognizeCompleteManualReset.Dispose();
            this.recognizeCompleteManualReset = null;
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
            this.recognizeCompleteManualReset.WaitOne(333);

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

            // Compute the implied start time based on the latest originating time,
            // less the total number of bytes written less overruns. This will be
            // the time that the audio stream "thinks" it started, and can thus be
            // used to compute originating time from RecognizedAudio.AudioPosition.
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
            // Create the speech recognition engine
            var recognizer = MicrosoftSpeech.CreateSpeechRecognitionEngine(this.Configuration.Language, this.Configuration.Grammars);

            // Attach the event handlers for speech recognition events
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
        /// Called whenever a partial recognition result is available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            // Convention for intervals is to use the end time as the originating time so we add the duration as well.
            DateTime originatingTime = this.streamStartTime + e.Result.Audio.AudioPosition + e.Result.Audio.Duration;

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechHypothesized, e, originatingTime);

            var result = this.BuildPartialSpeechRecognitionResult(e.Result);
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialRecognitionResults, result, originatingTime);
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
                    var intents = MicrosoftSpeech.BuildIntentData(e.Result.Semantics);
                    this.PostWithOriginatingTimeConsistencyCheck(this.IntentData, intents, originatingTime);
                }
            }
        }

        /// <summary>
        /// Called when the engine finalizes the recognition operation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            this.recognizeCompleteManualReset.Set();

            DateTime originatingTime = this.streamStartTime + e.AudioPosition;
            this.PostWithOriginatingTimeConsistencyCheck(this.RecognizeCompleted, e, originatingTime);
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
            this.lastPostedOriginatingTimes.TryGetValue(stream, out DateTime lastPostedOriginatingTime);

            // Enforce monotonically increasing originating time
            if (originatingTime <= lastPostedOriginatingTime)
            {
                originatingTime = lastPostedOriginatingTime.AddTicks(1);
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
    }
}
