// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a metadata object for visualizing a stream.
    /// </summary>
    public class VisualizerMetadata
    {
        private VisualizerMetadata(Type dataType, Type visualizationObjectType, Type streamAdapterType, Type summarizerType, string commandText, string iconSourcePath, VisualizationPanelType visualizationPanelType, string visualizationFormatString, bool isInNewPanel, bool isBelowSeparator)
        {
            this.DataType = dataType;
            this.VisualizationObjectType = visualizationObjectType;
            this.CommandText = commandText;
            this.VisualizationPanelType = visualizationPanelType;
            this.IconSourcePath = iconSourcePath;
            this.StreamAdapterType = streamAdapterType;
            this.SummarizerType = summarizerType;
            this.VisualizationFormatString = visualizationFormatString;
            this.IsInNewPanel = isInNewPanel;
            this.IsBelowSeparator = isBelowSeparator;
        }

        /// <summary>
        /// Gets the type of data in the stream.
        /// </summary>
        public Type DataType { get; private set; }

        /// <summary>
        /// Gets the visualization object type.
        /// </summary>
        public Type VisualizationObjectType { get; private set; }

        /// <summary>
        /// Gets the text to display in the command menu.
        /// </summary>
        public string CommandText { get; private set; }

        /// <summary>
        /// Gets the visualization panel type.
        /// </summary>
        public VisualizationPanelType VisualizationPanelType { get; private set; }

        /// <summary>
        /// Gets the path to the icon to display next to the command menu.
        /// </summary>
        public string IconSourcePath { get; private set; }

        /// <summary>
        /// Gets the stream adapter.
        /// </summary>
        public Type StreamAdapterType { get; private set; }

        /// <summary>
        /// Gets the summarizer.
        /// </summary>
        public Type SummarizerType { get; private set; }

        /// <summary>
        /// Gets format for the name of the visualization object.
        /// </summary>
        public string VisualizationFormatString { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this metadata object is for a "visualize in new panel" command.
        /// </summary>
        public bool IsInNewPanel { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this metadata object should be placed below the separator in a menu.
        /// </summary>
        public bool IsBelowSeparator { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this metadata object should be placed above the separator in a menu.
        /// </summary>
        public bool IsAboveSeparator => !this.IsBelowSeparator;

        /// <summary>
        /// Creates one or two visualizer metadatas depending on whether a "Visualize in new panel" icon source path was suplied by the visualization object.
        /// </summary>
        /// <param name="visualizationObjectType">The visualization object type.</param>
        /// <param name="summarizers">The list of known summarizers.</param>
        /// <param name="dataAdapters">The list of known data adapters.</param>
        /// <param name="logWriter">The log writer where errors should be written to.</param>
        /// <returns>A list of visualizer metadatas.</returns>
        public static List<VisualizerMetadata> Create(Type visualizationObjectType, Dictionary<Type, SummarizerMetadata> summarizers, List<StreamAdapterMetadata> dataAdapters, VisualizationLogWriter logWriter)
        {
            // Get the visualization object attribute
            VisualizationObjectAttribute visualizationObjectAttribute = GetVisualizationObjectAttribute(visualizationObjectType, logWriter);
            if (visualizationObjectAttribute == null)
            {
                return null;
            }

            // Get the visualization panel type attribute
            VisualizationPanelTypeAttribute visualizationPanelTypeAttribute = GetVisualizationPanelTypeAttribute(visualizationObjectType, logWriter);
            if (visualizationPanelTypeAttribute == null)
            {
                return null;
            }

            // Get the message data type for the visualization object.  We will get nothing back
            // if visualizationObjectType does not ultimately derive from VisualizationObject<TData>
            Type visualizationObjectDataType = GetVisualizationObjectDataType(visualizationObjectType, logWriter);
            if (visualizationObjectDataType == null)
            {
                return null;
            }

            // Get the summarizer type (if the visualizer uses a summarizer)
            SummarizerMetadata summarizerMetadata = null;
            if (visualizationObjectAttribute.SummarizerType != null)
            {
                if (summarizers.ContainsKey(visualizationObjectAttribute.SummarizerType))
                {
                    summarizerMetadata = summarizers[visualizationObjectAttribute.SummarizerType];
                }
                else
                {
                    logWriter.WriteError("Unable to load visualizer {0} because it relies on summarizer {1} which could not be found.", visualizationObjectType.Name, visualizationObjectAttribute.SummarizerType.Name);
                    return null;
                }
            }

            // If we have a summarizer, make sure its output type matches the input type of the visualization object
            if ((summarizerMetadata != null) && (summarizerMetadata.OutputType != visualizationObjectDataType))
            {
                logWriter.WriteError(
                    "Unable to load visualizer {0} with summarizer {1} because the output type of the summarizer ({2}) does not match the input type of the visualizer ({3}) .",
                    visualizationObjectType.Name,
                    summarizerMetadata.SummarizerType.Name,
                    summarizerMetadata.OutputType.Name,
                    visualizationObjectDataType.Name);

                return null;
            }

            // Work out the input (message) data type:
            //
            // 1) If there's a summarizer, use the summarizer's input type
            // 2) Otherwise, use the visualization object's data type
            Type dataType = summarizerMetadata != null ? summarizerMetadata.InputType : visualizationObjectDataType;

            List<VisualizerMetadata> metadatas = new List<VisualizerMetadata>();

            // Add the visualization metadata using no adapter
            Create(metadatas, dataType, visualizationObjectType, visualizationObjectAttribute, visualizationPanelTypeAttribute, null);

            // Find all the adapters that have an output type that's the same as the visualization object's data type (or summarizer input type)
            List<StreamAdapterMetadata> usableAdapters = dataAdapters.FindAll(a => dataType == a.OutputType || dataType.IsSubclassOf(a.OutputType));

            // Add the visualization metadata using each of the compatible adapters
            foreach (StreamAdapterMetadata adapterMetadata in usableAdapters)
            {
                Create(metadatas, adapterMetadata.InputType, visualizationObjectType, visualizationObjectAttribute, visualizationPanelTypeAttribute, adapterMetadata);
            }

            return metadatas;
        }

        private static void Create(List<VisualizerMetadata> metadatas, Type dataType, Type visualizationObjectType, VisualizationObjectAttribute visualizationObjectAttribute, VisualizationPanelTypeAttribute visualizationPanelTypeAttribute, StreamAdapterMetadata adapterMetadata)
        {
            // Create the metadata for the "visualize" menu command if required
            if (!string.IsNullOrWhiteSpace(visualizationObjectAttribute.IconSourcePath))
            {
                metadatas.Add(new VisualizerMetadata(dataType, visualizationObjectType, adapterMetadata?.AdapterType, visualizationObjectAttribute.SummarizerType, visualizationObjectAttribute.CommandText, visualizationObjectAttribute.IconSourcePath, visualizationPanelTypeAttribute.VisualizationPanelType, visualizationObjectAttribute.VisualizationFormatString, false, visualizationObjectAttribute.IsBelowSeparator));
            }

            // Create the metadata for the "visualize in new panel" menu command if required
            if (!string.IsNullOrWhiteSpace(visualizationObjectAttribute.NewPanelIconSourcePath))
            {
                metadatas.Add(new VisualizerMetadata(dataType, visualizationObjectType, adapterMetadata?.AdapterType, visualizationObjectAttribute.SummarizerType, visualizationObjectAttribute.CommandText + " in New Panel", visualizationObjectAttribute.NewPanelIconSourcePath, visualizationPanelTypeAttribute.VisualizationPanelType, visualizationObjectAttribute.VisualizationFormatString, true, visualizationObjectAttribute.IsBelowSeparator));
            }
        }

        private static VisualizationObjectAttribute GetVisualizationObjectAttribute(Type visualizationObjectType, VisualizationLogWriter logWriter)
        {
            VisualizationObjectAttribute visualizationObjectAttribute = visualizationObjectType.GetCustomAttribute<VisualizationObjectAttribute>();

            if (visualizationObjectAttribute == null)
            {
                logWriter.WriteError("Visualization object {0} could not be loaded because it is not decorated with a VisualizationObjectAttribute", visualizationObjectType.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(visualizationObjectAttribute.CommandText))
            {
                logWriter.WriteError("Visualization object {0} could not be loaded because its VisualizationObjectAttribute does not specify a Text property", visualizationObjectType.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(visualizationObjectAttribute.IconSourcePath) && string.IsNullOrWhiteSpace(visualizationObjectAttribute.NewPanelIconSourcePath))
            {
                logWriter.WriteError("Visualization object {0} could not be loaded because its VisualizationObjectAttribute does not specify either an IconSourcePath property or a NewPanelIconSourcePath property", visualizationObjectType.Name);
                return null;
            }

            if (!visualizationObjectAttribute.VisualizationFormatString.Contains(VisualizationObjectAttribute.DefaultVisualizationFormatString))
            {
                logWriter.WriteError("Visualization object {0} could not be loaded because its VisualizationObjectAttribute has an invalid value for the VisualizationFormatString property", visualizationObjectType.Name);
                return null;
            }

            return visualizationObjectAttribute;
        }

        private static VisualizationPanelTypeAttribute GetVisualizationPanelTypeAttribute(Type visualizationObjectType, VisualizationLogWriter logWriter)
        {
            VisualizationPanelTypeAttribute visualizationPanelTypeAttribute = visualizationObjectType.GetCustomAttribute<VisualizationPanelTypeAttribute>();

            if (visualizationPanelTypeAttribute == null)
            {
                logWriter.WriteError("Visualization object {0} could not be loaded because it is not decorated with a VisualizationPanelTypeAttribute", visualizationObjectType.Name);
                return null;
            }

            return visualizationPanelTypeAttribute;
        }

        private static Type GetVisualizationObjectDataType(Type visualizationObjectType, VisualizationLogWriter logWriter)
        {
            // Look into the visualization object's base types until we find VisualizationObject<TData>
            // (whose base type is in turn VisualizationObject).
            Type type = visualizationObjectType;
            while ((type != null) && (type.BaseType != typeof(VisualizationObject)))
            {
                type = type.BaseType;
            }

            // Make sure we ultimately derive from VisualizationObject<TData>
            if (type == null)
            {
                logWriter.WriteError("Could not load visualization object {0} because it does not ultimately derive from VisualizationObject", visualizationObjectType.Name);
                return null;
            }
            else
            {
                // The one and only type argument is the data type of the visualization object
                return type.GenericTypeArguments[0];
            }
        }
    }
}
