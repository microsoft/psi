// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Tasks;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a view model of a session.
    /// </summary>
    public class SessionViewModel : ObservableTreeNodeObject
    {
        private readonly Session session;
        private readonly DatasetViewModel datasetViewModel;
        private readonly ObservableCollection<PartitionViewModel> internalPartitionViewModels;
        private readonly ReadOnlyObservableCollection<PartitionViewModel> partitionViewModels;
        private bool containsLivePartitions = false;

        private RelayCommand addPartitionCommand;
        private RelayCommand removeSessionCommand;
        private RelayCommand visualizeSessionCommand;
        private RelayCommand<StackPanel> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionViewModel"/> class.
        /// </summary>
        /// <param name="datasetViewModel">The view model of the dataset to which this session belongs.</param>
        /// <param name="session">The session for which to create the view model.</param>
        public SessionViewModel(DatasetViewModel datasetViewModel, Session session)
        {
            this.session = session;
            this.datasetViewModel = datasetViewModel;
            this.datasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
            this.internalPartitionViewModels = new ObservableCollection<PartitionViewModel>();
            this.partitionViewModels = new ReadOnlyObservableCollection<PartitionViewModel>(this.internalPartitionViewModels);

            foreach (var partition in this.session.Partitions)
            {
                this.internalPartitionViewModels.Add(new PartitionViewModel(this, partition));
            }

            this.IsTreeNodeExpanded = true;
        }

        /// <summary>
        /// Gets the dataset viewmodel.
        /// </summary>
        [Browsable(false)]
        public DatasetViewModel DatasetViewModel => this.datasetViewModel;

        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        [PropertyOrder(0)]
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
        /// Gets a string representation of the originating time of the first message in the session.
        /// </summary>
        [PropertyOrder(1)]
        [DisplayName("FirstMessageOriginatingTime")]
        [Description("The originating time of the first message in the session.")]
        public string FirstMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.FirstMessageOriginatingTime);

        /// <summary>
        /// Gets a string representation of the originating time of the last message in the session.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("LastMessageOriginatingTime")]
        [Description("The originating time of the last message in the session.")]
        public string LastMessageOriginatingTimeString => DateTimeFormatHelper.FormatDateTime(this.LastMessageOriginatingTime);

        /// <summary>
        /// Gets the originating time of the first message in the session.
        /// </summary>
        [Browsable(false)]
        public DateTime? FirstMessageOriginatingTime => this.OriginatingTimeInterval.Left;

        /// <summary>
        /// Gets the originating time of the last message in the session.
        /// </summary>
        [Browsable(false)]
        public DateTime? LastMessageOriginatingTime => this.OriginatingTimeInterval.Right;

        /// <summary>
        /// Gets the opacity of UI elements associated with this session. UI element opacity is reduced for sessions that are not the current session.
        /// </summary>
        [Browsable(false)]
        public double UiElementOpacity => this.DatasetViewModel.CurrentSessionViewModel == this ? 1.0d : 0.5d;

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this session.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.partitionViewModels
                    .Where(p => p.OriginatingTimeInterval.Left > DateTime.MinValue && p.OriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(p => p.OriginatingTimeInterval));

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
        public RelayCommand AddPartitionCommand => this.addPartitionCommand ??= new RelayCommand(() => this.AddPartition());

        /// <summary>
        /// Gets the remove session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemoveSessionCommand => this.removeSessionCommand ??= new RelayCommand(() => this.RemoveSession());

        /// <summary>
        /// Gets the visualize session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand VisualizeSessionCommand => this.visualizeSessionCommand ??= new RelayCommand(() => this.DatasetViewModel.VisualizeSession(this));

        /// <summary>
        /// Gets the command that executes when opening the session context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<StackPanel> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<StackPanel>(panel => panel.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the underlying session.
        /// </summary>
        public Session Session => this.session;

        /// <summary>
        /// Updates the session view model based on the latest version of the corresponding session.
        /// </summary>
        public void Update()
        {
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
        /// Creates and a new store partition but does not add it to the session.
        /// </summary>
        /// <param name="streamReader">The stream reader for the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        public void AddStorePartition(IStreamReader streamReader, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? streamReader.Name);
            this.AddPartition(this.session.AddStorePartition(streamReader, partitionName));
        }

        /// <summary>
        /// Adds a new partition to the session.
        /// </summary>
        public void AddPartition()
        {
            var formats = VisualizationContext.Instance.PluginMap.GetStreamReaderExtensions();
            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog
            {
                DefaultExt = ".psi",
                Filter = string.Join("|", formats.Select(f => $"{f.Name}|*{f.Extensions}")),
            };

            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                var fileInfo = new FileInfo(dlg.FileName);
                var name = fileInfo.Name.Split('.')[0];
                var path = fileInfo.DirectoryName;

                var readerType = VisualizationContext.Instance.PluginMap.GetStreamReaderType(fileInfo.Extension);
                var streamReader = Psi.Data.StreamReader.Create(name, path, readerType);
                this.AddStorePartition(streamReader);
            }
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
        /// Attempts to find a stream source in the session that matches a stream binding.
        /// </summary>
        /// <param name="streamBinding">A stream binding that describes the stream to bind to.</param>
        /// <returns>A stream source if a source was found to bind to, otherwise returns null.</returns>
        public StreamSource GetStreamSource(StreamBinding streamBinding)
        {
            // Check if the session contains the required partition
            PartitionViewModel partitionViewModel = this.PartitionViewModels.FirstOrDefault(p => p.Name == streamBinding.PartitionName);

            // Pass the request to the partition
            return partitionViewModel != null ? partitionViewModel.GetStreamSource(streamBinding) : null;
        }

        /// <summary>
        /// Searches for a stream tree node within a session.
        /// </summary>
        /// <param name="partitionName">The name of the partition containing the stream.</param>
        /// <param name="streamName">The name of the stream to search for.</param>
        /// <returns>A stream tree node representing the stream, or null if the stream does not exist in the session.</returns>
        public StreamTreeNode FindStream(string partitionName, string streamName)
        {
            PartitionViewModel partitionViewModel = this.PartitionViewModels.FirstOrDefault(p => p.Name == partitionName);
            if (partitionViewModel != null)
            {
                return partitionViewModel.FindStream(streamName);
            }

            return null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Session: " + this.Name;
        }

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

        private void AddPartition(IPartition partition)
        {
            this.internalPartitionViewModels.Add(new PartitionViewModel(this, partition));
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

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.DatasetViewModel.CurrentSessionViewModel))
            {
                this.RaisePropertyChanged(nameof(this.UiElementOpacity));
            }
        }

        private ContextMenu CreateContextMenu()
        {
            // Create the context menu
            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.PartitionAdd, "Add Partition from Existing Store ...", this.AddPartitionCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.SessionRemove, "Remove", this.RemoveSessionCommand));
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(string.Empty, "Visualize", this.VisualizeSessionCommand));

            contextMenu.Items.Add(new Separator());

            // Add run batch processing task menu
            var runTasksMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Run Batch Processing Task", null);
            var batchProcessingTasks = VisualizationContext.Instance.PluginMap.GetSessionCompatibleBatchProcessingTasks();
            runTasksMenuItem.IsEnabled = batchProcessingTasks.Any();
            foreach (var batchProcessingTask in batchProcessingTasks)
            {
                runTasksMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        batchProcessingTask.IconSourcePath,
                        batchProcessingTask.Name,
                        new VisualizationCommand<BatchProcessingTaskMetadata>(async s => await VisualizationContext.Instance.RunSessionBatchProcessingTask(this, batchProcessingTask)),
                        batchProcessingTask));
            }

            contextMenu.Items.Add(runTasksMenuItem);

            return contextMenu;
        }
    }
}
