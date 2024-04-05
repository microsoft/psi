// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::StereoKit;
    using Microsoft.Azure.SpatialAnchors;
    using Microsoft.Psi;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.ResearchMode;
    using Microsoft.Psi.MixedReality.StereoKit;
    using Microsoft.Psi.Remoting;
    using Windows.Perception.Spatial;
    using Windows.Storage;
    using Task = System.Threading.Tasks.Task;

    /// <summary>
    /// Base class for implementing a StereoKit based mixed-reality client app.
    /// </summary>
    /// <typeparam name="TClientAppConfiguration">The type of the app configuration.</typeparam>
    public abstract class StereoKitClientApp<TClientAppConfiguration>
        where TClientAppConfiguration : ClientAppConfiguration
    {
        private const bool UseVerboseStartupMessages = false;
        private static readonly char[] Separators = new[] { '\r', '\n' };
        private readonly string name;
        private readonly string configurationFileName;
        private Dictionary<string, string> selectedComputeServerName = default;
        private Dictionary<string, bool> selectedIgnoreComputeServerHeartbeat = default;
        private TClientAppConfiguration autoStartConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="StereoKitClientApp{TConfiguration}"/> class.
        /// </summary>
        /// <param name="name">The name of the app.</param>
        /// <param name="configurationFileName">The path to the configuration file.</param>
        public StereoKitClientApp(
            string name,
            string configurationFileName)
        {
            this.name = name;
            this.configurationFileName = configurationFileName;

            // Initialize image encoder resources
            Resources.ImageToStreamEncoder = new ImageToJpegStreamEncoder(0.8);
            Resources.PreviewImageToStreamEncoder = new ImageToJpegStreamEncoder(0.3);
        }

        /// <summary>
        /// State machine for the app.
        /// </summary>
        private enum State
        {
            /// <summary>
            /// Initial state.
            /// </summary>
            Initial,

            /// <summary>
            /// Loading configuration.
            /// </summary>
            LoadingConfiguration,

            /// <summary>
            /// Waiting to start.
            /// </summary>
            WaitingToStart,

            /// <summary>
            /// Locate world spatial anchor.
            /// </summary>
            LocateWorldSpatialAnchor,

            /// <summary>
            /// Locating world spatial anchor.
            /// </summary>
            LocatingWorldSpatialAnchor,

            /// <summary>
            /// Construct pipeline.
            /// </summary>
            ConstructPipeline,

            /// <summary>
            /// Constructing pipeline.
            /// </summary>
            ConstructingPipeline,

            /// <summary>
            /// Calibrate cameras.
            /// </summary>
            CalibrateCameras,

            /// <summary>
            /// Calibrating cameras.
            /// </summary>
            CalibratingCameras,

            /// <summary>
            /// Connect to server.
            /// </summary>
            ConnectToServer,

            /// <summary>
            /// Connecting to server.
            /// </summary>
            ConnectingToServer,

            /// <summary>
            /// Running.
            /// </summary>
            Running,

            /// <summary>
            /// Stopping pipeline.
            /// </summary>
            StoppingPipeline,

            /// <summary>
            /// Stopped.
            /// </summary>
            Stopped,

            /// <summary>
            /// Exited.
            /// </summary>
            Exited,
        }

        /// <summary>
        /// Gets the list of available application configurations.
        /// </summary>
        public List<TClientAppConfiguration> AvailableConfigurations { get; private set; }

        /// <summary>
        /// Gets the selected configuration.
        /// </summary>
        public TClientAppConfiguration SelectedConfiguration { get; private set; }

        /// <summary>
        /// Runs the app.
        /// </summary>
        /// <exception cref="Exception">An initialization exception is thrown if StereoKit fails to initialize.</exception>
        public void Run()
        {
            // Initialize StereoKit
            if (!SK.Initialize(
                new SKSettings
                {
                    appName = this.name,
                    assetsFolder = "Assets",
                }))
            {
                throw new Exception("StereoKit failed to initialize.");
            }

            Pipeline pipeline = default;
            HoloLensStreams holoLensStreams = default;
            IClientServerCommunicationStreams userInterfaceStreams = default;

            string errorMessage = null;
            var startupMenuStereoKitPose = default(Pose);

            var computeServerRendezvousProcess = default(Rendezvous.Process);

            var startTime = DateTime.MinValue;
            var lastHeartBeatDateTime = DateTime.MaxValue;
            var lastHeartBeatMessage = default(Heartbeat);
            var heartBeatLostTimeout = TimeSpan.FromSeconds(5);
            var heartBeatNeverArrivedTimeout = TimeSpan.FromSeconds(20);

            // The boolean variables below are used to track the state machine
            // through which the application starts. Only one of them will be
            // true at a given point in time. We go from menu (where the user
            // chooses the configuration to run) to startUp, where the pipeline
            // is created and largely setup, then to either waitForComputeServer
            // (in case we are doing remote compute and need the output streams
            // from the compute server), or to finalizePipelineSetupAndRun
            // (in which the pipeline is finalized by using the output streams,
            // and started), and finally waitForStop (in which the pipeline is
            // running and we are waiting for the user to click a Stop button)
            var state = State.Initial;
            int hasStateBeenReset = 0;
            var documentsFolder = KnownFolders.DocumentsLibrary;
            DepthCamera depthCamera = default;

            var errorTextStyle = Text.MakeStyle(Font.Default, 0.006f, Color.White);
            Mesh worldAnchorMesh = null;
            bool showWorldSpatialAnchor = false;

            // Create the spatial anchor provider used for persistent world anchors
            using var persistentSpatialAnchorProvider = this.CreateSpatialAnchorProvider();

            // Stops the pipeline in a background task.
            void StopPipeline()
            {
                state = State.StoppingPipeline;
                Task.Run(() =>
                {
                    try
                    {
                        pipeline?.Dispose();
                    }
                    finally
                    {
                        pipeline = null;
                        state = State.Stopped;
                    }
                });
            }

            // Initialize MixedReality statics and the default world coordinate system
            MixedReality.Initialize(regenerateDefaultWorldSpatialAnchorIfNeeded: true);

            while (state != State.Exited)
            {
                try
                {
                    // We initially start with a head-relative user interface
                    var userInterfaceIsHeadRelative = true;

                    // Enclose SK.Run within a try-catch block to handle any exceptions thrown
                    // by the onStep action, other IStepper.Step methods, or StereoKit itself.
                    while (SK.Step(() =>
                    {
                        // Check and display a message if localization has been temporarily lost
                        if (StereoKitTransforms.WorldHierarchy == null)
                        {
                            Pose dialogPose;
                            dialogPose.position = Input.Head.position + (Input.Head.Forward * 0.5f);
                            dialogPose.orientation = Quat.LookAt(dialogPose.position, Input.Head.position);
                            Text.Add("Please wait: Localizing...", dialogPose.ToMatrix(), TextAlign.Center, TextAlign.Center);
                            return;
                        }

                        // Compute whether the UI is head relative
                        userInterfaceIsHeadRelative =
                            state == State.Initial ||
                            state == State.LoadingConfiguration ||
                            this.autoStartConfiguration != null;

                        // Compute the head relative user interface position
                        if (userInterfaceIsHeadRelative)
                        {
                            startupMenuStereoKitPose = Matrix.TR(
                                Input.Head.position + (Input.Head.Forward * 0.5f) + (Input.Head.Right * -0.1f) + (Input.Head.Up * 0.05f),
                                Quat.LookAt(Input.Head.position + Input.Head.Forward, Input.Head.position)).Pose;
                        }

                        // Display the world spatial anchor if requested
                        if (showWorldSpatialAnchor && MixedReality.LocalizationState == LocalizationState.Localized)
                        {
                            // Push the world origin so we can draw using world coordinates
                            Hierarchy.Push(StereoKitTransforms.WorldHierarchy.Value);
                            try
                            {
                                // Draw a 1 cm cube at the world origin
                                worldAnchorMesh ??= CreateRGBCube();
                                worldAnchorMesh.Draw(Default.Material, Matrix.S(1 * U.cm));
                                Text.Add(MixedReality.WorldSpatialAnchorId, Matrix.TR(new Vec3(0, 2 * U.cm, 0), new Vec3(0, 180, 0)), errorTextStyle);
                            }
                            finally
                            {
                                // Pop the world origin when done drawing
                                Hierarchy.Pop();
                            }
                        }

                        UI.HandleBegin(
                            "Handle",
                            ref startupMenuStereoKitPose,
                            Bounds.FromCorner(new Vec3(0, 0, -2) * U.cm, new Vec3(4, 4, 4) * U.cm),
                            drawHandle: !userInterfaceIsHeadRelative);

                        try
                        {
                            // When executing background tasks, we should surface exceptions in the UI.
                            // This function will capture the exception text and display it in an error
                            // message. Use this function instead of Task.Run when running background
                            // tasks from any of the state actions below.
                            void RunTaskWithExceptionHandling(Action action)
                            {
                                Task.Run(action).ContinueWith(
                                    task =>
                                    {
                                        if (task.Exception != null)
                                        {
                                            var ex = task.Exception?.GetBaseException();

                                            // Set errorMessage and stop the pipeline, which will cause
                                            // the error to be displayed once the pipeline is stopped.
                                            errorMessage = ex.ToString();
                                            Trace.WriteLine($"Task error: {errorMessage}");
                                            StopPipeline();
                                        }
                                    },
                                    TaskContinuationOptions.OnlyOnFaulted);
                            }

                            switch (state)
                            {
                                case State.Initial:

                                    if (this.AvailableConfigurations != null)
                                    {
                                        // Find if any configuration is set to auto-start
                                        this.autoStartConfiguration = this.AvailableConfigurations.FirstOrDefault(c => c.AutoStart);

                                        // Skip this step if the configuration was already loaded
                                        state = State.WaitingToStart;
                                        break;
                                    }

                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Loading configuration ..." : "Please wait. Initializing ... (10% done)");

                                    if (string.IsNullOrEmpty(this.configurationFileName))
                                    {
                                        // If no configuration file was supplied, create a new configuration collection containing the default configuration
                                        this.AvailableConfigurations = this.GetDefaultConfigurations();

                                        // Populate configuration defaults
                                        this.PopulateConfigurationDefaults();
                                        this.SelectedConfiguration = this.AvailableConfigurations.FirstOrDefault();

                                        // Find if any configuration is set to auto-start
                                        this.autoStartConfiguration = this.AvailableConfigurations.FirstOrDefault(c => c.AutoStart);

                                        state = State.WaitingToStart;
                                    }
                                    else
                                    {
                                        // Load or create the configuration
                                        RunTaskWithExceptionHandling(async () =>
                                        {
                                            if (!string.IsNullOrEmpty(this.configurationFileName))
                                            {
                                                try
                                                {
                                                    // Attempt to load the available configurations from the configuration file
                                                    var configurationFileStream = await documentsFolder.OpenStreamForReadAsync(this.configurationFileName);
                                                    this.AvailableConfigurations = ConfigurationHelper.ReadFromStream<List<TClientAppConfiguration>>(
                                                        configurationFileStream,
                                                        this.GetExtraTypes());
                                                }
                                                catch (FileNotFoundException)
                                                {
                                                    // If the configuration file does not exist, create one with the default configuration
                                                    this.AvailableConfigurations = this.GetDefaultConfigurations();
                                                    var fileStream = await documentsFolder.OpenStreamForWriteAsync(
                                                        this.configurationFileName,
                                                        CreationCollisionOption.ReplaceExisting);
                                                    ConfigurationHelper.WriteToStream(this.AvailableConfigurations, fileStream, this.GetExtraTypes());
                                                }
                                            }
                                            else
                                            {
                                                // If no configuration file was supplied, create a new configuration collection containing the default configuration
                                                this.AvailableConfigurations = this.GetDefaultConfigurations();
                                            }

                                            // Populate configuration defaults
                                            this.PopulateConfigurationDefaults();
                                            this.SelectedConfiguration = this.AvailableConfigurations.FirstOrDefault();

                                            // Find if any configuration is set to auto-start
                                            this.autoStartConfiguration = this.AvailableConfigurations.FirstOrDefault(c => c.AutoStart);

                                            state = State.WaitingToStart;
                                        });

                                        state = State.LoadingConfiguration;
                                    }

                                    break;

                                case State.LoadingConfiguration:
                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Loading configuration ..." : "Please wait. Initializing ... (20% done)");
                                    break;

                                case State.WaitingToStart:

                                    // If we need to auto-start a configuration
                                    if (this.autoStartConfiguration != null)
                                    {
                                        // Then select it
                                        this.SelectedConfiguration = this.autoStartConfiguration;
                                        state = State.LocateWorldSpatialAnchor;
                                    }
                                    else
                                    {
                                        // Enable hand interaction
                                        UI.EnableFarInteract = true;

                                        // Call the method for dealing with building the UI for waiting for start
                                        this.OnWaitingForStart();

                                        UI.Toggle("Show World Anchor", ref showWorldSpatialAnchor);

                                        // O/w display the start and exit buttons
                                        if (UI.Button($"Start"))
                                        {
                                            state = State.LocateWorldSpatialAnchor;
                                        }

                                        UI.SameLine();
                                        if (UI.Button("Exit"))
                                        {
                                            state = State.Exited;
                                            SK.Quit();
                                        }
                                    }

                                    break;

                                case State.LocateWorldSpatialAnchor:
                                    string worldSpatialAnchorId = this.SelectedConfiguration.WorldSpatialAnchorId;

                                    if (string.IsNullOrEmpty(worldSpatialAnchorId))
                                    {
                                        UI.Label("No world spatial anchor id was specified in config.\r\nPlease choose one of the following options:");

                                        if (UI.Button("Temporarily use current world spatial anchor"))
                                        {
                                            state = State.LocatingWorldSpatialAnchor;
                                        }

                                        if (UI.Button("Create new persistent world spatial anchor"))
                                        {
                                            // Create a new anchor and save the configuration
                                            MixedReality.SetWorldCoordinateSystem(
                                                spatialAnchorProvider: persistentSpatialAnchorProvider,
                                                createWorldSpatialAnchorIfNeeded: true);

                                            // Save the world spatial anchor id to the selected configuration
                                            this.SelectedConfiguration.WorldSpatialAnchorId = MixedReality.WorldSpatialAnchorId;
                                            if (!string.IsNullOrEmpty(this.configurationFileName))
                                            {
                                                // Start a task to save the new spatial anchor id to the configuration file
                                                RunTaskWithExceptionHandling(async () =>
                                                {
                                                    var fileStream = await documentsFolder.OpenStreamForWriteAsync(this.configurationFileName, CreationCollisionOption.ReplaceExisting);
                                                    ConfigurationHelper.WriteToStream(this.AvailableConfigurations, fileStream, this.GetExtraTypes());
                                                });
                                            }

                                            state = State.LocatingWorldSpatialAnchor;
                                        }

                                        if (UI.Button("Exit"))
                                        {
                                            state = State.Stopped;
                                        }
                                    }
                                    else
                                    {
                                        // Attempt to locate the specified world spatial anchor (do not create one if it cannot be found)
                                        MixedReality.SetWorldCoordinateSystem(
                                            spatialAnchorProvider: persistentSpatialAnchorProvider,
                                            worldSpatialAnchorId: worldSpatialAnchorId,
                                            createWorldSpatialAnchorIfNeeded: false);

                                        state = State.LocatingWorldSpatialAnchor;
                                    }

                                    break;

                                case State.LocatingWorldSpatialAnchor:
                                    switch (MixedReality.LocalizationState)
                                    {
                                        case LocalizationState.Localizing:
                                            UI.Label(UseVerboseStartupMessages ?
                                                $"Please wait: Locating world spatial anchor id: {MixedReality.WorldSpatialAnchorId}" :
                                                "Please wait. Initializing ... (30% done)");
                                            break;
                                        case LocalizationState.NotLocalized:
                                            UI.Label($"Error: Unable to locate world spatial anchor id: {MixedReality.WorldSpatialAnchorId}");
                                            break;
                                        case LocalizationState.Localized:
                                            UI.Label(UseVerboseStartupMessages ?
                                                $"Located world spatial anchor id: {MixedReality.WorldSpatialAnchorId}" :
                                                "Please wait. Initializing ... (30% done)");
                                            state = State.ConstructPipeline;
                                            break;
                                        default:
                                            UI.Label(UseVerboseStartupMessages ?
                                                $"Unexpected localization state: {MixedReality.LocalizationState}" :
                                                "Please wait. Initializing ... (30% done)");
                                            break;
                                    }

                                    if (this.autoStartConfiguration == null)
                                    {
                                        if (UI.Button($"Stop"))
                                        {
                                            state = State.Stopped;
                                        }
                                    }

                                    break;

                                case State.ConstructPipeline:

                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Constructing pipeline ..." : "Please wait. Initializing ... (40% done)");

                                    // Set the heartbeat to max value
                                    lastHeartBeatDateTime = DateTime.MaxValue;
                                    state = State.ConstructingPipeline;

                                    RunTaskWithExceptionHandling(() =>
                                    {
                                        // Create the pipeline
                                        pipeline = Pipeline.Create(
                                            enableDiagnostics: true,
                                            diagnosticsConfiguration: new DiagnosticsConfiguration()
                                            {
                                                SamplingInterval = TimeSpan.FromSeconds(1),
                                                AveragingTimeSpan = TimeSpan.FromSeconds(3),
                                            });

                                        // Get live input streams from hololens
                                        holoLensStreams = this.GetHoloLensStreams(pipeline, out depthCamera);

                                        // Create the user interface pipeline and resulting streams
                                        userInterfaceStreams = this.CreateUserInterfacePipeline(pipeline);

                                        state = State.CalibrateCameras;
                                    });

                                    break;

                                case State.ConstructingPipeline:
                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Constructing pipeline ..." : "Please wait. Initializing ... (50% done)");
                                    break;

                                case State.CalibrateCameras:
                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Calibrating cameras ..." : "Please wait. Initializing ... (60% done)");
                                    state = State.CalibratingCameras;

                                    // Calibrate all cameras with async tasks
                                    var calibrationTasks = new List<Task>();
                                    RunTaskWithExceptionHandling(async () =>
                                    {
                                        // Open (or create) the Documents folder containing calibration files
                                        var calibrationFolder = await documentsFolder.CreateFolderAsync("Calibration", CreationCollisionOption.OpenIfExists);
                                        calibrationTasks.Add(depthCamera?.CalibrateFromFileAsync(calibrationFolder));
                                        await Task.WhenAll(calibrationTasks.Where(t => t is not null).ToArray());
                                        state = State.ConnectToServer;
                                    });
                                    break;

                                case State.CalibratingCameras:
                                    UI.Label(UseVerboseStartupMessages ? "Please wait: Calibrating cameras ..." : "Please wait. Initializing ... (70% done)");
                                    break;

                                case State.ConnectToServer:
                                    var computeServerName = this.selectedComputeServerName[this.SelectedConfiguration.Name];
                                    UI.Label(UseVerboseStartupMessages ? $"Please wait: Connecting to server {computeServerName} ..." : "Please wait. Initializing ... (80% done)");
                                    state = State.ConnectingToServer;
                                    hasStateBeenReset = 0;
                                    string configName = this.SelectedConfiguration.Name;

                                    RunTaskWithExceptionHandling(() =>
                                    {
                                        // Create the rendezvous client and process
                                        var rendezvousClient = new RendezvousClient(computeServerName);
                                        rendezvousClient.Start();
                                        rendezvousClient.Connected.WaitOne();
                                        var headsetAddress = rendezvousClient.ClientAddress;

                                        // Try and remove previous processes (in case they have crashed)
                                        rendezvousClient.Rendezvous.TryRemoveProcess(configName);

                                        // Create the process with the name based on the specified pipeline type
                                        var process = new Rendezvous.Process(configName);

                                        // Sync clocks
                                        var remoteClock = new RemoteClockExporter(RemoteClockExporter.DefaultPort);
                                        process.AddEndpoint(remoteClock.ToRendezvousEndpoint(headsetAddress));

                                        // Write the input streams to the rendezvous process
                                        holoLensStreams.WriteToRendezvousProcess(process, headsetAddress);

                                        // Write the user interface streams to the rendezvous process
                                        userInterfaceStreams.WriteToRendezvousProcess(process, headsetAddress);

                                        void ResetState()
                                        {
                                            if (Interlocked.CompareExchange(ref hasStateBeenReset, 1, 0) == 0)
                                            {
                                                // Try and remove previous processes (in case they have crashed)
                                                rendezvousClient.Rendezvous.TryRemoveProcess(configName);
                                                rendezvousClient?.Stop();
                                                remoteClock?.Dispose();
                                                computeServerRendezvousProcess = null;
                                                state = State.Stopped;
                                            }
                                        }

                                        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                                        {
                                            ResetState();
                                        };

                                        pipeline.PipelineExceptionNotHandled += (_, ex) =>
                                        {
                                            errorMessage = ex.Exception.ToString();
                                            Trace.WriteLine($"Pipeline Error: {errorMessage}");
                                            ResetState();
                                        };

                                        pipeline.PipelineCompleted += (_, _) =>
                                        {
                                            ResetState();
                                        };

                                        rendezvousClient.Rendezvous.ProcessAdded += (_, process) =>
                                        {
                                            if (process.Name == "LiveComputeServer")
                                            {
                                                computeServerRendezvousProcess = process;
                                            }
                                        };

                                        rendezvousClient.Rendezvous.ProcessRemoved += (_, process) =>
                                        {
                                            if (process.Name == "LiveComputeServer")
                                            {
                                                Trace.WriteLine($"Server shutdown");
                                                StopPipeline();
                                            }
                                        };

                                        rendezvousClient.Rendezvous.TryAddProcess(process);
                                    });

                                    break;

                                case State.ConnectingToServer:

                                    if (this.autoStartConfiguration == null)
                                    {
                                        if (UI.Button($"Stop"))
                                        {
                                            StopPipeline();
                                        }

                                        UI.SameLine();
                                    }

                                    UI.Label(UseVerboseStartupMessages ?
                                        $"Please wait: Connecting to server {this.selectedComputeServerName[this.SelectedConfiguration.Name]} ..." :
                                        "Please wait. Initializing ... (80% done)");

                                    if (computeServerRendezvousProcess != default)
                                    {
                                        var heartbeat = this.GetAndConnectOutputStreams(pipeline, computeServerRendezvousProcess);
                                        heartbeat?.Do(
                                            m =>
                                            {
                                                lastHeartBeatDateTime = DateTime.UtcNow;
                                                lastHeartBeatMessage = m.DeepClone();
                                            },
                                            DeliveryPolicy.LatestMessage,
                                            name: "MonitorHeartbeat");

                                        // Setup the heartbeat at the pipeline start time
                                        pipeline.PipelineRun += (_, e) => lastHeartBeatDateTime = e.StartOriginatingTime;

                                        // Run the pipeline
                                        pipeline.RunAsync();
                                        startTime = DateTime.Now;
                                        state = State.Running;
                                    }

                                    break;

                                case State.Running:

                                    UI.EnableFarInteract = false;

                                    var timeSinceLastHeartBeat = DateTime.UtcNow - lastHeartBeatDateTime;
                                    if ((lastHeartBeatDateTime == pipeline.StartTime && timeSinceLastHeartBeat > heartBeatNeverArrivedTimeout) ||
                                        (!this.selectedIgnoreComputeServerHeartbeat[this.SelectedConfiguration.Name] && lastHeartBeatDateTime != pipeline.StartTime && timeSinceLastHeartBeat > heartBeatLostTimeout))
                                    {
                                        errorMessage = lastHeartBeatDateTime == pipeline.StartTime ?
                                            $"Heartbeat signal was never received (timeout at {heartBeatNeverArrivedTimeout.TotalSeconds} seconds from startup)." :
                                            $"Heartbeat signal was lost (timeout after {heartBeatLostTimeout.TotalSeconds} seconds).";

                                        StopPipeline();
                                    }

                                    if (this.autoStartConfiguration == null)
                                    {
                                        if (UI.Button($"Stop"))
                                        {
                                            StopPipeline();
                                        }

                                        UI.SameLine();
                                    }

                                    if (lastHeartBeatDateTime == pipeline.StartTime)
                                    {
                                        UI.Label(UseVerboseStartupMessages ?
                                            "Please wait: Waiting for server heartbeat ..." :
                                            "Please wait. Initializing ... (90% done)");
                                    }
                                    else
                                    {
                                        if (this.autoStartConfiguration == null)
                                        {
                                            UI.Label($"Running for {(int)(DateTime.Now - startTime).TotalMinutes} mins ({lastHeartBeatMessage})");
                                        }
                                    }

                                    if (errorMessage != null)
                                    {
                                        UI.Label($"Error: ");
                                        UI.PushTextStyle(errorTextStyle);
                                        UI.Text(errorMessage, TextAlign.TopLeft);
                                        UI.PopTextStyle();
                                    }

                                    break;

                                case State.StoppingPipeline:
                                    UI.Label("Waiting for pipeline to shutdown ...");
                                    break;

                                case State.Stopped:

                                    if (errorMessage != null)
                                    {
                                        // Display the error message until the user clicks OK
                                        UI.Label("Error: ");
                                        UI.SameLine();
                                        if (UI.Button("Close"))
                                        {
                                            if (this.autoStartConfiguration != null)
                                            {
                                                state = State.Exited;
                                                SK.Quit();
                                            }
                                            else
                                            {
                                                state = State.Initial;
                                                errorMessage = null;
                                            }
                                        }

                                        UI.PushTextStyle(errorTextStyle);
                                        UI.Text(errorMessage, TextAlign.TopLeft);
                                        UI.PopTextStyle();
                                    }
                                    else if (this.autoStartConfiguration != null)
                                    {
                                        // O/w if we auto started, then exit
                                        state = State.Exited;
                                        SK.Quit();
                                    }
                                    else
                                    {
                                        // O/w go back to the initial state
                                        state = State.Initial;
                                    }

                                    break;
                            }
                        }
                        finally
                        {
                            UI.HandleEnd();
                        }
                    }))
                    {
                    }
                }
                catch (Exception ex)
                {
                    // Display the exception when SK.Run restarts
                    errorMessage = ex.ToString();
                    Trace.WriteLine($"Error: {errorMessage}");

                    state = State.Stopped;
                }
                finally
                {
                    // Ensure that any running pipeline is stopped
                    pipeline?.Dispose();
                    pipeline = null;

                    // Reset world coordinate system
                    MixedReality.SetWorldCoordinateSystem();
                }
            }

            SK.Shutdown();
        }

        /// <summary>
        /// Populates various configuration defaults.
        /// </summary>
        public virtual void PopulateConfigurationDefaults()
        {
            this.selectedComputeServerName = this.AvailableConfigurations.ToDictionary(
                config => config.Name,
                config => config.ComputeServerNames.FirstOrDefault());

            this.selectedIgnoreComputeServerHeartbeat = this.AvailableConfigurations.ToDictionary(
                config => config.Name,
                config => config.IgnoreServerHeartbeat);
        }

        /// <summary>
        /// Populates the UI at the waiting to start point.
        /// </summary>
        public virtual void OnWaitingForStart()
        {
            // Display the list of available onfigurations
            UI.Label("Configurations:");
            int buttonCount = 0;
            foreach (var configuration in this.AvailableConfigurations)
            {
                if (buttonCount++ % 3 != 0)
                {
                    UI.SameLine();
                }

                if (UI.Radio(configuration.Name, configuration.Name == this.SelectedConfiguration.Name))
                {
                    this.SelectedConfiguration = configuration;
                }
            }

            // Display the list of compute servers for the selected configuration
            UI.Label("Compute server:");
            buttonCount = 0;
            foreach (var computeServerName in this.SelectedConfiguration.ComputeServerNames)
            {
                if (buttonCount++ % 3 != 0)
                {
                    UI.SameLine();
                }

                if (UI.Radio(computeServerName, computeServerName == this.selectedComputeServerName[this.SelectedConfiguration.Name]))
                {
                    this.selectedComputeServerName[this.SelectedConfiguration.Name] = computeServerName;
                }
            }

            // Display the option to ignore the heartbeat
            UI.Label("Ignore heartbeat:");
            UI.SameLine();
            if (UI.Radio("Yes", this.selectedIgnoreComputeServerHeartbeat[this.SelectedConfiguration.Name]))
            {
                this.selectedIgnoreComputeServerHeartbeat[this.SelectedConfiguration.Name] = true;
            }

            UI.SameLine();
            if (UI.Radio("No", !this.selectedIgnoreComputeServerHeartbeat[this.SelectedConfiguration.Name]))
            {
                this.selectedIgnoreComputeServerHeartbeat[this.SelectedConfiguration.Name] = false;
            }
        }

        /// <summary>
        /// Gets the extra types to be used for serialization.
        /// </summary>
        /// <returns>The extra types to be used for serialization.</returns>
        public virtual Type[] GetExtraTypes() => null;

        /// <summary>
        /// Gets the default configurations.
        /// </summary>
        /// <returns>The default configurations.</returns>
        public abstract List<TClientAppConfiguration> GetDefaultConfigurations();

        /// <summary>
        /// Gets the collection of streams from the HoloLens.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <param name="depthCamera">The depth camera.</param>
        /// <returns>The collection of streams from the HoloLens.</returns>
        public abstract HoloLensStreams GetHoloLensStreams(Pipeline pipeline, out DepthCamera depthCamera);

        /// <summary>
        /// Creates the user interface pipeline and returns the resulting streams.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <returns>The user interface streams.</returns>
        public abstract IClientServerCommunicationStreams CreateUserInterfacePipeline(Pipeline pipeline);

        /// <summary>
        /// Gets and connects the output streams.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <param name="computeServerRendezvousProcess">The compute server rendezvous process.</param>
        /// <returns>The heartbeat stream.</returns>
        public abstract IProducer<Heartbeat> GetAndConnectOutputStreams(Pipeline pipeline, Rendezvous.Process computeServerRendezvousProcess);

        /// <summary>
        /// Reads a file asynchronously.
        /// </summary>
        /// <param name="folder">The folder to read from.</param>
        /// <param name="filename">The name of the file to read.</param>
        /// <returns>The contents of the file.</returns>
        protected async Task<string> ReadFileAsync(StorageFolder folder, string filename)
        {
            string text = string.Empty;
            try
            {
                var keyFile = await folder.GetFileAsync(filename);
                text = await FileIO.ReadTextAsync(keyFile);
            }
            catch (FileNotFoundException)
            {
            }

            return text;
        }

        /// <summary>
        /// Creates a mesh representing a cube whose opposing sides are colored red, green and blue.
        /// </summary>
        /// <returns>A mesh representing the RGB cube.</returns>
        private static Mesh CreateRGBCube()
        {
            // Define RGB values for custom colors
            var red = new Color(1.0f, 0.0f, 0.0f);
            var green = new Color(0.0f, 1.0f, 0.0f);
            var blue = new Color(0.0f, 0.0f, 1.0f);

            // Define the vertices for a cube with custom colors
            var cubeVertices = new Vertex[]
            {
                // Front face
                new (new Vec3(-0.5f, -0.5f, 0.5f), new Vec3(0, 0, 1), Vec2.Zero, red),
                new (new Vec3(0.5f, -0.5f, 0.5f), new Vec3(0, 0, 1), Vec2.Zero, red),
                new (new Vec3(0.5f, 0.5f, 0.5f), new Vec3(0, 0, 1), Vec2.Zero, red),
                new (new Vec3(-0.5f, 0.5f, 0.5f), new Vec3(0, 0, 1), Vec2.Zero, red),

                // Back face
                new (new Vec3(0.5f, -0.5f, -0.5f), new Vec3(0, 0, -1), Vec2.Zero, red),
                new (new Vec3(-0.5f, -0.5f, -0.5f), new Vec3(0, 0, -1), Vec2.Zero, red),
                new (new Vec3(-0.5f, 0.5f, -0.5f), new Vec3(0, 0, -1), Vec2.Zero, red),
                new (new Vec3(0.5f, 0.5f, -0.5f), new Vec3(0, 0, -1), Vec2.Zero, red),

                // Left face
                new (new Vec3(-0.5f, -0.5f, -0.5f), new Vec3(-1, 0, 0), Vec2.Zero, green),
                new (new Vec3(-0.5f, -0.5f, 0.5f), new Vec3(-1, 0, 0), Vec2.Zero, green),
                new (new Vec3(-0.5f, 0.5f, 0.5f), new Vec3(-1, 0, 0), Vec2.Zero, green),
                new (new Vec3(-0.5f, 0.5f, -0.5f), new Vec3(-1, 0, 0), Vec2.Zero, green),

                // Right face
                new (new Vec3(0.5f, -0.5f, 0.5f), new Vec3(1, 0, 0), Vec2.Zero, green),
                new (new Vec3(0.5f, -0.5f, -0.5f), new Vec3(1, 0, 0), Vec2.Zero, green),
                new (new Vec3(0.5f, 0.5f, -0.5f), new Vec3(1, 0, 0), Vec2.Zero, green),
                new (new Vec3(0.5f, 0.5f, 0.5f), new Vec3(1, 0, 0), Vec2.Zero, green),

                // Top face
                new (new Vec3(0.5f, 0.5f, -0.5f), new Vec3(0, 1, 0), Vec2.Zero, blue),
                new (new Vec3(-0.5f, 0.5f, -0.5f), new Vec3(0, 1, 0), Vec2.Zero, blue),
                new (new Vec3(-0.5f, 0.5f, 0.5f), new Vec3(0, 1, 0), Vec2.Zero, blue),
                new (new Vec3(0.5f, 0.5f, 0.5f), new Vec3(0, 1, 0), Vec2.Zero, blue),

                // Bottom face
                new (new Vec3(-0.5f, -0.5f, -0.5f), new Vec3(0, -1, 0), Vec2.Zero, blue),
                new (new Vec3(0.5f, -0.5f, -0.5f), new Vec3(0, -1, 0), Vec2.Zero, blue),
                new (new Vec3(0.5f, -0.5f, 0.5f), new Vec3(0, -1, 0), Vec2.Zero, blue),
                new (new Vec3(-0.5f, -0.5f, 0.5f), new Vec3(0, -1, 0), Vec2.Zero, blue),
            };

            // Define the cube's indices (vertex order for triangles)
            var cubeIndices = new uint[]
            {
                0, 1, 2, 2, 3, 0, // Front face
                4, 5, 6, 6, 7, 4, // Back face
                8, 9, 10, 10, 11, 8, // Left face
                12, 13, 14, 14, 15, 12, // Right face
                16, 17, 18, 18, 19, 16, // Top face
                20, 21, 22, 22, 23, 20, // Bottom face
            };

            // Create a mesh from the vertices and indices
            var cubeMesh = new Mesh();
            cubeMesh.SetVerts(cubeVertices);
            cubeMesh.SetInds(cubeIndices);
            return cubeMesh;
        }

        /// <summary>
        /// Creates either a cloud or local spatial anchor provider depending on whether the Azure Spatial Anchors account info exists.
        /// </summary>
        /// <returns>The spatial anchor provider.</returns>
        private ISpatialAnchorProvider CreateSpatialAnchorProvider()
        {
            // Read the Azure Spatial Anchors account info from a file. File should contain the following 3 lines:
            // AccountId:<account id>
            // AccountDomain:<account domain>
            // AccountKey:<account key>
            string fileContents = this.ReadFileAsync(KnownFolders.DocumentsLibrary, "AzureSpatialAnchorsAccountInfo.txt").GetAwaiter().GetResult();

            // Parse file contents into a dictionary (will be empty if file does not exist)
            var asaAccountInfo = fileContents.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(':'))
                .ToDictionary(parts => parts[0].Trim().ToLower(), parts => parts[1].Trim());

            if (asaAccountInfo.TryGetValue("accountid", out string accountId) &&
                asaAccountInfo.TryGetValue("accountdomain", out string accountDomain) &&
                asaAccountInfo.TryGetValue("accountkey", out string accountKey))
            {
                // Create the cloud spatial anchor provider
                return new AzureSpatialAnchorProvider(accountId, accountDomain, accountKey, SessionLogLevel.All);
            }
            else
            {
                // Fallback to a locally-persisted spatial anchor provider
                return new LocalSpatialAnchorProvider(SpatialAnchorManager.RequestStoreAsync().AsTask().GetAwaiter().GetResult());
            }
        }
    }
}
