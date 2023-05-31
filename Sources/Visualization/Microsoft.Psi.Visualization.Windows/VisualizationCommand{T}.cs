// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS0067 // The event 'VisualizationCommand.CanExecuteChanged' is never used.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// Represents a command.
    /// </summary>
    /// <typeparam name="T">The type of the command argument.</typeparam>
    public class VisualizationCommand<T> : ICommand
    {
        private readonly Action<T> action;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationCommand{T}"/> class.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public VisualizationCommand(Action<T> action)
        {
            this.action = action;
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged
        {
            // event is never raised - no-op accessor prevents CS0067 warning
            add { }
            remove { }
        }

        /// <inheritdoc />
        public bool CanExecute(object parameter) => true;

        /// <inheritdoc />
        public void Execute(object parameter)
        {
            if (parameter is T value)
            {
                this.action(value);
            }
        }
    }
}