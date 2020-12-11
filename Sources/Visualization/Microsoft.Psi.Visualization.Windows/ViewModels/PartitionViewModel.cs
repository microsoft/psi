// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
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

        private readonly IPartition partition;
        private readonly SessionViewModel sessionViewModel;

        private readonly LiveMessageReceivedDelegate liveMessageCallback = null;
        private readonly UpdateStreamMetadataDelegate newMetadataCallback = null;

        // A Dictionary of streams in the Partition, keyed by stream id
        private readonly Dictionary<int, StreamTreeNode> streamsById;

        private StreamTreeNode streamTreeRoot;
        private bool isLivePartition = false;
        private Thread monitorWorker = null;
        private bool continueMonitoring = true;
        private bool disposed;

        private RelayCommand saveChangesCommand;
        private RelayCommand removePartitionCommand;
        private RelayCommand<StackPanel> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        /// <param name="sessionViewModel">The view model of the session to which this partition belongs.</param>
        /// <param name="partition">The partition for which to create the view model.</param>
        public PartitionViewModel(SessionViewModel sessionViewModel, IPartition partition)
        {
            this.partition = partition;
            this.sessionViewModel = sessionViewModel;
            this.sessionViewModel.DatasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
            this.streamsById = new Dictionary<int, StreamTreeNode>();
            this.StreamTreeRoot = new StreamTreeNode(this);
            foreach (var stream in this.partition.AvailableStreams)
            {
                this.streamsById[stream.Id] = this.StreamTreeRoot.AddPath(stream);
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
        /// Finalizes an instance of the <see cref="PartitionViewModel"/> class.
        /// </summary>
        ~PartitionViewModel()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the save partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveChangesCommand
        {
            get
            {
                if (this.saveChangesCommand == null)
                {
                    this.saveChangesCommand = new RelayCommand(
                        () => this.SaveChanges(),
                        () => this.IsDirty);
                }

                return this.saveChangesCommand;
            }
        }

        /// <summary>
        /// Gets or sets the partition name.
        /// </summary>
        [PropertyOrder(0)]
        [Description("The name of the partition.")]
        public string Name
        {
            get => this.partition.Name;
            set
            {
                if (this.partition.Name != value)
                {
                    this.RaisePropertyChanging(nameof(this.Name));
                    this.partition.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        [Browsable(false)]
        public string DisplayName => this.IsDirty ? this.Name + "*" : this.Name;

        /// <summary>
        /// Gets the store path of this partition.
        /// </summary>
        [PropertyOrder(1)]
        [Description("The full path to the Psi store that represents the partition.")]
        public string StorePath => this.partition.StorePath;

        /// <summary>
        /// Gets the stream reader type name of this partition.
        /// </summary>
        [PropertyOrder(2)]
        [Description("The type of stream reader used by the partition.")]
        public string StreamReaderTypeName => this.partition.StreamReaderTypeName;

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the partition.
        /// </summary>
        [PropertyOrder(3)]
        [DisplayName("FirstMessageOriginatingTime")]
        [Description("The originating time of the first message in the partition.")]
        public string FirstMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the partition.
        /// </summary>
        [PropertyOrder(4)]
        [DisplayName("LastMessageOriginatingTime")]
        [Description("The originating time of the last message in the partition.")]
        public string LastMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval => this.streamTreeRoot.OriginatingTimeInterval;

        /// <summary>
        /// Gets the originating time of the first message in the partition.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageOriginatingTime => this.OriginatingTimeInterval.Left;

        /// <summary>
        /// Gets the originating time of the last message in the partition.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime => this.OriginatingTimeInterval.Right;

        /// <summary>
        /// Gets the session that this partition belongs to.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel SessionViewModel => this.sessionViewModel;

        /// <summary>
        /// Gets the store name of this partition.
        /// </summary>
        [Browsable(false)]
        public string StoreName => this.partition.StoreName;

        /// <summary>
        /// Gets a value indicating whether the partition is dirty (i.e. has uncommitted changes).
        /// </summary>
        [Browsable(false)]
        public bool IsDirty { get; private set; } = false;

        /// <summary>
        /// Gets or sets the root stream tree node of this partition.
        /// </summary>
        [Browsable(false)]
        public StreamTreeNode StreamTreeRoot
        {
            get => this.streamTreeRoot;
            set => this.Set(nameof(this.StreamTreeRoot), ref this.streamTreeRoot, value);
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
        public string IconSource => this.IsLivePartition ? IconSourcePath.PartitionLive : IconSourcePath.Partition;

        /// <summary>
        /// Gets the opacity of UI elements associated with this session. UI element opacity is reduced for sessions that are not the current session.
        /// </summary>
        [Browsable(false)]
        public double UiElementOpacity => this.SessionViewModel.UiElementOpacity;

        /// <summary>
        /// Gets the remove partition command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemovePartitionCommand => this.removePartitionCommand ??= new RelayCommand(() => this.RemovePartition());

        /// <summary>
        /// Gets the command that executes when opening the partition context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<StackPanel> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<StackPanel>(panel => panel.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the underlying partition.
        /// </summary>
        internal IPartition Partition => this.partition;

        /// <summary>
        /// Attempts to find a stream source in the partition that matches a stream binding.
        /// </summary>
        /// <param name="streamBinding">A stream binding that describes the stream to bind to.</param>
        /// <returns>A stream source if a source was found to bind to, otherwise returns null.</returns>
        public StreamSource GetStreamSource(StreamBinding streamBinding)
        {
            StreamSource streamSource = null;

            // Check if the partition contains the required stream
            IStreamMetadata streamMetadata = this.Partition.AvailableStreams.FirstOrDefault(s => s.Name == streamBinding.StreamName);
            if (streamMetadata != default)
            {
                // Create the stream source
                streamSource = new StreamSource(
                    this,
                    TypeResolutionHelper.GetVerifiedType(this.StreamReaderTypeName),
                    streamBinding.StreamName,
                    streamMetadata,
                    streamBinding.StreamAdapter,
                    streamBinding.Summarizer);

                // Check with data manager as to whether the stream is already known to be unreadable
                if (DataManager.Instance.IsStreamUnreadable(streamSource))
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
        /// <returns>True if the stream was found and selected, otherwise false.</returns>
        public bool SelectNode(string nodeName)
        {
            if (this.StreamTreeRoot != null)
            {
                if (this.StreamTreeRoot.SelectNode(nodeName))
                {
                    this.IsTreeNodeExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for a stream within a partition.
        /// </summary>
        /// <param name="streamName">The name of the stream to search for.</param>
        /// <returns>A stream tree node representing the stream, or null if the stream does not exist in the partition.</returns>
        public StreamTreeNode FindStream(string streamName)
        {
            if (this.StreamTreeRoot != null)
            {
                return this.StreamTreeRoot.FindNode(streamName);
            }

            return null;
        }

        /// <summary>
        /// Saves all uncommitted changes of all streams in the partition to the store.
        /// </summary>
        public void SaveChanges()
        {
            // Create the progress window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, $"Saving store {this.StoreName}");
            var progress = new Progress<double>(p =>
            {
                progressWindow.Progress = p;

                if (p == 1.0)
                {
                    // close the status window when the task reports completion
                    progressWindow.Close();
                }
            });

            Task.Run(() => DataManager.Instance.SaveStore(this.StoreName, this.StorePath, progress));

            // Show the modal progress window.  If the task has already completed then it will have
            // closed the progress window and an invalid operation exception will be thrown.
            try
            {
                progressWindow.ShowDialog();
            }
            catch (InvalidOperationException)
            {
            }
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
        /// Prompts the user to save changes if the partition is dirty before continuing with an operation.
        /// </summary>
        /// <returns>True if the current operation should continue, false if the current operation should be cancelled.</returns>
        public bool PromptSaveChangesAndContinue()
        {
            if (this.IsDirty)
            {
                SaveStoreWindow saveStoreWindow = new SaveStoreWindow(Application.Current.MainWindow, this);
                saveStoreWindow.ShowDialog();
                switch (saveStoreWindow.UserSelection)
                {
                    case SaveStoreWindow.SaveStoreWindowResult.SaveChanges:
                        this.SaveChanges();
                        break;
                    case SaveStoreWindow.SaveStoreWindowResult.Cancel:
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
            this.RaisePropertyChanging(nameof(this.DisplayName));
            this.IsDirty = isDirty;
            this.RaisePropertyChanged(nameof(this.DisplayName));

            // Update the dirty flag on all of the streams that had changes saved.
            foreach (string streamName in streamNames)
            {
                StreamTreeNode streamTreeNode = this.StreamTreeRoot.FindNode(streamName);
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
        /// <returns>true if the partition is a live partition, otherwise false.</returns>
        internal bool UpdateLiveStatus()
        {
            try
            {
                this.IsLivePartition = PsiStoreReader.IsStoreLive(this.StoreName, this.StorePath);
                return this.IsLivePartition;
            }
            catch (AbandonedMutexException)
            {
                // This exception will be raised if the writer goes away
                return false;
            }
        }

        private void MonitorLivePartition()
        {
            // Create the reader that will monitor the store
            PsiStoreReader storeReader = new PsiStoreReader(this.StoreName, this.StorePath, this.OnMetadataUpdate, true);

            // Find the current extents of the partition, as of this moment
            (TimeInterval messageTimes, TimeInterval messageOriginatingTimes) = storeReader.GetLiveStoreExtents();

            // HACK: Use the partition extents as the extents of all the streams that already exist
            foreach (StreamTreeNode streamTreeNode in this.streamsById.Values)
            {
                streamTreeNode.StreamMetadata.Update(messageTimes, messageOriginatingTimes);
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
                streamTreeNode.StreamMetadata.Update(envelope, 0);
            }

            // Update the navigator's data range if this partition is in the session currently being visualized
            if (this.SessionViewModel.IsCurrentSession)
            {
                VisualizationContext.Instance.VisualizationContainer.Navigator.NotifyLiveMessageReceived(envelope.CreationTime);
            }
        }

        private void OnMetadataUpdate(IEnumerable<Metadata> metadata, RuntimeInfo runtimeVersion)
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
                        this.streamsById[streamMetadata.Id] = this.StreamTreeRoot.AddPath(streamMetadata);
                    }
                }
            }
        }

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.SessionViewModel.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
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

            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionRemove, "Remove", this.RemovePartitionCommand));

            contextMenu.Items.Add(new Separator());

            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionAdd, "Save Changes", this.SaveChangesCommand));

            return contextMenu;
        }
    }
}
