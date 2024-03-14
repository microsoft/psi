// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    /// <summary>
    /// Defines a base class for interaction models.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TInteractionState">The type of the interaction state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    /// <remarks>
    /// The interaction model provides access to configuration, persistent state, interaction state,
    /// and user interface state. In addition a virtual method constructs the user interface commands
    /// corresponding to the current interaction state.
    /// </remarks>
    public abstract class InteractionModel<TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        where TConfiguration : class, new()
        where TPersistentState : class, new()
        where TInteractionState : class, new()
        where TUserInterfaceState : class, new()
        where TUserInterfaceCommands : class, new()
    {
        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public TConfiguration Configuration { get; set; } = new TConfiguration();

        /// <summary>
        /// Gets or sets the persistent state.
        /// </summary>
        public TPersistentState PersistentState { get; set; } = new TPersistentState();

        /// <summary>
        /// Gets or sets the interaction state.
        /// </summary>
        public TInteractionState InteractionState { get; set; } = new TInteractionState();

        /// <summary>
        /// Gets or sets the user interface state.
        /// </summary>
        public TUserInterfaceState UserInterfaceState { get; set; } = new TUserInterfaceState();

        /// <summary>
        /// Computes the user interface commands.
        /// </summary>
        /// <param name="userInterfaceCommands">The user interface commands.</param>
        public abstract void ComputeUserInterfaceCommands(TUserInterfaceCommands userInterfaceCommands);

        /// <summary>
        /// Virtual method called when the interaction model is closed.
        /// </summary>
        public virtual void OnClose()
        {
        }
    }
}
