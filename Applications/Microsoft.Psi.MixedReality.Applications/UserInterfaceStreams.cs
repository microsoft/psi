// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.OpenXR;
    using Microsoft.Psi.MixedReality.WinRT;
    using HoloLensSerializers = HoloLensCaptureInterop.Serialization;

    /// <summary>
    /// Base abstract class for user interface streams.
    /// </summary>
    /// <typeparam name="TUserInterfaceState">The user interface state type.</typeparam>
    public class UserInterfaceStreams<TUserInterfaceState> : IClientServerCommunicationStreams
        where TUserInterfaceState : IInteropSerializable, new()
    {
        private const int BasePort = 17000;
        private const string Name = "UserInterfaceStreams";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceStreams{TUserInterfaceState}"/> class.
        /// </summary>
        /// <param name="userInterfaceState">The user interface state stream.</param>
        /// <param name="eyesAndHead">The eyes and head stream.</param>
        /// <param name="hands">The hands stream.</param>
        /// <param name="systemAudio">The system audio stream.</param>
        /// <param name="speechSynthesisProgress">The speech synthesis progress stream.</param>
        /// <param name="debugInfo">The debug info stream.</param>
        public UserInterfaceStreams(
            IProducer<TUserInterfaceState> userInterfaceState,
            IProducer<(Eyes, CoordinateSystem)> eyesAndHead,
            IProducer<(Hand, Hand)> hands,
            IProducer<AudioBuffer> systemAudio,
            IProducer<SpeechSynthesisProgress> speechSynthesisProgress,
            IProducer<UserInterfaceDebugInfo> debugInfo)
        {
            this.UserInterfaceState = userInterfaceState;
            this.EyesAndHead = eyesAndHead;
            this.Hands = hands;
            this.SystemAudio = systemAudio;
            this.SpeechSynthesisProgress = speechSynthesisProgress;
            this.DebugInfo = debugInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceStreams{TUserInterfaceState}"/> class.
        /// </summary>
        /// <param name="importer">The importer to read the streams from.</param>
        /// <param name="prefix">The prefix for the stream names.</param>
        public UserInterfaceStreams(Importer importer, string prefix)
        {
            this.UserInterfaceState = importer.OpenStreamOrDefault<TUserInterfaceState>($"{prefix}.{nameof(this.UserInterfaceState)}");
            this.EyesAndHead = importer.OpenStreamOrDefault<(Eyes, CoordinateSystem)>($"{prefix}.{nameof(this.EyesAndHead)}");
            this.Hands = importer.OpenStreamOrDefault<(Hand, Hand)>($"{prefix}.{nameof(this.Hands)}");
            this.SystemAudio = importer.OpenStreamOrDefault<AudioBuffer>($"{prefix}.{nameof(this.SystemAudio)}");
            this.SpeechSynthesisProgress = importer.OpenStreamOrDefault<SpeechSynthesisProgress>($"{prefix}.{nameof(this.SpeechSynthesisProgress)}");
            this.DebugInfo = importer.OpenStreamOrDefault<UserInterfaceDebugInfo>($"{prefix}.{nameof(this.DebugInfo)}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceStreams{TUserInterfaceState}"/> class.
        /// </summary>
        /// <param name="sessionImporter">The session importer to read the streams from.</param>
        /// <param name="prefix">The prefix for the stream names.</param>
        public UserInterfaceStreams(SessionImporter sessionImporter, string prefix)
        {
            this.UserInterfaceState = sessionImporter.OpenStreamOrDefault<TUserInterfaceState>($"{prefix}.{nameof(this.UserInterfaceState)}");
            this.EyesAndHead = sessionImporter.OpenStreamOrDefault<(Eyes, CoordinateSystem)>($"{prefix}.{nameof(this.EyesAndHead)}");
            this.Hands = sessionImporter.OpenStreamOrDefault<(Hand, Hand)>($"{prefix}.{nameof(this.Hands)}");
            this.SystemAudio = sessionImporter.OpenStreamOrDefault<AudioBuffer>($"{prefix}.{nameof(this.SystemAudio)}");
            this.SpeechSynthesisProgress = sessionImporter.OpenStreamOrDefault<SpeechSynthesisProgress>($"{prefix}.{nameof(this.SpeechSynthesisProgress)}");
            this.DebugInfo = sessionImporter.OpenStreamOrDefault<UserInterfaceDebugInfo>($"{prefix}.{nameof(this.DebugInfo)}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceStreams{TUserInterfaceState}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <param name="rendezvousProcess">The rendezvous process to read the streams from.</param>
        /// <param name="prefix">An optional prefix for the stream names.</param>
        public UserInterfaceStreams(Pipeline pipeline, Rendezvous.Process rendezvousProcess, string prefix = null)
        {
            prefix ??= Name;
            foreach (var endpoint in rendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    foreach (var stream in tcpEndpoint.Streams)
                    {
                        if (stream.StreamName == $"{prefix}.{nameof(this.UserInterfaceState)}")
                        {
                            this.UserInterfaceState = tcpEndpoint.ToTcpSource<TUserInterfaceState>(pipeline, InteropSerialization.GetFormat<TUserInterfaceState>(), name: nameof(this.UserInterfaceState));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.EyesAndHead)}")
                        {
                            this.EyesAndHead = tcpEndpoint.ToTcpSource<(Eyes, CoordinateSystem)>(pipeline, Serializers.EyesAndHeadFormat(), name: nameof(this.EyesAndHead));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.Hands)}")
                        {
                            this.Hands = tcpEndpoint.ToTcpSource<(Hand, Hand)>(pipeline, HoloLensSerializers.OpenXRHandsFormat(), name: nameof(this.Hands));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.SystemAudio)}")
                        {
                            this.SystemAudio = tcpEndpoint.ToTcpSource<AudioBuffer>(pipeline, HoloLensSerializers.AudioBufferFormat(), name: nameof(this.SystemAudio));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.SpeechSynthesisProgress)}")
                        {
                            this.SpeechSynthesisProgress = tcpEndpoint.ToTcpSource<SpeechSynthesisProgress>(pipeline, Serializers.SpeechSynthesisProgressFormat(), name: nameof(this.SpeechSynthesisProgress));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.DebugInfo)}")
                        {
                            this.DebugInfo = tcpEndpoint.ToTcpSource<UserInterfaceDebugInfo>(pipeline, UserInterfaceDebugInfo.Format, name: nameof(this.DebugInfo));
                        }
                    }
                }
                else if (endpoint is not Rendezvous.RemoteClockExporterEndpoint)
                {
                    throw new Exception("Unexpected endpoint type.");
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterfaceStreams{TUserInterfaceState}"/> class.
        /// </summary>
        protected UserInterfaceStreams()
        {
        }

        /// <summary>
        /// Gets or sets the user interface state stream.
        /// </summary>
        public IProducer<TUserInterfaceState> UserInterfaceState { get; protected set; }

        /// <summary>
        /// Gets or sets the eyes and head stream.
        /// </summary>
        public IProducer<(Eyes, CoordinateSystem)> EyesAndHead { get; protected set; }

        /// <summary>
        /// Gets or sets the hands stream.
        /// </summary>
        public IProducer<(Hand, Hand)> Hands { get; protected set; }

        /// <summary>
        /// Gets or sets the system audio stream.
        /// </summary>
        public IProducer<AudioBuffer> SystemAudio { get; protected set; }

        /// <summary>
        /// Gets or sets the speech synthesis progress stream.
        /// </summary>
        public IProducer<SpeechSynthesisProgress> SpeechSynthesisProgress { get; protected set; }

        /// <summary>
        /// Gets or sets the debug info stream.
        /// </summary>
        public IProducer<UserInterfaceDebugInfo> DebugInfo { get; protected set; }

        /// <inheritdoc/>
        public virtual void Write(string prefix, Exporter exporter)
        {
            this.UserInterfaceState?.Write($"{prefix}.{nameof(this.UserInterfaceState)}", exporter);
            this.EyesAndHead?.Write($"{prefix}.{nameof(this.EyesAndHead)}", exporter);
            this.EyesAndHead?.Select(us => us.Item2, DeliveryPolicy.SynchronousOrThrottle).Write($"{prefix}.{nameof(this.EyesAndHead)}.Head", exporter);
            this.EyesAndHead?.Select(us => us.Item1, DeliveryPolicy.SynchronousOrThrottle).Write($"{prefix}.{nameof(this.EyesAndHead)}.Eyes", exporter);
            this.Hands?.Write($"{prefix}.{nameof(this.Hands)}", exporter);
            this.Hands?.Select(h => h.Item1, DeliveryPolicy.SynchronousOrThrottle).Write($"{prefix}.{nameof(this.Hands)}.Left", exporter);
            this.Hands?.Select(h => h.Item2, DeliveryPolicy.SynchronousOrThrottle).Write($"{prefix}.{nameof(this.Hands)}.Right", exporter);
            this.SystemAudio?.Write($"{prefix}.{nameof(this.SystemAudio)}", exporter);
            this.SpeechSynthesisProgress?.Write($"{prefix}.{nameof(this.SpeechSynthesisProgress)}", exporter);
            this.DebugInfo?.Write($"{prefix}.{nameof(this.DebugInfo)}", exporter);
        }

        /// <inheritdoc/>
        public virtual void WriteToRendezvousProcess(Rendezvous.Process rendezvousProcess, string address, string prefix = null)
        {
            prefix ??= Name;
            var port = BasePort;
            this.UserInterfaceState?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.UserInterfaceState)}", rendezvousProcess, address, port++, InteropSerialization.GetFormat<TUserInterfaceState>(), DeliveryPolicy.Unlimited);
            this.EyesAndHead?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.EyesAndHead)}", rendezvousProcess, address, port++, Serializers.EyesAndHeadFormat(), DeliveryPolicy.LatestMessage);
            this.Hands?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.Hands)}", rendezvousProcess, address, port++, HoloLensSerializers.OpenXRHandsFormat(), DeliveryPolicy.LatestMessage);
            this.SystemAudio?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.SystemAudio)}", rendezvousProcess, address, port++, HoloLensSerializers.AudioBufferFormat(), DeliveryPolicy.Unlimited);
            this.SpeechSynthesisProgress?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.SpeechSynthesisProgress)}", rendezvousProcess, address, port++, Serializers.SpeechSynthesisProgressFormat(), DeliveryPolicy.Unlimited);
            this.DebugInfo?.WriteToRendezvousProcess(
                $"{prefix}.{nameof(this.DebugInfo)}", rendezvousProcess, address, port++, UserInterfaceDebugInfo.Format, DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Bridges the user interface streams to a different pipeline.
        /// </summary>
        /// <param name="targetPipeline">The target pipeline to bridge the streams to.</param>
        /// <returns>The user interface streams in the target pipeline.</returns>
        public UserInterfaceStreams<TUserInterfaceState> BridgeTo(Pipeline targetPipeline)
            => new (
                this.UserInterfaceState?.BridgeTo(targetPipeline),
                this.EyesAndHead?.BridgeTo(targetPipeline),
                this.Hands?.BridgeTo(targetPipeline),
                this.SystemAudio?.BridgeTo(targetPipeline),
                this.SpeechSynthesisProgress?.BridgeTo(targetPipeline),
                this.DebugInfo?.BridgeTo(targetPipeline));
    }
}
