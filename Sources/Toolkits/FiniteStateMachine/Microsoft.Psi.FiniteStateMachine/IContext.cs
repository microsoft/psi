// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.FiniteStateMachine
{
    /// <summary>
    /// The context threaded through the state machine.
    /// </summary>
    /// <remarks>
    /// Inputs to the system should be segregated into the Input property. These are values
    /// which are updated by code outside of the FSM (in response to messages for example).
    /// The FSM should not have side-effects. Instead, values meant to drive external code to
    /// cause side-effecting actions should be segregated into the Output property.
    /// All other values, which should be the concern only to the internals of the FSM (referenced
    /// by enter/transition/exit delegates for example, should be internal properties.
    /// </remarks>
    /// <typeparam name="TInput">Inputs to the FSM from the outside world.</typeparam>
    /// <typeparam name="TOutput">Output from the FSM to the outside world.</typeparam>
    public interface IContext<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets inputs to the FSM from the outside world.
        /// </summary>
        TInput Input { get; set; }

        /// <summary>
        /// Gets or sets output from the FSM to the outside world.
        /// </summary>
        TOutput Output { get; set; }
    }
}
