// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.Collections.Generic;
    using System.Windows.Input;

    /// <summary>
    /// Provides information for creating context menu items with command bindings.
    /// </summary>
    public class ContextMenuItemInfo
    {
        private readonly string displayName;
        private readonly ICommand command;
        private readonly string iconSourcePath;
        private readonly object tag;
        private readonly bool isEnabled;
        private readonly object commandParameter;
        private readonly List<ContextMenuItemInfo> subItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuItemInfo"/> class.
        /// </summary>
        /// <param name="icon">The path to the icon for this context menu item.</param>
        /// <param name="displayName">The display name of this context menu item.</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="tag">An optional, user-defined tag to associate with the context menu item (default is null).</param>
        /// <param name="isEnabled">An optional variable indicating whether the context menu item is enabled (default is true).</param>
        /// <param name="commandParameter">The command parameter, or null if the command does not take a parameter.</param>
        public ContextMenuItemInfo(string icon, string displayName, ICommand command, object tag = null, bool isEnabled = true, object commandParameter = null)
        {
            this.displayName = displayName;
            this.command = command;
            this.iconSourcePath = icon;
            this.commandParameter = commandParameter;
            this.isEnabled = isEnabled;
            this.tag = tag;
            this.subItems = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuItemInfo"/> class correspond to a sub-menu.
        /// </summary>
        /// <param name="displayName">The display name for the submenu.</param>
        public ContextMenuItemInfo(string displayName)
        {
            this.displayName = displayName;
            this.command = null;
            this.iconSourcePath = null;
            this.commandParameter = null;
            this.isEnabled = true;
            this.tag = null;
            this.subItems = new ();
        }

        /// <summary>
        /// Gets the icon source path for the context menu item.
        /// </summary>
        public string IconSourcePath => this.iconSourcePath;

        /// <summary>
        /// Gets the display name for the context menu item.
        /// </summary>
        public string DisplayName => this.displayName;

        /// <summary>
        /// Gets the command.
        /// </summary>
        public ICommand Command => this.command;

        /// <summary>
        /// Gets the command parameter.
        /// </summary>
        public object CommandParameter => this.commandParameter;

        /// <summary>
        /// Gets a value indicating whether the context menu item is enabled.
        /// </summary>
        public bool IsEnabled => this.isEnabled;

        /// <summary>
        /// Gets the tag for the context menu item.
        /// </summary>
        public object Tag => this.tag;

        /// <summary>
        /// Gets the list of subitems.
        /// </summary>
        public List<ContextMenuItemInfo> SubItems => this.subItems;

        /// <summary>
        /// Gets a value indicating whether the context menu item corresponds to a menu with subitems.
        /// </summary>
        public bool HasSubItems => this.subItems != null;
    }
}
