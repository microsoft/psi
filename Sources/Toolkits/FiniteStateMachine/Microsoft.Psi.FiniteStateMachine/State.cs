// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.FiniteStateMachine
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// States are the classic FSM states. They’re given a name (for logging/debugging)
    /// and have an (optional) OnEnter and OnExit function called as things change.
    /// </summary>
    /// <typeparam name="TContext">Context type.</typeparam>
    public sealed class State<TContext>
    {
        /// <summary>
        /// Enter event handler.
        /// </summary>
        private readonly Func<TContext, TContext> enterFn;

        /// <summary>
        /// Exit event handler.
        /// </summary>
        private readonly Func<TContext, TContext> exitFn;

        /// <summary>
        /// List of transitions.
        /// </summary>
        private List<Transition> transitions = new List<Transition>();

        /// <summary>
        /// Initializes a new instance of the <see cref="State{TContext}"/> class.
        /// </summary>
        /// <param name="name">State name (for debugging).</param>
        /// <param name="enterFn">On enter callback function (optional).</param>
        /// <param name="exitFn">On exit callback function (optional).</param>
        public State(string name, Func<TContext, TContext> enterFn = null, Func<TContext, TContext> exitFn = null)
        {
            this.Name = name;
            this.enterFn = enterFn;
            this.exitFn = exitFn;
        }

        /// <summary>
        /// Gets state name (for debugging).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// String representation of state.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Adds a transition to the state.
        /// </summary>
        /// <param name="condition">Function to evaluate condition.</param>
        /// <param name="to">Target state.</param>
        /// <param name="transitionFn">Function called on transition.</param>
        public void AddTransition(Func<TContext, bool> condition, State<TContext> to, Func<TContext, TContext> transitionFn = null)
        {
            this.transitions.Add(new Transition(condition, to, transitionFn));
        }

        /// <summary>
        /// Function to call whenever updates on state have occurred.
        /// </summary>
        /// <param name="context">Instance of context.</param>
        /// <param name="updated">Resulting updated context.</param>
        /// <returns>Resulting new state.</returns>
        public State<TContext> Updated(TContext context, out TContext updated)
        {
            var tran = (from t in this.transitions where t.Condition(context) select t).FirstOrDefault();
            if (tran == null)
            {
                // no change
                updated = context;
                return this;
            }

            if (tran.To != this)
            {
                Trace.WriteLine($"State transition {this.Name} -> {tran.To.Name} ({DateTime.UtcNow})");
            }

            updated = tran.Traverse(this.exitFn == null ? context : this.exitFn(context));
            return tran.To;
        }

        /// <summary>
        /// Transition class.
        /// </summary>
        private sealed class Transition
        {
            /// <summary>
            /// Gets function called to perform the transition.
            /// </summary>
            private readonly Func<TContext, TContext> transitionFn;

            /// <summary>
            /// Initializes a new instance of the <see cref="Transition"/> class.
            /// </summary>
            /// <param name="condition">Condition to fulfill to traverse the transition.</param>
            /// <param name="to">Target state.</param>
            /// <param name="transitionFn">Event handler (optional).</param>
            public Transition(Func<TContext, bool> condition, State<TContext> to, Func<TContext, TContext> transitionFn = null)
            {
                this.Condition = condition;
                this.To = to;
                this.transitionFn = transitionFn;
            }

            /// <summary>
            /// Gets function to evaluate transition condition.
            /// </summary>
            public Func<TContext, bool> Condition { get; private set; }

            /// <summary>
            /// Gets target state of the transition.
            /// </summary>
            public State<TContext> To { get; private set; }

            /// <summary>
            /// Traverses the transition.
            /// </summary>
            /// <param name="context">Context instance.</param>
            /// <returns>Resulting context.</returns>
            public TContext Traverse(TContext context)
            {
                TContext ctx = this.transitionFn == null ? context : this.transitionFn(context);
                return this.To.enterFn == null ? ctx : this.To.enterFn(ctx);
            }
        }
    }
}
