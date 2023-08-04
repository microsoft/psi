// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Data context for visualization.
    /// </summary>
    public class VisualizationContext : ObservableObject, IDisposable
    {
        private readonly DispatcherTimer liveStatusTimer = null;

        private VisualizationContainer visualizationContainer;
        private DatasetViewModel datasetViewModel = null;

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
            set => this.Set(nameof(this.VisualizationContainer), ref this.visualizationContainer, value);
        }

        /// <summary>
        /// Gets the  image to display on the Play/Pause button.
        /// </summary>
        public string PlayPauseButtonImage => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Icons\stop_x4.png" : @"Icons\play_x4.png";

        /// <summary>
        /// Gets the  image to display on the Play/Pause button.
        /// </summary>
        public string PlayPauseButtonToolTip => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Stop" : @"Play";

        /// <inheritdoc />
        public void Dispose()
        {
            this.VisualizationContainer?.Dispose();
        }

        /// <summary>
        /// Opens a previously persisted layout file.
        /// </summary>
        /// <param name="path">The path to the layout to open.</param>
        /// <param name="name">The name of the layout to open.</param>
        /// <param name="userConsented">A flag indicating whether consent to apply this layout was explicitly given. This only applies to layouts containing scripts.</param>
        /// <returns>True if the layout was successfully loaded, otherwise false.</returns>
        public bool OpenLayout(string path, string name, ref bool userConsented)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                // Load the new layout.  If this operation fails, then null will be returned.
                VisualizationContainer newVisualizationContainer = VisualizationContainer.Load(path, name, this.VisualizationContainer);
                if (newVisualizationContainer != null)
                {
                    // If the new layout contains scripts, seek confirmation from the user before binding to the data
                    // Check if the new visualization container contains any derived stream visualizers.
                    var derivedStreamVisualizationObjects = newVisualizationContainer.GetDerivedStreamVisualizationObjects();

                    // Checks whether the adapter is a ScriptAdapter or has a ScriptAdapter somewhere in its chain
                    static bool ContainsScriptAdapter(Type adapterType)
                    {
                        if (adapterType.IsGenericType)
                        {
                            var genericAdapterType = adapterType.GetGenericTypeDefinition();
                            if (genericAdapterType == typeof(ScriptAdapter<,>))
                            {
                                return true;
                            }
                            else if (genericAdapterType == typeof(ChainedStreamAdapter<,,,,>))
                            {
                                var genericTypeParams = adapterType.GetGenericArguments();
                                return ContainsScriptAdapter(genericTypeParams[4]) || ContainsScriptAdapter(genericTypeParams[3]);
                            }
                        }

                        return false;
                    }

                    // Check whether any of the VO bindings contain scripted streams, and display a warning if consent has not previously been granted
                    bool hasScripts = derivedStreamVisualizationObjects.Any(vo => ContainsScriptAdapter(vo.StreamBinding.DerivedStreamAdapterType));
                    if (hasScripts && !userConsented)
                    {
                        var confirmationWindow = new ConfirmLayoutWindow(Application.Current.MainWindow, name);
                        userConsented = confirmationWindow.ShowDialog() ?? false;
                    }

                    if (!hasScripts || userConsented)
                    {
                        // NOTE: If we unbind the current VOs before binding the VOs of the new visualization
                        // container we risk data layer objects being disposed of because they temporarily
                        // have no subscribers.  To avoid this, we'll bind the VOs in the new visualization
                        // container before we unbind the VOs from the visualization container that's being
                        // replaced.  This way we'll ensure the subscriber count never goes to zero for data
                        // layer objects that are used by both the old and the new visualization container.

                        // Bind the visualization objects in the new visualization container to their sources
                        newVisualizationContainer.UpdateStreamSources(this.DatasetViewModel?.CurrentSessionViewModel);

                        // Clear the current visualization container
                        this.ClearLayout();

                        // Set the new visualization container
                        this.VisualizationContainer = newVisualizationContainer;

                        // And re-read the stream values at cursor (to publish to stream value visualizers)
                        DataManager.Instance.ReadAndPublishStreamValue(this.VisualizationContainer.Navigator.Cursor);

                        return true;
                    }
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
            return TypeResolutionHelper.GetVerifiedType(typeName) ??
                (this.PluginMap.AdditionalTypeMappings.ContainsKey(typeName) ? this.PluginMap.AdditionalTypeMappings[typeName] : typeof(object));
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
            var viewModel = new RunBatchProcessingTaskWindowViewModel(this.VisualizationContainer, datasetViewModel, batchProcessingTaskMetadata);
            var progressReportWindow = new RunBatchProcessingTaskWindow(Application.Current.MainWindow, viewModel);

            // show the modal status window, which will be closed once the load dataset operation completes
            if (progressReportWindow.ShowDialog() == true)
            {
                // update the dataset view model as a new derived partition might have been created.
                datasetViewModel.Update(datasetViewModel.Dataset);

                // if the dataset has a known associated file, save it.
                if (datasetViewModel.FileName != null)
                {
                    await datasetViewModel.SaveAsAsync(datasetViewModel.FileName);
                }

                // Update bindings to the sources
                this.VisualizationContainer.UpdateStreamSources(this.DatasetViewModel?.CurrentSessionViewModel);

                // And re-read the stream values at cursor (to publish to stream value visualizers)
                DataManager.Instance.ReadAndPublishStreamValue(this.VisualizationContainer.Navigator.Cursor);
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
            var viewModel = new RunBatchProcessingTaskWindowViewModel(this.VisualizationContainer, sessionViewModel, batchProcessingTaskMetadata);
            var progressReportWindow = new RunBatchProcessingTaskWindow(Application.Current.MainWindow, viewModel);

            // show the modal status window, which will be closed once the load dataset operation completes
            if (progressReportWindow.ShowDialog() == true)
            {
                // update the session view model as a new derived partition might have been created
                sessionViewModel.Update(sessionViewModel.Session);

                // if the dataset has a known associated file, save it.
                if (sessionViewModel.DatasetViewModel.FileName != null)
                {
                    await sessionViewModel.DatasetViewModel.SaveAsAsync(sessionViewModel.DatasetViewModel.FileName);
                }

                // Update bindings to the sources
                this.VisualizationContainer.UpdateStreamSources(this.DatasetViewModel?.CurrentSessionViewModel);

                // And re-read the stream values at cursor (to publish to stream value visualizers)
                DataManager.Instance.ReadAndPublishStreamValue(this.VisualizationContainer.Navigator.Cursor);
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
            var visualizationObject = Activator.CreateInstance(visualizerMetadata.VisualizationObjectType) as IStreamVisualizationObject;
            visualizationObject.Name = visualizerMetadata.VisualizationFormatString.Replace(VisualizationObjectAttribute.DefaultVisualizationFormatString, streamTreeNode.FullName);

            // If the visualization object requires stream supplemental metadata to function, check that we're able to read the supplemental metadata from the stream.
            if (visualizationObject.RequiresSupplementalMetadata && !streamTreeNode.SupplementalMetadataTypeIsKnown)
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
                    var instantVisualizationContainer = Activator.CreateInstance(typeof(InstantVisualizationContainer), visualizationPanel) as InstantVisualizationContainer;
                    this.VisualizationContainer.AddPanel(instantVisualizationContainer);
                }
            }

            // If the target visualization panel is an instant visualization placeholder panel, replace it with a real panel of the correct type
            else if ((visualizationPanel is InstantVisualizationPlaceholderPanel placeholderPanel) && (visualizationPanel.ParentPanel is InstantVisualizationContainer instantVisualizationContainer))
            {
                var replacementPanel = VisualizationPanelFactory.CreateVisualizationPanel(visualizerMetadata.VisualizationPanelType);
                instantVisualizationContainer.ReplaceChildVisualizationPanel(placeholderPanel, replacementPanel);
                visualizationPanel = replacementPanel;
            }

            // Select the visualization panel in the tree
            visualizationPanel.IsTreeNodeSelected = true;

            // Add the visualization object to the panel
            visualizationPanel.AddVisualizationObject(visualizationObject as VisualizationObject);

            // Update the binding
            visualizationObject.StreamBinding = streamTreeNode.CreateStreamBinding(visualizerMetadata);
            visualizationObject.UpdateStreamSource(this.DatasetViewModel.CurrentSessionViewModel);

            // Trigger an data read to update the visualization
            DataManager.Instance.ReadAndPublishStreamValue(this.VisualizationContainer.Navigator.Cursor);

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
        /// <param name="autoSave">Indicates whether to enable autosave.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task OpenDatasetAsync(string filename, bool showStatusWindow, bool autoSave)
        {
            var loadDatasetTask = default(Task<bool>);
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
                loadDatasetTask = this.LoadDatasetOrStoreAsync(filename, progress, autoSave);

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
                loadDatasetTask = this.LoadDatasetOrStoreAsync(filename, autoSave: autoSave);
            }

            try
            {
                // Await completion of the open dataset task. The return value indicates whether the dataset was actually opened.
                if (await loadDatasetTask)
                {
                    this.DatasetViewModels.Clear();
                    this.DatasetViewModels.Add(this.DatasetViewModel);

                    // Check for live partitions
                    this.DatasetViewModel.UpdateLivePartitionStatuses();

                    // The first session (if there is one) will already have been selected in the dataset, so visualize it.
                    this.DatasetViewModel.VisualizeSession(this.DatasetViewModel.CurrentSessionViewModel);
                }
            }
            catch (Exception e)
            {
                // create an empty dataset
                this.DatasetViewModels.Clear();
                this.DatasetViewModel = new DatasetViewModel();
                this.DatasetViewModels.Add(this.DatasetViewModel);

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
            if (streamTreeNode.SubsumedFirstMessageOriginatingTime != null && streamTreeNode.SubsumedLastMessageOriginatingTime != null)
            {
                this.VisualizationContainer.Navigator.Zoom(streamTreeNode.SubsumedFirstMessageOriginatingTime.Value, streamTreeNode.SubsumedLastMessageOriginatingTime.Value);
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

        private async Task<bool> LoadDatasetOrStoreAsync(string filename, IProgress<(string, double)> progress = null, bool autoSave = false)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Extension == ".pds")
                {
                    progress?.Report(("Loading dataset...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.LoadAsync(filename, autoSave);
                }
                else
                {
                    var name = fileInfo.Name;

                    if (fileInfo.Extension == ".psi")
                    {
                        name = name.Substring(0, Path.GetFileNameWithoutExtension(filename).LastIndexOf('.'));

                        // Determine if the store is currently live
                        bool isLive = PsiStoreMonitor.IsStoreLive(name, fileInfo.DirectoryName);

                        // Determine if this is a remote store
                        bool isRemoteStore = this.IsRemoteStore(fileInfo);

                        // Memory mapped files are not updated if the file being mapped is on a remote machine.
                        // Therefore, if the store is both live and on a remote machine we should decline to load it.
                        if (isLive && isRemoteStore)
                        {
                            // If there's no progress window then the store open operation is happening
                            // without any UI. Only show the error message if the UI feedback is enabled.
                            if (progress != null)
                            {
                                new MessageBoxWindow(
                                    Application.Current.MainWindow,
                                    "Cannot Load Live Remote Store",
                                    "The store cannot be opened because it resides on a remote machine and is currently live.  When the remote store is no longer live it can then be opened.",
                                    "Close",
                                    null).ShowDialog();
                            }

                            return false;
                        }

                        progress?.Report(("Opening store...", 0));

                        // If the store is not closed, and nobody's holding a reference to it, assume it was closed improperly and needs to be repaired.
                        if (!PsiStore.IsClosed(name, fileInfo.DirectoryName) && !isLive)
                        {
                            progress?.Report(("Repairing store...", 0.5));
                            await Task.Run(() => PsiStore.Repair(name, fileInfo.DirectoryName));
                        }
                    }

                    progress?.Report(("Loading store...", 0.5));
                    var readerType = this.PluginMap.GetStreamReaderType(fileInfo.Extension);
                    var reader = Psi.Data.StreamReader.Create(name, fileInfo.DirectoryName, readerType);
                    this.DatasetViewModel = await DatasetViewModel.CreateFromStoreAsync(reader);
                }
            }
            finally
            {
                // report completion
                progress?.Report(("Done", 1.0));
            }

            return true;
        }

        private bool IsRemoteStore(FileInfo fileInfo)
        {
            // Determines whether a store is a remote store.  Note that MemoryMappedFiles do not update correctly
            // with local stores that are being accessed via a UNC path (i.e. a share path), so we'll result that
            // any store with a UNC path is remote, even if the share path points to the local machine. If the path
            // is not a UNC path, we still need to check the drive type of the path in case the user has mapped a
            // network drive.
            if (new Uri(fileInfo.FullName).IsUnc)
            {
                return true;
            }

            return new DriveInfo(fileInfo.DirectoryName).DriveType == DriveType.Network;
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
