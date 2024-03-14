// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Represents a set of precomputed streams for batch reprocessing.
    /// </summary>
    public class PrecomputedStreams
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrecomputedStreams"/> class.
        /// </summary>
        /// <param name="speechRecognitionResults">The speech recognition results stream.</param>
        /// <param name="voiceActivityDetection">The voice activity detection stream.</param>
        public PrecomputedStreams(
            IProducer<IStreamingSpeechRecognitionResult> speechRecognitionResults,
            IProducer<bool> voiceActivityDetection)
        {
            this.SpeechRecognitionResults = speechRecognitionResults;
            this.VoiceActivityDetection = voiceActivityDetection;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecomputedStreams"/> class.
        /// </summary>
        /// <param name="importer">The importer to read the streams from.</param>
        /// <param name="prefix">The prefix for the streams.</param>
        public PrecomputedStreams(Importer importer, string prefix)
        {
            // Check for backcompat
            if (importer.Contains($"Debug.SpeechRecognitionResults"))
            {
                this.SpeechRecognitionResults = importer.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"Debug.SpeechRecognitionResults");
                this.VoiceActivityDetection = importer.OpenStreamOrDefault<bool>($"Debug.VoiceActivityDetection");
            }
            else if (importer.Contains($"{prefix}.Speech.RecognitionResults"))
            {
                this.SpeechRecognitionResults = importer.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"{prefix}.Speech.RecognitionResults");
                this.VoiceActivityDetection = importer.OpenStreamOrDefault<bool>($"{prefix}.Speech.VoiceActivity");
            }
            else
            {
                this.SpeechRecognitionResults = importer.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"{prefix}.SpeechRecognition.RecognitionResults");
                this.VoiceActivityDetection = importer.OpenStreamOrDefault<bool>($"{prefix}.SpeechRecognition.VoiceActivity");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecomputedStreams"/> class.
        /// </summary>
        /// <param name="sessionImporter">The session importer.</param>
        /// <param name="prefix">The prefix for the streams.</param>
        public PrecomputedStreams(SessionImporter sessionImporter, string prefix)
        {
            // CHECK for backcompat (will eventually remove this)
            if (sessionImporter.Contains($"Debug.SpeechRecognitionResults"))
            {
                this.SpeechRecognitionResults = sessionImporter.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"Debug.SpeechRecognitionResults");
                this.VoiceActivityDetection = sessionImporter.OpenStreamOrDefault<bool>($"Debug.VoiceActivityDetection");
            }
            else if (sessionImporter.Contains($"{prefix}.Speech.RecognitionResults"))
            {
                this.SpeechRecognitionResults = sessionImporter.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"{prefix}.Speech.RecognitionResults");
                this.VoiceActivityDetection = sessionImporter.OpenStreamOrDefault<bool>($"{prefix}.Speech.VoiceActivity");
            }
            else
            {
                this.SpeechRecognitionResults = sessionImporter.OpenStreamOrDefault<IStreamingSpeechRecognitionResult>($"{prefix}.SpeechRecognition.RecognitionResults");
                this.VoiceActivityDetection = sessionImporter.OpenStreamOrDefault<bool>($"{prefix}.SpeechRecognition.VoiceActivity");
            }
        }

        /// <summary>
        /// Gets the speech recognition results stream.
        /// </summary>
        public IProducer<IStreamingSpeechRecognitionResult> SpeechRecognitionResults { get; private set; }

        /// <summary>
        /// Gets the voice activity detection stream.
        /// </summary>
        public IProducer<bool> VoiceActivityDetection { get; private set; }

        /// <summary>
        /// Bridges the streams to the target pipeline.
        /// </summary>
        /// <param name="targetPipeline">The target pipeline.</param>
        /// <returns>The precomputed streams in the target pipeline.</returns>
        public PrecomputedStreams BridgeTo(Pipeline targetPipeline)
            => new (
                this.SpeechRecognitionResults?.BridgeTo(targetPipeline),
                this.VoiceActivityDetection?.BridgeTo(targetPipeline));
    }
}
