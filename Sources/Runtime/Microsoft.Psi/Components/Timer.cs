// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A simple producer component that wakes up on a predefined interval and publishes a simple message.
    /// This is useful for components that need to poll some resource. Such components can simply subscribe to this
    /// clock component rather than registering a timer on their own.
    /// </summary>
    public abstract class Timer : IStartable, IDisposable
    {
        private readonly Pipeline pipeline;

        /// <summary>
        /// Delegate we need to hold on to, so that it doesn't get garbage collected.
        /// </summary>
        private readonly Time.TimerDelegate timerDelegate;

        /// <summary>
        /// The interval on which to publish messages.
        /// </summary>
        private readonly TimeSpan timerInterval;

        /// <summary>
        /// The id of the multimedia timer we use under the covers
        /// </summary>
        private Platform.ITimer timer;

        /// <summary>
        /// The last time published by the timer
        /// </summary>
        private DateTime currentTime;

        /// <summary>
        /// The start time of the timer.
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// The end time of the timer.
        /// </summary>
        private DateTime endTime;

        /// <summary>
        /// True if the timer is set
        /// </summary>
        private bool running;

        /// <summary>
        /// An action to call when done
        /// </summary>
        private Action onCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// The timer fires off messages at the rate specified  by timerInterval.
        /// </summary>
        /// <param name="pipeline">The pipeline this component will be part of</param>
        /// <param name="timerInterval">The timer firing interval, in ms.</param>
        public Timer(Pipeline pipeline, uint timerInterval)
        {
            this.pipeline = pipeline;
            this.timerInterval = TimeSpan.FromMilliseconds(timerInterval);
            this.timerDelegate = new Time.TimerDelegate(this.PublishTime);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Timer"/> class.
        /// Releases the underying unmanaged timer.
        /// </summary>
        ~Timer()
        {
            if (this.running)
            {
                this.timer.Stop();
            }
        }

        /// <summary>
        /// Called when the component is stopped.
        /// </summary>
        public void Dispose()
        {
            this.Stop();
        }

        /// <summary>
        /// Called when the component is about to start running.
        /// </summary>
        /// <param name="onCompleted">Callback to invoke when done</param>
        /// <param name="replayContext">Describes the playback constraints</param>
        public void Start(Action onCompleted, ReplayDescriptor replayContext)
        {
            if (replayContext.Start == DateTime.MinValue)
            {
                this.startTime = this.pipeline.GetCurrentTime();
            }
            else
            {
                this.startTime = replayContext.Start;
            }

            this.endTime = replayContext.End;
            this.onCompleted = onCompleted;
            this.currentTime = this.startTime;
            uint realTimeInterval = (uint)this.pipeline.ConvertToRealTime(this.timerInterval).TotalMilliseconds;
            this.timer = Platform.Specific.TimerStart(realTimeInterval, this.timerDelegate);
            this.running = true;
        }

        /// <summary>
        /// Stops the timer. Called by the runtime when the pipeline shuts down.
        /// </summary>
        public void Stop()
        {
            if (this.running)
            {
                this.timer.Stop();
                this.running = false;
                this.onCompleted();
                this.onCompleted = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by the timer. Override to publish actual messages
        /// </summary>
        /// <param name="absoluteTime">The current (virtual) time</param>
        /// <param name="relativeTime">The time elapsed since the generator was started</param>
        protected abstract void Generate(DateTime absoluteTime, TimeSpan relativeTime);

        /// <summary>
        /// Wakes up every timerInterval to publish a new message.
        /// </summary>
        /// <param name="timerID">The parameter is not used.</param>
        /// <param name="msg">The parameter is not used.</param>
        /// <param name="userCtx">The parameter is not used.</param>
        /// <param name="dw1">The parameter is not used.</param>
        /// <param name="dw2">The parameter is not used.</param>
        private void PublishTime(uint timerID, uint msg, UIntPtr userCtx, UIntPtr dw1, UIntPtr dw2)
        {
            var now = this.pipeline.GetCurrentTime();
            if (now >= this.endTime)
            {
                this.Stop();
            }
            else
            {
                // publish a new message.
                this.Generate(now, now - this.startTime);
            }

            this.currentTime += this.timerInterval;
        }
    }
}