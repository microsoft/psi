// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a metadata object for visualizing a stream.
    /// </summary>
    public class VisualizerMetadata
    {
        private VisualizerMetadata(
            Type dataType,
            Type visualizationObjectType,
            Type streamAdapterType,
            Type summarizerType,
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
            this.SummarizerType = summarizerType;
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
        /// Gets a value indicating whether this metadata represents a universal visualizer, i.e. if can be used with any data type including unknown types.
        /// </summary>
        public bool IsUniversalVisualizer { get; private set; }

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

        /// <summary>
        /// Gets the visualizer metadata whose data type is hierarchically closest to a stream's data type.
        /// Metadata objects that don't use an adapter are prioritized first.
        /// </summary>
        /// <param name="dataType">The data type of messages in the stream.</param>
        /// <param name="metadatas">A list of metadatas to select from.</param>
        /// <returns>The metadata whose data type is closest (hierarchically, prioritizing non-adapters) to the message data type.</returns>
        public static VisualizerMetadata GetClosestVisualizerMetadata(Type dataType, IEnumerable<VisualizerMetadata> metadatas)
        {
            // Get the collection of metadatas that don't use an adapter
            var nonAdaptedMetadatas = metadatas.Where(m => m.StreamAdapterType == null);

            // If there are any metadata objects that don't use an adapter, return the one
            // whose data type is closest to the message data type in the derivation hierarchy.
            if (nonAdaptedMetadatas.Any())
            {
                VisualizerMetadata metadata = GetVisualizerMetadataOfNearestBaseType(dataType, nonAdaptedMetadatas);
                if (metadata != default)
                {
                    return metadata;
                }
            }

            // Return the metadata object whose data type is closest
            // to the message data type in the derivation hierarchy.
            return GetVisualizerMetadataOfNearestBaseType(dataType, metadatas);
        }

        /// <summary>
        /// Instert a custom object adapter for viewing messages.
        /// </summary>
        /// <param name="visualizerMetadata">The visualizer metadata into which to insert the object adapter.</param>
        /// <param name="sourceDataType">The type of the source data.</param>
        /// <returns>A new visualizer metadata containing an object adapter.</returns>
        internal static VisualizerMetadata InsertObjectAdapter(VisualizerMetadata visualizerMetadata, Type sourceDataType)
        {
            // Clone the metadata
            VisualizerMetadata newMetadata = visualizerMetadata.DeepClone();

            // Create an object adapter from the node type to object so that the message
            // visualization object displays the actual data values for types that we understand.
            newMetadata.StreamAdapterType = typeof(ObjectAdapter<>).MakeGenericType(sourceDataType);

            return newMetadata;
        }

        /// <summary>
        /// Instert a custom object adapter for viewing message latencies.
        /// </summary>
        /// <param name="visualizerMetadata">The visualizer metadata into which to insert the object adapter.</param>
        /// <param name="sourceDataType">The type of the source data.</param>
        /// <returns>A new visualizer metadata containing an object adapter.</returns>
        internal static VisualizerMetadata InsertObjectToLatencyAdapter(VisualizerMetadata visualizerMetadata, Type sourceDataType)
        {
            // Clone the metadata
            VisualizerMetadata newMetadata = visualizerMetadata.DeepClone();

            // Create an object adapter from the node type to object so that the message
            // visualization object displays the actual data values for types that we understand.
            newMetadata.StreamAdapterType = typeof(ObjectToLatencyAdapter<>).MakeGenericType(sourceDataType);

            return newMetadata;
        }

        /// <summary>
        /// Creates a stream member adapter based on a specified visualizer metadata.  Any existing stream
        /// adapter in the given visualizer metadata will be preserved within the stream member adapter.
        /// </summary>
        /// <param name="visualizerMetadata">The visualizer metadata.</param>
        /// <param name="sourceDataType">The type of the stream's source data.</param>
        /// <returns>A new visualizer metadata containing a stream member adapter.</returns>
        internal static VisualizerMetadata CreateStreamMemberAdapter(VisualizerMetadata visualizerMetadata, Type sourceDataType)
        {
            // Clone the metadata
            var metadataClone = visualizerMetadata.DeepClone();

            // If the visualizer metadata already contains a stream adapter, create a stream member adapter that
            // encapsulates it, otherwise create a stream member adapter that adapts directly from the message
            // type to the member type.
            if (visualizerMetadata.StreamAdapterType != null)
            {
                IStreamAdapter existingStreamAdapter = (IStreamAdapter)Activator.CreateInstance(visualizerMetadata.StreamAdapterType);
                metadataClone.StreamAdapterType = typeof(StreamMemberAdapter<,,,>).MakeGenericType(sourceDataType, existingStreamAdapter.SourceType, visualizerMetadata.StreamAdapterType, existingStreamAdapter.DestinationType);
            }
            else
            {
                metadataClone.StreamAdapterType = typeof(StreamMemberAdapter<,>).MakeGenericType(sourceDataType, metadataClone.DataType);
            }

            return metadataClone;
        }

        /// <summary>
        /// Gets the visualizer metadata whose data type is hierarchially closest to a stream's data type.
        /// </summary>
        /// <param name="dataType">The data type of messages in the stream.</param>
        /// <param name="metadatas">A collection of metadatas to select from.</param>
        /// <returns>The metadata whose data type is hierarchically closest to the message data type.</returns>
        private static VisualizerMetadata GetVisualizerMetadataOfNearestBaseType(Type dataType, IEnumerable<VisualizerMetadata> metadatas)
        {
            Type type = dataType;
            do
            {
                VisualizerMetadata metadata = metadatas.FirstOrDefault(m => m.DataType == type);
                if (metadata != default)
                {
                    return metadata;
                }

                type = type.BaseType;
            }
            while (type != null);

            // The collection of metadata objects passed to this method should be guaranteed
            // to find a match.  If that failed, then there's a bug in our logic.
            throw new ApplicationException("No compatible metadata could be found for the message type");
        }

        private static void Create(
            List<VisualizerMetadata> metadatas,
            Type dataType,
            Type visualizationObjectType,
            VisualizationObjectAttribute visualizationObjectAttribute,
            VisualizationPanelTypeAttribute visualizationPanelTypeAttribute,
            StreamAdapterMetadata adapterMetadata)
        {
            var commandTitle = (visualizationObjectAttribute.IsUniversalVisualizer || adapterMetadata == null) ?
                $"Visualize {visualizationObjectAttribute.CommandText}" :
                $"Visualize as {visualizationObjectAttribute.CommandText}";

            metadatas.Add(new VisualizerMetadata(
                dataType,
                visualizationObjectType,
                adapterMetadata?.AdapterType,
                visualizationObjectAttribute.SummarizerType,
                commandTitle,
                visualizationObjectAttribute.IconSourcePath,
                visualizationPanelTypeAttribute.VisualizationPanelType,
                visualizationObjectAttribute.VisualizationFormatString,
                false,
                visualizationObjectAttribute.IsUniversalVisualizer));

            var inNewPanelCommandTitle = (visualizationObjectAttribute.IsUniversalVisualizer || adapterMetadata == null) ?
                $"Visualize {visualizationObjectAttribute.CommandText} in New Panel" :
                $"Visualize as {visualizationObjectAttribute.CommandText} in New Panel";

            metadatas.Add(new VisualizerMetadata(
                dataType,
                visualizationObjectType,
                adapterMetadata?.AdapterType,
                visualizationObjectAttribute.SummarizerType,
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
