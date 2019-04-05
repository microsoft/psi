// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Executive;
    using Microsoft.Psi.Scheduling;

    /// <summary>
    /// Represents a graph of components and controls scheduling and message passing.
    /// </summary>
    /// <remarks>This is essentially a pipeline as a component within other pipelines.</remarks>
    public class Subpipeline : Pipeline, ISourceComponent
    {
        private Pipeline parent;
        private Action<DateTime> notifyCompletionTime;
        private bool hasSourceComponents;
        private bool stopping = false;
        private bool finalizeComponents = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subpipeline"/> class.
        /// </summary>
        /// <param name="parent">Parent pipeline.</param>
        /// <param name="name">Subpipeline name (inherits "Sub<Parent>" name if unspecified)</Parent>.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy (inherits from parent if unspecified).</param>
        public Subpipeline(Pipeline parent, string name = null, DeliveryPolicy deliveryPolicy = null)
            : base(name ?? $"Sub{parent.Name}", deliveryPolicy ?? parent.DeliveryPolicy, new Scheduler(parent.Scheduler))
        {
            this.parent = parent;

            // ensures that the subpipeline is registered with the parent
            parent.AddComponent(this);
        }

        /// <summary>
        /// Create subpipeline.
        /// </summary>
        /// <param name="parent">Parent pipeline.</param>
        /// <param name="name">Subpipeline name.</param>
        /// <param name="deliveryPolicy">Pipeline-level delivery policy.</param>
        /// <returns>Created subpipeline.</returns>
        public static Subpipeline Create(Pipeline parent, string name = null, DeliveryPolicy deliveryPolicy = null)
        {
            return new Subpipeline(parent, name, deliveryPolicy);
        }

        /// <summary>
        /// Initialize subpipeline as a finite source component.
        /// </summary>
        /// <remarks>This is called by the parent subpipeline, if any.</remarks>
        /// <param name="notifyCompletionTime">Delegate to call to notify of completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.notifyCompletionTime = notifyCompletionTime;

            // start the subpipeline
            this.RunAsync(this.parent.ReplayDescriptor);
            this.InitializeCompletionTimes();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            this.StopComponents();
        }

        /// <summary>
        /// Proposes a replay time interval for the pipeline.
        /// </summary>
        /// <param name="activeInterval">Active time interval.</param>
        /// <param name="originatingTimeInterval">Originating time interval.</param>
        public override void ProposeReplayTime(TimeInterval activeInterval, TimeInterval originatingTimeInterval)
        {
            base.ProposeReplayTime(activeInterval, originatingTimeInterval);

            // propagate the proposed replay time interval back up to the parent
            this.parent.ProposeReplayTime(activeInterval, originatingTimeInterval);
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public override IDisposable RunAsync(ReplayDescriptor descriptor)
        {
            return this.RunAsync(descriptor, this.parent.Clock);
        }

        /// <summary>
        /// Stop subpipeline.
        /// </summary>
        /// <param name="finalOriginatingTime">Originating time of final message scheduled.</param>
        public void Stop(DateTime finalOriginatingTime)
        {
            if (finalOriginatingTime > this.FinalOriginatingTime)
            {
                this.FinalOriginatingTime = finalOriginatingTime;
            }

            this.Final();
        }

        /// <summary>
        /// Finalize subpipeline.
        /// </summary>
        public void Final()
        {
            this.finalizeComponents = true;
            this.Stop(false);
            this.Complete(true);
        }

        internal override bool NotifyCompletionTime(PipelineElement component, DateTime finalOriginatingTime)
        {
            var onlyInfiniteRemaining = base.NotifyCompletionTime(component, finalOriginatingTime);
            if (onlyInfiniteRemaining)
            {
                // only infinite children remain, notify that subpipeline itself is infinite
                this.notifyCompletionTime(DateTime.MaxValue);
            }

            return onlyInfiniteRemaining;
        }

        /// <summary>
        /// Stops the pipeline by disabling message passing between the pipeline components.
        /// The pipeline configuration is not changed and the pipeline can be restarted later.
        /// </summary>
        /// <param name="abandonPendingWorkitems">Abandons the pending work items</param>
        protected override void Stop(bool abandonPendingWorkitems = false)
        {
            if (this.stopping)
            {
                this.Completed.WaitOne();
                return;
            }

            this.stopping = true;

            if (this.finalizeComponents)
            {
                this.Scheduler.NotifyPipelineFinalizing(this.FinalOriginatingTime == DateTime.MinValue ? this.GetCurrentTime() : this.FinalOriginatingTime);
            }

            // stop components
            var stopped = false;
            do
            {
                stopped = this.StopComponents() > 0;
                this.PauseForQuiescence();
            }
            while (stopped);

            if (this.finalizeComponents)
            {
                this.FinalizeComponents();
                this.PauseForQuiescence();
                this.Scheduler.Stop(abandonPendingWorkitems);
            }

            if (this.hasSourceComponents)
            {
                this.notifyCompletionTime(this.GetCurrentTime());
            }

            this.Completed.Set();
        }

        private void InitializeCompletionTimes()
        {
            this.hasSourceComponents = this.Components.Where(c => c.StateObject != this).Any(c => c.IsSource);
            if (!this.hasSourceComponents)
            {
                // no source components, so purely reactive
                this.notifyCompletionTime(DateTime.MaxValue); // MaxValue is special; meaning this Subpipeline was *never* a finite source
            }

            // emitters created from this subpipeline look like components, but should not prevent completion
            foreach (var c in this.Components.Where(c => c.StateObject == this))
            {
                this.NotifyCompletionTime(c, DateTime.MinValue);
            }
        }
    }
}