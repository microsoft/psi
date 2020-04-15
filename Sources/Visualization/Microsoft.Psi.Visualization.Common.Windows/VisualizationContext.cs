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
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Commands;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using WpfControls = System.Windows.Controls;

    /// <summary>
    /// Data context for visualization.
    /// </summary>
    public class VisualizationContext : ObservableObject
    {
        private VisualizationContainer visualizationContainer;
        private DatasetViewModel datasetViewModel;
        private DispatcherTimer liveStatusTimer = null;

        private List<TypeKeyedActionCommand> typeVisualizerActions = new List<TypeKeyedActionCommand>();

        // A map of message types to visualizers and visualization panels
        private VisualizerMap visualizerMap = new VisualizerMap();

        static VisualizationContext()
        {
            VisualizationContext.Instance = new VisualizationContext();
        }

        private VisualizationContext()
        {
            this.RegisterCustomSerializers();

            var booleanSchema = new AnnotationSchema("Boolean");
            booleanSchema.AddSchemaValue(null, System.Drawing.Color.Gray);
            booleanSchema.AddSchemaValue("false", System.Drawing.Color.Red);
            booleanSchema.AddSchemaValue("true", System.Drawing.Color.Green);
            AnnotationSchemaRegistry.Default.Register(booleanSchema);

            this.DatasetViewModel = new DatasetViewModel();
            this.DatasetViewModels = new ObservableCollection<DatasetViewModel> { this.datasetViewModel };

            // Periodically check if there's any live partitions in the dataset
            this.liveStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Normal, new EventHandler(this.OnLiveStatusTimer), Application.Current.Dispatcher);
            this.liveStatusTimer.Start();
        }

        /// <summary>
        /// Gets the visualization context singleton.
        /// </summary>
        public static VisualizationContext Instance { get; private set; }

        /// <summary>
        /// Gets the visualizer map.
        /// </summary>
        public VisualizerMap VisualizerMap => this.visualizerMap;

        /// <summary>
        /// Gets the annotation schema registry.
        /// </summary>
        public AnnotationSchemaRegistry AnnotationSchemaRegistry => AnnotationSchemaRegistry.Default;

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
            this.VisualizationContainer.Clear();

            if (!string.IsNullOrWhiteSpace(path))
            {
                // Load the new layout.  If this operation fails, then null will be returned.
                VisualizationContainer visualizationContainer = VisualizationContainer.Load(path, name);
                if (visualizationContainer != null)
                {
                    this.VisualizationContainer = visualizationContainer;

                    // zoom into the current session if there is one
                    SessionViewModel sessionViewModel = this.DatasetViewModel.CurrentSessionViewModel;
                    if (sessionViewModel != null)
                    {
                        // Zoom to the current session extents
                        this.VisualizationContainer.ZoomToRange(sessionViewModel.OriginatingTimeInterval);

                        // set the data range to the dataset
                        this.VisualizationContainer.Navigator.DataRange.SetRange(this.DatasetViewModel.OriginatingTimeInterval);
                    }

                    // update store bindings
                    this.VisualizationContainer.UpdateStreamBindings(sessionViewModel?.Session);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the message type for a stream.  If the message type is unknown (i.e. the assembly
        /// that contains the message type is not currently being referenced by PsiStudio, then
        /// we return the generic object type.
        /// </summary>
        /// <param name="streamTreeNode">The stream tree node.</param>
        /// <returns>The type of messages in the stream.</returns>
        public Type GetStreamType(StreamTreeNode streamTreeNode)
        {
            return TypeResolutionHelper.GetVerifiedType(streamTreeNode.TypeName) ?? TypeResolutionHelper.GetVerifiedType(streamTreeNode.TypeName.Split(',')[0]) ?? typeof(object);
        }

        /// <summary>
        /// Visualizes a streamin the visualization container.
        /// </summary>
        /// <param name="streamTreeNode">The stream to visualize.</param>
        /// <param name="visualizerMetadata">The visualizer metadata to use.</param>
        /// <param name="visualizationPanel">The visualization panel to add the stream to (or null).</param>
        public void VisualizeStream(StreamTreeNode streamTreeNode, VisualizerMetadata visualizerMetadata, VisualizationPanel visualizationPanel)
        {
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

            // Create the visualization object
            IStreamVisualizationObject visualizationObject = Activator.CreateInstance(visualizerMetadata.VisualizationObjectType) as IStreamVisualizationObject;
            visualizationObject.Name = visualizerMetadata.VisualizationFormatString.Replace(VisualizationObjectAttribute.DefaultVisualizationFormatString, streamTreeNode.StreamName);

            // Add the visualization object to the panel
            visualizationPanel.AddVisualizationObject(visualizationObject as VisualizationObject);

            // NOTE: Special case for message visualization object, we need to insert the object adapter.  This is because StreamReader<T> has a templated
            // ReadStream<T> method and we need us use the same type as the type of the actual stream or the cast when creating the view will fail.
            Type streamAdapterType = visualizerMetadata.StreamAdapterType;
            if (visualizerMetadata.VisualizationObjectType == typeof(MessageVisualizationObject))
            {
                streamAdapterType = typeof(ObjectAdapter<>).MakeGenericType(this.GetStreamType(streamTreeNode));
            }

            // Update the binding
            visualizationObject.StreamBinding = new StreamBinding(streamTreeNode.StreamName, streamTreeNode.Partition.Name, typeof(SimpleReader), streamAdapterType, visualizerMetadata.SummarizerType, null);
            visualizationObject.UpdateStreamBinding(this.DatasetViewModel.CurrentSessionViewModel.Session);

            // Check if this is a live stream
            this.DatasetViewModel.CurrentSessionViewModel.UpdateLivePartitionStatuses();

            // Select the new visualization object in the visualizations tree
            visualizationObject.IsTreeNodeSelected = true;
        }

        /// <summary>
        /// Asynchronously opens a previously persisted dataset.
        /// </summary>
        /// <param name="filename">Fully qualified path to dataset file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task OpenDatasetAsync(string filename)
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
            var loadDatasetTask = this.LoadDatasetOrStoreAsync(filename, progress);

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
            if (this.DatasetViewModel.CurrentSessionViewModel.ContainsLivePartitions && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live)
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
        /// Gets the menu for a stream in the datasets view.
        /// </summary>
        /// <param name="streamTreeNode">The stream tree node.</param>
        /// <returns>The contextmenu for the stream.</returns>
        internal WpfControls.ContextMenu GetDatasetStreamMenu(StreamTreeNode streamTreeNode)
        {
            // Create the context menu
            WpfControls.ContextMenu contextMenu = new WpfControls.ContextMenu();

            if (streamTreeNode.IsStream)
            {
                // Get the message type.  Type of object will be returned if we don't reference the
                // assembly that contains the message type.  This will allow us to still display
                // the visualize messages and visualize latency menuitems.
                Type messageType = this.GetStreamType(streamTreeNode);

                // Get the list of visualization commands for this stream tree node
                List<VisualizerMetadata> metadataItems = this.VisualizerMap.GetByDataType(messageType);

                // Standard commands are placed below the menu separator.  The above method returned a collection
                // where the "above the separator" items were placed before the "below the separator" items.
                bool containsItemsAboveSeparator = false;
                bool separatorAdded = false;

                // Add menuitems for each command available
                foreach (VisualizerMetadata metadata in metadataItems)
                {
                    containsItemsAboveSeparator |= metadata.IsAboveSeparator;

                    // check if it's time to add the menu separator
                    if (containsItemsAboveSeparator && metadata.IsBelowSeparator && !separatorAdded)
                    {
                        contextMenu.Items.Add(new WpfControls.Separator());
                        separatorAdded = true;
                    }

                    contextMenu.Items.Add(this.CreateVisualizeMenuItem(streamTreeNode, metadata));
                }

                // Add the "zoom to stream command"
                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.ZoomToStream, ContextMenuName.ZoomToStreamExtents, new VisualizationCommand((s) => this.ZoomToStreamExtents(streamTreeNode))));
            }

            return contextMenu;
        }

        private WpfControls.MenuItem CreateVisualizeMenuItem(StreamTreeNode streamTreeNode, VisualizerMetadata metadata)
        {
            return this.CreateMenuItem(
                metadata.IconSourcePath,
                metadata.CommandText,
                new VisualizationCommand((s) => this.VisualizeStream(streamTreeNode, metadata, VisualizationContext.Instance.VisualizationContainer.CurrentPanel)),
                metadata);
        }

        private WpfControls.MenuItem CreateMenuItem(string iconSourcePath, string commandText, VisualizationCommand command, object tag = null)
        {
            // Create the bitmap for the icon
            System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(iconSourcePath, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();

            // Create the icon
            WpfControls.Image icon = new WpfControls.Image();
            icon.Height = 16;
            icon.Width = 16;
            icon.Margin = new Thickness(4, 0, 0, 0);
            icon.Source = bitmapImage;

            // Create the menuitem
            WpfControls.MenuItem menuItem = new WpfControls.MenuItem();
            menuItem.Height = 25;
            menuItem.Icon = icon;
            menuItem.Header = commandText;
            menuItem.Command = command;
            menuItem.Tag = tag;

            return menuItem;
        }

        private async Task LoadDatasetOrStoreAsync(string filename, IProgress<(string, double)> progress = null)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Extension == ".psi")
                {
                    var name = fileInfo.Name.Substring(0, Path.GetFileNameWithoutExtension(filename).LastIndexOf('.'));

                    progress?.Report(("Opening store...", 0));

                    // If the store is not closed, and nobody's holding a reference to it, assume it was closed improperly and needs to be repaired.
                    if (!Store.IsClosed(name, fileInfo.DirectoryName) && !StoreReader.IsStoreLive(name, fileInfo.DirectoryName))
                    {
                        progress?.Report(("Repairing store...", 0.5));
                        await Task.Run(() => Store.Repair(name, fileInfo.DirectoryName));
                    }

                    progress?.Report(("Loading store...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.CreateFromExistingStoreAsync(name, fileInfo.DirectoryName);
                }
                else
                {
                    progress?.Report(("Loading dataset...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.LoadAsync(filename);
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

                // If the're no longer any live partitions, exit live mode
                if ((this.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == false) && (this.VisualizationContainer.Navigator.CursorMode == CursorMode.Live))
                {
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
                }
            }
        }

        private void RegisterCustomSerializers()
        {
            KnownSerializers.Default.Register<MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage<double>>(null);
        }
    }
}
