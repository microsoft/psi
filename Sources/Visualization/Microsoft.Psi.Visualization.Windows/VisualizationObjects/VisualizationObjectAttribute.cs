// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using SourcePaths = Microsoft.Psi.Visualization.IconSourcePath;

    /// <summary>
    /// Represents a visualization object attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VisualizationObjectAttribute : Attribute
    {
        /// <summary>
        /// The default visualization format string.
        /// </summary>
        public const string DefaultVisualizationFormatString = "%StreamName%";

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObjectAttribute"/> class.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="summarizerType">The type of summarizer the visualization object will use, or null if the visualization object does not create stream summaries.</param>
        /// <param name="iconSourcePath">The path to the command menu's icon.</param>
        /// <param name="newPanelIconSourcePath">The path to the show in new panel menu's icon.</param>
        /// <param name="visualizationFormatString">The format of the name that the visualization will be given in the visualizers tab.</param>
        /// <param name="isUniversalVisualizer">True if the visualization object can be used to visualize messages of any type, even when the type is unknown.</param>
        public VisualizationObjectAttribute(string commandText, Type summarizerType = null, string iconSourcePath = SourcePaths.Stream, string newPanelIconSourcePath = SourcePaths.StreamInPanel, string visualizationFormatString = DefaultVisualizationFormatString, bool isUniversalVisualizer = false)
        {
            this.CommandText = commandText;
            this.SummarizerType = summarizerType;
            this.IconSourcePath = iconSourcePath;
            this.NewPanelIconSourcePath = newPanelIconSourcePath;
            this.VisualizationFormatString = visualizationFormatString;
            this.IsUniversalVisualizer = isUniversalVisualizer;
        }

        /// <summary>
        /// Gets the text of the command.
        /// </summary>
        public string CommandText { get; private set; }

        /// <summary>
        /// Gets the type of summarizer the visualization object will use, or null if the visualization object does not create stream summaries.
        /// </summary>
        public Type SummarizerType { get; private set; }

        /// <summary>
        /// Gets the path to the menu's icon.
        /// </summary>
        public string IconSourcePath { get; private set; }

        /// <summary>
        /// Gets the path to the show in new panel menu's icon.
        /// </summary>
        public string NewPanelIconSourcePath { get; private set; }

        /// <summary>
        /// Gets the name that the visualization will be given in the visualizers tab.
        /// </summary>
        public string VisualizationFormatString { get; private set; }

        /// <summary>
        /// Gets a value indicating whether visualization object can be used to visualize any message type, even an unknown message type.
        /// </summary>
        public bool IsUniversalVisualizer { get; private set; }
    }
}