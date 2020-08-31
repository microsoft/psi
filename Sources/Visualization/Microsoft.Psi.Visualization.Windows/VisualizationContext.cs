// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Commands;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Tasks;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Data context for visualization.
    /// </summary>
    public class VisualizationContext : ObservableObject
    {
        private VisualizationContainer visualizationContainer;
        private DatasetViewModel datasetViewModel = null;
        private DispatcherTimer liveStatusTimer = null;

        private List<TypeKeyedActionCommand> typeVisualizerActions = new List<TypeKeyedActionCommand>();

        static VisualizationContext()
        {
            VisualizationContext.Instance = new VisualizationContext();
        }

        private VisualizationContext()
        {
            this.DatasetViewModels = new ObservableCollection<DatasetViewModel>();

            // Periodically check if there's any live partitions in the dataset
            this.liveStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Normal, new EventHandler(this.OnLiveStatusTimer), Application.Current.Dispatcher);
            this.liveStatusTimer.Start();
        }

        /// <summary>
        /// An event that fires when an object requests that its properties be displayed.
        /// </summary>
        public event EventHandler<RequestDisplayObjectPropertiesEventArgs> RequestDisplayObjectProperties;

        /// <summary>
        /// Gets the visualization context singleton.
        /// </summary>
        public static VisualizationContext Instance { get; private set; }

        /// <summary>
        /// Gets the plugin map.
        /// </summary>
        public PluginMap PluginMap { get; } = new PluginMap();

        /// <summary>
        /// Gets the collection of dataset view models.
        /// </summary>
        public ObservableCollection<DatasetViewModel> DatasetViewModels { get; private set; }

        /// <summary>
        /// Gets or sets the current dataset view model.
        /// </summary>
        public DatasetViewModel DatasetViewModel
        {
            get => this.datasetViewModel;
            set => this.Set(nameof(this.DatasetViewModel), ref this.datasetViewModel, value);
        }

        /// <summary>
        /// Gets or sets the visualization container.
        /// </summary>
        public VisualizationContainer VisualizationContainer
        {
            get => this.visualizationContainer;

            set
            {
                this.Set(nameof(this.VisualizationContainer), ref this.visualizationContainer, value);
            }
        }

        /// <summary>
        /// Gets the  image to display on the Play/Pause button.
        /// </summary>
        public string PlayPauseButtonImage => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Icons\stop_x4.png" : @"Icons\play_x4.png";

        /// <summary>
        /// Gets the  image to display on the Play/Pause button.
        /// </summary>
        public string PlayPauseButtonToolTip => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Stop" : @"Play";

        /// <summary>
        /// Opens a previously persisted layout file.
        /// </summary>
        /// <param name="path">The path to the layout to open.</param>
        /// <param name="name">The name of the layout to open.</param>
        /// <returns>True if the layout was successfully loaded, otherwise false.</returns>
        public bool OpenLayout(string path, string name)
        {
            // Clear the current layout
            this.ClearLayout();

            if (!string.IsNullOrWhiteSpace(path))
            {
                // Load the new layout.  If this operation fails, then null will be returned.
                VisualizationContainer visualizationContainer = VisualizationContainer.Load(path, name, this.VisualizationContainer);
                if (visualizationContainer != null)
                {
                    // Set the new visualization container
                    this.VisualizationContainer = visualizationContainer;

                    // Update bindings to the sources
                    this.VisualizationContainer.UpdateStreamSources(this.DatasetViewModel?.CurrentSessionViewModel);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the current layout.
        /// </summary>
        public void ClearLayout()
        {
            this.VisualizationContainer?.Clear();
        }

        /// <summary>
        /// Gets the message data type for a stream.  If the data type is unknown (i.e. the assembly
        /// that contains the message type is not currently being referenced by PsiStudio, then
        /// we return the generic object type.
        /// </summary>
        /// <param name="typeName">The type name.</param>
        /// <returns>The type of messages in the stream.</returns>
        public Type GetDataType(string typeName)
        {
            return TypeResolutionHelper.GetVerifiedType(typeName) ?? TypeResolutionHelper.GetVerifiedType(typeName.Split(',')[0]) ?? typeof(object);
        }

        /// <summary>
        /// Asychronously runs a specified batch processing task over a dataset.
        /// </summary>
        /// <param name="datasetViewModel">The dataset view model.</param>
        /// <param name="batchProcessingTaskMetadata">Batch task processing metadata.</param>
        /// <returns>A task that represents the asynchronous batch processing operation.</returns>
        public async Task RunDatasetBatchProcessingTaskAsync(DatasetViewModel datasetViewModel, BatchProcessingTaskMetadata batchProcessingTaskMetadata)
        {
            // Initialize the progress reporting window
            var progressReportWindow = new RunBatchProcessingTaskWindow(Application.Current.MainWindow, batchProcessingTaskMetadata.Name, null);

            // Initialize progress reporter for the status window
            IProgress<(string, double)> progress = new Progress<(string, double)>(tuple =>
            {
                progressReportWindow.Progress = tuple.Item2 * 100;
                progressReportWindow.Target = tuple.Item1;
                if (tuple.Item2 == 1.0)
                {
                    // close the status window when the task reports completion
                    progressReportWindow.Close();
                }
            });

            try
            {
                var task = datasetViewModel.Dataset.CreateDerivedPartitionAsync(
                    (pipeline, sessionImporter, exporter) => batchProcessingTaskMetadata.MethodInfo.Invoke(null, new object[] { pipeline, sessionImporter, exporter }),
                    "Derived",
                    overwrite: true,
                    replayDescriptor: ReplayDescriptor.ReplayAll,
                    progress: progress);

                // show the modal status window, which will be closed once the load dataset operation completes
                progressReportWindow.ShowDialog();

                await task;

                // update the dataset view model as a new derived partition might have been created
                datasetViewModel.Update();
            }
            catch (InvalidOperationException)
            {
                // This indicates that the window has already been closed in the async task,
                // which means the operation has already completed, so just ignore and continue.
            }
        }

        /// <summary>
        /// Runs a specified batch processing task over a session.
        /// </summary>
        /// <param name="sessionViewModel">The session view model.</param>
        /// <param name="batchProcessingTaskMetadata">Batch task processing metadata.</param>
        /// <returns>A task that represents the asynchronous batch processing operation.</returns>
        public async Task RunSessionBatchProcessingTask(SessionViewModel sessionViewModel, BatchProcessingTaskMetadata batchProcessingTaskMetadata)
        {
            // Initialize the progress reporting window
            var progressReportWindow = new RunBatchProcessingTaskWindow(Application.Current.MainWindow, batchProcessingTaskMetadata.Name, null);

            // Initialize progress reporter for the status window
            IProgress<(string, double)> progress = new Progress<(string, double)>(tuple =>
            {
                progressReportWindow.Progress = tuple.Item2 * 100;
                progressReportWindow.Target = tuple.Item1;
                if (tuple.Item2 == 1.0)
                {
                    // close the status window when the task reports completion
                    progressReportWindow.Close();
                }
            });

            try
            {
                var task = sessionViewModel.Session.CreateDerivedPartitionAsync(
                    (pipeline, sessionImporter, exporter) => batchProcessingTaskMetadata.MethodInfo.Invoke(null, new object[] { pipeline, sessionImporter, exporter }),
                    "Derived",
                    overwrite: true,
                    replayDescriptor: ReplayDescriptor.ReplayAll,
                    progress: progress);

                // show the modal status window, which will be closed once the load dataset operation completes
                progressReportWindow.ShowDialog();

                await task;

                // update the session view model as a new derived partition might have been created
                sessionViewModel.Update();
            }
            catch (InvalidOperationException)
            {
                // This indicates that the window has already been closed in the async task,
                // which means the operation has already completed, so just ignore and continue.
            }
        }

        /// <summary>
        /// Visualizes a streaming the visualization container.
        /// </summary>
        /// <param name="streamTreeNode">The stream to visualize.</param>
        /// <param name="visualizerMetadata">The visualizer metadata to use.</param>
        /// <param name="visualizationPanel">The visualization panel to add the stream to (or null).</param>
        public void VisualizeStream(StreamTreeNode streamTreeNode, VisualizerMetadata visualizerMetadata, VisualizationPanel visualizationPanel)
        {
            // Create the visualization object
            IStreamVisualizationObject visualizationObject = Activator.CreateInstance(visualizerMetadata.VisualizationObjectType) as IStreamVisualizationObject;
            visualizationObject.Name = visualizerMetadata.VisualizationFormatString.Replace(VisualizationObjectAttribute.DefaultVisualizationFormatString, streamTreeNode.Path);

            // If the visualization object requires stream supplemental metadata to function, check that we're able to read the supplemental metadata from the stream.
            if (visualizationObject.RequiresSupplementalMetadata && !streamTreeNode.SupplementalMetadataIsKnownType)
            {
                new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Unable to Visualize Stream",
                    "The stream cannot be visualized because the stream's supplemental metadata cannot be read, possibly because a DLL containing types required by the supplemental metadata are not loaded.",
                    "Close",
                    null).ShowDialog();

                return;
            }

            // If we're dropping the stream into an empty area of the visualization container
            // (i.e. visualizationPanel is null), then create a new panel of the correct type.
            if (visualizationPanel == null || visualizerMetadata.IsInNewPanel)
            {
                // Create the new panel
                visualizationPanel = VisualizationPanelFactory.CreateVisualizationPanel(visualizerMetadata.VisualizationPanelType);

                // If the panel is a timeline panel, add it to the container, otherwise create an instant visualization container to hold
                // the new visualization panel, add the new panel to the instant visualization container, and add the instant visualization
                // container to the container.
                if (visualizerMetadata.VisualizationPanelType == VisualizationPanelType.Timeline)
                {
                    this.VisualizationContainer.AddPanel(visualizationPanel);
                }
                else
                {
                    InstantVisualizationContainer instantVisualizationContainer = Activator.CreateInstance(typeof(InstantVisualizationContainer), visualizationPanel) as InstantVisualizationContainer;
                    this.VisualizationContainer.AddPanel(instantVisualizationContainer);
                }
            }

            // If the target visualization panel is an instant visualization placeholder panel, replace if with a real panel of the correct type
            else if ((visualizationPanel is InstantVisualizationPlaceholderPanel placeholderPanel) && (visualizationPanel.ParentPanel is InstantVisualizationContainer instantVisualizationContainer))
            {
                VisualizationPanel replacementPanel = VisualizationPanelFactory.CreateVisualizationPanel(visualizerMetadata.VisualizationPanelType);
                instantVisualizationContainer.ReplaceChildVisualizationPanel(placeholderPanel, replacementPanel);
                visualizationPanel = replacementPanel;
            }

            // Make the target panel the current one
            this.VisualizationContainer.CurrentPanel = visualizationPanel;

            // Add the visualization object to the panel
            visualizationPanel.AddVisualizationObject(visualizationObject as VisualizationObject);

            // Update the binding
            visualizationObject.StreamBinding = new StreamBinding(
                streamTreeNode.StreamMetadata.Name,
                streamTreeNode.StreamMetadata.PartitionName,
                visualizerMetadata.StreamAdapterType,
                string.IsNullOrWhiteSpace(streamTreeNode.MemberPath) || visualizationObject is LatencyVisualizationObject ? null : new object[] { streamTreeNode.MemberPath },
                visualizerMetadata.SummarizerType,
                null,
                !string.IsNullOrWhiteSpace(streamTreeNode.MemberPath));
            visualizationObject.UpdateStreamSource(this.DatasetViewModel.CurrentSessionViewModel);

            // Check if this is a live stream
            this.DatasetViewModel.CurrentSessionViewModel.UpdateLivePartitionStatuses();

            // Select the new visualization object in the visualizations tree
            visualizationObject.IsTreeNodeSelected = true;
        }

        /// <summary>
        /// Asynchronously opens a previously persisted dataset.
        /// </summary>
        /// <param name="filename">Fully qualified path to dataset file.</param>
        /// <param name="showStatusWindow">Indicates whether to show the status window.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task OpenDatasetAsync(string filename, bool showStatusWindow = true)
        {
            var loadDatasetTask = default(Task);
            if (showStatusWindow)
            {
                // Window that will be used to indicate that an open operation is in progress.
                // Progress notification and cancellation are not yet fully supported.
                var statusWindow = new LoadingDatasetWindow(Application.Current.MainWindow, filename);

                // progress reporter for the load dataset task
                var progress = new Progress<(string s, double p)>(t =>
                {
                    statusWindow.Status = t.s;
                    if (t.p == 1.0)
                    {
                        // close the status window when the task reports completion
                        statusWindow.Close();
                    }
                });

                // start the load dataset task
                loadDatasetTask = this.LoadDatasetOrStoreAsync(filename, progress);

                try
                {
                    // show the modal status window, which will be closed once the load dataset operation completes
                    statusWindow.ShowDialog();
                }
                catch (InvalidOperationException)
                {
                    // This indicates that the window has already been closed in the async task,
                    // which means the operation has already completed, so just ignore and continue.
                }
            }
            else
            {
                loadDatasetTask = this.LoadDatasetOrStoreAsync(filename);
            }

            try
            {
                // await completion of the open dataset task
                await loadDatasetTask;

                this.DatasetViewModels.Clear();
                this.DatasetViewModels.Add(this.DatasetViewModel);

                // Check for live partitions
                this.DatasetViewModel.UpdateLivePartitionStatuses();

                // The first session (if there is one) will already have been selected in the dataset, so visualize it.
                this.DatasetViewModel.VisualizeSession(this.DatasetViewModel.CurrentSessionViewModel);
            }
            catch (Exception e)
            {
                // catch and display any exceptions that occurred during the open dataset operation
                var exception = e.InnerException ?? e;
                MessageBox.Show(exception.Message, exception.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Pause or resume playback of streams.
        /// </summary>
        public void PlayOrPause()
        {
            switch (this.VisualizationContainer.Navigator.CursorMode)
            {
                case CursorMode.Playback:
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
                    break;
                case CursorMode.Manual:
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Playback);
                    break;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a dataset is currently loaded.
        /// </summary>
        /// <returns>True if a dataset is currently loaded, otherwise false.</returns>
        public bool IsDatasetLoaded()
        {
            return this.DatasetViewModel?.CurrentSessionViewModel?.PartitionViewModels.FirstOrDefault() != null;
        }

        /// <summary>
        /// Toggle into or out of live mode.
        /// </summary>
        public void ToggleLiveMode()
        {
            // Only enter live mode if the current session contains live partitions
            if (this.DatasetViewModel != null && this.DatasetViewModel.CurrentSessionViewModel.ContainsLivePartitions && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live)
            {
                this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Live);
            }
            else
            {
                this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
            }
        }

        /// <summary>
        /// Zoom to the extents of the stream.
        /// </summary>
        /// <param name="streamTreeNode">The stream to zoom to.</param>
        public void ZoomToStreamExtents(StreamTreeNode streamTreeNode)
        {
            if (streamTreeNode.FirstMessageOriginatingTime.HasValue && streamTreeNode.LastMessageOriginatingTime.HasValue)
            {
                this.VisualizationContainer.Navigator.Zoom(streamTreeNode.FirstMessageOriginatingTime.Value, streamTreeNode.LastMessageOriginatingTime.Value);
            }
            else
            {
                this.VisualizationContainer.Navigator.ZoomToDataRange();
            }
        }

        /// <summary>
        /// Called by an object to request that its properties be displayed by the owner of this visualization context.
        /// </summary>
        /// <param name="requestingObject">The object that is requesting its properties be displayed.</param>
        public void DisplayObjectProperties(object requestingObject)
        {
            this.RequestDisplayObjectProperties?.Invoke(this, new RequestDisplayObjectPropertiesEventArgs(requestingObject));
        }

        private async Task LoadDatasetOrStoreAsync(string filename, IProgress<(string, double)> progress = null)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Extension == ".pds")
                {
                    progress?.Report(("Loading dataset...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.LoadAsync(filename);
                }
                else
                {
                    var name = fileInfo.Name;

                    if (fileInfo.Extension == ".psi")
                    {
                        name = name.Substring(0, Path.GetFileNameWithoutExtension(filename).LastIndexOf('.'));

                        progress?.Report(("Opening store...", 0));

                        // If the store is not closed, and nobody's holding a reference to it, assume it was closed improperly and needs to be repaired.
                        if (!PsiStore.IsClosed(name, fileInfo.DirectoryName) && !PsiStoreReader.IsStoreLive(name, fileInfo.DirectoryName))
                        {
                            progress?.Report(("Repairing store...", 0.5));
                            await Task.Run(() => PsiStore.Repair(name, fileInfo.DirectoryName));
                        }
                    }

                    progress?.Report(("Loading store...", 0.5));
                    var readerType = VisualizationContext.Instance.PluginMap.GetStreamReaderType(fileInfo.Extension);
                    var reader = Psi.Data.StreamReader.Create(name, fileInfo.DirectoryName, readerType);
                    this.DatasetViewModel = await DatasetViewModel.CreateFromStoreAsync(reader);
                }
            }
            finally
            {
                // report completion
                progress?.Report(("Done", 1.0));
            }
        }

        private void OnLiveStatusTimer(object sender, EventArgs e)
        {
            if (this.DatasetViewModel != null)
            {
                // Update the list of live partitions
                this.DatasetViewModel.UpdateLivePartitionStatuses();

                // If there are no longer any live partitions, exit live mode
                if ((this.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == false) && (this.VisualizationContainer.Navigator.CursorMode == CursorMode.Live))
                {
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
                }
            }
        }
    }
}
