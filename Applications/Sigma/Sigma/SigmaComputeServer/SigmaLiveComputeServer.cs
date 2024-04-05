// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Implements the live compute server for the Sigma app.
    /// </summary>
    public class SigmaLiveComputeServer : LiveComputeServer<SigmaComputeServerPipelineConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaLiveComputeServer"/> class.
        /// </summary>
        /// <param name="configuration">The live compute server configuration.</param>
        private SigmaLiveComputeServer(SigmaLiveComputeServerConfiguration configuration)
            : base("Sigma", configuration.PipelineConfigurations)
        {
            // This may have already been set by the caller, but set it anyway just in case
            AppConsole.LogFilename = configuration.ServerLogFilename;
        }

        /// <summary>
        /// Runs the Sigma live compute server.
        /// </summary>
        /// <param name="configuration">The live compute server configuration.</param>
        public static void Run(SigmaLiveComputeServerConfiguration configuration)
            => new SigmaLiveComputeServer(configuration).Run();

        /// <inheritdoc/>
        protected override void CreateComputeServerPipeline(
            SigmaComputeServerPipelineConfiguration configuration,
            HoloLensStreams hololensStreams,
            Rendezvous.Process inputRendezvousProcess,
            Rendezvous.Process outputRendezvousProcess,
            Exporter exporter)
        {
            // Log
            AppConsole.TimedWriteLine($"Creating compute server pipeline, exporting to {Path.GetFileName(exporter.Path)}.");

            // Create the live compute server pipeline, intiialize and write the streams to the exporter
            var computeServerPipeline = configuration.CreateLiveComputeServerPipeline(this.ComputeServerPipeline, hololensStreams, inputRendezvousProcess, exporter);
            computeServerPipeline.Initialize();
            computeServerPipeline.Write("Sigma", exporter);

            // Write the output streams to the rendezvous process
            computeServerPipeline.OutputStreams.WriteToRendezvousProcess(outputRendezvousProcess, this.ServerAddress);

            // Log
            AppConsole.TimedWriteLine($"Created compute server pipeline.");
        }
    }
}
