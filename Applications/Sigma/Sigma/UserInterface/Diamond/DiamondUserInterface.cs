// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using StereoKit;

    /// <summary>
    /// Implements a component for the user interface elements of the Diamond version of the Sigma app.
    /// application.
    /// </summary>
    public class DiamondUserInterface : SigmaUserInterface<
        DiamondTask,
        DiamondUserInterfaceState,
        DiamondPersistentState,
        DiamondUserInterfaceCommands>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondUserInterface"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="availableModels">A dictionary of available models.</param>
        /// <param name="name">An optional name for the component.</param>
        public DiamondUserInterface(Pipeline pipeline, DiamondUserInterfaceConfiguration configuration, Dictionary<string, Model> availableModels, string name = null)
            : base(pipeline, configuration, availableModels, name ?? nameof(DiamondUserInterface))
        {
        }
    }
}
