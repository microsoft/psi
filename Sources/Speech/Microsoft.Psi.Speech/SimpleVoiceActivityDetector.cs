// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that performs voice activity detection via a simple heuristic using the energy in the audio stream.
    /// </summary>
    /// <remarks>
    /// This component monitors an input audio stream and outputs a boolean flag for each input message indicating
    /// whether or not voice activity was present in the corresponding <see cref="AudioBuffer"/>.
    /// </remarks>
    public sealed class SimpleVoiceActivityDetector : IConsumerProducer<AudioBuffer, bool>
    {
        private readonly SimpleVoiceActivityDetectorConfiguration configuration;
        private readonly Connector<AudioBuffer> audioInputConnector;
        private readonly AcousticFeaturesExtractor acousticFeaturesExtractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleVoiceActivityDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public SimpleVoiceActivityDetector(Pipeline pipeline, SimpleVoiceActivityDetectorConfiguration configuration = null)
        {
            this.configuration = configuration ?? new SimpleVoiceActivityDetectorConfiguration();

            // The input audio - must be 16kHz 1-channel PCM
            this.audioInputConnector = pipeline.CreateConnector<AudioBuffer>(nameof(this.audioInputConnector));

            this.Out = pipeline.CreateEmitter<bool>(this, nameof(this.Out));

            // Currently using only the log energy feature for voice activity detection
            var acousticFeaturesExtractorConfiguration = new AcousticFeaturesExtractorConfiguration()
            {
                FrameDurationInSeconds = (float)this.configuration.FrameDuration,
                FrameRateInHz = (float)this.configuration.FrameRate,
                ComputeLogEnergy = true,
            };

            // Pipe the input audio to the audio features extractor component
            this.acousticFeaturesExtractor = new AcousticFeaturesExtractor(pipeline, acousticFeaturesExtractorConfiguration);
            this.audioInputConnector.PipeTo(this.acousticFeaturesExtractor);

            // Use a simple threshold for detection
            var logEnergy = this.acousticFeaturesExtractor.LogEnergy;
            var logEnergyThreshold = logEnergy.Select(e => (e > this.configuration.LogEnergyThreshold) ? 1.0f : 0);

            // We use a sliding window of frames for both detection of voice activity and silence
            int voiceActivityDetectionFrames = (int)Math.Ceiling(this.configuration.VoiceActivityDetectionWindow * this.configuration.FrameRate);
            int silenceDetectionFrames = (int)Math.Ceiling(this.configuration.SilenceDetectionWindow * this.configuration.FrameRate);

            // For front-end voice activity detection, we use a forward-looking Window() operator as this will use the timestamp
            // of the first frame in the window. For detection of silence during voice activity, we want to use the last frame's timestamp.
            var voiceActivityDetected = logEnergyThreshold.Window(0, voiceActivityDetectionFrames - 1).Average();
            var silenceDetected = logEnergyThreshold.Window(-(silenceDetectionFrames - 1), 0).Average();

            // Use Aggregate opertator to update the state (isSpeaking) based on the current state.
            var vad = voiceActivityDetected.Join(silenceDetected).Aggregate(
                false,
                (isSpeaking, v) => isSpeaking ? v.Item2 != 0 : v.Item1 == 1.0);

            // Sync the output to the timestamps of the original audio frames (since we split the audio into fixed
            // length frames during the computation of the acoustic features).
            this.Out = this.audioInputConnector.Join(vad, TimeSpan.MaxValue).Select(a => a.Item2).Out;
        }

        /// <inheritdoc/>
        public Receiver<AudioBuffer> In => this.audioInputConnector.In;

        /// <inheritdoc/>
        public Emitter<bool> Out { get; }
    }
}
