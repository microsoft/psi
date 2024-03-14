// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.Drawing;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.MediaCapture;
    using Microsoft.Psi.MixedReality.WinRT;
    using StereoKit;
    using Windows.Media.Capture;
    using Hand = Microsoft.Psi.MixedReality.OpenXR.Hand;

    /// <summary>
    /// Represents the configuration for the <see cref="SigmaApp"/>.
    /// </summary>
    public abstract class SigmaAppConfiguration : ClientAppConfiguration
    {
        /// <summary>
        /// Gets or sets the microphone configuration.
        /// </summary>
        public MicrophoneConfiguration MicrophoneConfiguration { get; set; } = new () { MediaCategory = MediaCategory.Speech };

        /// <summary>
        /// Gets or sets the video frame rate to be used by the client app.
        /// </summary>
        public int VideoFrameRate { get; set; } = 15;

        /// <summary>
        /// Gets or sets the video resolution to be used by the client app.
        /// </summary>
        public Rectangle VideoResolution { get; set; } = new (0, 0, 896, 504);

        /// <summary>
        /// Gets or sets a value indicating whether to output the preview stream.
        /// </summary>
        public bool OutputPreviewStream { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use triggered video streams.
        /// </summary>
        public bool UseTriggeredVideoStream { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use triggered preview streams.
        /// </summary>
        public bool UseTriggeredPreviewStream { get; set; } = true;

        /// <summary>
        /// Gets or sets the format to resample audio output streams to, or null to not resample.
        /// </summary>
        public WaveFormat AudioResampleFormat { get; set; } = WaveFormat.Create16kHz1Channel16BitPcm();

        /// <summary>
        /// Gets or sets the size in milliseconds for each output <see cref="AudioBuffer"/>.
        /// Audio buffers will be reframed to this size prior to being sent to the server.
        /// Set this to zero to retain the original audio buffer size and to not reframe.
        /// </summary>
        public int AudioReframeSizeMs { get; set; } = 100;

        /// <summary>
        /// Creates a Sigma user interface.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the components to.</param>
        /// <param name="availableModels">The list of available models.</param>
        /// <returns>The Sigma user interface.</returns>
        public abstract ISigmaUserInterface CreateSigmaUserInterface(Pipeline pipeline, Dictionary<string, Model> availableModels);

        /// <summary>
        /// Gets the user interface streams.
        /// </summary>
        /// <param name="sigmaUserInterface">The Sigma user interface.</param>
        /// <param name="gazeSensor">The gaze sensor stream.</param>
        /// <param name="handsSensor">The ahnds sensor stream.</param>
        /// <param name="systemAudio">The system audio stream.</param>
        /// <param name="speechSynthesisProgress">The speech synthesis progress stream.</param>
        /// <returns>The collection of user interface streams.</returns>
        public abstract IClientServerCommunicationStreams GetUserInterfaceStreams(
            ISigmaUserInterface sigmaUserInterface,
            IProducer<(Eyes, CoordinateSystem)> gazeSensor,
            IProducer<(Hand, Hand)> handsSensor,
            IProducer<AudioBuffer> systemAudio,
            IProducer<SpeechSynthesisProgress> speechSynthesisProgress);

        /// <summary>
        /// Gets and connects the output streams.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the components to.</param>
        /// <param name="sigmaUserInterface">The Sigma user interface.</param>
        /// <param name="computeServerRendezvousProcess">The compute server rendezvous process.</param>
        /// <returns>The output streams.</returns>
        public abstract IProducer<Heartbeat> GetAndConnectOutputStreams(Pipeline pipeline, ISigmaUserInterface sigmaUserInterface, Rendezvous.Process computeServerRendezvousProcess);
    }
}
