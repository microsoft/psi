// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using Microsoft.Psi;

    /// <summary>
    /// Component that implements the interaction state manager for the Diamond version of the Sigma app.
    /// </summary>
    public class DiamondInteractionStateManager : SigmaInteractionStateManager<
        DiamondTask,
        DiamondConfiguration,
        DiamondInteractionModel,
        DiamondPersistentState,
        DiamondInteractionState,
        DiamondUserInterfaceState,
        DiamondUserInterfaceCommands>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondInteractionStateManager"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        public DiamondInteractionStateManager(
            Pipeline pipeline,
            DiamondConfiguration configuration,
            string name = nameof(DiamondInteractionStateManager))
            : base(
                  pipeline,
                  configuration,
                  new DiamondPersistentState(configuration.PersistentStateFilename),
                  new DiamondDialogStates.Intro(),
                  name)
        {
        }
    }
}