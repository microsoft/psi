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
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
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
        private readonly Dictionary<Type, SummarizerMetadata> summarizers = new ();

        // The list of stream adapters that were found during discovery.
        private readonly List<StreamAdapterMetadata> streamAdapters = new ();

        // The list of batch processing tasks that were found during discovery.
        private readonly List<BatchProcessingTaskMetadata> batchProcessingTasks = new ();

        // The list of visualization objects that were found during discovery.
        private readonly List<VisualizerMetadata> visualizers = new ();

        // This list of stream readers that were found during discovery.
        private readonly List<(string Name, string Extension, Type ReaderType)> streamReaders = new ();

        // This dictionary provides a set of additional type mappings.
        private readonly Dictionary<string, Type> additionalTypeMappings = new ();

        /// <summary>
        /// Gets a value indicating whether or not Initialize() has been called.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Gets the set of available visualizers.
        /// </summary>
        public IReadOnlyList<VisualizerMetadata> Visualizers => this.visualizers.AsReadOnly();

        /// <summary>
        /// Gets the set of additional type mappings.
        /// </summary>
        public IReadOnlyDictionary<string, Type> AdditionalTypeMappings => this.additionalTypeMappings;

        /// <summary>
        /// Gets the set of available batch processing tasks.
        /// </summary>
        public IReadOnlyList<BatchProcessingTaskMetadata> BatchProcessingTasks => this.batchProcessingTasks.AsReadOnly();

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
        /// <param name="additionalTypeMappings">A set of additional type mappings.</param>
        /// <param name="loadLogFilename">The full path to the log file to be created and written to while initializing.</param>
        /// <param name="showErrorLog">Indicates whether to show the error log.</param>
        /// <param name="batchProcessingTaskConfigurationsPath">The folder under which batch processing task configurations are saved.</param>
        public void Initialize(
            List<string> additionalAssembliesToSearch,
            Dictionary<string, string> additionalTypeMappings,
            string loadLogFilename,
            bool showErrorLog,
            string batchProcessingTaskConfigurationsPath)
        {
            // Append the additional assemblies to the list of default assemblies to search for plugins
            List<string> assembliesToSearch = this.defaultAssemblies.ToList();
            if ((additionalAssembliesToSearch != null) && (additionalAssembliesToSearch.Count > 0))
            {
                assembliesToSearch.AddRange(additionalAssembliesToSearch);
            }

            // Load all the visualizers, summarizers, stream adapters, stream readers, and batch processing tasks
            this.DiscoverPlugins(assembliesToSearch, loadLogFilename, showErrorLog, batchProcessingTaskConfigurationsPath);

            // Set up the additional type mappings
            foreach (var kvp in additionalTypeMappings)
            {
                var verifiedType = TypeResolutionHelper.GetVerifiedType(kvp.Value);
                if (verifiedType != null)
                {
                    this.additionalTypeMappings.Add(kvp.Key, verifiedType);
                }
            }

            this.IsInitialized = true;
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

        private void DiscoverPlugins(List<string> assemblies, string loadLogFilename, bool showErrorLog, string batchProcessingTaskConfigurationsPath)
        {
            bool hasErrors = false;

            // Create the log writer
            using (FileStream fileStream = File.Create(loadLogFilename))
            {
                using var logWriter = new VisualizationLogWriter(fileStream);

                // Log preamble
                logWriter.WriteLine("Loading PsiStudio Plugins ({0})", DateTime.UtcNow.ToString("G"));
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
                    var types = this.GetTypesFromAssembly(assemblyPath, logWriter);

                    // Look for attributes denoting visualization objects, summarizers, and stream adapters.
                    foreach (var type in types)
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

                        // Check if the type derives from BatchProcessingTask and has a batch processing task
                        // attribute specified
                        if (type.IsBatchProcessingTaskType())
                        {
                            foreach (var attr in type.GetCustomAttributes(typeof(BatchProcessingTaskAttribute)))
                            {
                                this.AddBatchProcessingTask(type, (BatchProcessingTaskAttribute)attr, logWriter, assemblyPath, batchProcessingTaskConfigurationsPath);
                            }
                        }

                        // Look through the static public method for batch processing task methods
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                        {
                            var parameters = method.GetParameters();
                            if (parameters.Length == 3 &&
                                parameters[0].ParameterType == typeof(Pipeline) &&
                                parameters[1].ParameterType == typeof(SessionImporter) &&
                                parameters[2].ParameterType == typeof(Exporter))
                            {
                                foreach (var attr in method.GetCustomAttributes(typeof(BatchProcessingTaskAttribute)))
                                {
                                    this.AddBatchProcessingTask(method, (BatchProcessingTaskAttribute)attr, logWriter, assemblyPath, batchProcessingTaskConfigurationsPath);
                                }
                            }
                        }
                    }
                }

                // Load all of the visualization object types that were found earlier
                var visualizationObjectTypesEnumerator = visualizationObjectTypes.GetEnumerator();
                while (visualizationObjectTypesEnumerator.MoveNext())
                {
                    this.AddVisualizer(visualizationObjectTypesEnumerator.Current.Key, logWriter, visualizationObjectTypesEnumerator.Current.Value);
                }

                // Log complete
                logWriter.WriteLine();
                logWriter.WriteLine("PsiStudio plugin loading has completed. ({0})", DateTime.UtcNow.ToString("G"));

                hasErrors = logWriter.HasErrors;
            }

            // If there were any errors while loading the visualizers etc, inform the user and allow him to view the log.
            if (hasErrors && showErrorLog)
            {
                var messageBoxWindow = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Plugins Load Errors",
                    "One or more plugins were not loaded because they contained errors.\r\n\r\nWould you like to see the log?",
                    "Yes",
                    "No");

                if (messageBoxWindow.ShowDialog() == true)
                {
                    // Display the log file in the default application for text files.
                    Process.Start("notepad.exe", loadLogFilename);
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
            var visualizer = VisualizerMetadata.Create(visualizationObjectType, this.summarizers, this.streamAdapters, logWriter);
            if (visualizer != null)
            {
                this.visualizers.AddRange(visualizer);
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

        private void AddBatchProcessingTask(
            Type batchProcessingTaskType,
            BatchProcessingTaskAttribute batchProcessingTaskAttribute,
            VisualizationLogWriter logWriter,
            string assemblyPath,
            string batchProcessingTaskConfigurationsPath)
        {
            logWriter.WriteLine("Loading Batch Processing Task {0} from {1}...", batchProcessingTaskAttribute.Name, assemblyPath);
            var batchProcessingTaskMetadata = new BatchProcessingTaskMetadata(batchProcessingTaskType, batchProcessingTaskAttribute, batchProcessingTaskConfigurationsPath);
            if (batchProcessingTaskMetadata != null)
            {
                this.batchProcessingTasks.Add(batchProcessingTaskMetadata);
            }
        }

        private void AddBatchProcessingTask(
            MethodInfo batchProcessingMethodInfo,
            BatchProcessingTaskAttribute batchProcessingTaskAttribute,
            VisualizationLogWriter logWriter,
            string assemblyPath,
            string batchProcessingTaskConfigurationsPath)
        {
            logWriter.WriteLine("Loading Batch Processing Task {0} from {1}...", batchProcessingTaskAttribute.Name, assemblyPath);
            var batchProcessingTaskMetadata = new BatchProcessingTaskMetadata(batchProcessingMethodInfo, batchProcessingTaskAttribute, batchProcessingTaskConfigurationsPath);
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
