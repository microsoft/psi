// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.ResearchMode;
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;
    using Windows.Storage;
    using GazeSensor = Microsoft.Psi.MixedReality.WinRT.GazeSensor;
    using HandsSensor = Microsoft.Psi.MixedReality.OpenXR.HandsSensor;

    /// <summary>
    /// Implements the Sigma client application.
    /// </summary>
    public class SigmaApp : StereoKitClientApp<SigmaAppConfiguration>
    {
        private const string DefaultConfigurationFileName = "sigma.client.config.xml";

        private readonly Type[] sigmaAppConfigurationTypes;
        private UserStateConstructor userStateConstructor;
        private ISigmaUserInterface sigmaUserInterface;

        private Dictionary<string, bool> selectedOutputPreviewStream = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaApp"/> class.
        /// </summary>
        /// <param name="sigmaAppConfigurationTypes">The available configuration types.</param>
        public SigmaApp(Type[] sigmaAppConfigurationTypes)
            : base ("Sigma", DefaultConfigurationFileName)
        {
            this.sigmaAppConfigurationTypes = sigmaAppConfigurationTypes;
        }

        /// <inheritdoc/>
        public override List<SigmaAppConfiguration> GetDefaultConfigurations()
            => this.sigmaAppConfigurationTypes.Select(t => Activator.CreateInstance(t) as SigmaAppConfiguration).ToList();

        /// <inheritdoc/>
        public override Type[] GetExtraTypes() => this.sigmaAppConfigurationTypes;

        /// <inheritdoc/>
        public override void PopulateConfigurationDefaults()
        {
            base.PopulateConfigurationDefaults();
            this.selectedOutputPreviewStream = this.AvailableConfigurations.ToDictionary(config => config.Name, config => config.OutputPreviewStream);
        }

        /// <inheritdoc/>
        public override void OnWaitingForStart()
        {
            base.OnWaitingForStart();

            UI.Label("Output preview stream:");
            UI.SameLine();
            if (UI.Radio("Yes", this.selectedOutputPreviewStream[this.SelectedConfiguration.Name]))
            {
                this.selectedOutputPreviewStream[this.SelectedConfiguration.Name] = true;
            }

            UI.SameLine();
            if (UI.Radio("No", !this.selectedOutputPreviewStream[this.SelectedConfiguration.Name]))
            {
                this.selectedOutputPreviewStream[this.SelectedConfiguration.Name] = false;
            }
        }

        /// <inheritdoc/>
        public override HoloLensStreams GetHoloLensStreams(Pipeline pipeline, out DepthCamera depthCamera)
            => LiveHoloLensStreams.Create(pipeline, out depthCamera, this.SelectedConfiguration, this.selectedOutputPreviewStream[this.SelectedConfiguration.Name]);

        /// <inheritdoc/>
        public override IClientServerCommunicationStreams CreateUserInterfacePipeline(Pipeline pipeline)
        {
            // Create the hand XR sensor
            var handsSensor = new HandsSensor(pipeline, TimeSpan.FromSeconds(0.05));

            // Create the gaze sensor
            var gazeSensor = new GazeSensor(pipeline);

            // Read the models file if one exists
            var modelsText = this.ReadFileAsync(KnownFolders.DocumentsLibrary, "sigma_models.txt").GetAwaiter().GetResult();
            var models = string.IsNullOrEmpty(modelsText) ?
                [] :
                modelsText
                    .Split(Environment.NewLine)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .ToDictionary(
                        line => line.Split('\t')[0].Trim(),
                        line => ModelImporter.FromFile(line.Split('\t')[1].Trim(), KnownFolders.Objects3D));

            // Create the Sigma user interface component
            this.sigmaUserInterface = this.SelectedConfiguration.CreateSigmaUserInterface(pipeline, models);

            // Pipe eyes, head and hands to interface
            this.userStateConstructor = new UserStateConstructor(pipeline);
            Generators.Repeat(pipeline, 0, TimeSpan.FromSeconds(1 / 20.0)).PipeTo(this.userStateConstructor, DeliveryPolicy.Unlimited);
            this.userStateConstructor.PipeTo(this.sigmaUserInterface.UserStateInput, DeliveryPolicy.Unlimited);
            gazeSensor.PipeTo(this.userStateConstructor.EyesAndHead, DeliveryPolicy.LatestMessage);
            handsSensor.PipeTo(this.userStateConstructor.Hands, DeliveryPolicy.LatestMessage);

            // Construct the speech synthesis components
            var speechSynthesisKey = this.ReadFileAsync(KnownFolders.DocumentsLibrary, "CognitiveServicesSpeechKey.txt").GetAwaiter().GetResult();

            // Setup the speech synthesis
            var config = new SpeechSynthesizerConfiguration()
            {
                SubscriptionKey = speechSynthesisKey,
                Region = "westus",
            };

            // Construct the speech sythesizer
            var speechSynthesizer = new SpeechSynthesizer(pipeline, config);

            // Send in the speech synthesis commands from the user interface
            this.sigmaUserInterface.SpeechSynthesisCommand.PipeTo(speechSynthesizer, DeliveryPolicy.Unlimited);

            // Resample the audio and send it to the spatial sound generator
            var audioResamplerConfiguration = new AudioResamplerConfiguration()
            {
                InputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                OutputFormat = WaveFormat.CreateIeeeFloat(48000, 1),
            };

            var spatialSound = new SpatialSound(pipeline, Point3D.Origin);
            var systemAudio = speechSynthesizer
                .Resample(audioResamplerConfiguration, DeliveryPolicy.Unlimited);
            systemAudio
                .PipeTo(spatialSound, DeliveryPolicy.Unlimited);
            this.sigmaUserInterface.Position.PipeTo(spatialSound.PositionInput, DeliveryPolicy.Unlimited);

            // Get the actual system audio output
            systemAudio = spatialSound.Out;

            // Resample the audio if specified in the configuration
            if (this.SelectedConfiguration.AudioResampleFormat != null)
            {
                audioResamplerConfiguration = new AudioResamplerConfiguration()
                {
                    InputFormat = WaveFormat.CreateIeeeFloat(48000, 1),
                    OutputFormat = this.SelectedConfiguration.AudioResampleFormat,
                };

                systemAudio = systemAudio.Resample(audioResamplerConfiguration, DeliveryPolicy.Unlimited);
            }

            // Reframe the audio if specified in the configuration
            if (this.SelectedConfiguration.AudioReframeSizeMs > 0)
            {
                systemAudio = systemAudio.Reframe(
                    TimeSpan.FromMilliseconds(this.SelectedConfiguration.AudioReframeSizeMs),
                    DeliveryPolicy.Unlimited);
            }

            // Create the user interface streams
            return this.SelectedConfiguration.GetUserInterfaceStreams(this.sigmaUserInterface, gazeSensor, handsSensor, systemAudio, speechSynthesizer.SynthesisProgress);
        }

        /// <inheritdoc/>
        public override IProducer<Heartbeat> GetAndConnectOutputStreams(Pipeline pipeline, Rendezvous.Process computeServerRendezvousProcess)
            => this.SelectedConfiguration.GetAndConnectOutputStreams(pipeline, this.sigmaUserInterface, computeServerRendezvousProcess);
    }
}
