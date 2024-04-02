// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.MixedReality.Applications;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a user interface for the timers.
    /// </summary>
    public class TimersUserInterface
    {
        private readonly TimersUserInterfaceConfiguration configuration;
        private readonly Dictionary<Guid, TimerUserInterface> timerDisplays = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimersUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The timers user interface configuration.</param>
        public TimersUserInterface(TimersUserInterfaceConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Updates the timers user interface.
        /// </summary>
        /// <param name="commands">The timer user interface commands.</param>
        public void Update(Dictionary<Guid, TimerUserInterfaceCommand> commands)
        {
            this.timerDisplays.Update(
                commands.Keys,
                createKey: guid => new TimerUserInterface(this.configuration, commands[guid].Location, commands[guid].ExpiryDateTime));
            foreach (var timerDisplay in this.timerDisplays.Values)
            {
                timerDisplay.Update();
            }
        }

        /// <summary>
        /// Handles user inputs.
        /// </summary>
        /// <param name="userState">The user state.</param>
        public void HandleUserInputs(UserState userState)
        {
            foreach (var timerDisplay in this.timerDisplays.Values)
            {
                timerDisplay.HandleUserInputs(userState);
            }
        }

        /// <summary>
        /// Renders the user interface.
        /// </summary>
        /// <param name="renderer">The renderer to use.</param>
        public void Render(Renderer renderer)
        {
            // Render the timer displays
            foreach (var timerDisplay in this.timerDisplays.Values)
            {
                timerDisplay.Render(renderer, null);
            }
        }
    }
}