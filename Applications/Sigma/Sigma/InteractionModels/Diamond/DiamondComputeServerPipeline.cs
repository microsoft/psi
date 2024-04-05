// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Composite component that implements the compute server pipeline for the Diamond version of the Sigma app.
    /// </summary>
    public class DiamondComputeServerPipeline : SigmaComputeServerPipeline<
        DiamondTask,
        DiamondConfiguration,
        DiamondInteractionModel,
        DiamondInteractionStateManager,
        DiamondPersistentState,
        DiamondInteractionState,
        DiamondUserInterfaceState,
        DiamondUserInterfaceCommands>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondComputeServerPipeline"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="hololensStreams">The set of hololens streams.</param>
        /// <param name="userInterfaceStreams">The set of user interface streams.</param>
        /// <param name="precomputedStreams">The set of precomputed streams.</param>
        public DiamondComputeServerPipeline(
            Pipeline pipeline,
            DiamondConfiguration configuration,
            HoloLensStreams hololensStreams,
            UserInterfaceStreams<DiamondUserInterfaceState> userInterfaceStreams,
            PrecomputedStreams precomputedStreams = null)
            : base(pipeline, configuration, hololensStreams, userInterfaceStreams, precomputedStreams)
        {
        }

        /// <inheritdoc/>
        public override DiamondInteractionStateManager CreateInteractionStateManager()
            => new (this, this.Configuration);
    }
}
