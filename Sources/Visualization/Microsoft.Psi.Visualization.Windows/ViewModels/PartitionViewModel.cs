// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Windows;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// The delegate for updating a stream's metadata.
    /// </summary>
    /// <param name="metadata">A collection of store metadata objects.</param>
    internal delegate void UpdateStreamMetadataDelegate(IEnumerable<Metadata> metadata);

    /// <summary>
    /// The delegate for new live messages.
    /// </summary>
    /// <param name="envelope">The message envelope.</param>
    internal delegate void LiveMessageReceivedDelegate(Envelope envelope);

    /// <summary>
    /// Represents a view model of a partition.
    /// </summary>
    public class PartitionViewModel : ObservableTreeNodeObject, IDisposable
    {
        private const int MonitorSleepDurationMs = 100;
        private const int LiveUiUpdateFrequencyMs = 50;

        private readonly SessionViewModel sessionViewModel;

        // A Dictionary of streams in the Partition, keyed by stream id
        private readonly Dictionary<int, StreamTreeNode> streamsById;

        private IPartition partition;
        private LiveMessageReceivedDelegate liveMessageCallback = null;
        private UpdateStreamMetadataDelegate newMetadataCallback = null;

        private string auxiliaryInfo = string.Empty;

        private StreamTreeNode rootStreamTreeNode;
        private bool isDirty = false;
        private bool isLivePartition = false;
        private Thread monitorWorker = null;
        private bool continueMonitoring = true;
        private bool disposed;

        private RelayCommand saveChangesCommand;
        private RelayCommand exportStoreCommand;
        private RelayCommand removePartitionCommand;
        private RelayCommand removePartitionFromAllSessionsCommand;
        private RelayCommand deletePartitionCommand;
        private RelayCommand deletePartitionFromAllSessionsCommand;
        private RelayCommand<Grid> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        /// <param name="sessionViewModel">The view model of the session to which this partition belongs.</param>
        /// <param name="partition">The partition for which to create the view model.</param>
        public PartitionViewModel(SessionViewModel sessionViewModel, IPartition partition)
        {
            this.partition = partition;
            this.sessionViewModel = sessionViewModel;
            this.sessionViewModel.DatasetViewModel.PropertyChanged += this.OnDatasetViewModelPropertyChanged;
            this.streamsById = new Dictionary<int, StreamTreeNode>();
            this.RootStreamTreeNode = StreamTreeNode.CreateRoot(this);
            foreach (var stream in this.partition.AvailableStreams)
            {
                this.streamsById[stream.Id] = this.RootStreamTreeNode.AddChild(stream.Name, stream, null, null);
                this.streamsById[stream.Id].IsTreeNodeExpanded = true;
            }

            // Check if this is a live partition (i.e. it still has a writer attached)
            this.UpdateLiveStatus(true);
            if (this.IsLivePartition)
            {
                this.liveMessageCallback = new LiveMessageReceivedDelegate(this.OnMessageWritten);
                this.newMetadataCallback = new UpdateStreamMetadataDelegate(this.UpdateStreamMetadata);
                this.MonitorLivePartition();
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        ~PartitionViewModel()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether the partition is valid.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsValidPartition => this.partition.IsStoreValid;

        /// <summary>
        /// Gets the save partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveChangesCommand
            => this.saveChangesCommand ??= new RelayCommand(() => this.SaveChanges(), () => this.IsDirty);

        /// <summary>
        /// Gets the export store command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExportStoreCommand
            => this.exportStoreCommand ??= new RelayCommand(
                () => this.ExportStore(),
                () => this.IsPsiPartition && !this.IsLivePartition && !this.isDirty && this.Partition.MessageOriginatingTimeInterval != TimeInterval.Empty);

        /// <summary>
        /// Gets or sets the partition name.
        /// </summary>
        [PropertyOrder(0)]
        [DisplayName("Partition Name")]
        [Description("The name of the partition.")]
        public string Name
        {
            get => this.partition.Name;
            set
            {
                if (this.partition.Name != value)
                {
                    this.RaisePropertyChanging(nameof(this.Name));
                    this.RaisePropertyChanging(nameof(this.DisplayName));
                    this.partition.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                    this.RaisePropertyChanged(nameof(this.DisplayName));
                }
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        [Browsable(false)]
        public string DisplayName => this.IsDirty ? this.Name + "*" : this.Name;

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
        /// Gets the store path of this partition.
        /// </summary>
        [DisplayName("Store Name")]
        [Description("The name of the store that represents the partition.")]
        public string StoreName => this.partition.StoreName;

        /// <summary>
        /// Gets the store path of this partition.
        /// </summary>
        [DisplayName("Store Path")]
        [Description("The full path to the store that represents the partition.")]
        public string StorePath => this.partition.StorePath;

        /// <summary>
        /// Gets the stream reader type name of this partition.
        /// </summary>
        [DisplayName("Stream Reader Type")]
        [Description("The type of stream reader used by the partition.")]
        public string StreamReaderTypeDisplayName => TypeSpec.Simplify(this.partition.StreamReaderTypeName);

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the partition.
        /// </summary>
        [DisplayName("First Message OriginatingTime")]
        [Description("The originating time of the first message in the partition.")]
        public string FirstMessageOriginatingTimeString => DateTimeHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the partition.
        /// </summary>
        [DisplayName("Last Message OriginatingTime")]
        [Description("The originating time of the last message in the partition.")]
        public string LastMessageOriginatingTimeString => DateTimeHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets the originating time of the first message in the partition.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageOriginatingTime => this.rootStreamTreeNode.SubsumedFirstMessageOriginatingTime;

        /// <summary>
        /// Gets the originating time of the last message in the partition.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime => this.rootStreamTreeNode.SubsumedLastMessageOriginatingTime;

        /// <summary>
        /// Gets the dataset that this partition belongs to.
        /// </summary>
        [Browsable(false)]
        public DatasetViewModel DatasetViewModel => this.sessionViewModel.DatasetViewModel;

        /// <summary>
        /// Gets the session that this partition belongs to.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel SessionViewModel => this.sessionViewModel;

        /// <summary>
        /// Gets a value indicating whether the partition is dirty (i.e. has uncommitted changes).
        /// </summary>
        [Browsable(false)]
        public bool IsDirty
        {
            get => this.isDirty;
            private set
            {
                if (value != this.isDirty)
                {
                    this.RaisePropertyChanging(nameof(this.IsDirty));
                    this.RaisePropertyChanging(nameof(this.DisplayName));
                    this.isDirty = value;
                    this.RaisePropertyChanged(nameof(this.IsDirty));
                    this.RaisePropertyChanged(nameof(this.DisplayName));
                }
            }
        }

        /// <summary>
        /// Gets or sets the root stream tree node of this partition.
        /// </summary>
        [Browsable(false)]
        public StreamTreeNode RootStreamTreeNode
        {
            get => this.rootStreamTreeNode;
            set => this.Set(nameof(this.RootStreamTreeNode), ref this.rootStreamTreeNode, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this partition is currently receiving new messages from an active writer.
        /// </summary>
        [Browsable(false)]
        public bool IsLivePartition
        {
            get => this.isLivePartition;

            set
            {
                if (value != this.isLivePartition)
                {
                    this.RaisePropertyChanging(nameof(this.IsLivePartition));
                    this.RaisePropertyChanging(nameof(this.IconSource));
                    this.isLivePartition = value;
                    this.RaisePropertyChanged(nameof(this.IsLivePartition));
                    this.RaisePropertyChanged(nameof(this.IconSource));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this partition is a \psi partition.
        /// </summary>
        [Browsable(false)]
        public bool IsPsiPartition
        {
            get
            {
                return this.Partition.StreamReaderTypeName == typeof(PsiStoreStreamReader).AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets the icon to use in UI representations of this partition.
        /// </summary>
        [Browsable(false)]
        public string IconSource
        {
            get
            {
                if (this.Partition.IsStoreValid)
                {
                    return this.IsLivePartition ? IconSourcePath.PartitionLive : IconSourcePath.Partition;
                }
                else
                {
                    return IconSourcePath.PartitionInvalid;
                }
            }
        }

        /// <summary>
        /// Gets the opacity of UI elements associated with this session. UI element opacity is reduced for sessions that are not the current session.
        /// </summary>
        [Browsable(false)]
        public double UiElementOpacity => this.sessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets the remove partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemovePartitionCommand => this.removePartitionCommand ??= new RelayCommand(() => this.RemovePartition());

        /// <summary>
        /// Gets the command for removing this partition from all sessions.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemovePartitionFromAllSessionsCommand =>
            this.removePartitionFromAllSessionsCommand ??= new RelayCommand(() => this.DatasetViewModel.RemovePartitionFromAllSessions(this.Name));

        /// <summary>
        /// Gets the delete partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DeletePartitionCommand => this.deletePartitionCommand ??= new RelayCommand(() => this.DeletePartition(), () => !this.IsLivePartition);

        /// <summary>
        /// Gets the delete partition from all sessions command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DeletePartitionFromAllSessionsCommand =>
            this.deletePartitionFromAllSessionsCommand ??= new RelayCommand(() => this.DatasetViewModel.DeletePartitionFromAllSessions(this.Name));

        /// <summary>
        /// Gets the command that executes when opening the partition context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<Grid> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<Grid>(grid => grid.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the underlying partition.
        /// </summary>
        internal IPartition Partition => this.partition;

        /// <summary>
        /// Updates the partition view model based on the latest version of the corresponding session.
        /// </summary>
        /// <param name="partition">The partition to point to.</param>
        public void Update(IPartition partition)
        {
            this.partition = partition;
            this.streamsById.Clear();
            this.RootStreamTreeNode = StreamTreeNode.CreateRoot(this);
            foreach (var stream in this.partition.AvailableStreams)
            {
                this.streamsById[stream.Id] = this.RootStreamTreeNode.AddChild(stream.Name, stream, null, null);
            }

            // Check if this is a live partition (i.e. it still has a writer attached)
            this.UpdateLiveStatus();
            if (this.IsLivePartition)
            {
                this.liveMessageCallback = new LiveMessageReceivedDelegate(this.OnMessageWritten);
                this.newMetadataCallback = new UpdateStreamMetadataDelegate(this.UpdateStreamMetadata);
                this.MonitorLivePartition();
            }

            this.IsTreeNodeExpanded = true;
        }

        /// <summary>
        /// Attempts to create a stream source based on a specified stream binding.
        /// </summary>
        /// <param name="streamBinding">A stream binding that describes the stream to bind to.</param>
        /// <param name="allocator">The allocator to use when reading data.</param>
        /// <param name="deallocator">The deallocator to use when reading data.</param>
        /// <returns>A stream source if a source was found to bind to, otherwise returns null.</returns>
        public StreamSource CreateStreamSource(StreamBinding streamBinding, Func<dynamic> allocator, Action<dynamic> deallocator)
        {
            StreamSource streamSource = null;

            // Check if the partition contains the required stream
            var streamMetadata = this.Partition.AvailableStreams.FirstOrDefault(s => s.Name == streamBinding.SourceStreamName);
            if (streamMetadata != default)
            {
                if (streamBinding.Summarizer != null)
                {
                    allocator = streamBinding.Summarizer.SourceAllocator;
                    deallocator = streamBinding.Summarizer.SourceDeallocator;
                }
                else if (streamBinding.StreamAdapter != null)
                {
                    allocator = streamBinding.StreamAdapter.SourceAllocator;
                    deallocator = streamBinding.StreamAdapter.SourceDeallocator;
                }

                // Create the stream source
                streamSource = new StreamSource(
                    this,
                    TypeResolutionHelper.GetVerifiedType(this.Partition.StreamReaderTypeName),
                    streamBinding.SourceStreamName,
                    streamMetadata,
                    streamBinding.StreamAdapter,
                    streamBinding.Summarizer,
                    allocator,
                    deallocator);

                // Check with data manager as to whether the stream is already known to be unreadable
                if (DataManager.Instance.IsUnreadable(streamSource))
                {
                    streamSource = null;
                }
            }

            return streamSource;
        }

        /// <summary>
        /// Finds a node in the partition by name and selects it.
        /// </summary>
        /// <param name="nodeName">The full name of the node to select.</param>
        /// <returns>True if the node was found and selected, otherwise false.</returns>
        public bool SelectStreamTreeNode(string nodeName)
        {
            if (this.RootStreamTreeNode != null)
            {
                if (this.RootStreamTreeNode.SelectChild(nodeName))
                {
                    this.SessionViewModel.IsTreeNodeExpanded = true;
                    this.IsTreeNodeExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds a node in the partition.
        /// </summary>
        /// <param name="nodeName">The full name of the node to find.</param>
        /// <returns>The found stream tree node, or null if the node does not exist in the partition.</returns>
        public StreamTreeNode FindStreamTreeNode(string nodeName) =>
            this.RootStreamTreeNode?.FindChild(nodeName);

        /// <summary>
        /// Saves all uncommitted changes of all streams in the partition to the store.
        /// </summary>
        public void SaveChanges()
        {
            // Run the task in the progress window
            ProgressWindow.RunWithProgress(
                $"Saving store {this.StoreName}",
                progress =>
                {
                    DataManager.Instance.SaveStore(this.StoreName, this.StorePath, new Progress<double>(p => progress.Report((string.Empty, p))));
                });
        }

        /// <summary>
        /// Removes this partition from the session that it belongs to. If the partition
        /// has unsaved changes, the user will be prompted to save them before continuing.
        /// </summary>
        /// <returns>True if the partiton was removed, otherwise false.</returns>
        public bool RemovePartition()
        {
            if (!this.PromptSaveChangesAndContinue())
            {
                return false;
            }

            this.sessionViewModel.RemovePartition(this);
            return true;
        }

        /// <summary>
        /// Removes this partition from the session that it belongs to and deletes all
        /// of its associated files on disk.
        /// </summary>
        public void DeletePartition()
        {
            var confirmation = new MessageBoxWindow(
               Application.Current.MainWindow,
               "Are you sure?",
               "Are you sure you want to delete this partition? This will permanently delete it from disk.",
               "Yes",
               "Cancel");

            if (confirmation.ShowDialog() == true)
            {
                try
                {
                    PsiStore.Delete((this.StoreName, this.StorePath), true);
                    this.sessionViewModel.RemovePartition(this);
                }
                catch (Exception e)
                {
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Delete Partition Error",
                        $"An error occurred while attempting to delete the partition: {e.Message}",
                        "Close",
                        null).ShowDialog();
                }
            }
        }

        /// <summary>
        /// Prompts the user to save changes if the partition is dirty before continuing with an operation.
        /// </summary>
        /// <returns>True if the current operation should continue, false if the current operation should be cancelled.</returns>
        public bool PromptSaveChangesAndContinue()
        {
            if (this.IsDirty)
            {
                var saveStoreWindow = new ConfirmOperationWindow(
                    Application.Current.MainWindow,
                    "Save changes to store",
                    $"The partition {this.Name} has unsaved changes.{Environment.NewLine}{Environment.NewLine}Do you wish to save these changes to disk before continuing?");

                saveStoreWindow.ShowDialog();
                switch (saveStoreWindow.UserSelection)
                {
                    case ConfirmOperationWindow.ConfirmOperationResult.Yes:
                        // Save changes and continue
                        this.SaveChanges();
                        break;

                    case ConfirmOperationWindow.ConfirmOperationResult.No:
                        // Continue without saving
                        break;

                    default:
                        // Cancel the operation by returning false
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called when the status of the data store backing this partition changes.
        /// </summary>
        /// <param name="isDirty">If true then the store and streams are now dirty.</param>
        /// <param name="streamNames">The list of names of streams in the partition whose status has changed.</param>
        public void ChangeStoreStatus(bool isDirty, string[] streamNames)
        {
            // Update the dirty flag on the partition.
            this.IsDirty = isDirty;

            // Update the dirty flag on all of the streams that had changes saved.
            foreach (string streamName in streamNames)
            {
                var streamTreeNode = this.RootStreamTreeNode.FindChild(streamName);
                if (streamTreeNode != null)
                {
                    streamTreeNode.IsDirty = isDirty;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Partition: " + this.Name;
        }

        /// <summary>
        /// Checks whether the partition has an active writer attached and updates its IsLivePartition property.
        /// </summary>
        /// <param name="initialCheck">True if the check should be made even if the store is not currently shown as live.</param>
        /// <returns>true if the partition is a live partition, otherwise false.</returns>
        internal bool UpdateLiveStatus(bool initialCheck = false)
        {
            // If a store is not live, then it will never change its status back to live
            if (this.IsLivePartition || initialCheck)
            {
                this.IsLivePartition = PsiStoreMonitor.IsStoreLive(this.StoreName, this.StorePath);
            }

            return this.IsLivePartition;
        }

        private void ExportStore()
        {
            // Set the initial crop interval to be the same as the partition's originating time interval
            var partitionInterval = this.Partition.TimeInterval;
            DateTime cropIntervalLeft = partitionInterval.Left;
            DateTime cropIntervalRight = partitionInterval.Right;

            // If the selection start or end marker are set, and they are within the
            // partition interval, update the crop interval to use those values instead.
            TimeInterval selectionRange = VisualizationContext.Instance.VisualizationContainer.Navigator.SelectionRange.AsTimeInterval;
            if (selectionRange.Left > partitionInterval.Left && selectionRange.Left < partitionInterval.Right)
            {
                cropIntervalLeft = selectionRange.Left;
            }

            if (selectionRange.Right < partitionInterval.Right && selectionRange.Right > partitionInterval.Left)
            {
                cropIntervalRight = selectionRange.Right;
            }

            // Show the export partition dialog
            var dlg = new ExportPsiPartitionWindow(this.StoreName + "Exported", this.StorePath, new TimeInterval(cropIntervalLeft, cropIntervalRight), Application.Current.MainWindow);
            if (dlg.ShowDialog() == true)
            {
                // Get the requested crop interval
                var requestedCropInterval = dlg.CropInterval;

                // Make sure the requested crop interval does not fall outside the partition interval
                if (requestedCropInterval.Left < partitionInterval.Left)
                {
                    requestedCropInterval = new TimeInterval(partitionInterval.Left, requestedCropInterval.Right);
                }

                if (requestedCropInterval.Right > partitionInterval.Right)
                {
                    requestedCropInterval = new TimeInterval(requestedCropInterval.Left, partitionInterval.Right);
                }

                // Export the store and show progress in the progress window
                ProgressWindow.RunWithProgress(
                    $"Cropping Store {this.StoreName}",
                    progress => PsiStore.Crop(
                        (this.StoreName, this.StorePath),
                        (dlg.StoreName, dlg.StorePath),
                        requestedCropInterval.Left - partitionInterval.Left,
                        RelativeTimeInterval.Future(requestedCropInterval.Span),
                        false,
                        new Progress<double>(p => progress.Report((string.Empty, p)))));

                // Update the session
                this.SessionViewModel.Update(this.SessionViewModel.Session);
            }
        }

        private void MonitorLivePartition()
        {
            // Create the reader that will monitor the store
            var storeReader = new PsiStoreReader(this.StoreName, this.StorePath, this.OnMetadataUpdate, true);

            // Find the current extents of the partition, as of this moment
            (TimeInterval messageTimes, TimeInterval messageOriginatingTimes) = storeReader.GetLiveStoreExtents();

            // Use the partition extents as the extents of all the streams that already exist
            foreach (var streamTreeNode in this.streamsById.Values)
            {
                streamTreeNode.SourceStreamMetadata.Update(messageTimes, messageOriginatingTimes);
            }

            // Begin monitoring the store for new messages
            this.continueMonitoring = true;
            this.monitorWorker = new Thread(new ParameterizedThreadStart(this.Monitor))
            {
                Name = string.Format("Live Partition Monitor ({0})", this.Name),
            };
            this.monitorWorker.Start(storeReader);
        }

        private void Monitor(object parameter)
        {
            PsiStoreReader storeReader = parameter as PsiStoreReader;

            DateTime lastUiUpdateTime = DateTime.UtcNow;
            Envelope? latestLiveMessageReceived = null;

            try
            {
                // Keep waiting on messages until the partition exits live mode or we're signaled to stop
                while (this.continueMonitoring && this.IsLivePartition && Application.Current != null)
                {
                    // If there's a new message, squirrel it away as the latest recent message, otherwise sleep
                    if (storeReader.MoveNext(out Envelope envelope))
                    {
                        if ((!latestLiveMessageReceived.HasValue) || (envelope.CreationTime > latestLiveMessageReceived.Value.CreationTime))
                        {
                            latestLiveMessageReceived = envelope;
                        }
                    }
                    else
                    {
                        Thread.Sleep(MonitorSleepDurationMs);
                    }

                    // If it's been more than 50ms since we last updated the UI, and we have received at least
                    // one message since the last update, then update the UI again with the last received message
                    if (latestLiveMessageReceived.HasValue && lastUiUpdateTime.AddMilliseconds(LiveUiUpdateFrequencyMs) <= DateTime.UtcNow)
                    {
                        lastUiUpdateTime = DateTime.UtcNow;

                        // Switch to the main UI thread for updating the cursor position to the time of the last message received
                        Application.Current?.Dispatcher.Invoke(this.liveMessageCallback, latestLiveMessageReceived.Value);
                        latestLiveMessageReceived = null;
                    }
                }
            }
            finally
            {
                storeReader.CloseAllStreams();
                storeReader.Dispose();
            }
        }

        private void OnMessageWritten(Envelope envelope)
        {
            // Update the data range of the stream's metadata
            if (this.streamsById.TryGetValue(envelope.SourceId, out StreamTreeNode streamTreeNode))
            {
                streamTreeNode.SourceStreamMetadata.Update(envelope, 0);
            }

            // Update the navigator's data range if this partition is in the session currently being visualized
            if (this.SessionViewModel.IsCurrentSession)
            {
                VisualizationContext.Instance.VisualizationContainer.Navigator.NotifyLiveMessageReceived(envelope.CreationTime);
            }
        }

        private void OnMetadataUpdate(IEnumerable<Metadata> metadata, RuntimeInfo runtimeInfo)
        {
            // Switch to the main UI thread for handling this message
            Application.Current?.Dispatcher.Invoke(this.newMetadataCallback, metadata);
        }

        private void UpdateStreamMetadata(IEnumerable<Metadata> metadata)
        {
            foreach (Metadata metadataEntry in metadata)
            {
                if (metadataEntry.Kind == MetadataKind.StreamMetadata)
                {
                    PsiStreamMetadata psiStreamMetadata = metadataEntry as PsiStreamMetadata;

                    // If we don't already have a node for this stream, add one
                    if (!this.streamsById.ContainsKey(psiStreamMetadata.Id))
                    {
                        IStreamMetadata streamMetadata = new PsiLiveStreamMetadata(psiStreamMetadata.Name, psiStreamMetadata.Id, psiStreamMetadata.TypeName, psiStreamMetadata.SupplementalMetadataTypeName, this.StoreName, this.StorePath);
                        this.streamsById[streamMetadata.Id] = this.RootStreamTreeNode.AddChild(streamMetadata.Name, streamMetadata, null, null);
                    }
                }
            }
        }

        private void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
            }
            else if (e.PropertyName == nameof(this.DatasetViewModel.ShowAuxiliaryPartitionInfo))
            {
                this.UpdateAuxiliaryInfo();
            }
        }

        private void UpdateAuxiliaryInfo()
        {
            switch (this.DatasetViewModel.ShowAuxiliaryPartitionInfo)
            {
                case AuxiliaryPartitionInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliaryPartitionInfo.Duration:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Span.ToString(@"d\.hh\:mm\:ss");
                    break;
                case AuxiliaryPartitionInfo.StartDate:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToShortDateString();
                    break;
                case AuxiliaryPartitionInfo.StartDateLocal:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToLocalTime().ToShortDateString();
                    break;
                case AuxiliaryPartitionInfo.StartTime:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToShortTimeString();
                    break;
                case AuxiliaryPartitionInfo.StartTimeLocal:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToLocalTime().ToShortTimeString();
                    break;
                case AuxiliaryPartitionInfo.StartDateTime:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToString();
                    break;
                case AuxiliaryPartitionInfo.StartDateTimeLocal:
                    this.AuxiliaryInfo = this.Partition.MessageOriginatingTimeInterval.IsEmpty ? "?" : this.Partition.MessageOriginatingTimeInterval.Left.ToLocalTime().ToString();
                    break;
                case AuxiliaryPartitionInfo.Size:
                    this.AuxiliaryInfo = this.Partition.Size.HasValue ? SizeHelper.FormatSize(this.Partition.Size.Value) : "?";
                    break;
                case AuxiliaryPartitionInfo.DataThroughputPerHour:
                    this.AuxiliaryInfo = this.Partition.Size.HasValue && !this.Partition.MessageOriginatingTimeInterval.IsEmpty ?
                        SizeHelper.FormatThroughput(this.Partition.Size.Value / this.Partition.MessageOriginatingTimeInterval.Span.TotalHours, "hour") :
                        "?";
                    break;
                case AuxiliaryPartitionInfo.DataThroughputPerMinute:
                    this.AuxiliaryInfo = this.Partition.Size.HasValue && !this.Partition.MessageOriginatingTimeInterval.IsEmpty ?
                        SizeHelper.FormatThroughput(this.Partition.Size.Value / this.Partition.MessageOriginatingTimeInterval.Span.TotalMinutes, "min") :
                        "?";
                    break;
                case AuxiliaryPartitionInfo.DataThroughputPerSecond:
                    this.AuxiliaryInfo = this.Partition.Size.HasValue && !this.Partition.MessageOriginatingTimeInterval.IsEmpty ?
                        SizeHelper.FormatThroughput(this.Partition.Size.Value / this.Partition.MessageOriginatingTimeInterval.Span.TotalSeconds, "sec") :
                        "?";
                    break;
                case AuxiliaryPartitionInfo.StreamCount:
                    this.AuxiliaryInfo = this.Partition.StreamCount.HasValue ? (this.Partition.StreamCount == 0 ? "0" : $"{this.Partition.StreamCount.Value:0,0.}") : "?";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Disposes of an instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method call comes from a Dispose method (its value is true) or from its destructor (its value is false).</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // Free any managed objects here.
                }

                // If we're monitoring a live stream, wait for it to exit
                if (this.monitorWorker != null)
                {
                    this.continueMonitoring = false;
                    this.monitorWorker.Join(2000);
                }

                this.disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private ContextMenu CreateContextMenu()
        {
            // Create the context menu
            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionExport, "Export Partition", this.ExportStoreCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionAdd, "Save Changes", this.SaveChangesCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionRemove, "Remove", this.RemovePartitionCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(null, "Remove From All Sessions", this.RemovePartitionFromAllSessionsCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(null, "Delete Partition", this.DeletePartitionCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(null, "Delete Partition From All Sessions", this.DeletePartitionFromAllSessionsCommand));
            contextMenu.Items.Add(new Separator());

            // Add the visualize session context menu if the partition is not in the currently visualized session
            if (!this.SessionViewModel.IsCurrentSession)
            {
                contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(string.Empty, ContextMenuName.VisualizeSession, this.SessionViewModel.VisualizeSessionCommand));
                contextMenu.Items.Add(new Separator());
            }

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
                    this.SessionViewModel.Name));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.Name));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Store Name",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.StoreName));

            copyToClipboardMenuItem.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    null,
                    "Partition Store Path",
                    VisualizationContext.Instance.VisualizationContainer.Navigator.CopyToClipboardCommand,
                    null,
                    true,
                    this.StorePath));

            contextMenu.Items.Add(copyToClipboardMenuItem);

            // Add show partition info menu
            var showPartitionInfoMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Show Partitions Info", null);
            foreach (var auxiliaryPartitionInfo in Enum.GetValues(typeof(AuxiliaryPartitionInfo)))
            {
                var auxiliaryPartitionInfoValue = (AuxiliaryPartitionInfo)auxiliaryPartitionInfo;
                var auxiliaryPartitionInfoName = auxiliaryPartitionInfoValue switch
                {
                    AuxiliaryPartitionInfo.None => "None",
                    AuxiliaryPartitionInfo.Duration => "Duration",
                    AuxiliaryPartitionInfo.StartDate => "Start Date (UTC)",
                    AuxiliaryPartitionInfo.StartDateLocal => "Start Date (Local)",
                    AuxiliaryPartitionInfo.StartTime => "Start Time (UTC)",
                    AuxiliaryPartitionInfo.StartTimeLocal => "Start Time (Local)",
                    AuxiliaryPartitionInfo.StartDateTime => "Start DateTime (UTC)",
                    AuxiliaryPartitionInfo.StartDateTimeLocal => "Start DateTime (Local)",
                    AuxiliaryPartitionInfo.Size => "Size",
                    AuxiliaryPartitionInfo.DataThroughputPerHour => "Throughput (bytes per hour)",
                    AuxiliaryPartitionInfo.DataThroughputPerMinute => "Throughput (bytes per minute)",
                    AuxiliaryPartitionInfo.DataThroughputPerSecond => "Throughput (bytes per second)",
                    AuxiliaryPartitionInfo.StreamCount => "Number of Streams",
                    _ => throw new NotImplementedException(),
                };

                showPartitionInfoMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        this.DatasetViewModel.ShowAuxiliaryPartitionInfo == auxiliaryPartitionInfoValue ? IconSourcePath.Checkmark : null,
                        auxiliaryPartitionInfoName,
                        new RelayCommand<AuxiliaryPartitionInfo>(api => this.DatasetViewModel.ShowAuxiliaryPartitionInfo = api),
                        commandParameter: auxiliaryPartitionInfoValue));
            }

            contextMenu.Items.Add(showPartitionInfoMenuItem);

            // Add open partition folder in windows explorer
            if (!string.IsNullOrEmpty(this.StorePath))
            {
                contextMenu.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Open Partition Folder in Explorer",
                        new RelayCommand(() => { Process.Start("explorer.exe", this.StorePath); }),
                        commandParameter: default));
            }

            return contextMenu;
        }
    }
}
