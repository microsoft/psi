// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.Linq;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a user interface for the text billboards.
    /// </summary>
    public class TextBillboardsUserInterface
    {
        private readonly TextBillboardsUserInterfaceConfiguration configuration;
        private readonly Dictionary<int, TextBillboardUserInterface> textBillboards = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBillboardsUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The text billboards user interface configuration.</param>
        public TextBillboardsUserInterface(TextBillboardsUserInterfaceConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Updates the timers user interface.
        /// </summary>
        /// <param name="commands">The timer user interface commands.</param>
        public void Update(List<TextBillboardUserInterfaceCommand> commands)
        {
            this.textBillboards.Update(
                Enumerable.Range(0, commands.Count),
                createKey: i => new TextBillboardUserInterface(this.configuration, commands[i].Location, commands[i].Text, $"TextDisplay[{i}]"));
            for (int i = 0; i < commands.Count; i++)
            {
                this.textBillboards[i].Update(commands[i].Text, commands[i].Location);
            }
        }

        /// <summary>
        /// Renders the user interface.
        /// </summary>
        /// <param name="renderer">The renderer to use.</param>
        public void Render(Renderer renderer)
        {
            // Render the text billboards
            foreach (var textBillboard in this.textBillboards.Values)
            {
                textBillboard.Render(renderer, null);
            }
        }
    }
}