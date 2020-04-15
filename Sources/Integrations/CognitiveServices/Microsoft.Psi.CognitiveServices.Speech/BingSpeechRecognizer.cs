// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech.Service;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Component that performs speech recognition using the <a href="https://docs.microsoft.com/en-us/azure/cognitive-services/speech/">Microsoft Cognitive Services Bing Speech API</a>.
    /// </summary>
    /// <remarks>
    /// DEPRECATED - As the Bing Speech service will be retired soon, you can no longer
    /// obtain a new subscription key for this service. If you have previously obtained a subscription
    /// key for the Bing Speech service, then this key should continue to work with this component
    /// until the service is retired. If you do not have an existing subscription
    /// key for the Bing Speech service, please use the <see cref="AzureSpeechRecognizer"/> component
    /// instead. You may obtain a subscription key for the Azure Speech service here:
    /// https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-services.
    ///
    /// This component takes in a stream of audio and performs speech-to-text recognition. It works in conjunction with the
    /// <a href="https://docs.microsoft.com/en-us/azure/cognitive-services/speech/">Microsoft Cognitive Services Bing Speech API</a>
    /// and requires a subscription key in order to use. For more information, see the complete documentation for the
    /// <a href="https://docs.microsoft.com/en-us/azure/cognitive-services/speech/">Microsoft Cognitive Services Bing Speech API</a>.
    /// </remarks>
    [Obsolete("The Bing Speech service will be retired soon. Please use the AzureSpeechRecognizer instead.", false)]
    public sealed class BingSpeechRecognizer : AsyncConsumerProducer<ValueTuple<AudioBuffer, bool>, IStreamingSpeechRecognitionResult>, ISourceComponent, IDisposable
    {
        // For cancelling any pending recognition tasks before disposal
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The client that communicates with the cloud speech recognition service.
        /// </summary>
        private SpeechRecognitionClient speechRecognitionClient;

        /// <summary>
        /// The last partial recognition result.
        /// </summary>
        private string lastPartialResult;

        /// <summary>
        /// The time the VAD last detected the start of speech.
        /// </summary>
        private DateTime lastVADSpeechStartTime;

        /// <summary>
        /// The time the VAD last detected the end of speech.
        /// </summary>
        private DateTime lastVADSpeechEndTime;

        /// <summary>
        /// The time the last audio input contained speech.
        /// </summary>
        private DateTime lastAudioContainingSpeechTime;

        /// <summary>
        /// The time interval of the last detected speech segment.
        /// </summary>
        private TimeInterval lastVADSpeechTimeInterval = new TimeInterval(DateTime.Now, DateTime.Now);

        /// <summary>
        /// The originating time of the most recently received audio packet.
        /// </summary>
        private DateTime lastAudioOriginatingTime;

        /// <summary>
        /// The last originating time that was recorded for each output stream.
        /// </summary>
        private Dictionary<int, DateTime> lastPostedOriginatingTimes;

        /// <summary>
        /// A flag indicating whether the last audio packet received contained speech.
        /// </summary>
        private bool lastAudioContainedSpeech = false;

        /// <summary>
        /// Queue of current audio buffers for the pending recognition task.
        /// </summary>
        private ConcurrentQueue<ValueTuple<AudioBuffer, bool>> currentQueue = new ConcurrentQueue<ValueTuple<AudioBuffer, bool>>();

        /// <summary>
        /// Last contiguous audio buffer collected pending recognition.
        /// </summary>
        private byte[] lastAudioBuffer;

        /// <summary>
        /// The last conversation error.
        /// </summary>
        private Exception conversationError;

        /// <summary>
        /// Flag to indicate that a fatal error has occurred.
        /// </summary>
        private bool fatalError;

        /// <summary>
        /// Initializes a new instance of the <see cref="BingSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public BingSpeechRecognizer(Pipeline pipeline, BingSpeechRecognizerConfiguration configuration)
            : base(pipeline)
        {
            this.Configuration = configuration ?? new BingSpeechRecognizerConfiguration();
            this.PartialRecognitionResults = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.PartialRecognitionResults));

            // create emitters for all possible Bing speech recognition events
            this.PartialSpeechResponseEvent = pipeline.CreateEmitter<PartialSpeechResponseEventArgs>(this, nameof(PartialSpeechResponseEventArgs));
            this.SpeechErrorEvent = pipeline.CreateEmitter<SpeechErrorEventArgs>(this, nameof(SpeechErrorEventArgs));
            this.SpeechResponseEvent = pipeline.CreateEmitter<SpeechResponseEventArgs>(this, nameof(SpeechResponseEventArgs));

            // create table of last stream originating times
            this.lastPostedOriginatingTimes = new Dictionary<int, DateTime>();

            // Create the Cognitive Services DataRecognitionClient
            this.speechRecognitionClient = this.CreateSpeechRecognitionClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BingSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public BingSpeechRecognizer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new BingSpeechRecognizerConfiguration() : new ConfigurationHelper<BingSpeechRecognizerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the output stream of partial recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> PartialRecognitionResults { get; }

        /// <summary>
        /// Gets the output stream of PartialSpeechResponseEventArgs.
        /// </summary>
        public Emitter<PartialSpeechResponseEventArgs> PartialSpeechResponseEvent { get; }

        /// <summary>
        /// Gets the output stream of SpeechErrorEventArgs.
        /// </summary>
        public Emitter<SpeechErrorEventArgs> SpeechErrorEvent { get; }

        /// <summary>
        /// Gets the output stream of SpeechResponseEventArgs.
        /// </summary>
        public Emitter<SpeechResponseEventArgs> SpeechResponseEvent { get; }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private BingSpeechRecognizerConfiguration Configuration { get; }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();

            if (this.speechRecognitionClient != null)
            {
                // Event handlers for speech recognition results
                this.speechRecognitionClient.OnResponseReceived -= this.OnResponseReceivedHandler;
                this.speechRecognitionClient.OnPartialResponseReceived -= this.OnPartialResponseReceivedHandler;
                this.speechRecognitionClient.OnConversationError -= this.OnConversationErrorHandler;

                this.speechRecognitionClient.Dispose();
                this.speechRecognitionClient = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }

        /// <summary>
        /// Receiver for the combined VAD signal and audio data.
        /// </summary>
        /// <param name="data">A message containing the combined VAD signal and audio data.</param>
        /// <param name="e">The message envelope.</param>
        /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task ReceiveAsync(ValueTuple<AudioBuffer, bool> data, Envelope e)
        {
            byte[] audioData = data.Item1.Data;
            bool hasSpeech = data.Item2;

            var previousAudioOriginatingTime = this.lastAudioOriginatingTime;
            this.lastAudioOriginatingTime = e.OriginatingTime;

            // Throw if a fatal error has occurred in the OnConversationError event handler
            if (this.fatalError)
            {
                if (this.conversationError != null)
                {
                    var error = this.conversationError;
                    this.conversationError = null;
                    throw error;
                }

                // Stop processing until the pipeline terminates
                return;
            }

            if (hasSpeech)
            {
                this.lastAudioContainingSpeechTime = e.OriginatingTime;
            }

            if (hasSpeech || this.lastAudioContainedSpeech)
            {
                // Send the audio data to the cloud
                await this.speechRecognitionClient.SendAudioAsync(audioData, this.cancellationTokenSource.Token);

                // Add audio to the current utterance queue so we can reconstruct it in the recognition result later
                this.currentQueue.Enqueue(data.DeepClone(this.In.Recycler));
            }

            // If this is the last audio packet containing speech
            if (!hasSpeech && this.lastAudioContainedSpeech)
            {
                this.lastVADSpeechEndTime = this.lastAudioContainingSpeechTime;
                this.lastVADSpeechTimeInterval = new TimeInterval(this.lastVADSpeechStartTime, this.lastVADSpeechEndTime);

                // Allocate a buffer large enough to hold the buffered audio
                BufferWriter bw = new BufferWriter(this.currentQueue.Sum(b => b.Item1.Length));

                // Get the audio associated with the recognized text from the current queue.
                ValueTuple<AudioBuffer, bool> buffer;
                while (this.currentQueue.TryDequeue(out buffer))
                {
                    bw.Write(buffer.Item1.Data);

                    // We are done with this buffer so enqueue it for recycling
                    this.In.Recycle(buffer);
                }

                // Save the buffered audio
                this.lastAudioBuffer = bw.Buffer;

                // Call EndAudio to signal that this is the last packet
                await this.speechRecognitionClient.SendEndAudioAsync(this.cancellationTokenSource.Token);
            }
            else if (hasSpeech && !this.lastAudioContainedSpeech)
            {
                // If this is the first audio packet containing speech, mark the time of the previous audio packet
                // as the start of the actual speech
                this.lastVADSpeechStartTime = previousAudioOriginatingTime;

                // Also post a null partial recognition result
                this.lastPartialResult = string.Empty;
                this.PostWithOriginatingTimeConsistencyCheck(this.PartialRecognitionResults, this.BuildPartialSpeechRecognitionResult(this.lastPartialResult), e.OriginatingTime);
            }

            // Remember last audio state.
            this.lastAudioContainedSpeech = hasSpeech;
        }

        private void PostWithOriginatingTimeConsistencyCheck<T>(Emitter<T> stream, T message, DateTime originatingTime)
        {
            // Get the last posted originating time on this stream
            this.lastPostedOriginatingTimes.TryGetValue(stream.Id, out DateTime lastPostedOriginatingTime);

            // Enforce monotonically increasing originating time
            if (originatingTime <= lastPostedOriginatingTime)
            {
                originatingTime = lastPostedOriginatingTime.AddTicks(1);
            }

            stream.Post(message, originatingTime);
            this.lastPostedOriginatingTimes[stream.Id] = originatingTime;
        }

        /// <summary>
        /// Creates a new recognition client.
        /// </summary>
        /// <returns>A new speech recognition client object.</returns>
        private SpeechRecognitionClient CreateSpeechRecognitionClient()
        {
            // Create cloud-based speech recognition client
            var client = new SpeechRecognitionClient(
                this.Configuration.RecognitionMode,
                this.Configuration.Language,
                this.Configuration.SubscriptionKey);

            // Event handlers for speech recognition results
            client.OnResponseReceived += this.OnResponseReceivedHandler;
            client.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            client.OnConversationError += this.OnConversationErrorHandler;

            // Set the audio format (16-bit, 1-channel PCM samples). Currently
            // only 16kHz and 8kHz sampling rates are supported.
            client.SetAudioFormat(this.Configuration.InputFormat);

            return client;
        }

        /// <summary>
        /// Called when a final response is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            // Prefer VAD end time as originating time, but if there is no VAD input
            // then use last audio packet time (which may give over-optimistic results).
            DateTime originatingTime = (this.lastVADSpeechEndTime == DateTime.MinValue) ?
                this.lastAudioOriginatingTime : this.lastVADSpeechEndTime;

            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.Success)
            {
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, this.BuildSpeechRecognitionResult(e.PhraseResponse), originatingTime);
            }
            else if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.InitialSilenceTimeout)
            {
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, this.BuildSpeechRecognitionResult(string.Empty), originatingTime);
            }
            else if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.NoMatch)
            {
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, this.BuildSpeechRecognitionResult(this.lastPartialResult), originatingTime);
            }

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechResponseEvent, e, originatingTime);
        }

        /// <summary>
        /// Called when a partial response is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.lastPartialResult = e.PartialResult.Text;
            var result = this.BuildPartialSpeechRecognitionResult(e.PartialResult.Text);

            // Since this is a partial response, VAD may not yet have signalled the end of speech
            // so just use the last audio packet time (which will probably be ahead).
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialRecognitionResults, result, this.lastAudioOriginatingTime);

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialSpeechResponseEvent, e, this.lastAudioOriginatingTime);
        }

        /// <summary>
        /// Called when an error is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            switch (e.SpeechErrorCode)
            {
                // Authentication failure
                case SpeechClientStatus.HttpForbidden:
                case SpeechClientStatus.HttpUnauthorized:
                    // Create an exception to represent this error
                    this.conversationError = new InvalidOperationException(
                            nameof(BingSpeechRecognizer) +
                            ": Login failed. Please check that the subscription key that you supplied in the " +
                            nameof(BingSpeechRecognizerConfiguration) +
                            " object is valid and that your subscription is active. For details on obtaining " +
                            "a subscription to the Bing Speech API service, please see " +
                            "https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account");

                    // Set a flag to indicate that this is a fatal error and the component should stop processing until the pipeline terminates
                    this.fatalError = true;
                    break;

                // Incorrect message format error - usually as a result of sending up an unsupported audio format
                case SpeechClientStatus.WebSocketInvalidPayloadData:
                    // Audio/message format error
                    this.conversationError = new InvalidOperationException(
                        e.SpeechErrorText +
                        " The speech service rejected the last message that was sent to it. This could be due " +
                        "to an incorrect audio format header, or some other audio encoding or message formatting " +
                        "error. Please check to ensure that the audio being sent is encoded as one-channel 16-bit " +
                        "PCM sampled at 16 kHz.");

                    // Set a flag to indicate that this is a fatal error and the component should stop processing until the pipeline terminates
                    this.fatalError = true;
                    break;

                default:
                    // Other (possibly transient) error
                    this.conversationError = new Exception(e.SpeechErrorText);
                    break;
            }

            // Do not post further errors if a fatal error condition exists - the pipeline may be shutting down
            if (!this.fatalError)
            {
                this.PostWithOriginatingTimeConsistencyCheck(this.SpeechErrorEvent, e, this.lastAudioOriginatingTime);
            }
        }

        /// <summary>
        /// Builds a StreamingSpeechRecognitionResult object from a RecognitionResult returned by the recognizer.
        /// </summary>
        /// <param name="result">The RecognitionResult object.</param>
        /// <returns>A StreamingSpeechRecognitionResult object containing the recognition results.</returns>
        private StreamingSpeechRecognitionResult BuildSpeechRecognitionResult(RecognitionResult result)
        {
            return new StreamingSpeechRecognitionResult(
                true,
                result.Results[0].LexicalForm,
                result.Results[0].Confidence,
                result.Results.Select(p => new SpeechRecognitionAlternate(p.LexicalForm, p.Confidence)),
                new AudioBuffer(this.lastAudioBuffer, this.Configuration.InputFormat),
                this.lastVADSpeechTimeInterval.Span);
        }

        /// <summary>
        /// Builds a StreamingSpeechRecognitionResult object for an empty recognition.
        /// </summary>
        /// <param name="result">The RecognitionResult object.</param>
        /// <param name="confidence">The confidence score (if any).</param>
        /// <returns>A StreamingSpeechRecognitionResult object containing the recognition results.</returns>
        private StreamingSpeechRecognitionResult BuildSpeechRecognitionResult(string result, double? confidence = null)
        {
            return new StreamingSpeechRecognitionResult(
                true,
                result,
                confidence,
                new SpeechRecognitionAlternate[] { new SpeechRecognitionAlternate(result, confidence) },
                new AudioBuffer(this.lastAudioBuffer, this.Configuration.InputFormat),
                this.lastVADSpeechTimeInterval.Span);
        }

        /// <summary>
        /// Builds a partial StreamingSpeechRecognitionResult object from a partial text result returned by the recognizer.
        /// </summary>
        /// <param name="partialResult">The partial result from the recognizer.</param>
        /// <param name="confidence">The confidence score of the result (if any).</param>
        /// <returns>A StreamingSpeechRecognitionResult object containing the partial recognition result.</returns>
        private StreamingSpeechRecognitionResult BuildPartialSpeechRecognitionResult(string partialResult, double? confidence = null)
        {
            return new StreamingSpeechRecognitionResult(
                false,
                partialResult,
                confidence,
                new SpeechRecognitionAlternate[] { new SpeechRecognitionAlternate(partialResult, confidence) });
        }
    }
}
