// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Composite component that implements the speech recognition pipeline.
    /// </summary>
    public class SpeechRecognitionPipeline : Subpipeline, IConsumer<AudioBuffer>
    {
        private readonly Connector<AudioBuffer> inConnector;
        private readonly Connector<AudioBuffer> audioOutputConnector;
        private readonly Connector<bool> voiceActivityConnector;
        private readonly Connector<IStreamingSpeechRecognitionResult> partialRecognitionResultsConnector;
        private readonly Connector<IStreamingSpeechRecognitionResult> recognitionResultsConnector;

        private readonly IProducer<IStreamingSpeechRecognitionResult> speechRecognizerPartialRecognitionResults;
        private readonly IProducer<IStreamingSpeechRecognitionResult> speechRecognizerRecognitionResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionPipeline"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the speech recognition pipeline.</param>
        /// <param name="name">An optional name for the component.</param>
        public SpeechRecognitionPipeline(
            Pipeline pipeline,
            SpeechRecognitionPipelineConfiguration configuration = null,
            string name = nameof(SpeechRecognitionPipeline))
            : base(pipeline, name)
        {
            this.inConnector = this.CreateInputConnectorFrom<AudioBuffer>(pipeline, nameof(this.In));
            this.audioOutputConnector = this.CreateOutputConnectorTo<AudioBuffer>(pipeline, nameof(this.AudioOutput));
            this.voiceActivityConnector = this.CreateOutputConnectorTo<bool>(pipeline, nameof(this.VoiceActivity));
            this.recognitionResultsConnector = this.CreateOutputConnectorTo<IStreamingSpeechRecognitionResult>(pipeline, nameof(this.RecognitionResults));
            this.partialRecognitionResultsConnector = this.CreateOutputConnectorTo<IStreamingSpeechRecognitionResult>(pipeline, nameof(this.PartialRecognitionResults));

            // Resample the audio
            var audioResampler = Resources.AudioResamplerConstructor(this);
            this.inConnector.PipeTo(audioResampler, DeliveryPolicy.Unlimited);
            audioResampler.PipeTo(this.audioOutputConnector, DeliveryPolicy.Unlimited);

            // Create and connect the voice activity detector
            var voiceActivityDetector = Resources.VoiceActivityDetectorConstructor(this);
            audioResampler.PipeTo(voiceActivityDetector, DeliveryPolicy.Unlimited);
            voiceActivityDetector.PipeTo(this.voiceActivityConnector);

            // Construct the Azure recognizer
            var azureSpeechRecognizer = new AzureSpeechRecognizer(
                this,
                new AzureSpeechRecognizerConfiguration()
                {
                    SubscriptionKey = Environment.GetEnvironmentVariable("CognitiveServices.Speech"),
                    Region = Environment.GetEnvironmentVariable("CognitiveServices.SpeechRegion") ?? "westus",
                });
            this.speechRecognizerRecognitionResults = azureSpeechRecognizer.Out;
            this.speechRecognizerPartialRecognitionResults = azureSpeechRecognizer.PartialRecognitionResults;

            // Send the resampled audio with the VAD to the recognizer
            audioResampler
                .Join(voiceActivityDetector, DeliveryPolicy.Unlimited, DeliveryPolicy.Unlimited)
                .PipeTo(azureSpeechRecognizer, DeliveryPolicy.Unlimited);

            // If we are using an utterance continuation detector
            if (configuration.UseContinuationDetector)
            {
                // Construct the continuation detector
                var continuationDetector = new SpeechRecognitionContinuationDetector(this, configuration.ContinuationDetectorConfiguration);
                azureSpeechRecognizer.Out.PipeTo(continuationDetector.SpeechRecognitionResultsInput, DeliveryPolicy.Unlimited);
                voiceActivityDetector.PipeTo(continuationDetector.VoiceActivityDetectorInput);
                azureSpeechRecognizer.PartialRecognitionResults.PipeTo(continuationDetector.PartialSpeechRecognitionResultsInput, DeliveryPolicy.Unlimited);

                // Get the recognition results from the continuation detector
                continuationDetector.SpeechRecognitionResultsOutput.PipeTo(this.recognitionResultsConnector);
                continuationDetector.PartialSpeechRecognitionResultsOutput.PipeTo(this.partialRecognitionResultsConnector);
            }
            else
            {
                azureSpeechRecognizer.PipeTo(this.recognitionResultsConnector);
                azureSpeechRecognizer.PartialRecognitionResults.PipeTo(this.partialRecognitionResultsConnector);
            }
        }

        /// <summary>
        /// Gets the receiver for the audio input.
        /// </summary>
        public Receiver<AudioBuffer> In => this.inConnector.In;

        /// <summary>
        /// Gets the emitter for the audio output.
        /// </summary>
        public Emitter<AudioBuffer> AudioOutput => this.audioOutputConnector.Out;

        /// <summary>
        /// Gets the receiver for the voice activity detection input.
        /// </summary>
        public Emitter<bool> VoiceActivity => this.voiceActivityConnector.Out;

        /// <summary>
        /// Gets the emitter for the recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> RecognitionResults => this.recognitionResultsConnector.Out;

        /// <summary>
        /// Gets the emitter for the partial recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> PartialRecognitionResults => this.partialRecognitionResultsConnector.Out;

        /// <summary>
        /// Write the various streams in the pipeline to an exporter.
        /// </summary>
        /// <param name="prefix">The prefix to use when writing the streams.</param>
        /// <param name="exporter">The exporter to write the streams to.</param>
        public void Write(string prefix, Exporter exporter)
        {
            this.VoiceActivity?.Write($"{prefix}.{nameof(this.VoiceActivity)}", exporter);
            this.speechRecognizerRecognitionResults.Out?.Write($"{prefix}.SpeechRecognizer.RecognitionResults", exporter);
            this.speechRecognizerPartialRecognitionResults.Out?.Write($"{prefix}.SpeechRecognizer.PartialRecognitionResults", exporter);
            this.RecognitionResults?.Write($"{prefix}.{nameof(this.RecognitionResults)}", exporter);
            this.PartialRecognitionResults?.Write($"{prefix}.{nameof(this.PartialRecognitionResults)}", exporter);
        }
    }
}
