// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents the set of output streams sent by the compute server pipeline to the client app.
    /// </summary>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of user interface commands.</typeparam>
    public class OutputStreams<TPersistentState, TUserInterfaceCommands> : IClientServerCommunicationStreams
        where TPersistentState : IInteropSerializable, new()
        where TUserInterfaceCommands : IInteropSerializable, new()
    {
        private const int BasePort = 16000;
        private const string Name = "OutputStreams";

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStreams{TPersistentState, TUserInterfaceCommands}"/> class.
        /// </summary>
        public OutputStreams()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStreams{TPersistentState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <param name="rendezvousProcess">The rendezvous process to read the streams from.</param>
        /// <param name="prefix">An optional prefix to prepend to the stream names.</param>
        public OutputStreams(Pipeline pipeline, Rendezvous.Process rendezvousProcess, string prefix = null)
        {
            prefix ??= Name;
            foreach (var endpoint in rendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    foreach (var stream in tcpEndpoint.Streams)
                    {
                        if (stream.StreamName == $"{prefix}.{nameof(this.Heartbeat)}")
                        {
                            this.Heartbeat = tcpEndpoint.ToTcpSource<Heartbeat>(pipeline, Applications.Heartbeat.Format, name: nameof(this.Heartbeat));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.PersistentState)}")
                        {
                            this.PersistentState = tcpEndpoint.ToTcpSource<TPersistentState>(pipeline, InteropSerialization.GetFormat<TPersistentState>(), name: nameof(this.PersistentState));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.UserInterfaceCommands)}")
                        {
                            this.UserInterfaceCommands = tcpEndpoint.ToTcpSource<TUserInterfaceCommands>(pipeline, InteropSerialization.GetFormat<TUserInterfaceCommands>(), name: nameof(this.UserInterfaceCommands));
                        }
                    }
                }
                else
                {
                    throw new Exception("Unexpected endpoint type.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the heartbeat stream.
        /// </summary>
        public IProducer<Heartbeat> Heartbeat { get; set; }

        /// <summary>
        /// Gets or sets the persistent state stream.
        /// </summary>
        public IProducer<TPersistentState> PersistentState { get; set; }

        /// <summary>
        /// Gets or sets the user interface commands stream.
        /// </summary>
        public IProducer<TUserInterfaceCommands> UserInterfaceCommands { get; set; }

        /// <inheritdoc/>
        public virtual void Write(string prefix, Exporter exporter)
        {
            this.Heartbeat?.Write($"{prefix}.{nameof(this.Heartbeat)}", exporter);
            this.PersistentState?.Write($"{prefix}.{nameof(this.PersistentState)}", exporter);
            this.UserInterfaceCommands?.Write($"{prefix}.{nameof(this.UserInterfaceCommands)}", exporter);
        }

        /// <inheritdoc/>
        public virtual void WriteToRendezvousProcess(Rendezvous.Process rendezvousProcess, string address, string prefix = null)
        {
            prefix ??= Name;
            var port = BasePort;
            this.Heartbeat?.WriteToRendezvousProcess($"{prefix}.{nameof(this.Heartbeat)}", rendezvousProcess, address, port++, Applications.Heartbeat.Format, DeliveryPolicy.LatestMessage);
            this.PersistentState?.WriteToRendezvousProcess($"{prefix}.{nameof(this.PersistentState)}", rendezvousProcess, address, port++, InteropSerialization.GetFormat<TPersistentState>(), DeliveryPolicy.Unlimited);
            this.UserInterfaceCommands?.WriteToRendezvousProcess($"{prefix}.{nameof(this.UserInterfaceCommands)}", rendezvousProcess, address, port++, InteropSerialization.GetFormat<TUserInterfaceCommands>(), DeliveryPolicy.LatestMessage);
        }
    }
}
