// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Task that runs the speech recognition pipeline.
    /// </summary>
    [BatchProcessingTask(
        "Mixed Reality Applications - Run speech recognition pipeline",
        Description = "Re-runs the speech recognition pipeline for a mixed reality app.",
        OutputStoreName = RunSpeechRecognitionPipelineTaskConfiguration.DefaultStoreName,
        OutputPartitionName = RunSpeechRecognitionPipelineTaskConfiguration.DefaultPartitionName)]
    public class SpeechRecognitionPipelineTask : BatchProcessingTask<RunSpeechRecognitionPipelineTaskConfiguration>
    {
        /// <summary>
        /// Runs the speech recognition pipeline task.
        /// </summary>
        /// <param name="pipeline">The pipeline to run the task on.</param>
        /// <param name="audio">The audio stream.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>A stream collection containing the resuling streams.</returns>
        public static StreamCollection Run(
            Pipeline pipeline,
            IProducer<AudioBuffer> audio,
            RunSpeechRecognitionPipelineTaskConfiguration configuration)
        {
            Resources.AudioResamplerConstructor = PlatformResources.GetDefault<Func<Pipeline, IAudioResampler>>();
            Resources.VoiceActivityDetectorConstructor = PlatformResources.GetDefault<Func<Pipeline, IVoiceActivityDetector>>();

            var speechRecognitionPipeline = new SpeechRecognitionPipeline(pipeline, configuration.SpeechRecognitionPipelineConfiguration);
            audio.PipeTo(speechRecognitionPipeline.In, DeliveryPolicy.Unlimited);

            var streamCollection = new StreamCollection();
            speechRecognitionPipeline.RecognitionResults.Write("RecognitionResults", streamCollection);
            speechRecognitionPipeline.PartialRecognitionResults.Write("PartialRecognitionResults", streamCollection);
            speechRecognitionPipeline.VoiceActivity.Write("VoiceActivity", streamCollection);

            // Compute the log energy of the audio
            var acousticFeaturesExtractor = new AcousticFeaturesExtractor(pipeline, new AcousticFeaturesExtractorConfiguration()
            {
                InputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                ComputeLogEnergy = true,
                ComputeZeroCrossingRate = false,
                ComputeFrequencyDomainEnergy = false,
                ComputeLowFrequencyEnergy = false,
                ComputeHighFrequencyEnergy = false,
                ComputeSpectralEntropy = false,
            });

            speechRecognitionPipeline.AudioOutput.PipeTo(acousticFeaturesExtractor.In, DeliveryPolicy.Unlimited);
            acousticFeaturesExtractor.LogEnergy.Write("LogEnergy", streamCollection);

            return streamCollection;
        }

        /// <inheritdoc/>
        public override RunSpeechRecognitionPipelineTaskConfiguration GetDefaultConfiguration() =>
            new ()
            {
                ReplayAllRealTime = true,
                DeliveryPolicySpec = DeliveryPolicySpec.Unlimited,
                OutputStoreName = RunSpeechRecognitionPipelineTaskConfiguration.DefaultStoreName,
                OutputPartitionName = RunSpeechRecognitionPipelineTaskConfiguration.DefaultPartitionName,
            };

        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, RunSpeechRecognitionPipelineTaskConfiguration configuration)
        {
            var audio = sessionImporter.OpenStreamOrDefault<AudioBuffer>(configuration.AudioStreamName);
            Run(pipeline, audio, configuration).Write(nameof(SpeechRecognitionPipelineTask), exporter);
        }
    }

    /// <summary>
    /// Represents the configuration for the <see cref="SpeechRecognitionPipelineTask"/>.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class RunSpeechRecognitionPipelineTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
    {
        /// <summary>
        /// Gets the default store name.
        /// </summary>
        internal const string DefaultStoreName = nameof(SpeechRecognitionPipelineTask);

        /// <summary>
        /// Gets the default partition name.
        /// </summary>
        internal const string DefaultPartitionName = nameof(SpeechRecognitionPipelineTask);

        private string audioStreamName = string.Empty;
        private SpeechRecognitionPipelineConfiguration speechRecognitionPipelineConfiguration = new ();

        /// <summary>
        /// Gets or sets the name of the audio stream.
        /// </summary>
        [DataMember]
        [DisplayName("Audio stream")]
        [Description("The name of the audio stream.")]
        public string AudioStreamName
        {
            get => this.audioStreamName;
            set { this.Set(nameof(this.AudioStreamName), ref this.audioStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the speech recognition pipeline configuration.
        /// </summary>
        [DataMember]
        [DisplayName("Speech Recognition Pipeline Configuration")]
        [Description("The name of the speech recognition pipeline configuration.")]
        public SpeechRecognitionPipelineConfiguration SpeechRecognitionPipelineConfiguration
        {
            get => this.speechRecognitionPipelineConfiguration;
            set { this.Set(nameof(this.SpeechRecognitionPipelineConfiguration), ref this.speechRecognitionPipelineConfiguration, value); }
        }
    }
}
