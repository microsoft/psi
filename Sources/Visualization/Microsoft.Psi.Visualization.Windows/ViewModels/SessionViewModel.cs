// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents a view model of a session.
    /// </summary>
    public class SessionViewModel : ObservableTreeNodeObject
    {
        private readonly DatasetViewModel datasetViewModel;
        private readonly ObservableCollection<PartitionViewModel> internalPartitionViewModels;
        private readonly ReadOnlyObservableCollection<PartitionViewModel> partitionViewModels;

        private Session session;
        private bool containsLivePartitions = false;

        private string auxiliaryInfo = string.Empty;

        private RelayCommand addPartitionFromFileCommand;
        private RelayCommand addMultiplePartitionsFromFolderCommand;
        private RelayCommand removeSessionCommand;
        private RelayCommand deleteSessionCommand;
        private RelayCommand visualizeSessionCommand;
        private RelayCommand<MouseButtonEventArgs> mouseDoubleClickCommand;
        private RelayCommand<Grid> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionViewModel"/> class.
        /// </summary>
        /// <param name="datasetViewModel">The view model of the dataset to which this session belongs.</param>
        /// <param name="session">The session for which to create the view model.</param>
        public SessionViewModel(DatasetViewModel datasetViewModel, Session session)
        {
            this.session = session;
            this.datasetViewModel = datasetViewModel;
            this.datasetViewModel.PropertyChanged += this.OnDatasetViewModelPropertyChanged;
            this.internalPartitionViewModels = new ObservableCollection<PartitionViewModel>();
            this.partitionViewModels = new ReadOnlyObservableCollection<PartitionViewModel>(this.internalPartitionViewModels);

            foreach (var partition in this.session.Partitions)
            {
                this.internalPartitionViewModels.Add(new PartitionViewModel(this, partition));
            }
        }

        /// <summary>
        /// Gets the dataset viewmodel.
        /// </summary>
        [Browsable(false)]
        public DatasetViewModel DatasetViewModel => this.datasetViewModel;

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        [DisplayName("Session Name")]
        [Description("The name of the session.")]
        public string Name
        {
            get => this.session.Name;
            set
            {
                if (this.session.Name != value)
                {
                    this.RaisePropertyChanging(nameof(this.Name));
                    this.session.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the session view models contains invalid partitions.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool ContainsInvalidPartitions => this.PartitionViewModels.Any(p => !p.IsValidPartition);

        /// <summary>
        /// Gets the auxiliary info.
        /// </summary>
        [Browsable(false)]
        public string AuxiliaryInfo
        {
            get => this.auxiliaryInfo;
            private set => this.Set(nameof(this.AuxiliaryInfo), ref this.auxiliaryInfo, value);
        }

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the session.
        /// </summary>
        [DisplayName("First Message OriginatingTime")]
        [Description("The originating time of the first message in the session.")]
        public string FirstMessageOriginatingTimeString => DateTimeHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the session.
        /// </summary>
        [DisplayName("Last Message OriginatingTime")]
        [Description("The originating time of the last message in the session.")]
        public string LastMessageOriginatingTimeString => DateTimeHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets the originating time of the first message in the session.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageOriginatingTime
            => this.partitionViewModels.Count > 0 ? this.partitionViewModels.Min(p => p.FirstMessageOriginatingTime) : default;

        /// <summary>
        /// Gets the originating time of the last message in the session.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime
            => this.partitionViewModels.Count > 0 ? this.partitionViewModels.Max(p => p.LastMessageOriginatingTime) : default;

        /// <summary>
        /// Gets the opacity of UI elements associated with this session. UI element opacity is reduced for sessions that are not the current session.
        /// </summary>
        [Browsable(false)]
        public double UiElementOpacity => this.DatasetViewModel.CurrentSessionViewModel == this ? 1.0d : 0.5d;

        /// <summary>
        /// Gets the originating time interval (earliest to latest) for this session.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.partitionViewModels
                    .Where(p => p.FirstMessageOriginatingTime != null && p.LastMessageOriginatingTime != null)
                    .Select(p => new TimeInterval(p.FirstMessageOriginatingTime.Value, p.LastMessageOriginatingTime.Value)));

        /// <summary>
        /// Gets the collection of partitions in this session.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<PartitionViewModel> PartitionViewModels => this.partitionViewModels;

        /// <summary>
        /// Gets a value indicating whether this session is the parent dataset's current session.
        /// </summary>
        [Browsable(false)]
        public bool IsCurrentSession => this.DatasetViewModel.CurrentSessionViewModel == this;

        /// <summary>
        /// Gets a value indicating whether this session contains live partitions.
        /// </summary>
        [Browsable(false)]
        public bool ContainsLivePartitions
        {
            get => this.containsLivePartitions;

            private set
            {
                this.RaisePropertyChanging(nameof(this.ContainsLivePartitions));
                this.containsLivePartitions = value;
                this.RaisePropertyChanged(nameof(this.ContainsLivePartitions));
            }
        }

        /// <summary>
        /// Gets the add partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddPartitionFromFileCommand => this.addPartitionFromFileCommand ??= new RelayCommand(async () => await this.AddPartitionFromFileAsync());

        /// <summary>
        /// Gets the add multiple partitions command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddMultiplePsiStorePartitionsFromFolderCommand => this.addMultiplePartitionsFromFolderCommand ??= new RelayCommand(async () => await this.AddMultiplePsiStorePartitionsFromFolderAsync());

        /// <summary>
        /// Gets the remove session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemoveSessionCommand => this.removeSessionCommand ??= new RelayCommand(() => this.RemoveSession());

        /// <summary>
        /// Gets the delete session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DeleteSessionCommand => this.deleteSessionCommand ??= new RelayCommand(() => this.DeleteSession(), () => !this.ContainsLivePartitions);

        /// <summary>
        /// Gets the visualize session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand VisualizeSessionCommand => this.visualizeSessionCommand ??= new RelayCommand(() => this.DatasetViewModel.VisualizeSession(this));

        /// <summary>
        /// Gets the mouse double click command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseDoubleClickCommand
            => this.mouseDoubleClickCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.OnMouseDoubleClick(e));

        /// <summary>
        /// Gets the command that executes when opening the session context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<Grid> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<Grid>(grid => grid.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the underlying session.
        /// </summary>
        [Browsable(false)]
        public Session Session => this.session;

        /// <summary>
        /// Updates the session view model based on the latest version of the corresponding session.
        /// </summary>
        /// <param name="session">The new session.</param>
        public void Update(Session session)
        {
            this.session = session;

            var oldPartitions = new HashSet<PartitionViewModel>();
            foreach (var existingPartition in this.internalPartitionViewModels)
            {
                oldPartitions.Add(existingPartition);
            }

            foreach (var partition in this.session.Partitions)
            {
                var existingPartition = this.internalPartitionViewModels.FirstOrDefault(s => s.Name == partition.Name);
                if (existingPartition != null)
                {
                    existingPartition.Update(partition);
                    oldPartitions.Remove(existingPartition);
                }
                else
                {
                    this.internalPartitionViewModels.Add(new PartitionViewModel(this, partition));
                }
            }

            // Now remove all the old partitions that are no longer in the dataset
            foreach (var partition in oldPartitions)
            {
                this.internalPartitionViewModels.Remove(partition);
            }
        }

        /// <summary>
        /// Adds a new partition based on a specified stream reader.
        /// </summary>
        /// <param name="streamReader">The stream reader for the data store.</param>
        /// <param name="partitionName">The partition name (defaults to the stream reader name).</param>
        /// <param name="progress">An optional progress updates receiver.</param>
        /// <returns>The task for adding a new partition based on a specified stream reader.</returns>
        public async Task AddPartitionAsync(IStreamReader streamReader, string partitionName = null, IProgress<(string, double)> progress = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? streamReader.Name);
            this.AddPartition(await this.session.AddPartitionAsync(streamReader, partitionName, progress));
        }

        /// <summary>
        /// Adds a new partition from a user-specified file.
        /// </summary>
        /// <returns>The task for adding a new partition from a user-specified file.</returns>
        public async Task AddPartitionFromFileAsync()
        {
            var formats = VisualizationContext.Instance.PluginMap.GetStreamReaderExtensions();
            var openFileDialog = new Win32.OpenFileDialog
            {
                DefaultExt = ".psi",
                Filter = string.Join("|", formats.Select(f => $"{f.Name}|*{f.Extensions}")),
            };

            // Get the path to the last partition added to prepopulate the dialog
            var lastPartition = this.session.Partitions.LastOrDefault();
            if (lastPartition != default)
            {
                openFileDialog.InitialDirectory = lastPartition.StorePath;
            }

            bool? result = openFileDialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                var name = fileInfo.Name.Split('.')[0];
                var path = fileInfo.DirectoryName;

                // Get the stream reader type and add the partition
                var readerType = VisualizationContext.Instance.PluginMap.GetStreamReaderType(fileInfo.Extension);
                var streamReader = Psi.Data.StreamReader.Create(name, path, readerType);
                await ProgressWindow.RunWithProgressAsync(
                    $"Adding partition from {openFileDialog.FileName} ...",
                    progress => this.AddPartitionAsync(streamReader, progress: progress));

                // Update stream bindings if this is the current session being visualized
                if (this.IsCurrentSession)
                {
                    var visualizationContainer = VisualizationContext.Instance.VisualizationContainer;
                    var sessionExtents = this.OriginatingTimeInterval;
                    visualizationContainer.Navigator.DataRange.Set(sessionExtents);
                    visualizationContainer.UpdateStreamSources(this);
                }
            }
        }

        /// <summary>
        /// Adds multiple partitions from a user-specified folder.
        /// </summary>
        /// <returns>The task for adding multiple partitions from a user-specified folder.</returns>
        public async Task AddMultiplePsiStorePartitionsFromFolderAsync()
        {
            var selectFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder containing the partitions to be added to the session.",
                ShowNewFolderButton = false,
            };

            // Get the path to the last partition added to prepopulate the dialog
            var lastPartition = this.session.Partitions.LastOrDefault();
            if (lastPartition != default)
            {
                selectFolderDialog.SelectedPath = lastPartition.StorePath;
            }

            if (selectFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var existingPartitions = await ProgressWindow.RunWithProgressAsync(
                    $"Adding multiple partitions from {selectFolderDialog.SelectedPath} ...",
                    async progress => await this.AddMultiplePsiStorePartitionsFromFolderAsync(selectFolderDialog.SelectedPath, progress));

                if (existingPartitions.Count > 0)
                {
                    var message = "The following partitions were already in the session and were not added:\r\n\r\n" +
                        $"{string.Join("\r\n", existingPartitions.Select(p => Path.Combine(p.StorePath, p.StoreName)))}";

                    // Inform the user of partitions that were already present in the session
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Existing Partitions",
                        message,
                        "Close",
                        null).ShowDialog();
                }

                // Update stream bindings if this is the current session being visualized
                if (this.IsCurrentSession)
                {
                    var visualizationContainer = VisualizationContext.Instance.VisualizationContainer;
                    var sessionExtents = this.OriginatingTimeInterval;
                    visualizationContainer.Navigator.DataRange.Set(sessionExtents);
                    visualizationContainer.UpdateStreamSources(this);
                }
            }
        }

        /// <summary>
        /// Adds multiple psi store partitions from a specified folder.
        /// </summary>
        /// <param name="folderName">The folder to add partitions from.</param>
        /// <param name="progress">An optional progress updates receiver.</param>
        /// <returns>The task of adding multiple psi store partitions from a specified folder.</returns>
        public async Task<List<IPartition>> AddMultiplePsiStorePartitionsFromFolderAsync(string folderName, IProgress<(string, double)> progress = null)
        {
            var existingPartitions = new List<IPartition>();
            var stores = PsiStore.EnumerateStores(folderName, false).ToArray();
            var i = 0;
            foreach (var store in stores)
            {
                // Check if the session already contains a partition for this store
                var partition = this.session.Partitions.FirstOrDefault(p => p.StoreName == store.Name && p.StorePath == store.Path);
                if (partition != default)
                {
                    existingPartitions.Add(partition);
                    continue;
                }

                // Add the new partition, ensuring that the partition name does not clash with an existing one
                partition = await this.session.AddPartitionAsync(
                    new PsiStoreStreamReader(store.Name, store.Path),
                    this.EnsureUniquePartitionName(store.Name),
                    new Progress<(string, double)>(t => progress?.Report(($"Adding partition {store.Name}\n{t.Item1}", (i + t.Item2) / stores.Length))));
                this.AddPartition(partition);

                i++;
            }

            progress?.Report((string.Empty, 1));

            return existingPartitions;
        }

        /// <summary>
        /// Removes a specified partition from the underlying session.
        /// </summary>
        /// <param name="partitionViewModel">The view model of the partition to be removed.</param>
        public void RemovePartition(PartitionViewModel partitionViewModel)
        {
            this.session.RemovePartition(partitionViewModel.Partition);
            this.internalPartitionViewModels.Remove(partitionViewModel);
            this.DatasetViewModel.UpdateLivePartitionStatuses();

            // Update stream bindings if this is the current session being visualized
            if (this.IsCurrentSession)
            {
                var visualizationContainer = VisualizationContext.Instance.VisualizationContainer;
                var sessionExtents = this.OriginatingTimeInterval;
                visualizationContainer.UnbindVisualizationObjectsFromStore(partitionViewModel.StoreName, partitionViewModel.StorePath, partitionViewModel.Name);
                visualizationContainer.Navigator.DataRange.Set(sessionExtents);
            }
        }

        /// <summary>
        /// Removes this session from the dataset it belongs to.
        /// </summary>
        public void RemoveSession()
        {
            // Prompt the user to save changes to any dirty partitions before continuing.
            foreach (PartitionViewModel partitionViewModel in this.PartitionViewModels)
            {
                if (!partitionViewModel.PromptSaveChangesAndContinue())
                {
                    return;
                }
            }

            this.datasetViewModel.RemoveSession(this);
        }

        /// <summary>
        /// Removes this session and permanently deletes all of its partitions from disk.
        /// </summary>
        public void DeleteSession()
        {
            var confirmation = new MessageBoxWindow(
               Application.Current.MainWindow,
               "Are you sure?",
               "Are you sure you want to delete this session? This will permanently delete all of its partitions from disk.",
               "Yes",
               "Cancel");

            if (confirmation.ShowDialog() == true)
            {
                try
                {
                    // Delete all partitions from disk
                    foreach (PartitionViewModel partitionViewModel in this.PartitionViewModels)
                    {
                        PsiStore.Delete((partitionViewModel.StoreName, partitionViewModel.StorePath), true);
                    }

                    this.datasetViewModel.RemoveSession(this);
                }
                catch (Exception e)
                {
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Delete Partition Error",
                        $"An error occurred while attempting to delete the session: {e.Message}",
                        "Close",
                        null).ShowDialog();
                }
            }
        }

        /// <summary>
        /// Attempts to create a stream source for a specified stream binding.
        /// </summary>
        /// <param name="streamBinding">A stream binding that describes the stream to bind to.</param>
        /// <param name="allocator">The allocator to use when reading data.</param>
        /// <param name="deallocator">The deallocator to use when reading data.</param>
        /// <returns>A stream source if a source was found to bind to, otherwise returns null.</returns>
        public StreamSource CreateStreamSource(StreamBinding streamBinding, Func<dynamic> allocator, Action<dynamic> deallocator) =>
            this.PartitionViewModels
                .FirstOrDefault(p => p.Name == streamBinding.PartitionName)?
                .CreateStreamSource(streamBinding, allocator, deallocator);

        /// <summary>
        /// Finds a stream tree node within a session.
        /// </summary>
        /// <param name="partitionName">The name of the partition containing the stream.</param>
        /// <param name="streamName">The name of the stream to search for.</param>
        /// <returns>A stream tree node representing the stream, or null if the stream does not exist in the session.</returns>
        public StreamTreeNode FindStreamTreeNode(string partitionName, string streamName) =>
            this.PartitionViewModels
                .FirstOrDefault(p => p.Name == partitionName)?
                .FindStreamTreeNode(streamName);

        /// <summary>
        /// Ensures that the derived stream tree nodes necessary for the set of visualization objects in a specified container.
        /// </summary>
        /// <param name="visualizationContainer">The visualization container.</param>
        public void EnsureDerivedStreamTreeNodesExist(VisualizationContainer visualizationContainer)
        {
            // Check if the visualization container contains any stream member visualizers.
            var derivedStreamVisualizationObjects = visualizationContainer.GetDerivedStreamVisualizationObjects();

            foreach (IStreamVisualizationObject derivedStreamVisualizationObject in derivedStreamVisualizationObjects)
            {
                // Find the stream tree node corresponding to the source data
                var sourceStreamTreeNode = this.FindStreamTreeNode(
                    derivedStreamVisualizationObject.StreamBinding.PartitionName,
                    derivedStreamVisualizationObject.StreamBinding.SourceStreamName);

                // If the source exists
                if (sourceStreamTreeNode != null)
                {
                    // Find the derived stream tree node used by this visualizer
                    var streamTreeNode = this.FindStreamTreeNode(
                        derivedStreamVisualizationObject.StreamBinding.PartitionName,
                        derivedStreamVisualizationObject.StreamBinding.StreamName);

                    // Find the partition view model
                    var partitionViewModel = this.PartitionViewModels.FirstOrDefault(
                        p => p.Name == derivedStreamVisualizationObject.StreamBinding.PartitionName);

                    // If the derived stream does not exist, and we have the partition
                    if (streamTreeNode == null && partitionViewModel != null)
                    {
                        partitionViewModel.RootStreamTreeNode.AddChild(
                            derivedStreamVisualizationObject.StreamBinding.StreamName,
                            sourceStreamTreeNode.SourceStreamMetadata,
                            derivedStreamVisualizationObject.StreamBinding.DerivedStreamAdapterType,
                            derivedStreamVisualizationObject.StreamBinding.DerivedStreamAdapterArguments,
                            true);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"Session: {this.Name}";

        /// <summary>
        /// Checks all partitions in the session to determine whether they have an active writer attached and updates their IsLivePartition property.
        /// </summary>
        internal void UpdateLivePartitionStatuses()
        {
            this.ContainsLivePartitions = false;
            foreach (PartitionViewModel partitionViewModel in this.PartitionViewModels)
            {
                this.ContainsLivePartitions |= partitionViewModel.UpdateLiveStatus();
            }
        }

        private PartitionViewModel AddPartition(IPartition partition)
        {
            var partitionViewModel = new PartitionViewModel(this, partition);
            this.internalPartitionViewModels.Add(partitionViewModel);
            return partitionViewModel;
        }

        private string EnsureUniquePartitionName(string partitionName)
        {
            int suffix = 0;
            string partitionNamePrefix = partitionName;

            // ensure that partition name is unique
            while (this.PartitionViewModels.Any(pvm => pvm.Name == partitionName))
            {
                // append numeric suffix to ensure uniqueness
                partitionName = $"{partitionNamePrefix}_{++suffix}";
            }

            return partitionName;
        }

        private void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
            }
            else if (e.PropertyName == nameof(this.DatasetViewModel.ShowAuxiliarySessionInfo))
            {
                this.UpdateAuxiliaryInfo();
            }
        }

        private void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (this.DatasetViewModel.CurrentSessionViewModel != this)
            {
                // Visualize the current session
                this.DatasetViewModel.VisualizeSession(this);

                // Expand the session tree node to show partitions
                this.IsTreeNodeExpanded = true;

                // If a single partition, expand the partition to show streams
                if (this.partitionViewModels.Count == 1)
                {
                    this.partitionViewModels.First().IsTreeNodeExpanded = true;
                }

                e.Handled = true;
            }
        }

        private void UpdateAuxiliaryInfo()
        {
            switch (this.DatasetViewModel.ShowAuxiliarySessionInfo)
            {
                case AuxiliarySessionInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliarySessionInfo.Duration:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Span.ToString(@"d\.hh\:mm\:ss");
                    break;
                case AuxiliarySessionInfo.StartDate:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToShortDateString();
                    break;
                case AuxiliarySessionInfo.StartDateLocal:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToLocalTime().ToShortDateString();
                    break;
                case AuxiliarySessionInfo.StartTime:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToShortTimeString();
                    break;
                case AuxiliarySessionInfo.StartTimeLocal:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToLocalTime().ToShortTimeString();
                    break;
                case AuxiliarySessionInfo.StartDateTime:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToString();
                    break;
                case AuxiliarySessionInfo.StartDateTimeLocal:
                    this.AuxiliaryInfo = this.Session.MessageOriginatingTimeInterval.Left.ToLocalTime().ToString();
                    break;
                case AuxiliarySessionInfo.Size:
                    this.AuxiliaryInfo = this.Session.Size.HasValue ? SizeHelper.FormatSize(this.Session.Size.Value) : "?";
                    break;
                case AuxiliarySessionInfo.DataThroughputPerHour:
                    this.AuxiliaryInfo = this.Session.Size.HasValue ? SizeHelper.FormatThroughput(this.Session.Size.Value / this.Session.MessageOriginatingTimeInterval.Span.TotalHours, "hour") : "?";
                    break;
                case AuxiliarySessionInfo.DataThroughputPerMinute:
                    this.AuxiliaryInfo = this.Session.Size.HasValue ? SizeHelper.FormatThroughput(this.Session.Size.Value / this.Session.MessageOriginatingTimeInterval.Span.TotalMinutes, "min") : "?";
                    break;
                case AuxiliarySessionInfo.DataThroughputPerSecond:
                    this.AuxiliaryInfo = this.Session.Size.HasValue ? SizeHelper.FormatThroughput(this.Session.Size.Value / this.Session.MessageOriginatingTimeInterval.Span.TotalSeconds, "sec") : "?";
                    break;
                case AuxiliarySessionInfo.StreamCount:
                    this.AuxiliaryInfo = this.Session.StreamCount.HasValue ? (this.Session.StreamCount == 0 ? "0" : $"{this.Session.StreamCount.Value:0,0}") : "?";
                    break;
                default:
                    break;
            }
        }

        private ContextMenu CreateContextMenu()
        {
            // Create the context menu
            var contextMenu = new ContextMenu();

            // Add the visualize session context menu if this is not the currently visualized session
            if (!this.IsCurrentSession)
            {
                contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(string.Empty, ContextMenuName.VisualizeSession, this.VisualizeSessionCommand));
                contextMenu.Items.Add(new Separator());
            }

            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionAdd, "Add Partition from File ...", this.AddPartitionFromFileCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionAddMultiple, "Add Multiple Partitions from Folder ...", this.AddMultiplePsiStorePartitionsFromFolderCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.SessionRemove, "Remove", this.RemoveSessionCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(null, "Delete Session", this.DeleteSessionCommand));
            contextMenu.Items.Add(new Separator());

            // Add run batch processing task menu
            var runTasksMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Run Batch Processing Task", null);
            var batchProcessingTasks = VisualizationContext.Instance.PluginMap.BatchProcessingTasks;
            runTasksMenuItem.IsEnabled = batchProcessingTasks.Any();
            foreach (var batchProcessingTaskNamespace in batchProcessingTasks.Select(bpt => bpt.Namespace).Distinct())
            {
                var namespaceMenuItem = MenuItemHelper.CreateMenuItem(
                    null,
                    batchProcessingTaskNamespace,
                    null);

                foreach (var batchProcessingTask in batchProcessingTasks.Where(bpt => bpt.Namespace == batchProcessingTaskNamespace))
                {
                    namespaceMenuItem.Items.Add(
                        MenuItemHelper.CreateMenuItem(
                            batchProcessingTask.IconSourcePath,
                            batchProcessingTask.Name,
                            new VisualizationCommand<BatchProcessingTaskMetadata>(async bpt => await VisualizationContext.Instance.RunSessionBatchProcessingTask(this, bpt)),
                            tag: batchProcessingTask,
                            isEnabled: true,
                            commandParameter: batchProcessingTask));
                }

                runTasksMenuItem.Items.Add(namespaceMenuItem);
            }

            contextMenu.Items.Add(runTasksMenuItem);
            contextMenu.Items.Add(new Separator());

            // Add copy to clipboard menu with sub-menu items
            var copyToClipboardMenuItem = MenuItemHelper.CreateMenuItem(
                string.Empty,
                "Copy to Clipboard",
                null);

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Session Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.Name));

            contextMenu.Items.Add(copyToClipboardMenuItem);

            // Add show session info menu
            var showSessionInfoMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Show Sessions Info", null);
            foreach (var auxiliarySessionInfo in Enum.GetValues(typeof(AuxiliarySessionInfo)))
            {
                var auxiliarySessionInfoValue = (AuxiliarySessionInfo)auxiliarySessionInfo;
                var auxiliarySessionInfoName = auxiliarySessionInfoValue switch
                {
                    AuxiliarySessionInfo.None => "None",
                    AuxiliarySessionInfo.Duration => "Duration",
                    AuxiliarySessionInfo.StartDate => "Start Date (UTC)",
                    AuxiliarySessionInfo.StartDateLocal => "Start Date (Local)",
                    AuxiliarySessionInfo.StartTime => "Start Time (UTC)",
                    AuxiliarySessionInfo.StartTimeLocal => "Start Time (Local)",
                    AuxiliarySessionInfo.StartDateTime => "Start DateTime (UTC)",
                    AuxiliarySessionInfo.StartDateTimeLocal => "Start DateTime (Local)",
                    AuxiliarySessionInfo.Size => "Size",
                    AuxiliarySessionInfo.DataThroughputPerHour => "Throughput (bytes per hour)",
                    AuxiliarySessionInfo.DataThroughputPerMinute => "Throughput (bytes per minute)",
                    AuxiliarySessionInfo.DataThroughputPerSecond => "Throughput (bytes per second)",
                    AuxiliarySessionInfo.StreamCount => "Number of Streams",
                    _ => throw new NotImplementedException(),
                };

                showSessionInfoMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        this.DatasetViewModel.ShowAuxiliarySessionInfo == auxiliarySessionInfoValue ? IconSourcePath.Checkmark : null,
                        auxiliarySessionInfoName,
                        new VisualizationCommand<AuxiliarySessionInfo>(asi => this.DatasetViewModel.ShowAuxiliarySessionInfo = asi),
                        commandParameter: auxiliarySessionInfoValue));
            }

            contextMenu.Items.Add(showSessionInfoMenuItem);

            // Add open session folder in windows explorer
            if (this.PartitionViewModels.Any() && this.PartitionViewModels.Select(pvm => pvm.Partition.StorePath).Distinct().Count() == 1)
            {
                contextMenu.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Open Session Folder in Explorer",
                        new VisualizationCommand(() => { Process.Start("explorer.exe", this.PartitionViewModels.First().StorePath); }),
                        commandParameter: default));
            }

            return contextMenu;
        }
    }
}
