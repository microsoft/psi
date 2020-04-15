// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CS0067 // The event 'TypeKeyedActionCommand.CanExecuteChanged' is never used.

namespace Microsoft.Psi.Visualization.Commands
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// Base class for context menu commands based on stream types.
    /// </summary>
    public abstract class TypeKeyedActionCommand : ICommand
    {
        private string displayName;
        private Type typeKey;
        private string icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeKeyedActionCommand"/> class.
        /// </summary>
        /// <param name="displayName">Name displayed in menu.</param>
        /// <param name="typeKey">Type key of the command.</param>
        /// <param name="icon">The path to the icon to display next to the menu.</param>
        public TypeKeyedActionCommand(string displayName, Type typeKey, string icon)
        {
            this.displayName = displayName;
            this.typeKey = typeKey;
            this.icon = icon;
        }

        /// <inheritdoc />
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => this.displayName;

        /// <summary>
        /// Gets the icon for the command.
        /// </summary>
        public string Icon => this.icon;

        /// <summary>
        /// Gets the type key.
        /// </summary>
        public Type TypeKey => this.typeKey;

        /// <inheritdoc />
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <inheritdoc />
        public abstract void Execute(object parameter);
    }
}
