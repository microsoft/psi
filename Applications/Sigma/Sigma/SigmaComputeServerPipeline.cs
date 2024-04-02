// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.OpenXR;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Composite component that implements the Sigma compute server pipeline.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TInteractionModel">The interaction model.</typeparam>
    /// <typeparam name="TInteractionStateManager">The type of the interaction state manager.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TInteractionState">The type of the interaction state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public abstract class SigmaComputeServerPipeline<TTask, TConfiguration, TInteractionModel, TInteractionStateManager, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands> : Subpipeline, ISigmaComputeServerPipeline
        where TTask : Task, IInteropSerializable, new()
        where TConfiguration : SigmaComputeServerPipelineConfiguration, new()
        where TInteractionModel : SigmaInteractionModel<TTask, TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>, new()
        where TInteractionStateManager : SigmaInteractionStateManager<TTask, TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        where TPersistentState : SigmaPersistentState<TTask>, new()
        where TInteractionState : SigmaInteractionState<TTask>, new()
        where TUserInterfaceState : SigmaUserInterfaceState, new()
        where TUserInterfaceCommands : SigmaUserInterfaceCommands, new()
    {
        private IProducer<TimeIntervalAnnotationSet> speechSynthesisAnnotations;

        private IProducer<UserInterfaceDebugInfo> userInterfaceDebugInfo;

        private IProducer<Object3DTrackingResults> object3DTrackedResultsDisplayStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaComputeServerPipeline{TTask, TConfiguration, TInteractionModel, TInteractionStateManager, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add this component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="hololensStreams">The hololens streams.</param>
        /// <param name="userInterfaceStreams">The user interface streams.</param>
        /// <param name="precomputedStreams">An optional set of precomputed streams.</param>
        protected SigmaComputeServerPipeline(
            Pipeline pipeline,
            TConfiguration configuration,
            HoloLensStreams hololensStreams,
            UserInterfaceStreams<TUserInterfaceState> userInterfaceStreams,
            PrecomputedStreams precomputedStreams = null)
            : base(
                pipeline,
                nameof(SigmaComputeServerPipeline<TTask, TConfiguration, TInteractionModel, TInteractionStateManager, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>),
                defaultDeliveryPolicy: DeliveryPolicy.LatestMessage)
        {
            // Capture the configuration
            this.Configuration = configuration;

            // Create the output streams
            this.OutputStreams = new OutputStreams<TPersistentState, TUserInterfaceCommands>();

            // Bridge the input streams to the current pipeline
            this.HololensStreams = hololensStreams.BridgeTo(this);
            this.UserInterfaceStreams = userInterfaceStreams.BridgeTo(this);
            this.PrecomputedStreams = precomputedStreams?.BridgeTo(this);
        }

        /// <summary>
        /// Gets the interaction state manager.
        /// </summary>
        public TInteractionStateManager InteractionStateManager { get; private set; }

        /// <summary>
        /// Gets the output streams.
        /// </summary>
        public OutputStreams<TPersistentState, TUserInterfaceCommands> OutputStreams { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public TConfiguration Configuration { get; }

        /// <inheritdoc/>
        IClientServerCommunicationStreams ISigmaComputeServerPipeline.OutputStreams => this.OutputStreams;

        /// <summary>
        /// Gets the hololens streams.
        /// </summary>
        protected HoloLensStreams HololensStreams { get; }

        /// <summary>
        /// Gets the user interface streams.
        /// </summary>
        protected UserInterfaceStreams<TUserInterfaceState> UserInterfaceStreams { get; }

        /// <summary>
        /// Gets the precomputed streams.
        /// </summary>
        protected PrecomputedStreams PrecomputedStreams { get; }

        /// <summary>
        /// Gets the object tracking pipeline.
        /// </summary>
        protected ObjectTrackingPipeline ObjectTrackingPipeline { get; private set; }

        /// <summary>
        /// Gets the speech recognition pipeline.
        /// </summary>
        protected SpeechRecognitionPipeline SpeechRecognitionPipeline { get; private set; }

        /// <summary>
        /// Gets the LLM query runner.
        /// </summary>
        protected LLMQueryRunner LLMQueryRunner { get; private set; }

        /// <summary>
        /// Gets the user state stream.
        /// </summary>
        protected IProducer<UserState> UserState { get; private set; } = default;

        /// <summary>
        /// Gets the head pose stream.
        /// </summary>
        protected IProducer<CoordinateSystem> Head { get; private set; }

        /// <summary>
        /// Gets the voice activity detection stream.
        /// </summary>
        protected IProducer<bool> VoiceActivityDetection { get; private set; } = default;

        /// <summary>
        /// Gets the speech recognition results stream.
        /// </summary>
        protected IProducer<IStreamingSpeechRecognitionResult> SpeechRecognitionResults { get; private set; } = default;

        /// <summary>
        /// Gets the partial speech recognition results stream.
        /// </summary>
        protected IProducer<IStreamingSpeechRecognitionResult> PartialSpeechRecognitionResults { get; private set; } = default;

        /// <summary>
        /// Creates the interaction state manager.
        /// </summary>
        /// <returns>The interaction state manager.</returns>
        public abstract TInteractionStateManager CreateInteractionStateManager();

        /// <summary>
        /// Initializes the composite component.
        /// </summary>
        public virtual void Initialize()
        {
            // Capture the user interface debug info
            this.userInterfaceDebugInfo = this.UserInterfaceStreams.DebugInfo;

            // Compute the user state stream
            this.Head = this.UserInterfaceStreams.EyesAndHead.Item2(DeliveryPolicy.SynchronousOrThrottle, name: "GetHead");
            var headVelocity = this.Head.GetHeadVelocity(DeliveryPolicy.SynchronousOrThrottle);
            this.UserState = this.UserInterfaceStreams.EyesAndHead
                .Join(
                    headVelocity,
                    Reproducible.Exact<CoordinateSystemVelocity3D>(),
                    primaryDeliveryPolicy: DeliveryPolicy.SynchronousOrThrottle,
                    secondaryDeliveryPolicy: DeliveryPolicy.SynchronousOrThrottle)
                .Join(
                    this.UserInterfaceStreams.Hands,
                    Reproducible.NearestOrDefault<(Hand, Hand)>(),
                    (t1, t2) => new UserState()
                    {
                        Eyes = t1.Item1,
                        Head = t1.Item2,
                        HeadVelocity3D = t1.Item3,
                        HandLeft = t2.Item1,
                        HandRight = t2.Item2,
                    },
                    primaryDeliveryPolicy: DeliveryPolicy.SynchronousOrThrottle,
                    secondaryDeliveryPolicy: DeliveryPolicy.SynchronousOrThrottle);

            // Setup the speech reco sub-pipeline
            if (this.HololensStreams.Audio != null)
            {
                if (this.PrecomputedStreams == null || this.Configuration.UseLiveSpeechRecoInBatchMode)
                {
                    this.SpeechRecognitionPipeline = new SpeechRecognitionPipeline(this, this.Configuration.SpeechRecognitionPipelineConfiguration);
                    this.HololensStreams.Audio.PipeTo(this.SpeechRecognitionPipeline, DeliveryPolicy.Unlimited);
                    this.VoiceActivityDetection = this.SpeechRecognitionPipeline.VoiceActivity;
                    this.SpeechRecognitionResults = this.SpeechRecognitionPipeline.RecognitionResults;
                    this.PartialSpeechRecognitionResults = this.SpeechRecognitionPipeline.PartialRecognitionResults;
                }
                else
                {
                    // O/w get the recognition results from the stored compute streams
                    this.VoiceActivityDetection = this.PrecomputedStreams.VoiceActivityDetection;
                    this.SpeechRecognitionResults = this.PrecomputedStreams.SpeechRecognitionResults;
                }
            }

            // Setup the object tracking pipeline
            if (this.Configuration.ObjectTrackingPipelineConfiguration != null)
            {
                this.ObjectTrackingPipeline = new ObjectTrackingPipeline(this, this.Configuration.ObjectTrackingPipelineConfiguration);
                this.HololensStreams.VideoImageCameraView.PipeTo(this.ObjectTrackingPipeline.VideoImageCameraView);
                this.HololensStreams.DepthImageCameraView.PipeTo(this.ObjectTrackingPipeline.DepthImageCameraView);
                this.UserState.PipeTo(this.ObjectTrackingPipeline.UserState);
            }

            // Setup the LLM query engine
            if (this.Configuration.UsesLLMQueryLibrary)
            {
                this.LLMQueryRunner = new LLMQueryRunner(this, this.Configuration.LLMQueryLibraryFilename);
            }

            // Setup the interaction state manager
            this.InteractionStateManager = this.CreateInteractionStateManager();
            this.UserState.PipeTo(this.InteractionStateManager.UserStateInput, DeliveryPolicy.Unlimited);
            this.UserInterfaceStreams.UserInterfaceState.PipeTo(this.InteractionStateManager.UserInterfaceStateInput, DeliveryPolicy.Unlimited);
            this.UserInterfaceStreams.SpeechSynthesisProgress.PipeTo(this.InteractionStateManager.SpeechSynthesisProgressInput, DeliveryPolicy.Unlimited);
            this.SpeechRecognitionResults?.PipeTo(this.InteractionStateManager.SpeechRecognitionResultsInput, DeliveryPolicy.Unlimited);
            this.PartialSpeechRecognitionResults?.PipeTo(this.InteractionStateManager.PartialSpeechRecognitionResultsInput, DeliveryPolicy.Unlimited);

            // Connect the interaction state manager to the object tracking pipeline
            if (this.ObjectTrackingPipeline != null)
            {
                // Pass the object detection classes to the object tracking pipeline and get the results
                this.InteractionStateManager.InteractionStateOutput.Select(s => s.ObjectClasses).PipeTo(this.ObjectTrackingPipeline.ObjectClasses);
                this.ObjectTrackingPipeline.TrackedObjectsLocations.PipeTo(this.InteractionStateManager.TrackedObjectsLocationsInput);

                // Collect the results and form a display stream that shows the objects only during the gather step
                this.object3DTrackedResultsDisplayStream = this.ObjectTrackingPipeline.Object3DTrackingResults
                    .Join(this.InteractionStateManager.InteractionStateOutput, TimeSpan.FromSeconds(1))
                    .Select(t => t.Item2.SelectedStep is GatherStep ? t.Item1 : new Object3DTrackingResults());
            }

            // Connect the llm query runner to the interaction state manager
            if (this.Configuration.UsesLLMQueryLibrary)
            {
                this.InteractionStateManager.LLMQuery.PipeTo(this.LLMQueryRunner, DeliveryPolicy.Unlimited);
                this.LLMQueryRunner.PipeTo(this.InteractionStateManager.LLMQueryResultsInput, DeliveryPolicy.Unlimited);
            }

            // Generate the speech synthesis annotations stream
            this.speechSynthesisAnnotations = this.UserInterfaceStreams.SpeechSynthesisProgress.ToAnnotations();

            // Compute the output streams
            this.OutputStreams.PersistentState = this.InteractionStateManager.PersistentStateOutput;
            this.OutputStreams.UserInterfaceCommands = this.InteractionStateManager.UserInterfaceCommandsOutput;
            this.OutputStreams.Heartbeat = this.GetHeartbeat(this.HololensStreams, this.UserInterfaceStreams);
        }

        /// <summary>
        /// Gets the heartbeat based on the hololens and user interface streasm.
        /// </summary>
        /// <param name="holoLensStreams">The hololens streams.</param>
        /// <param name="userInterfaceStreams">The user interface streams.</param>
        /// <returns>The heartbeat stream.</returns>
        public IProducer<Heartbeat> GetHeartbeat(HoloLensStreams holoLensStreams, UserInterfaceStreams<TUserInterfaceState> userInterfaceStreams)
        {
            var depthClock = holoLensStreams.DepthImageCameraView.Select(_ => 1, DeliveryPolicy.SynchronousOrThrottle);
            var imageClock = holoLensStreams.VideoEncodedImageCameraView.Select(_ => 1, DeliveryPolicy.SynchronousOrThrottle);
            var eyesAndHeadClock = userInterfaceStreams.EyesAndHead.Select(_ => 1, DeliveryPolicy.SynchronousOrThrottle);
            var timeInterval = TimeSpan.FromSeconds(1);
            return depthClock
                .Join(imageClock, timeInterval, DeliveryPolicy.SynchronousOrThrottle, DeliveryPolicy.SynchronousOrThrottle)
                .Join(eyesAndHeadClock, timeInterval, DeliveryPolicy.SynchronousOrThrottle, DeliveryPolicy.SynchronousOrThrottle)
                .Window(
                    -5,
                    0,
                    el => new Heartbeat()
                    {
                        FrameRate = 5.0 / (el.Last().OriginatingTime - el.First().OriginatingTime).TotalSeconds,
                        Latency = (el.Last().CreationTime - el.Last().OriginatingTime).TotalSeconds,
                        AuxiliaryInfo = null,
                    },
                    DeliveryPolicy.SynchronousOrThrottle)
                .ToPresent();
        }

        /// <inheritdoc/>
        public virtual void Write(string prefix, Exporter exporter)
        {
            this.UserInterfaceStreams.Write("UserInterfaceStreams", exporter);
            this.OutputStreams.Write("OutputStreams", exporter);

            this.ObjectTrackingPipeline?.Write($"{prefix}.ObjectTracking", exporter);
            this.LLMQueryRunner?.Write($"{prefix}.LLMQueryRunner.Results", exporter);
            this.LLMQueryRunner?.LLMQuery?.Write($"{prefix}.LLMQueryRunner.Query", exporter);
            this.SpeechRecognitionPipeline?.Write($"{prefix}.SpeechRecognition", exporter);
            this.speechSynthesisAnnotations?.Write(AnnotationSchemas.SpeechSynthesisAnnotationSchema, $"{prefix}.SpeechSynthesis", exporter);
            this.InteractionStateManager?.Write($"{prefix}.InteractionStateManager", exporter);

            // Compute and log some derived streams off of the user state
            var windowedUserInterfaceDebugInfo = this.userInterfaceDebugInfo.Window(RelativeTimeInterval.Past(TimeSpan.FromSeconds(3)));
            windowedUserInterfaceDebugInfo
                .Select(wudi => wudi.Average(di => di.OuterStepTimeMs), DeliveryPolicy.SynchronousOrThrottle)
                .Write($"{nameof(UserInterfaceDebugInfo)}.AverageOuterStepTimeMs", exporter);
            windowedUserInterfaceDebugInfo
                .Select(wudi => wudi.Average(di => di.InnerStepTimeMs), DeliveryPolicy.SynchronousOrThrottle)
                .Write($"{nameof(UserInterfaceDebugInfo)}.AverageInnerStepTimeMs", exporter);
            windowedUserInterfaceDebugInfo
                .Select(wudi => wudi.Average(di => di.UpdateStateTimeMs), DeliveryPolicy.SynchronousOrThrottle)
                .Write($"{nameof(UserInterfaceDebugInfo)}.AverageUpdateStateTimeMs", exporter);
            windowedUserInterfaceDebugInfo
                .Select(wudi => wudi.Average(di => di.ProcessCpuPercent), DeliveryPolicy.SynchronousOrThrottle)
                .Write($"{nameof(UserInterfaceDebugInfo)}.AverageProcessCpuPercent", exporter);

            // Write the tracked objects info stream
            this.object3DTrackedResultsDisplayStream?.Write($"{prefix}.ObjectTracking.DisplayStream", exporter);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            this.ObjectTrackingPipeline?.Dispose();
            this.SpeechRecognitionPipeline?.Dispose();
        }
    }
}
