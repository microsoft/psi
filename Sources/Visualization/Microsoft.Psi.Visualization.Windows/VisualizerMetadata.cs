// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a metadata object that describes a stream visualizer.
    /// </summary>
    public class VisualizerMetadata
    {
        private VisualizerMetadata(
            Type dataType,
            Type visualizationObjectType,
            Type streamAdapterType,
            object[] streamAdapterArguments,
            Type summarizerType,
            object[] summarizerArguments,
            string commandText,
            string iconSourcePath,
            VisualizationPanelType visualizationPanelType,
            string visualizationFormatString,
            bool isInNewPanel,
            bool isUniversalVisualizer)
        {
            this.DataType = dataType;
            this.VisualizationObjectType = visualizationObjectType;
            this.CommandText = commandText;
            this.VisualizationPanelType = visualizationPanelType;
            this.IconSourcePath = iconSourcePath;
            this.StreamAdapterType = streamAdapterType;
            this.StreamAdapterArguments = streamAdapterArguments;
            this.SummarizerType = summarizerType;
            this.SummarizerArguments = summarizerArguments;
            this.VisualizationFormatString = visualizationFormatString;
            this.IsInNewPanel = isInNewPanel;
            this.IsUniversalVisualizer = isUniversalVisualizer;
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
        /// Gets the stream adapter type.
        /// </summary>
        public Type StreamAdapterType { get; private set; }

        /// <summary>
        /// Gets the stream adapter arguments.
        /// </summary>
        public object[] StreamAdapterArguments { get; private set; }

        /// <summary>
        /// Gets the summarizer type.
        /// </summary>
        public Type SummarizerType { get; private set; }

        /// <summary>
        /// Gets the summarizer arguments.
        /// </summary>
        public object[] SummarizerArguments { get; private set; }

        /// <summary>
        /// Gets format for the name of the visualization object.
        /// </summary>
        public string VisualizationFormatString { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this metadata object is for a "visualize in new panel" command.
        /// </summary>
        public bool IsInNewPanel { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this metadata represents a universal visualizer, i.e. if can be used with any data type including unknown types.
        /// </summary>
        public bool IsUniversalVisualizer { get; private set; }

        /// <summary>
        /// Creates one or two visualizer metadatas depending on whether a "Visualize in new panel" icon source path was suplied by the visualization object.
        /// </summary>
        /// <param name="visualizationObjectType">The visualization object type.</param>
        /// <param name="summarizers">The dictionary of all known summarizers, keyed by type.</param>
        /// <param name="streamAdapters">The list of all known stream adapters.</param>
        /// <param name="logWriter">The log writer where errors should be written to.</param>
        /// <returns>A list of visualizer metadatas.</returns>
        public static List<VisualizerMetadata> Create(
            Type visualizationObjectType,
            Dictionary<Type, SummarizerMetadata> summarizers,
            List<StreamAdapterMetadata> streamAdapters,
            VisualizationLogWriter logWriter)
        {
            // Get the visualization object attribute
            var visualizationObjectAttribute = GetVisualizationObjectAttribute(visualizationObjectType, logWriter);
            if (visualizationObjectAttribute == null)
            {
                return null;
            }

            // Get the visualization panel type attribute
            var visualizationPanelTypeAttribute = GetVisualizationPanelTypeAttribute(visualizationObjectType, logWriter);
            if (visualizationPanelTypeAttribute == null)
            {
                return null;
            }

            // Get the message data type for the visualization object.  We will get nothing back
            // if visualizationObjectType does not ultimately derive from VisualizationObject<TData>
            var visualizationObjectDataType = GetVisualizationObjectDataType(visualizationObjectType, logWriter);
            if (visualizationObjectDataType == null)
            {
                return null;
            }

            // Get the summarizer type (if the visualizer uses a summarizer)
            var summarizerMetadata = default(SummarizerMetadata);
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

            var visualizers = new List<VisualizerMetadata>();

            // Add the visualization metadata using no adapter
            Create(visualizers, dataType, visualizationObjectType, visualizationObjectAttribute, visualizationPanelTypeAttribute, null);

            // Find all the adapters that have an output type that's the same as the visualization object's data type (or summarizer input type)
            var applicableStreamAdapters = streamAdapters.FindAll(a => dataType == a.OutputType || dataType.IsSubclassOf(a.OutputType));

            // Add the visualization metadata using each of the compatible adapters
            foreach (var streamAdapter in applicableStreamAdapters)
            {
                Create(visualizers, streamAdapter.InputType, visualizationObjectType, visualizationObjectAttribute, visualizationPanelTypeAttribute, streamAdapter);
            }

            return visualizers;
        }

        /// <summary>
        /// Generates a clone of the visualizer metadata with a different stream adapter type.
        /// </summary>
        /// <param name="streamAdapterType">The new stream adapter type.</param>
        /// <returns>A clone of the visualizer metadata with a different stream adapter type.</returns>
        internal VisualizerMetadata GetCloneWithNewStreamAdapterType(Type streamAdapterType)
        {
            var newMetadata = this.DeepClone();
            newMetadata.StreamAdapterType = streamAdapterType;

            // Change the command menu name to visualize as if it's not already that.
            if (streamAdapterType != null &&
                newMetadata.CommandText.StartsWith(ContextMenuName.Visualize) &&
                !newMetadata.CommandText.StartsWith(ContextMenuName.VisualizeAs))
            {
                newMetadata.CommandText = ContextMenuName.VisualizeAs + newMetadata.CommandText.Substring(ContextMenuName.Visualize.Length);
            }

            return newMetadata;
        }

        private static void Create(
            List<VisualizerMetadata> visualizers,
            Type dataType,
            Type visualizationObjectType,
            VisualizationObjectAttribute visualizationObjectAttribute,
            VisualizationPanelTypeAttribute visualizationPanelTypeAttribute,
            StreamAdapterMetadata streamAdapterMetadata)
        {
            // First, check for the case where the list of visualizer metadatas already contains a
            // visualizer with an adapter with the same type signature. If so, we need to generate
            // the command names by appending a specification of the adapter name.
            static bool HasSameAdapterTypes(Type streamAdapterType, Type otherStreamAdapterType)
            {
                if (streamAdapterType == null || otherStreamAdapterType == null)
                {
                    return false;
                }

                if (streamAdapterType.BaseType.IsGenericType && streamAdapterType.BaseType.GetGenericTypeDefinition() == typeof(StreamAdapter<,>))
                {
                    var streamAdapterGenericArguments = streamAdapterType.BaseType.GetGenericArguments();
                    if (otherStreamAdapterType.BaseType.IsGenericType && otherStreamAdapterType.BaseType.GetGenericTypeDefinition() == typeof(StreamAdapter<,>))
                    {
                        var otherStreamAdapterGenericArguments = otherStreamAdapterType.BaseType.GetGenericArguments();
                        return streamAdapterGenericArguments[0] == otherStreamAdapterGenericArguments[0] &&
                            streamAdapterGenericArguments[1] == otherStreamAdapterGenericArguments[1];
                    }
                }

                return false;
            }

            var sameAdapterVisualizers = visualizers.Where(m => HasSameAdapterTypes(m.StreamAdapterType, streamAdapterMetadata?.AdapterType));

            var commandTitle = default(string);
            var inNewPanelCommandTitle = default(string);

            // If there are other stream adapters with the same type signature
            if (sameAdapterVisualizers.Any())
            {
                // Then elaborate the name of the command to include the name of the stream adapter.
                var streamAdapterAttribute = streamAdapterMetadata.AdapterType.GetCustomAttribute<StreamAdapterAttribute>();
                var viaStreamAdapterName = string.IsNullOrEmpty(streamAdapterAttribute.Name) ? " (via unnamed adapter)" : $" (via {streamAdapterAttribute.Name} adapter)";
                commandTitle = $"{ContextMenuName.VisualizeAs} {visualizationObjectAttribute.CommandText}{viaStreamAdapterName}";
                inNewPanelCommandTitle = $"{ContextMenuName.VisualizeAs} {visualizationObjectAttribute.CommandText} in New Panel{viaStreamAdapterName}";

                // Also for all the matching adapters, elaborate the names of the corresponding commands
                foreach (var sameAdapterOtherVisualizerMetadata in sameAdapterVisualizers)
                {
                    var otherStreamAdapterAttribute = sameAdapterOtherVisualizerMetadata.StreamAdapterType.GetCustomAttribute<StreamAdapterAttribute>();
                    var viaOtherStreamAdapterName = string.IsNullOrEmpty(otherStreamAdapterAttribute.Name) ? " (via unnamed adapter)" : $" (via {otherStreamAdapterAttribute.Name} adapter)";

                    if (!sameAdapterOtherVisualizerMetadata.CommandText.EndsWith(viaOtherStreamAdapterName))
                    {
                        sameAdapterOtherVisualizerMetadata.CommandText += viaOtherStreamAdapterName;
                    }
                }
            }
            else
            {
                commandTitle = (visualizationObjectAttribute.IsUniversalVisualizer || streamAdapterMetadata == null) ?
                    $"{ContextMenuName.Visualize} {visualizationObjectAttribute.CommandText}" :
                    $"{ContextMenuName.VisualizeAs} {visualizationObjectAttribute.CommandText}";
                inNewPanelCommandTitle = (visualizationObjectAttribute.IsUniversalVisualizer || streamAdapterMetadata == null) ?
                    $"{ContextMenuName.Visualize} {visualizationObjectAttribute.CommandText} in New Panel" :
                    $"{ContextMenuName.VisualizeAs} {visualizationObjectAttribute.CommandText} in New Panel";
            }

            visualizers.Add(
                new VisualizerMetadata(
                    dataType,
                    visualizationObjectType,
                    streamAdapterMetadata?.AdapterType,
                    new object[] { },
                    visualizationObjectAttribute.SummarizerType,
                    new object[] { },
                    commandTitle,
                    visualizationObjectAttribute.IconSourcePath,
                    visualizationPanelTypeAttribute.VisualizationPanelType,
                    visualizationObjectAttribute.VisualizationFormatString,
                    false,
                    visualizationObjectAttribute.IsUniversalVisualizer));

            visualizers.Add(
                new VisualizerMetadata(
                    dataType,
                    visualizationObjectType,
                    streamAdapterMetadata?.AdapterType,
                    new object[] { },
                    visualizationObjectAttribute.SummarizerType,
                    new object[] { },
                    inNewPanelCommandTitle,
                    visualizationObjectAttribute.NewPanelIconSourcePath,
                    visualizationPanelTypeAttribute.VisualizationPanelType,
                    visualizationObjectAttribute.VisualizationFormatString,
                    true,
                    visualizationObjectAttribute.IsUniversalVisualizer));
        }

        private static VisualizationObjectAttribute GetVisualizationObjectAttribute(Type visualizationObjectType, VisualizationLogWriter logWriter)
        {
            VisualizationObjectAttribute visualizationObjectAttribute = visualizationObjectType.GetCustomAttribute<VisualizationObjectAttribute>();

            if (visualizationObjectAttribute == null)
            {
                logWriter.WriteError($"Visualization object {0} could not be loaded because it is not decorated with a {nameof(VisualizationObjectAttribute)}.", visualizationObjectType.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(visualizationObjectAttribute.CommandText))
            {
                logWriter.WriteError($"Visualization object {0} could not be loaded because its {nameof(VisualizationObjectAttribute)} does not specify a {nameof(VisualizationObjectAttribute.CommandText)} property.", visualizationObjectType.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(visualizationObjectAttribute.IconSourcePath) && string.IsNullOrWhiteSpace(visualizationObjectAttribute.NewPanelIconSourcePath))
            {
                logWriter.WriteError($"Visualization object {0} could not be loaded because its {nameof(VisualizationObjectAttribute)} does not specify either an {nameof(VisualizationObjectAttribute.IconSourcePath)} property or a {nameof(VisualizationObjectAttribute.NewPanelIconSourcePath)} property.", visualizationObjectType.Name);
                return null;
            }

            if (!visualizationObjectAttribute.VisualizationFormatString.Contains(VisualizationObjectAttribute.DefaultVisualizationFormatString))
            {
                logWriter.WriteError($"Visualization object {0} could not be loaded because its {nameof(VisualizationObjectAttribute)} has an invalid value for the {nameof(VisualizationObjectAttribute.DefaultVisualizationFormatString)} property.", visualizationObjectType.Name);
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
