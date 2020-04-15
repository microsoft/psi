// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Helpers;
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

        private IPartition partition;
        private StreamTreeNode streamTreeRoot;
        private SessionViewModel sessionViewModel;
        private bool isLivePartition = false;
        private Thread monitorWorker = null;
        private bool continueMonitoring = true;
        private bool disposed;

        private LiveMessageReceivedDelegate liveMessageCallback = null;
        private UpdateStreamMetadataDelegate newMetadataCallback = null;

        // A Dictionary of streams in the Partition, keyed by stream id
        private Dictionary<int, StreamTreeNode> streamsById;

        private RelayCommand removePartitionCommand;

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
        /// Gets the store path of this partition.
        /// </summary>
        [PropertyOrder(1)]
        [Description("The full path to the Psi store that represents the partition.")]
        public string StorePath => this.partition.StorePath;

        /// <summary>
        /// Gets a string representation of the originating time of the first message in the partition.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("FirstMessageOriginatingTime")]
        [Description("The originating time of the first message in the partition.")]
        public string FirstMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the partition.
        /// </summary>
        [PropertyOrder(3)]
        [DisplayName("LastMessageOriginatingTime")]
        [Description("The originating time of the last message in the partition.")]
        public string LastMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this session.
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
        public RelayCommand RemovePartitionCommand
        {
            get
            {
                if (this.removePartitionCommand == null)
                {
                    this.removePartitionCommand = new RelayCommand(() => this.RemovePartition());
                }

                return this.removePartitionCommand;
            }
        }

        /// <summary>
        /// Gets the underlying partition.
        /// </summary>
        internal IPartition Partition => this.partition;

        /// <summary>
        /// Finds a stream in the partition by name and selects it.
        /// </summary>
        /// <param name="streamName">The name of the stream to select.</param>
        /// <returns>True if the stream was found and selected, otherwise false.</returns>
        public bool SelectStream(string streamName)
        {
            if (this.StreamTreeRoot != null)
            {
                if (this.StreamTreeRoot.SelectNode(streamName) != null)
                {
                    this.IsTreeNodeExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes this partition from the session that it belongs to.
        /// </summary>
        public void RemovePartition()
        {
            this.sessionViewModel.RemovePartition(this);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Checks whether the partition has an active writer attached and updates its IsLivePartition property.
        /// </summary>
        /// <returns>true if the partition is a live partition, otherwise false.</returns>
        internal bool UpdateLiveStatus()
        {
            try
            {
                this.IsLivePartition = StoreReader.IsStoreLive(this.StoreName, this.StorePath);
                VisualizationContext.Instance.VisualizationContainer.NotifyLivePartitionStatus(this.StorePath, this.IsLivePartition);
                return this.isLivePartition;
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
            StoreReader storeReader = new StoreReader(this.StoreName, this.StorePath, this.OnMetadataUpdate, true);

            // Find the current extents of the partition, as of this moment
            (TimeInterval messageTimes, TimeInterval messageOriginatingTimes) = storeReader.GetLiveStoreExtents();

            // HACK: Use the partition extents as the extents of all the streams that already exist
            foreach (StreamTreeNode streamTreeNode in this.streamsById.Values)
            {
                streamTreeNode.StreamMetadata.Update(messageTimes, messageOriginatingTimes);
            }

            // Begin monitoring the store for new messages
            this.continueMonitoring = true;
            this.monitorWorker = new Thread(new ParameterizedThreadStart(this.Monitor));
            this.monitorWorker.Name = string.Format("Live Partition Monitor ({0})", this.Name);
            this.monitorWorker.Start(storeReader);
        }

        private void Monitor(object parameter)
        {
            StoreReader storeReader = parameter as StoreReader;

            DateTime lastUiUpdateTime = DateTime.UtcNow;
            Envelope? latestLiveMessageReceived = null;

            try
            {
                // Keep waiting on messages until the partition exits live mode or we're signalled to stop
                while (this.continueMonitoring && this.IsLivePartition && Application.Current != null)
                {
                    // If there's a new message, squirrel it away as the lastest recent message, otherwise sleep
                    if (storeReader.MoveNext(out Envelope envelope))
                    {
                        if ((!latestLiveMessageReceived.HasValue) || (envelope.Time > latestLiveMessageReceived.Value.Time))
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
            StreamTreeNode streamTreeNode;
            if (this.streamsById.TryGetValue(envelope.SourceId, out streamTreeNode))
            {
                streamTreeNode.StreamMetadata.Update(envelope, 0);
            }

            // Update the navigator's data range if this partition is in the session currently being visualized
            if (this.SessionViewModel.IsCurrentSession)
            {
                VisualizationContext.Instance.VisualizationContainer.Navigator.NotifyLiveMessageReceived(envelope.Time);
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
                        IStreamMetadata streamMetadata = new PsiLiveStreamMetadata(psiStreamMetadata.Name, psiStreamMetadata.Id, psiStreamMetadata.TypeName, this.StoreName, this.StorePath);
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
    }
}
