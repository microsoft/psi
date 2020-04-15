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
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents mappings from stream datatypes to visualizers.
    /// </summary>
    public class VisualizerMap
    {
        // The list of default assemblies in which the visualizer mapper will search for visualization objects, adapter, and summarizers.
        private string[] defaultAssemblies = new[] { "Microsoft.Psi.Visualization.Common.Windows.dll" };

        /// <summary>
        /// True if the Initialize() has been called, otherwise false.
        /// </summary>
        private bool isInitialized = false;

        // The list of summarizers that were found during discovery.
        private Dictionary<Type, SummarizerMetadata> summarizers = new Dictionary<Type, SummarizerMetadata>();

        // The list of stream adapters that were found during discovery.
        private List<StreamAdapterMetadata> streamAdapters = new List<StreamAdapterMetadata>();

        // The list of visualization objects that were found during discovery.
        private List<VisualizerMetadata> visualizers = new List<VisualizerMetadata>();

        /// <summary>
        /// Initializes the visualizer map.
        /// </summary>
        /// <param name="additionalAssembliesToSearch">A list of assemblies to search for visualizer objects in addition to the default assembly list.
        /// If no additional assemblies contain visualizers, this parameter can be null or an empty list.</param>
        /// <param name="visualizerLoadLogFilename">The full path to the log file to be created and written to while initializing the visualizer map.</param>
        public void Initialize(List<string> additionalAssembliesToSearch, string visualizerLoadLogFilename)
        {
            // Append the additional assemblies to the list of default assemblies to search for visualizers
            List<string> assembliesToSearch = this.defaultAssemblies.ToList();
            if ((additionalAssembliesToSearch != null) && (additionalAssembliesToSearch.Count > 0))
            {
                assembliesToSearch.AddRange(additionalAssembliesToSearch);
            }

            // Load all the visualizers, summarizers, stream adapters
            this.DiscoverVisualizerObjects(assembliesToSearch, visualizerLoadLogFilename);

            this.isInitialized = true;
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
        /// Gets all entries for a given message datatype.
        /// </summary>
        /// <param name="dataType">The datatype to search for.</param>
        /// <returns>A list of visualizer metadata objects.</returns>
        public List<VisualizerMetadata> GetByDataType(Type dataType)
        {
            this.EnsureInitialized();
            return this.visualizers.FindAll(e => dataType == e.DataType || dataType.IsSubclassOf(e.DataType)).OrderBy(e => e.IsBelowSeparator).ToList();
        }

        /// <summary>
        /// Gets all the above the separator entries for a given datatype and visualization panel.
        /// </summary>
        /// <param name="dataType">The datatype to search for.</param>
        /// <param name="visualizationPanel">The type of visualization panel to search for.</param>
        /// <returns>A list of visualizer metadata objects.</returns>
        public List<VisualizerMetadata> GetByDataTypeAndPanelAboveSeparator(Type dataType, VisualizationPanel visualizationPanel)
        {
            this.EnsureInitialized();
            List<VisualizerMetadata> results;

            if (visualizationPanel == null)
            {
                // Dropping stream into empty space.  Find the "in new panel" commands that are above the separator and have a compatible data type
                results = this.visualizers.FindAll(e => (dataType == e.DataType || dataType.IsSubclassOf(e.DataType)) && e.IsInNewPanel && e.IsAboveSeparator);
            }
            else
            {
                // Dropping onto a specific panel.  Find the non "in new panel" commands that are above the separator, have a compatible panel type, and compatible data type
                List<VisualizationPanelType> compatiblePanels = this.GetCompatiblePanelTypes(visualizationPanel);
                results = this.visualizers.FindAll(e => (dataType == e.DataType || dataType.IsSubclassOf(e.DataType)) && compatiblePanels.Contains(e.VisualizationPanelType) && !e.IsInNewPanel && e.IsAboveSeparator);
            }

            return results;
        }

        private void DiscoverVisualizerObjects(List<string> assemblies, string visualizerLoadLogFilename)
        {
            bool hasErrors = false;

            // Create the log writer
            using (FileStream fileStream = File.Create(visualizerLoadLogFilename))
            {
                using (VisualizationLogWriter logWriter = new VisualizationLogWriter(fileStream))
                {
                    // Log preamble
                    logWriter.WriteLine("Loading PsiStudio Visualizers ({0})", DateTime.Now.ToString("G"));
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
                    Dictionary<Type, string> visualizationObjectTypes = new Dictionary<Type, string>();

                    foreach (string assemblyPath in assemblies)
                    {
                        // Get the list of types in the assembly
                        Type[] types = this.GetTypesFromAssembly(assemblyPath, logWriter);

                        // Look for attributes denoting visualziation objects, summarizers, and stream adapters.
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
                    logWriter.WriteLine("PsiStudio visualizer loading has completed. ({0})", DateTime.Now.ToString("G"));

                    hasErrors = logWriter.HasErrors;
                }
            }

            // If there were any errors while loading the visualizers etc, inform the user and allow him to view the log.
            if (hasErrors)
            {
                MessageBoxWindow dlg = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Visualizers Load Errors",
                    "One or more visualizers were not loaded because they contained errors.\r\n\r\nWould you like to see the visualizer load log?",
                    "Yes",
                    "No");

                if (dlg.ShowDialog() == true)
                {
                    // Display the log file in the default application for text files.
                    Process.Start(visualizerLoadLogFilename);
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

        private void EnsureInitialized()
        {
            if (!this.isInitialized)
            {
                throw new InvalidOperationException("VisualizerMap.Initialize() must be called before calling this method.");
            }
        }
    }
}
