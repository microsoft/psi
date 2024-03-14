// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System.Collections.Generic;
    using Microsoft.Psi;
    using StereoKit;

    /// <summary>
    /// Represents the configuration for the Diamond version of the <see cref="SigmaApp"/>.
    /// </summary>
    public class DiamondAppConfiguration : SigmaAppConfiguration<
        DiamondTask,
        DiamondPersistentState,
        DiamondUserInterfaceConfiguration,
        DiamondUserInterfaceState,
        DiamondUserInterfaceCommands>
    {
        /// <inheritdoc/>
        public override ISigmaUserInterface CreateSigmaUserInterface(Pipeline pipeline, Dictionary<string, Model> availableModels) =>
            new DiamondUserInterface(pipeline, this.UserInterfaceConfiguration, availableModels);
    }
}
