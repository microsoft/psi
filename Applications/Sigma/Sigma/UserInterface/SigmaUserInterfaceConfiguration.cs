// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// The configuration for the <see cref="SigmaUserInterfaceConfiguration"/> component.
    /// </summary>
    public class SigmaUserInterfaceConfiguration
    {
        /// <summary>
        /// Gets or sets the task panel user interface configuration.
        /// </summary>
        public TaskPanelUserInterfaceConfiguration TaskPanelUserInterfaceConfiguration { get; set; } = new ();

        /// <summary>
        /// Gets or sets the configuration for the bubble dialog user interface.
        /// </summary>
        public BubbleDialogUserInterfaceConfiguration BubbleDialogUserInterfaceConfiguration { get; set; } = new ();

        /// <summary>
        /// Gets or sets the configuration for the gem user interface.
        /// </summary>
        public GemUserInterfaceConfiguration GemUserInterfaceConfiguration { get; set; } = new ();

        /// <summary>
        /// Gets or sets the configuration for the timers user interface.
        /// </summary>
        public TimersUserInterfaceConfiguration TimersUserInterfaceConfiguration { get; set; } = new ();

        /// <summary>
        /// Gets or sets the configuration for the self-orienting text user interface.
        /// </summary>
        public TextBillboardsUserInterfaceConfiguration TextBillboardsUserInterfaceConfiguration { get; set; } = new ();
    }
}