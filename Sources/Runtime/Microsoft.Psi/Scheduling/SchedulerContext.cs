// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;
    using System.Threading;

    /// <summary>
    /// Provides a context in which work items may be scheduled and tracked as a group.
    /// Maintains a count of the number of work items currently in-flight and an event
    /// to signal when there are no remaining work items in the context.
    /// </summary>
    public sealed class SchedulerContext
    {
        private readonly object syncLock = new object();
        private readonly ManualResetEvent empty = new ManualResetEvent(true);
        private int workItemCount;

        /// <summary>
        /// Gets or sets the finalization time of the context after which no further work will be scheduled.
        /// This is initialized to <see cref="DateTime.MaxValue"/> when scheduling on the context is enabled.
        /// It may later be set to a finite time prior to terminating scheduling on the context once the
        /// final scheduling time on the context is known.
        /// </summary>
        public DateTime FinalizeTime { get; set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets a wait handle that signals when there are no remaining work items in the context.
        /// </summary>
        public WaitHandle Empty => this.empty;

        internal Clock Clock { get; private set; } = new Clock(DateTime.MinValue, 0);

        internal bool Started { get; private set; } = false;

        /// <summary>
        /// Starts scheduling work on the context.
        /// </summary>
        /// <param name="clock">The scheduler clock.</param>
        public void Start(Clock clock)
        {
            this.Clock = clock;
            this.Started = true;
        }

        /// <summary>
        /// Stops scheduling work on the context.
        /// </summary>
        public void Stop()
        {
            this.Started = false;
            this.Clock = new Clock(DateTime.MinValue, 0);
        }

        /// <summary>
        /// Enters the context before scheduling a work item.
        /// </summary>
        internal void Enter()
        {
            lock (this.syncLock)
            {
                if (++this.workItemCount == 1)
                {
                    this.empty.Reset();
                }
            }
        }

        /// <summary>
        /// Exits the context after a work item has completed or been abandoned.
        /// </summary>
        internal void Exit()
        {
            lock (this.syncLock)
            {
                if (--this.workItemCount == 0)
                {
                    this.empty.Set();
                }
            }
        }
    }
}
