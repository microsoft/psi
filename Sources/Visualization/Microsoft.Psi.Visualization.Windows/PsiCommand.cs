// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS0067 // The event 'VisualizationCommand.CanExecuteChanged' is never used.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// Represents a visualization menu command.
    /// </summary>
    public class PsiCommand : ICommand
    {
        private Action action;
        private bool canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiCommand"/> class.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="canExecute">True if the command can be executed, otherwise false.</param>
        public PsiCommand(Action action, bool canExecute = true)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged
        {
            // event is never raised - no-op accessor prevents CS0067 warning
            add { }
            remove { }
        }

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return this.canExecute;
        }

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            this.action();
        }
    }
}
