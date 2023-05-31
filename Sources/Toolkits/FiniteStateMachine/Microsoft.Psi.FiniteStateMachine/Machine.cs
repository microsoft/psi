// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.FiniteStateMachine
{
    /// <summary>
    /// The Machine is an abstract class expected to be implemented with app-specific methods
    /// representing inputs to the system and setting up the initial states and transitions
    /// of the system. Machine contains the current State and (for convenience) the current
    /// context. A single Update() method is expected to be called with context changes
    /// due to external inputs.
    /// </summary>
    /// <typeparam name="TInput">Inputs to the FSM from the outside world.</typeparam>
    /// <typeparam name="TOutput">Output from the FSM to the outside world.</typeparam>
    /// <typeparam name="TContext">Concrete context type implementing IContext.</typeparam>
    public abstract class Machine<TInput, TOutput, TContext>
        where TContext : IContext<TInput, TOutput>
    {
        /// <summary>
        /// Gets current state.
        /// </summary>
        protected State<TContext> State { get; private set; }

        /// <summary>
        /// Gets or sets current context.
        /// </summary>
        protected TContext Context { get; set; }

        /// <summary>
        /// Update the state machine.
        /// </summary>
        /// <param name="input">Input portion of context.</param>
        /// <returns>Output portion of context.</returns>
        public virtual TOutput Update(TInput input)
        {
            this.Context.Input = input;
            TContext newContext;
            this.State = this.State.Updated(this.Context, out newContext);
            this.ContextUpdated(this.State.Name, newContext);

            this.Context = newContext;
            return this.Context.Output;
        }

        /// <summary>
        /// Initializes Machine with initial context and state.
        /// </summary>
        /// <remarks>Causes transition to initial state; firing onEnter if appropriate.</remarks>
        /// <param name="context">Initial context.</param>
        /// <param name="state">Initial state.</param>
        protected void Initialize(TContext context, State<TContext> state)
        {
            this.Context = context;
            this.State = new State<TContext>("Init");
            this.State.AddTransition(_ => true, state);
            this.Update(context.Input);
        }

        /// <summary>
        /// Report state changes.
        /// </summary>
        /// <param name="state">Current state name.</param>
        /// <param name="context">Current context.</param>
        protected virtual void ContextUpdated(string state, TContext context)
        {
        }
    }
}