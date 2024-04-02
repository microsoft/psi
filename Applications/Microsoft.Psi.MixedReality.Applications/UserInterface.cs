// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.Diagnostics;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.MixedReality.StereoKit;

    /// <summary>
    /// Implements a base class for the user interface component.
    /// </summary>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    /// <remarks>
    /// This component takes in the streams containing the persistent state and interaction states from
    /// the compute pipeline, and takes in the user state streams (containing gaze, head and hands),
    /// and produces an output stream containing the user interface state.
    /// The component is structured for state protection and uses buffered variables for capturing
    /// the incoming state information.
    /// </remarks>
    public abstract class UserInterface<TPersistentState, TUserInterfaceState, TUserInterfaceCommands> : StereoKitRenderer, IProducer<TUserInterfaceState>, ISourceComponent
        where TUserInterfaceState : class, new()
        where TUserInterfaceCommands : class
    {
        private readonly Stopwatch stepStopwatch = new ();
        private readonly Stopwatch updateStateStopwatch = new ();
        private readonly Process currentProcess = Process.GetCurrentProcess();
        private readonly object stateLock = new ();

        private bool active = false;
        private bool persistentStateReceived = false;

        private UserState captureUserState = default;
        private TUserInterfaceCommands captureUserInterfaceCommands = default;
        private TUserInterfaceCommands userInterfaceCommands = default;

        private bool firstStep = true;
        private int outerStepTimeMs = 0;
        private int innerStepTimeMs = 0;
        private int updateStateTimeMs = 0;
        private TimeSpan processorTimeCounterStart;
        private int processCpuPercent = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInterface{TPersistentState, TUserInterfaceState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public UserInterface(Pipeline pipeline, string name = null)
            : base(pipeline, name ?? nameof(UserInterface<TPersistentState, TUserInterfaceState, TUserInterfaceCommands>))
        {
            this.PersistentStateInput = pipeline.CreateReceiver<TPersistentState>(this, this.ReceivePersistentState, nameof(this.PersistentStateInput));
            this.UserStateInput = pipeline.CreateReceiver<UserState>(this, this.ReceiveUserState, nameof(this.UserStateInput));
            this.UserInterfaceCommandsInput = pipeline.CreateReceiver<TUserInterfaceCommands>(this, this.ReceiveUserInterfaceCommands, nameof(this.UserInterfaceCommandsInput));

            this.Out = pipeline.CreateEmitter<TUserInterfaceState>(this, nameof(this.Out));
            this.DebugInfo = pipeline.CreateEmitter<UserInterfaceDebugInfo>(this, nameof(this.DebugInfo));
        }

        /// <summary>
        /// Gets the receiver for the user interface input.
        /// </summary>
        public Receiver<UserState> UserStateInput { get; private set; }

        /// <summary>
        /// Gets the receiver for the user interface commands input.
        /// </summary>
        public Receiver<TUserInterfaceCommands> UserInterfaceCommandsInput { get; private set; }

        /// <summary>
        /// Gets the receiver for the persistent state input.
        /// </summary>
        public Receiver<TPersistentState> PersistentStateInput { get; private set; }

        /// <summary>
        /// Gets the emitter for the user interface state.
        /// </summary>
        public Emitter<TUserInterfaceState> Out { get; private set; }

        /// <summary>
        /// Gets the emitter for debug information.
        /// </summary>
        public Emitter<UserInterfaceDebugInfo> DebugInfo { get; private set; }

        /// <summary>
        /// Gets the renderer used by the user interface.
        /// </summary>
        protected Renderer Renderer { get; } = new ();

        /// <summary>
        /// Gets or sets the user state.
        /// </summary>
        protected UserState UserState { get; set; } = default;

        /// <summary>
        /// Gets the user interface state.
        /// </summary>
        protected TUserInterfaceState UserInterfaceState { get; } = new TUserInterfaceState();

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.active = true;
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.active = false;
            notifyCompleted();
        }

        /// <inheritdoc />
        public override void Step()
        {
            // Start the frame stopwatch if this is the first time we're called
            if (this.firstStep)
            {
                this.firstStep = false;
                this.stepStopwatch.Start();

                // Start measuring the processor time
                this.currentProcess.Refresh();
                this.processorTimeCounterStart = this.currentProcess.TotalProcessorTime;
            }
            else
            {
                this.stepStopwatch.Stop();
                this.outerStepTimeMs = (int)this.stepStopwatch.ElapsedMilliseconds;

                this.processCpuPercent = (int)(100 * (this.currentProcess.TotalProcessorTime - this.processorTimeCounterStart).TotalMilliseconds /
                    (Environment.ProcessorCount * this.stepStopwatch.ElapsedMilliseconds));

                // Start measuring again with the frame stopwatch and current process
                this.stepStopwatch.Restart();
                this.currentProcess.Refresh();
                this.processorTimeCounterStart = this.currentProcess.TotalProcessorTime;
            }

            // Get the state
            this.GetStateFromCapture();

            // Update the UI
            this.updateStateStopwatch.Start();
            this.Update(this.userInterfaceCommands, default);
            this.updateStateStopwatch.Stop();
            this.updateStateTimeMs = (int)this.updateStateStopwatch.ElapsedMilliseconds;
            this.updateStateStopwatch.Reset();

            // Set the head pose for the renderer
            this.Renderer.SetHeadPose(this.UserState?.Head);

            // update the user interface state according to various user inputs
            if (this.UserState != null)
            {
                this.HandleUserInputs();
            }

            // Do the rendering (rendering is setup and executed in base.Step).
            base.Step();

            // post the updated user interface state
            var currentTime = this.Out.Pipeline.GetCurrentTimeFromOpenXr();
            if (this.active)
            {
                if (currentTime > this.Out.LastEnvelope.OriginatingTime)
                {
                    this.Out.Post(this.UserInterfaceState, currentTime);
                }
            }

            // Update and post debug info if necessary
            this.PostDebugInfo(currentTime);

            this.innerStepTimeMs = (int)this.stepStopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Performs user interface state updates based on the incoming initial interaction state.
        /// </summary>
        /// <param name="persistentState">The persistent state.</param>
        /// <param name="envelope">The envelope.</param>
        protected abstract void Initialize(TPersistentState persistentState, Envelope envelope);

        /// <summary>
        /// Performs user interface state updates based on the incoming user interface commands.
        /// </summary>
        /// <param name="userInterfaceCommands">The user interface commands.</param>
        /// <param name="envelope">The envelope.</param>
        protected abstract void Update(TUserInterfaceCommands userInterfaceCommands, Envelope envelope);

        /// <summary>
        /// Updates the state of the user interface based on user actions.
        /// </summary>
        /// <remarks>
        /// This method is called in the StereoKit stepper loop, and should
        /// perform UI updates in response to user actions such as hand and gaze
        /// manipulations of the UI.
        /// </remarks>
        protected virtual void HandleUserInputs()
        {
        }

        private void GetStateFromCapture()
        {
            lock (this.stateLock)
            {
                if (this.captureUserState != default)
                {
                    (this.captureUserState, this.UserState) = (default, this.captureUserState);
                }

                if (this.captureUserInterfaceCommands != default)
                {
                    (this.captureUserInterfaceCommands, this.userInterfaceCommands) = (default, this.captureUserInterfaceCommands);
                }
            }
        }

        private void ReceiveUserState(UserState userState, Envelope envelope)
        {
            lock (this.stateLock)
            {
                if (!this.persistentStateReceived)
                {
                    return;
                }

                userState.DeepClone(ref this.captureUserState);
            }
        }

        private void ReceiveUserInterfaceCommands(TUserInterfaceCommands interactionState, Envelope envelope)
        {
            lock (this.stateLock)
            {
                if (!this.persistentStateReceived)
                {
                    return;
                }

                interactionState.DeepClone(ref this.captureUserInterfaceCommands);
            }
        }

        private void ReceivePersistentState(TPersistentState persistentState, Envelope envelope)
        {
            lock (this.stateLock)
            {
                this.Initialize(persistentState, envelope);
                this.persistentStateReceived = true;
            }
        }

        private void PostDebugInfo(DateTime originatingTime)
        {
            // Accumulate and post debug info at the specified interval
            if (!this.firstStep && originatingTime > this.DebugInfo.LastEnvelope.OriginatingTime)
            {
                this.DebugInfo.Post(
                    new UserInterfaceDebugInfo
                    {
                        OuterStepTimeMs = this.outerStepTimeMs,
                        InnerStepTimeMs = this.innerStepTimeMs,
                        UpdateStateTimeMs = this.updateStateTimeMs,
                        ProcessCpuPercent = this.processCpuPercent,
                    },
                    originatingTime);
            }
        }
    }
}
