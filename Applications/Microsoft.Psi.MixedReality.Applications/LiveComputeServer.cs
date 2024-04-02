// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;

    /// <summary>
    /// Implements an abstract base class for a live compute server.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration object.</typeparam>
    public abstract class LiveComputeServer<TConfiguration> : IDisposable
        where TConfiguration : ComputeServerPipelineConfiguration
    {
        private readonly string name;
        private readonly RendezvousServer rendezvousServer = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveComputeServer{TConfiguration}"/> class.
        /// </summary>
        /// <param name="name">The name for the live compute server.</param>
        /// <param name="availableConfigurations">The set of available configurations.</param>
        public LiveComputeServer(string name, IEnumerable<TConfiguration> availableConfigurations)
        {
            this.name = name;

            // Organize the available configurations by name
            this.AvailableConfigurations = availableConfigurations.ToDictionary(config => config.Name);
        }

        /// <summary>
        /// Gets the available configurations.
        /// </summary>
        protected Dictionary<string, TConfiguration> AvailableConfigurations { get; private set; }

        /// <summary>
        /// Gets the compute server pipeline.
        /// </summary>
        protected Pipeline ComputeServerPipeline { get; private set; }

        /// <summary>
        /// Gets the server address.
        /// </summary>
        protected string ServerAddress => this.rendezvousServer.ServerAddress;

        /// <summary>
        /// Method called upon running the live compute server.
        /// </summary>
        public virtual void OnRun()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.rendezvousServer.Dispose();
        }

        /// <summary>
        /// Runs the live compute server.
        /// </summary>
        /// <exception cref="Exception">Exception thrown while running the live compute server.</exception>
        public void Run()
        {
            AppConsole.WriteLine("Available configurations: ");
            foreach (var config in this.AvailableConfigurations.Keys)
            {
                AppConsole.WriteLine($"  {config}");
            }

            this.OnRun();

            // Listen for the client app to connect, and, when connected, create the processing pipeline
            this.rendezvousServer.Rendezvous.ProcessAdded += (_, process) =>
            {
                this.ReportProcessAdded(process);

                // The client app's process name will be used to select the configuration
                // so we should check that it exists before proceeding any further.
                if (this.AvailableConfigurations.ContainsKey(process.Name))
                {
                    AppConsole.TimedWriteLine($"  Starting Pipeline {process.Name}");

                    this.CreateAndRunComputeServerPipeline(process);

                    this.ComputeServerPipeline.PipelineExceptionNotHandled += (_, args) =>
                    {
                        this.StopComputeServerPipeline(
                            $"SERVER PIPELINE RUNTIME EXCEPTION: {args.Exception.Message}" + Environment.NewLine +
                            args.Exception.StackTrace.ToString());
                    };
                }
                else if (process.Name != nameof(LiveComputeServer<TConfiguration>))
                {
                    AppConsole.TimedWriteLine($"Pipeline not started for unknown process {process.Name}");
                    throw new Exception($"Connection received from unexpected process named {process.Name}.");
                }
            };

            // When the client app is stopped (removed), disposed of the compute server pipeline
            this.rendezvousServer.Rendezvous.ProcessRemoved += (_, process) =>
            {
                this.ReportProcessRemoved(process);
                if (process.Name == this.ComputeServerPipeline?.Name)
                {
                    this.StopComputeServerPipeline("Client stopped.");
                }
            };

            this.rendezvousServer.Error += (_, ex) =>
            {
                AppConsole.TimedWriteLine();
                this.StopComputeServerPipeline($"RENDEZVOUS ERROR: {ex.Message}\n{ex.StackTrace}");
            };

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                this.StopComputeServerPipeline($"SERVER EXITED");
            };

            this.rendezvousServer.Start();
            AppConsole.TimedWriteLine($"Listening on TCP port {RendezvousServer.DefaultPort} for client app ({this.name}).");
            AppConsole.TimedWriteLine("Be sure to check firewall settings (may need to enable Public).");

            // Wait to press a key to exit
            AppConsole.TimedWriteLine("Press any key to exit.");
            Console.ReadKey();

            // Stop the compute server
            this.rendezvousServer.Stop();
            this.StopComputeServerPipeline("Server manually stopped");
        }

        /// <summary>
        /// Method that creates the compute server pipeline.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        /// <param name="hololensStreams">The set of hololens sensor streams.</param>
        /// <param name="inputRendezvousProcess">The input rendezvous process.</param>
        /// <param name="outputRendezvousProcess">The output rendezvous process.</param>
        /// <param name="exporter">The exporter to write streams to.</param>
        protected abstract void CreateComputeServerPipeline(
            TConfiguration configuration,
            HoloLensStreams hololensStreams,
            Rendezvous.Process inputRendezvousProcess,
            Rendezvous.Process outputRendezvousProcess,
            Exporter exporter);

        private void CreateAndRunComputeServerPipeline(Rendezvous.Process inputRendezvousProcess)
        {
            // If a pipeline is already running, stop it first
            if (this.ComputeServerPipeline != null)
            {
                this.StopComputeServerPipeline("CLIENT STARTED NEW SESSION WHILE PREVIOUS STILL RUNNING");
            }

            // Get the selected configuartion name from the client app's process name
            string selectedConfigurationName = inputRendezvousProcess.Name;
            var selectedConfiguration = this.AvailableConfigurations[selectedConfigurationName];

            // Create the pipeline, store and output diagnostics. Note that the pipeline
            // is named using the selected configuration name so that we can correctly
            // identify and stop the pipeline when the client rendezvous process is removed.
            this.ComputeServerPipeline = Pipeline.Create(
                name: selectedConfigurationName,
                enableDiagnostics: true,
                diagnosticsConfiguration: new () { SamplingInterval = TimeSpan.FromSeconds(5) });

            var dateTime = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var outputDirectory = Path.Combine(selectedConfiguration.OutputPath, dateTime);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Detect app disconnect
            var lastAppHeartBeat = DateTime.MaxValue;
            var appHeartBeatTimeout = TimeSpan.FromSeconds(20);
            Timers.Timer(this.ComputeServerPipeline, TimeSpan.FromSeconds(1)).Do(_ =>
            {
                if (DateTime.UtcNow - lastAppHeartBeat > appHeartBeatTimeout)
                {
                    this.StopComputeServerPipeline("APPLICATION HEARTBEAT LOST");
                }
            });

            // Connect to remote clock on the client app to synchronize clocks
            foreach (var endpoint in inputRendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockExporterEndpoint)
                {
                    var remoteClock = remoteClockExporterEndpoint.ToRemoteClockImporter(this.ComputeServerPipeline);
                    AppConsole.TimedWrite("    Connecting to clock sync ...");
                    if (!remoteClock.Connected.WaitOne(10000))
                    {
                        AppConsole.WriteLine("FAILED.");
                        throw new Exception("Failed to connect to remote clock exporter.");
                    }

                    AppConsole.WriteLine("DONE.");
                }
            }

            // Create the hololens and user interface streams from the client process endpoints, and log them
            var hololensStreams = new HoloLensStreams(this.ComputeServerPipeline, inputRendezvousProcess);
            if (selectedConfiguration.ReEncodePreviewStream)
            {
                hololensStreams.ReEncodePreviewStream();
            }

            // Capture the heart beat
            hololensStreams.VideoEncodedImageCameraView.Do(_ => lastAppHeartBeat = DateTime.UtcNow, name: "CaptureAppHeartbeat");

            // Create the rendezvous process for this pipeline to enable it to publish
            var liveComputeServerProcess = new Rendezvous.Process(nameof(LiveComputeServer<TConfiguration>));

            // Create the store to write outputs to
            var exporter = PsiStore.Create(this.ComputeServerPipeline, this.name, outputDirectory, createSubdirectory: false);

            this.CreateComputeServerPipeline(selectedConfiguration, hololensStreams, inputRendezvousProcess, liveComputeServerProcess, exporter);

            // Write the hololens streams to the store
            hololensStreams.Write("HoloLensStreams", exporter);

            // Write the diagnostics
            this.ComputeServerPipeline.Diagnostics.Write("Diagnostics", exporter, largeMessages: true);

            // Add this process to the rendezvous server
            this.rendezvousServer.Rendezvous.TryAddProcess(liveComputeServerProcess);

            this.ComputeServerPipeline.RunAsync();
            AppConsole.TimedWriteLine("    Running...");
        }

        private void StopComputeServerPipeline(string message)
        {
            if (this.ComputeServerPipeline != null)
            {
                AppConsole.TimedWriteLine(message);
                AppConsole.TimedWriteLine($"  Stopping Compute Server Pipeline @{DateTime.Now}.");
                this.rendezvousServer.Rendezvous.TryRemoveProcess(nameof(LiveComputeServer<TConfiguration>));
                AppConsole.TimedWriteLine($"  Removed {nameof(LiveComputeServer<TConfiguration>)} process @{DateTime.Now}.");
                this.ComputeServerPipeline?.Dispose();
                this.ComputeServerPipeline = null;
                AppConsole.TimedWriteLine($"  Stopped Compute Server Pipeline @{DateTime.Now}. ");
            }
        }

        private void ReportProcessAdded(Rendezvous.Process process)
        {
            AppConsole.TimedWriteLine();
            AppConsole.TimedWriteLine($"PROCESS ADDED: {process.Name}");
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    AppConsole.TimedWriteLine($"  ENDPOINT: TCP {tcpEndpoint.Host} {tcpEndpoint.Port}");
                }
                else if (endpoint is Rendezvous.NetMQSourceEndpoint netMQEndpoint)
                {
                    AppConsole.TimedWriteLine($"  ENDPOINT: NetMQ {netMQEndpoint.Address}");
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                {
                    AppConsole.TimedWriteLine($"  ENDPOINT: Remote {remoteExporterEndpoint.Host} {remoteExporterEndpoint.Port} {remoteExporterEndpoint.Transport}");
                }
                else if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockExporterEndpoint)
                {
                    AppConsole.TimedWriteLine($"  ENDPOINT: Remote Clock {remoteClockExporterEndpoint.Host} {remoteClockExporterEndpoint.Port}");
                }
                else
                {
                    throw new ArgumentException($"Unknown type of Endpoint ({endpoint.GetType().Name}).");
                }

                foreach (var stream in endpoint.Streams)
                {
                    AppConsole.TimedWriteLine($"    STREAM: {stream.StreamName}");
                }
            }
        }

        private void ReportProcessRemoved(Rendezvous.Process process)
        {
            AppConsole.TimedWriteLine();
            AppConsole.TimedWriteLine($"PROCESS REMOVED: {process.Name}");
        }
    }
}
