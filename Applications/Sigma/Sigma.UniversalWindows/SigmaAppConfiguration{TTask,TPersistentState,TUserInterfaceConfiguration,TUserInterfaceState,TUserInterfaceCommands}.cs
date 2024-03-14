// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Xml.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.WinRT;
    using Hand = Microsoft.Psi.MixedReality.OpenXR.Hand;

    /// <summary>
    /// Represents the configuration for the <see cref="SigmaApp"/>.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TUserInterfaceConfiguration">The type of the user interface configuration.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public abstract class SigmaAppConfiguration<TTask, TPersistentState, TUserInterfaceConfiguration, TUserInterfaceState, TUserInterfaceCommands> : SigmaAppConfiguration
        where TTask : Task, IInteropSerializable, new()
        where TPersistentState : SigmaPersistentState<TTask>, new()
        where TUserInterfaceConfiguration : SigmaUserInterfaceConfiguration, new()
        where TUserInterfaceState : SigmaUserInterfaceState, new()
        where TUserInterfaceCommands : SigmaUserInterfaceCommands, new()
    {
        /// <summary>
        /// Gets or sets the user interface configuration.
        /// </summary>
        [XmlIgnore]
        public TUserInterfaceConfiguration UserInterfaceConfiguration { get; set; } = new TUserInterfaceConfiguration();

        /// <inheritdoc/>
        public override IClientServerCommunicationStreams GetUserInterfaceStreams(
            ISigmaUserInterface sigmaUserInterface,
            IProducer<(Eyes, CoordinateSystem)> gazeSensor,
            IProducer<(Hand, Hand)> handsSensor,
            IProducer<AudioBuffer> systemAudio,
            IProducer<SpeechSynthesisProgress> speechSynthesisProgress)
            => new UserInterfaceStreams<TUserInterfaceState>(
                (sigmaUserInterface as SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands>).Out,
                gazeSensor.Out,
                handsSensor.Out,
                systemAudio,
                speechSynthesisProgress,
                (sigmaUserInterface as SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands>).DebugInfo);

        /// <inheritdoc/>
        public override IProducer<Heartbeat> GetAndConnectOutputStreams(Pipeline pipeline, ISigmaUserInterface sigmaUserInterface, Rendezvous.Process computeServerRendezvousProcess)
        {
            // Get the output streams from the rendezvous server
            var sigmaOutputStreams = new OutputStreams<TPersistentState, TUserInterfaceCommands>(pipeline, computeServerRendezvousProcess);

            // Finalize the rendering parts of the pipeline construction
            // by connecting the output streams to the user interface pipeline
            sigmaOutputStreams.PersistentState?.PipeTo((sigmaUserInterface as SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands>).PersistentStateInput, DeliveryPolicy.Unlimited);
            sigmaOutputStreams.UserInterfaceCommands?.PipeTo((sigmaUserInterface as SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands>).UserInterfaceCommandsInput, DeliveryPolicy.LatestMessage);
            return sigmaOutputStreams.Heartbeat;
        }
    }
}
