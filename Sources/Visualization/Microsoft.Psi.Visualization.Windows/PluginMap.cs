// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Navigation;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Tasks;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents mappings for known visualizers, adapters, summarizers and stream readers.
    /// </summary>
    public class PluginMap
    {
        // The list of default assemblies in which the plugin mapper will search for visualization objects, adapter, and summarizers.
        private readonly string[] defaultAssemblies = new[]
        {
            "Microsoft.Psi.dll",
            "Microsoft.Psi.Audio.dll",
            "Microsoft.Psi.Data.dll",
            "Microsoft.Psi.Visualization.Windows.dll",
        };

        // The list of summarizers that were found during discovery.
        private readonly Dictionary<Type, SummarizerMetadata> summarizers = new Dictionary<Type, SummarizerMetadata>();

        // The list of stream adapters that were found during discovery.
        private readonly List<StreamAdapterMetadata> streamAdapters = new List<StreamAdapterMetadata>();

        // The list of batch processing tasks that were found during discovery.
        private readonly List<BatchProcessingTaskMetadata> batchProcessingTasks = new List<BatchProcessingTaskMetadata>();

        // The list of visualization objects that were found during discovery.
        private readonly List<VisualizerMetadata> visualizers = new List<VisualizerMetadata>();

        // This list of stream readers that were found during discovery.
        private readonly List<(string Name, string Extension, Type ReaderType)> streamReaders = new List<(string Name, string Extension, Type ReaderType)>();

        /// <summary>
        /// Gets a value indicating whether or not Initialize() has been called.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        private IEnumerable<(string Name, string Extension, Type ReaderType)> StreamReaders
        {
            get
            {
                return Enumerable.Concat(
                    new[] { ("Psi Store", ".psi", typeof(PsiStoreStreamReader)) },
                    this.streamReaders.OrderBy(sr => sr.Name));
            }
        }

        /// <summary>
        /// Initializes the plugin map.
        /// </summary>
        /// <param name="additionalAssembliesToSearch">A list of assemblies to search for plugins in addition to the default assembly list.
        /// If no additional assemblies contain plugins, this parameter can be null or an empty list.</param>
        /// <param name="loadLogFilename">The full path to the log file to be created and written to while initializing.</param>
        public void Initialize(List<string> additionalAssembliesToSearch, string loadLogFilename)
        {
            // Append the additional assemblies to the list of default assemblies to search for plugins
            List<string> assembliesToSearch = this.defaultAssemblies.ToList();
            if ((additionalAssembliesToSearch != null) && (additionalAssembliesToSearch.Count > 0))
            {
                assembliesToSearch.AddRange(additionalAssembliesToSearch);
            }

            // Load all the visualizers, summarizers, stream adapters, stream readers, and batch processing tasks
            this.DiscoverPlugins(assembliesToSearch, loadLogFilename);

            this.IsInitialized = true;
        }

        /// <summary>
        /// Get a list of visualization panel types that are compatible with given a visualization panel.
        /// </summary>
        /// <param name="visualizationPanel">A visualization panel.</param>
        /// <returns>The list of compatible visualization panel types.</returns>
        public List<VisualizationPanelType> GetCompatiblePanelTypes(VisualizationPanel visualizationPanel)
        {
            this.EnsureInitialized();

            List<VisualizationPanelType> results = new List<VisualizationPanelType>();

            if (visualizationPanel is TimelineVisualizationPanel)
            {
                results.Add(VisualizationPanelType.Timeline);
            }
            else if (visualizationPanel is XYVisualizationPanel)
            {
                results.Add(VisualizationPanelType.XY);
            }
            else if (visualizationPanel is XYZVisualizationPanel)
            {
                results.Add(VisualizationPanelType.XYZ);
            }
            else if (visualizationPanel is InstantVisualizationPlaceholderPanel)
            {
                results.Add(VisualizationPanelType.XY);
                results.Add(VisualizationPanelType.XYZ);
            }

            return results;
        }

        /// <summary>
        /// Gets a collection of visualizer metadata objects for a given target data type.
        /// </summary>
        /// <param name="dataType">The data type to search for.</param>
        /// <param name="visualizationPanel">The visualization panel where it is intended to visualize the data, or visualizers targeting any panels should be returned.</param>
        /// <param name="isUniversal">A nullable boolean indicating constraints on whether the visualizer should be a universal one (visualize messages, visualize latency etc).</param>
        /// <param name="isInNewPanel">A nullable boolean indicating constraints on whether the visualizer should be a "in new panel" one.</param>
        /// <returns>A list of visualizer metadata objects.</returns>
        public List<VisualizerMetadata> GetCompatibleVisualizers(
            Type dataType,
            VisualizationPanel visualizationPanel = null,
            bool? isUniversal = null,
            bool? isInNewPanel = null)
        {
            return this.GetCompatibleVisualizers(
                dataType,
                dataType,
                false,
                visualizationPanel,
                isUniversal,
                isInNewPanel);
        }

        /// <summary>
        /// Gets a collection of visualizer metadata objects for a given target stream tree node.
        /// </summary>
        /// <param name="streamTreeNode">The stream tree node whose type should be searched for.</param>
        /// <param name="visualizationPanel">The visualization panel where it is intended to visualize the data, or visualizers targeting any panels should be returned.</param>
        /// <param name="isUniversal">A nullable boolean indicating constraints on whether the visualizer should be a universal one (visualize messages, visualize latency etc).</param>
        /// <param name="isInNewPanel">A nullable boolean indicating constraints on whether the visualizer should be a "in new panel" one.</param>
        /// <returns>A list of visualizer metadata objects.</returns>
        public List<VisualizerMetadata> GetCompatibleVisualizers(
            StreamTreeNode streamTreeNode,
            VisualizationPanel visualizationPanel = null,
            bool? isUniversal = null,
            bool? isInNewPanel = null)
        {
            return this.GetCompatibleVisualizers(
                VisualizationContext.Instance.GetDataType(streamTreeNode.StreamTypeName),
                VisualizationContext.Instance.GetDataType(streamTreeNode.NodeTypeName),
                !string.IsNullOrWhiteSpace(streamTreeNode.MemberPath),
                visualizationPanel,
                isUniversal,
                isInNewPanel);
        }

        /// <summary>
        /// Gets the available collection of batch processing tasks that can run on a dataset.
        /// </summary>
        /// <returns>The collection of batch processing task metadata objects.</returns>
        /// <remarks>Currently only batch processing tasks that have parameters for pipeline, sessionimporter
        /// and exporter, in that order, can be executed on a dataset.</remarks>
        public IEnumerable<BatchProcessingTaskMetadata> GetDatasetCompatibleBatchProcessingTasks()
        {
            return this.batchProcessingTasks.Where(bpt =>
            {
                var parameters = bpt.MethodInfo.GetParameters();
                return parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(Pipeline) &&
                    parameters[1].ParameterType == typeof(SessionImporter) &&
                    parameters[2].ParameterType == typeof(Exporter);
            });
        }

        /// <summary>
        /// Gets the available collection of batch processing tasks that can run on a session.
        /// </summary>
        /// <returns>The collection of batch processing task metadata objects.</returns>
        /// <remarks>Currently only batch processing tasks that have parameters for pipeline, sessionimporter
        /// and exporter, in that order, can be executed on a session.</remarks>
        public IEnumerable<BatchProcessingTaskMetadata> GetSessionCompatibleBatchProcessingTasks()
        {
            return this.batchProcessingTasks.Where(bpt =>
            {
                var parameters = bpt.MethodInfo.GetParameters();
                return parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(Pipeline) &&
                    parameters[1].ParameterType == typeof(SessionImporter) &&
                    parameters[2].ParameterType == typeof(Exporter);
            });
        }

        /// <summary>
        /// Gets the available collection of batch processing tasks that can run on a partition.
        /// </summary>
        /// <returns>The collection of batch processing task metadata objects.</returns>
        /// <remarks>Currently only batch processing tasks that have parameters for pipeline, importer
        /// and exporter, in that order, can be executed on a partition.</remarks>
        public IEnumerable<BatchProcessingTaskMetadata> GetPartitionCompatibleBatchProcessingTasks()
        {
            return this.batchProcessingTasks.Where(bpt =>
            {
                var parameters = bpt.MethodInfo.GetParameters();
                return parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(Pipeline) &&
                    parameters[1].ParameterType == typeof(Importer) &&
                    parameters[2].ParameterType == typeof(Exporter);
            });
        }

        /// <summary>
        /// Gets file info for known stream readers.
        /// </summary>
        /// <returns>Sequence of stream reader file info.</returns>
        public IEnumerable<(string Name, string Extensions)> GetStreamReaderExtensions()
        {
            return this.StreamReaders.Select(sr => (sr.Name, sr.Extension));
        }

        /// <summary>
        /// Gets stream reader type for given file extension.
        /// </summary>
        /// <param name="extension">File extension.</param>
        /// <returns>Stream reader type.</returns>
        public Type GetStreamReaderType(string extension)
        {
            return this.StreamReaders.Where(sr => sr.Extension == extension).First().ReaderType;
        }

        private List<VisualizerMetadata> GetCompatibleVisualizers(
            Type streamType,
            Type dataType,
            bool isStreamMember,
            VisualizationPanel visualizationPanel,
            bool? isUniversal,
            bool? isInNewPanel)
        {
            this.EnsureInitialized();

            var results = new List<VisualizerMetadata>();

            // If we're looking for visualizers that fit in any panel
            if (visualizationPanel == null)
            {
                results.AddRange(this.visualizers.FindAll(v =>
                    (dataType == v.DataType || dataType.IsSubclassOf(v.DataType)) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal))
                    .OrderBy(v => this.GetVisualizerRank(v, dataType)));
            }
            else
            {
                // o/w find out the compatible panel types
                var compatiblePanels = this.GetCompatiblePanelTypes(visualizationPanel);
                results.AddRange(this.visualizers.FindAll(v =>
                    compatiblePanels.Contains(v.VisualizationPanelType) &&
                    (dataType == v.DataType || dataType.IsSubclassOf(v.DataType)) &&
                    (!isInNewPanel.HasValue || v.IsInNewPanel == isInNewPanel.Value) &&
                    (!isUniversal.HasValue || v.IsUniversalVisualizer == isUniversal))
                    .OrderBy(v => this.GetVisualizerRank(v, dataType)));
            }

            // We force-add the latency visualizer b/c it's not detectable by data type
            // (the adapter to make it work will be added automatically later in
            // CustomizeVisualizerMetadata). Latency visualizer is only compatible with
            // timeline visualization panels.
            if (isUniversal.HasValue && isUniversal.Value)
            {
                if (isInNewPanel.HasValue && isInNewPanel.Value)
                {
                    results.Add(this.visualizers.FirstOrDefault(v => v.CommandText == ContextMenuName.VisualizeLatencyInNewPanel));
                }
                else if (visualizationPanel is TimelineVisualizationPanel)
                {
                    results.Add(this.visualizers.FirstOrDefault(v => v.CommandText == ContextMenuName.VisualizeLatency));
                }
            }

            // Customize each visualizer metadata.
            this.CustomizeVisualizerMetadata(results, streamType, dataType, isStreamMember);

            return results;
        }

        private int GetVisualizerRank(VisualizerMetadata visualizerMetadata, Type dataType)
        {
            // For now, keep the visualizer that has a matching type at the top.
            if (dataType == visualizerMetadata.DataType)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Customizes a collection of visualizer metadata objects for certain scenarios including whether the
        /// metadata represents a message visualization object and whether we need to insert a stream member
        /// adapter for visualizers that visualize a member of a stream rather than the entire stream.
        /// </summary>
        /// <param name="metadatas">A collection of visualizer metadata objects to insert stream member adapters into.</param>
        /// <param name="messageDataType">The type of the messages in the source stream.</param>
        /// <param name="targetDataType">The type of data to be displayed.</param>
        /// <param name="isStreamMember">True if the visualizer metadata collection represents visualizers for a stream member
        /// rather than for the entire stream, otherwise false.</param>
        private void CustomizeVisualizerMetadata(List<VisualizerMetadata> metadatas, Type messageDataType, Type targetDataType, bool isStreamMember)
        {
            // For each of the non-universal visualization objects, add a data adapter from the stream data type to the subfield data type
            for (int index = 0; index < metadatas.Count; index++)
            {
                // For message visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(MessageVisualizationObject))
                {
                    metadatas[index] = VisualizerMetadata.InsertObjectAdapter(metadatas[index], targetDataType);
                }

                // For latency visualization object insert a custom object adapter so values can be displayed for known types.
                if (metadatas[index].VisualizationObjectType == typeof(LatencyVisualizationObject))
                {
                    metadatas[index] = VisualizerMetadata.InsertObjectToLatencyAdapter(metadatas[index], targetDataType);
                }

                // For all but the latency visualization object, add a stream member adapter
                // if the metadata is for a member of the stream rather than the entire stream.
                if (isStreamMember && (metadatas[index].VisualizationObjectType != typeof(LatencyVisualizationObject)))
                {
                    metadatas[index] = VisualizerMetadata.CreateStreamMemberAdapter(metadatas[index], messageDataType);
                }
            }
        }

        private void DiscoverPlugins(List<string> assemblies, string loadLogFilename)
        {
            bool hasErrors = false;

            // Create the log writer
            using (FileStream fileStream = File.Create(loadLogFilename))
            {
                using VisualizationLogWriter logWriter = new VisualizationLogWriter(fileStream);

                // Log preamble
                logWriter.WriteLine("Loading PsiStudio Plugins ({0})", DateTime.Now.ToString("G"));
                logWriter.WriteLine("----------------------------------------------------");
                logWriter.WriteLine();

                foreach (string assembly in assemblies)
                {
                    logWriter.WriteLine("Search Assembly: {0}...", assembly);
                }

                logWriter.WriteLine();

                // Note: Visualization object types depend on both summarizer types and stream adapter types,
                // so we need to make sure those types have all been loaded before we try to load the
                // visualization object types.
                var visualizationObjectTypes = new Dictionary<Type, string>();

                foreach (string assemblyPath in assemblies)
                {
                    // Get the list of types in the assembly
                    Type[] types = this.GetTypesFromAssembly(assemblyPath, logWriter);

                    // Look for attributes denoting visualization objects, summarizers, and stream adapters.
                    foreach (Type type in types)
                    {
                        if (type.GetCustomAttribute<VisualizationObjectAttribute>() != null)
                        {
                            // Don't load these yet, wait until we've loaded the summarizers and adapters.
                            visualizationObjectTypes[type] = assemblyPath;
                        }

                        if (type.GetCustomAttribute<SummarizerAttribute>() != null)
                        {
                            this.AddSummarizer(type, logWriter, assemblyPath);
                        }

                        if (type.GetCustomAttribute<StreamAdapterAttribute>() != null)
                        {
                            this.AddStreamAdapter(type, logWriter, assemblyPath);
                        }

                        var streamReaderAttribute = type.GetCustomAttribute<StreamReaderAttribute>();
                        if (streamReaderAttribute != null)
                        {
                            this.AddStreamReader(streamReaderAttribute, type, logWriter, assemblyPath);
                        }

                        // Look throught the static public method for batch processing task methods
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                        {
                            foreach (var attr in method.GetCustomAttributes(typeof(BatchProcessingTaskAttribute)))
                            {
                                this.AddBatchProcessingTask(type, method, (BatchProcessingTaskAttribute)attr, logWriter, assemblyPath);
                            }
                        }
                    }
                }

                // Load all of the visualization object types that were found earlier
                Dictionary<Type, string>.Enumerator visualizationObjectTypesEnumerator = visualizationObjectTypes.GetEnumerator();
                while (visualizationObjectTypesEnumerator.MoveNext())
                {
                    this.AddVisualizer(visualizationObjectTypesEnumerator.Current.Key, logWriter, visualizationObjectTypesEnumerator.Current.Value);
                }

                // Log complete
                logWriter.WriteLine();
                logWriter.WriteLine("PsiStudio plugin loading has completed. ({0})", DateTime.Now.ToString("G"));

                hasErrors = logWriter.HasErrors;
            }

            // If there were any errors while loading the visualizers etc, inform the user and allow him to view the log.
            if (hasErrors)
            {
                MessageBoxWindow dlg = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Plugins Load Errors",
                    "One or more plugins were not loaded because they contained errors.\r\n\r\nWould you like to see the log?",
                    "Yes",
                    "No");

                if (dlg.ShowDialog() == true)
                {
                    // Display the log file in the default application for text files.
                    Process.Start(loadLogFilename);
                }
            }
        }

        private Type[] GetTypesFromAssembly(string assemblyPath, VisualizationLogWriter logWriter)
        {
            try
            {
                // Load the assembly
                Assembly assembly = Assembly.LoadFrom(assemblyPath.Trim());

                // Get the list of types in the assembly.  (This action will fail if there's missing dependent assemblies)
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Error most likely caused by trying to load a type that references a missing dependent assembly.
                logWriter.WriteError("Could not load assembly {0}: {1}", assemblyPath, ex.Message);

                // Look into the loader exceptions so we can write out the list of missing dependent assemblies.
                if (ex.LoaderExceptions != null)
                {
                    foreach (Exception loaderException in ex.LoaderExceptions)
                    {
                        logWriter.WriteError(loaderException.Message);
                    }
                }

                return new Type[] { };
            }
            catch (Exception ex)
            {
                // General error loading assembly
                logWriter.WriteError("Could not load assembly {0}: {1}", assemblyPath, ex.Message);
                return new Type[] { };
            }
        }

        private void AddVisualizer(Type visualizationObjectType, VisualizationLogWriter logWriter, string assemblyPath)
        {
            // Add both a "Visualize" and a "Visualize in new panel" command metadata
            logWriter.WriteLine("Loading Visualizer {0} from {1}...", visualizationObjectType.Name, assemblyPath);
            List<VisualizerMetadata> visualizerMetadata = VisualizerMetadata.Create(visualizationObjectType, this.summarizers, this.streamAdapters, logWriter);
            if (visualizerMetadata != null)
            {
                this.visualizers.AddRange(visualizerMetadata);
            }
        }

        private void AddSummarizer(Type summarizerType, VisualizationLogWriter logWriter, string assemblyPath)
        {
            logWriter.WriteLine("Loading Summarizer {0} from {1}...", summarizerType.Name, assemblyPath);
            SummarizerMetadata summarizerMetadata = SummarizerMetadata.Create(summarizerType, logWriter);
            if (summarizerMetadata != null)
            {
                this.summarizers[summarizerType] = summarizerMetadata;
            }
        }

        private void AddStreamAdapter(Type adapterType, VisualizationLogWriter logWriter, string assemblyPath)
        {
            logWriter.WriteLine("Loading StreamAdapter {0} from {1}...", adapterType.Name, assemblyPath);
            StreamAdapterMetadata streamAdapterMetadata = StreamAdapterMetadata.Create(adapterType, logWriter);
            if (streamAdapterMetadata != null)
            {
                this.streamAdapters.Add(streamAdapterMetadata);
            }
        }

        private void AddBatchProcessingTask(Type batchProcessingTaskType, MethodInfo methodInfo, BatchProcessingTaskAttribute attribute, VisualizationLogWriter logWriter, string assemblyPath)
        {
            logWriter.WriteLine("Loading Batch Processing Task {0} from {1}...", attribute.Name, assemblyPath);
            var batchProcessingTaskMetadata = BatchProcessingTaskMetadata.Create(batchProcessingTaskType, methodInfo, attribute, logWriter);
            if (batchProcessingTaskMetadata != null)
            {
                this.batchProcessingTasks.Add(batchProcessingTaskMetadata);
            }
        }

        private void AddStreamReader(StreamReaderAttribute streamReaderAttribute, Type streamReaderType, VisualizationLogWriter logWriter, string assemblyPath)
        {
            logWriter.WriteLine("Loading StreamReader {0} from {1}...", streamReaderType.Name, assemblyPath);
            this.streamReaders.Add((streamReaderAttribute.Name, streamReaderAttribute.Extension, streamReaderType));
        }

        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                throw new InvalidOperationException($"{nameof(this.Initialize)} must be called before calling this method.");
            }
        }
    }
}
