// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Represents a graph of components and controls scheduling and message passing.
    /// </summary>
    /// <remarks>This is essentially a pipeline as a component within other pipelines.</remarks>
    public class Subpipeline : Pipeline, IFiniteSourceComponent
    {
        private Action onCompleted;
        private bool hasSourceComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subpipeline"/> class.
        /// </summary>
        /// <param name="parent">Parent pipeline.</param>
        /// <param name="name">Subpipeline name (inherits "Sub<Parent>" name if unspecified)</Parent>.</param>
        /// <param name="globalPolicy">Delivery policy (inherits from parent if unspecified).</param>
        public Subpipeline(Pipeline parent, string name = null, DeliveryPolicy globalPolicy = null)
            : base(name ?? $"Sub{parent.Name}", globalPolicy ?? parent.GlobalPolicy, parent.Scheduler)
        {
            parent.RegisterPipelineStartHandler(this, () => this.Start(parent));
            parent.RegisterPipelineStopHandler(this, () => this.Stop(false, false));
            parent.PipelineCompletionEvent += (_, e) => this.Complete(true);
        }

        /// <summary>
        /// Create subpipeline.
        /// </summary>
        /// <param name="parent">Parent pipeline.</param>
        /// <param name="name">Subpipeline name.</param>
        /// <param name="globalPolicy">Global delivery policy.</param>
        /// <returns>Created subpipeline.</returns>
        public static Subpipeline Create(Pipeline parent, string name = null, DeliveryPolicy globalPolicy = null)
        {
            return new Subpipeline(parent, name, globalPolicy);
        }

        /// <summary>
        /// Initialize subpipeline as a finite source component.
        /// </summary>
        /// <remarks>This is called by the parent subpipeline, if any.</remarks>
        /// <param name="onCompleted">Delegate to call when the subpipeline shuts down.</param>
        public void Initialize(Action onCompleted)
        {
            this.onCompleted = onCompleted;
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public override IDisposable RunAsync(ReplayDescriptor descriptor)
        {
            return this.RunAsync(descriptor, this.Scheduler.Clock);
        }

        /// <summary>
        /// Suspend pipeline.
        /// </summary>
        /// <remarks>Deactivate components.</remarks>
        public void Suspend()
        {
            this.Stop(false, false);
        }

        /// <summary>
        /// Stops the pipeline by disabling message passing between the pipeline components.
        /// The pipeline configuration is not changed and the pipeline can be restarted later.
        /// </summary>
        /// <param name="abandonPendingWorkitems">Abandons the pending work items</param>
        /// <param name="stopScheduler">Stops the scheduler.</param>
        protected override void Stop(bool abandonPendingWorkitems = false, bool stopScheduler = true)
        {
            base.Stop(abandonPendingWorkitems, false);
            if (this.hasSourceComponents)
            {
                this.onCompleted();
            }
        }

        private void Start(Pipeline parent)
        {
            this.hasSourceComponents = this.Components.Where(c => c.StateObject != this).Any(c => c.IsSource);
            this.RunAsync(parent.ReplayDescriptor); // TODO: allow specifying replay?
            if (!this.hasSourceComponents)
            {
                // no source components, so purely reactive
                this.onCompleted();
            }

            // emitters created from this subpipeline look like components, but should not prevent completion
            foreach (var c in this.Components.Where(c => c.StateObject == this))
            {
                this.NotifyCompleted(c);
            }
        }
    }
}
