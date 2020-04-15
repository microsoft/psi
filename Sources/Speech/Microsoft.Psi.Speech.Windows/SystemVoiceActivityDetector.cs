// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Speech.AudioFormat;
    using System.Speech.Recognition;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that performs voice activity detection by using the desktop speech recognition engine from `System.Speech`.
    /// </summary>
    /// <remarks>
    /// This component monitors an input audio stream and outputs a boolean flag for each input message indicating
    /// whether or not voice activity was present in the corresponding <see cref="AudioBuffer"/>. It relies on the
    /// System.Speech.Recognition.SpeechRecognitionEngine in .NET and uses its AudioStateChanged event to estimate
    /// when speech activity begins and ends. It estimates the originating times of these events using the current
    /// audio position of the underlying speech recognition engine to obtain an estimate of the time the event occurred.
    /// These results may be further fine-tuned to potentially obtain better estimates with the VoiceActivityStartOffsetMs
    /// and VoiceActivityEndOffsetMs configuration parameters, which are added to the inferred times.
    /// </remarks>
    public sealed class SystemVoiceActivityDetector : ConsumerProducer<AudioBuffer, bool>, IDisposable
    {
        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly SystemVoiceActivityDetectorConfiguration configuration;

        /// <summary>
        /// Stream for buffering audio samples to send to the speech recognition engine.
        /// </summary>
        private readonly BufferedAudioStream inputAudioStream;

        /// <summary>
        /// Queue of input message originating times.
        /// </summary>
        private readonly Queue<DateTime> messageOriginatingTimes;

        /// <summary>
        /// The System.Speech speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// The implied stream start time.
        /// </summary>
        private DateTime streamStartTime;

        /// <summary>
        /// The most recent speech detected state.
        /// </summary>
        private bool lastSpeechDetectedState;

        /// <summary>
        /// The most recent time speech was detected.
        /// </summary>
        private DateTime lastSpeechDetectedTime;

        /// <summary>
        /// The most recent time end-of-speech was detected.
        /// </summary>
        private DateTime lastSilenceDetectedTime;

        /// <summary>
        /// Event to signal that the recognizer has been stopped.
        /// </summary>
        private ManualResetEvent recognizeComplete;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemVoiceActivityDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public SystemVoiceActivityDetector(Pipeline pipeline, SystemVoiceActivityDetectorConfiguration configuration)
            : base(pipeline)
        {
            this.configuration = configuration ?? new SystemVoiceActivityDetectorConfiguration();

            // Create a BufferedAudioStream with an internal buffer large enough
            // to accommodate the specified number of milliseconds of audio data.
            this.inputAudioStream = new BufferedAudioStream(
                this.Configuration.InputFormat.AvgBytesPerSec * this.Configuration.BufferLengthInMs / 1000);

            this.messageOriginatingTimes = new Queue<DateTime>();
            this.recognizeComplete = new ManualResetEvent(false);

            // create the recognition engine
            this.speechRecognitionEngine = this.CreateSpeechRecognitionEngine();

            // start the speech recognition engine
            this.speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemVoiceActivityDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public SystemVoiceActivityDetector(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new SystemVoiceActivityDetectorConfiguration() : new ConfigurationHelper<SystemVoiceActivityDetectorConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private SystemVoiceActivityDetectorConfiguration Configuration => this.configuration;

        /// <summary>
        /// Disposes of managed resources.
        /// </summary>
        public void Dispose()
        {
            if (this.speechRecognitionEngine != null)
            {
                // Unregister handlers so they won't fire while disposing.
                this.speechRecognitionEngine.AudioStateChanged -= this.OnAudioStateChanged;
                this.speechRecognitionEngine.RecognizeCompleted -= this.OnRecognizeCompleted;

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

            this.recognizeComplete.Dispose();
            this.recognizeComplete = null;
        }

        /// <summary>
        /// Receiver for audio data.
        /// </summary>
        /// <param name="audioBuffer">A buffer containing the next chunk of audio data.</param>
        /// <param name="e">The message envelope for the audio data.</param>
        protected override void Receive(AudioBuffer audioBuffer, Envelope e)
        {
            // Feed the data to the speech detector input stream.
            this.inputAudioStream.Write(audioBuffer.Data, 0, audioBuffer.Length);

            // Compute the implied start time based on the latest originating time,
            // less the total number of bytes written less overruns. This will be
            // the time that the audio stream "thinks" it started, and can thus be
            // used to compute originating time from RecognizedAudio.AudioPosition.
            this.streamStartTime = e.OriginatingTime -
                TimeSpan.FromSeconds((double)(this.inputAudioStream.BytesWritten - this.inputAudioStream.BytesOverrun) /
                    this.Configuration.InputFormat.AvgBytesPerSec);

            // The latest originating time for which we can post output messages
            // with the current VAD state. If the current state is false (no voice
            // activity), then we can post messages up to the current recognizer
            // audio position plus the voice activity start offset, since this is
            // the earliest possible VAD state transition time. Similarly, when
            // the current state is true, the earliest time that a state transition
            // to silence can occur would be the recognizer position plus the
            // configured end offset.
            DateTime earliestStateTransitionTime = this.streamStartTime +
                this.speechRecognitionEngine.RecognizerAudioPosition +
                (this.lastSpeechDetectedState ?
                    TimeSpan.FromMilliseconds(this.Configuration.VoiceActivityEndOffsetMs) :
                    TimeSpan.FromMilliseconds(this.Configuration.VoiceActivityStartOffsetMs));

            // Save the current message originating time
            this.messageOriginatingTimes.Enqueue(e.OriginatingTime);

            // Post output messages with the current VAD state for each originating
            // time less than or equal to the next possible state transition time.
            while ((this.messageOriginatingTimes.Count > 0) &&
                (this.messageOriginatingTimes.Peek() <= earliestStateTransitionTime))
            {
                this.Out.Post(this.lastSpeechDetectedState, this.messageOriginatingTimes.Dequeue());
            }
        }

        /// <summary>
        /// Creates a new speech recognition engine.
        /// </summary>
        /// <returns>A new speech recognition engine object.</returns>
        private SpeechRecognitionEngine CreateSpeechRecognitionEngine()
        {
            // Create speech recognition engine
            var recognizer = SystemSpeech.CreateSpeechRecognitionEngine(this.Configuration.Language, this.Configuration.Grammars);

            // Attach event handlers for speech recognition events
            recognizer.AudioStateChanged += this.OnAudioStateChanged;
            recognizer.RecognizeCompleted += this.OnRecognizeCompleted;

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

            // Set the speech recognition engine parameters
            recognizer.InitialSilenceTimeout = TimeSpan.FromMilliseconds(this.Configuration.InitialSilenceTimeoutMs);
            recognizer.BabbleTimeout = TimeSpan.FromMilliseconds(this.Configuration.BabbleTimeoutMs);
            recognizer.EndSilenceTimeout = TimeSpan.FromMilliseconds(this.Configuration.EndSilenceTimeoutMs);
            recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(this.Configuration.EndSilenceTimeoutAmbiguousMs);

            return recognizer;
        }

        /// <summary>
        /// Called when the audio state of the recognizer changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnAudioStateChanged(object sender, AudioStateChangedEventArgs e)
        {
            // Don't start processing until stream start time has been synchronized with the first message.
            if (this.streamStartTime == DateTime.MinValue)
            {
                return;
            }

            switch (e.AudioState)
            {
                case AudioState.Speech:
                    this.lastSpeechDetectedTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition +
                        TimeSpan.FromMilliseconds(this.Configuration.VoiceActivityStartOffsetMs);

                    if (this.lastSpeechDetectedTime < this.lastSilenceDetectedTime)
                    {
                        // speech start time must not be before last speech ended time
                        this.lastSpeechDetectedTime = this.lastSilenceDetectedTime;
                    }

                    this.lastSpeechDetectedState = true;
                    break;

                case AudioState.Silence:
                case AudioState.Stopped:
                    this.lastSilenceDetectedTime = this.streamStartTime + this.speechRecognitionEngine.RecognizerAudioPosition +
                        TimeSpan.FromMilliseconds(this.Configuration.VoiceActivityEndOffsetMs);

                    if (this.lastSilenceDetectedTime < this.lastSpeechDetectedTime)
                    {
                        // speech end time must not be before last speech started time
                        this.lastSilenceDetectedTime = this.lastSpeechDetectedTime;
                    }

                    this.lastSpeechDetectedState = false;
                    break;
            }
        }

        /// <summary>
        /// Called when the engine finalizes the recognition operation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains the event data.</param>
        private void OnRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            this.recognizeComplete.Set();
        }
    }
}
