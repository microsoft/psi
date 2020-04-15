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
    /// Component that performs speech recognition using the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/speech/">Microsoft Cognitive Services Azure Speech API</a>.
    /// </summary>
    /// <remarks>The component takes in a stream of audio and performs speech-to-text recognition. It works in conjunction with the
    /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/speech/">Microsoft Cognitive Services Azure Speech API</a>
    /// and requires a subscription key in order to use. For more information, see the complete documentation for the
    /// <a href="https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/">Microsoft Cognitive Services Azure Speech API</a>.
    /// </remarks>
    public sealed class AzureSpeechRecognizer : AsyncConsumerProducer<ValueTuple<AudioBuffer, bool>, IStreamingSpeechRecognitionResult>, IDisposable
    {
        // For cancelling any pending recognition tasks before disposal
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The client that communicates with the cloud speech recognition service.
        /// </summary>
        private SpeechRecognitionClient speechRecognitionClient;

        // The current recognition task
        private SpeechRecognitionTask currentRecognitionTask;

        // The queue of pending recognition tasks
        private ConcurrentQueue<SpeechRecognitionTask> pendingRecognitionTasks = new ConcurrentQueue<SpeechRecognitionTask>();

        /// <summary>
        /// The time the last audio input contained speech.
        /// </summary>
        private DateTime lastAudioContainingSpeechTime;

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
        private Queue<ValueTuple<AudioBuffer, bool>> currentQueue = new Queue<ValueTuple<AudioBuffer, bool>>();

        /// <summary>
        /// The last conversation error.
        /// </summary>
        private Exception conversationError;

        /// <summary>
        /// Flag to indicate that a fatal error has occurred.
        /// </summary>
        private bool fatalError;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public AzureSpeechRecognizer(Pipeline pipeline, AzureSpeechRecognizerConfiguration configuration)
            : base(pipeline)
        {
            this.Configuration = configuration ?? new AzureSpeechRecognizerConfiguration();
            this.PartialRecognitionResults = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.PartialRecognitionResults));

            // create emitters for all possible Azure speech recognition events
            this.PartialSpeechResponseEvent = pipeline.CreateEmitter<PartialSpeechResponseEventArgs>(this, nameof(PartialSpeechResponseEventArgs));
            this.SpeechErrorEvent = pipeline.CreateEmitter<SpeechErrorEventArgs>(this, nameof(SpeechErrorEventArgs));
            this.SpeechResponseEvent = pipeline.CreateEmitter<SpeechResponseEventArgs>(this, nameof(SpeechResponseEventArgs));

            // create table of last stream originating times
            this.lastPostedOriginatingTimes = new Dictionary<int, DateTime>();

            // Create the Cognitive Services DataRecognitionClient
            this.speechRecognitionClient = this.CreateSpeechRecognitionClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public AzureSpeechRecognizer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new AzureSpeechRecognizerConfiguration() : new ConfigurationHelper<AzureSpeechRecognizerConfiguration>(configurationFilename).Configuration)
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
        private AzureSpeechRecognizerConfiguration Configuration { get; }

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

            if (this.lastAudioOriginatingTime == default)
            {
                this.lastAudioOriginatingTime = e.OriginatingTime - data.Item1.Duration;
            }

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

                bool newSession = false;
                if (!this.lastAudioContainedSpeech)
                {
                    // queue a new recognition task
                    this.currentRecognitionTask = new SpeechRecognitionTask { SpeechStartTime = previousAudioOriginatingTime };
                    this.pendingRecognitionTasks.Enqueue(this.currentRecognitionTask);

                    // create a new session when sending the first audio packet
                    newSession = true;
                }

                // Send the audio data to the cloud
                await this.speechRecognitionClient.SendAudioAsync(audioData, this.cancellationTokenSource.Token, newSession);

                // Add audio to the current utterance queue so we can reconstruct it in the recognition result later
                this.currentQueue.Enqueue(data.DeepClone(this.In.Recycler));
            }

            // If this is the last audio packet containing speech
            if (!hasSpeech && this.lastAudioContainedSpeech)
            {
                // If this is the first audio packet containing no speech, use the time of the previous audio packet
                // as the end of the actual speech, since that is the last packet that contained any speech.
                var lastVADSpeechEndTime = this.lastAudioContainingSpeechTime;

                // update the latest in-progress recognition

                // Allocate a buffer large enough to hold the buffered audio
                BufferWriter bw = new BufferWriter(this.currentQueue.Sum(b => b.Item1.Length));

                // Get the audio associated with the recognized text from the current queue.
                ValueTuple<AudioBuffer, bool> buffer;
                while (this.currentQueue.Count > 0)
                {
                    buffer = this.currentQueue.Dequeue();
                    bw.Write(buffer.Item1.Data);

                    // We are done with this buffer so enqueue it for recycling
                    this.In.Recycle(buffer);
                }

                // Save the buffered audio
                this.currentRecognitionTask.Audio = new AudioBuffer(bw.Buffer, this.Configuration.InputFormat);
                this.currentRecognitionTask.SpeechEndTime = lastVADSpeechEndTime;

                // Call EndAudio to signal that this is the last packet
                await this.speechRecognitionClient.SendEndAudioAsync(this.cancellationTokenSource.Token);
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
                SpeechRecognitionMode.Conversation, // not currently used by the new Speech Service
                this.Configuration.Language,
                this.Configuration.SubscriptionKey,
                this.Configuration.Region);

            // Event handlers for speech recognition results
            client.OnResponseReceived += this.OnResponseReceivedHandler;
            client.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            client.OnConversationError += this.OnConversationErrorHandler;

            // Set the audio format (16-bit, 1-channel PCM samples). Currently
            // only 16kHz sampling rate is supported.
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
            // get the current (oldest) recognition task from the queue
            if (!this.pendingRecognitionTasks.TryPeek(out var currentRecognitionTask))
            {
                // This proabaly means that we have just received an end-of-dictation response which normally
                // arrives after a successful recognition result, so we would have already completed the
                // recognition task. Hence we just ignore the response.
                return;
            }

            // update the in-progress recognition task
            currentRecognitionTask.AppendResult(e.PhraseResponse);

            if (currentRecognitionTask.IsDoneSpeaking)
            {
                // current recognition task is no longer in progress so finalize and remove it
                currentRecognitionTask.IsFinalized = true;
                this.PostWithOriginatingTimeConsistencyCheck(this.Out, currentRecognitionTask.BuildSpeechRecognitionResult(), currentRecognitionTask.SpeechEndTime);
                this.pendingRecognitionTasks.TryDequeue(out _);
            }

            // Post the raw result from the underlying recognition engine
            var originatingTime = currentRecognitionTask.SpeechStartTime.Add(e.PhraseResponse.Offset).Add(e.PhraseResponse.Duration);
            this.PostWithOriginatingTimeConsistencyCheck(this.SpeechResponseEvent, e, originatingTime);
        }

        /// <summary>
        /// Called when a partial response is received.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            // get the current (oldest) recognition task from the queue
            if (!this.pendingRecognitionTasks.TryPeek(out var currentRecognitionTask))
            {
                // ignore if there is no in-progress task
                return;
            }

            // add the offset and duration to the VAD start time of the utterance
            var originatingTime = currentRecognitionTask.SpeechStartTime.Add(e.PartialResult.Offset).Add(e.PartialResult.Duration);

            if (currentRecognitionTask.IsDoneSpeaking && originatingTime > currentRecognitionTask.SpeechEndTime)
            {
                // ignore if the computed originating time exceeds the VAD end time
                return;
            }

            // update the in-progress recognition task
            currentRecognitionTask.AppendResult(e.PartialResult);

            // Since this is a partial response, VAD may not yet have signalled the end of speech
            // so just use the last audio packet time (which will probably be ahead).
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialRecognitionResults, currentRecognitionTask.BuildPartialSpeechRecognitionResult(), originatingTime);

            // Post the raw result from the underlying recognition engine
            this.PostWithOriginatingTimeConsistencyCheck(this.PartialSpeechResponseEvent, e, originatingTime);
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
                            nameof(AzureSpeechRecognizer) +
                            ": Login failed. Please check that the subscription key and region that you supplied in the " +
                            nameof(AzureSpeechRecognizerConfiguration) +
                            " object are correct and that your subscription is active. For details on obtaining " +
                            "a subscription to the Azure Speech service, please see " +
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
    }
}
