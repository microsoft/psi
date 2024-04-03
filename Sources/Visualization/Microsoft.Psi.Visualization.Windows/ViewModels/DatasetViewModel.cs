﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Defines types of auxiliary dataset information to display.
    /// </summary>
    public enum AuxiliaryDatasetInfo
    {
        /// <summary>
        /// No auxiliary dataset info.
        /// </summary>
        None,

        /// <summary>
        /// The dataset extent.
        /// </summary>
        Extent,

        /// <summary>
        /// The dataset total duration.
        /// </summary>
        TotalDuration,

        /// <summary>
        /// The start date for the dataset, in utc.
        /// </summary>
        StartDate,

        /// <summary>
        /// The start date for the dataset, in local time.
        /// </summary>
        StartDateLocal,

        /// <summary>
        /// The start time of day for the dataset, in utc.
        /// </summary>
        StartTime,

        /// <summary>
        /// The start time of day for the dataset, in local time.
        /// </summary>
        StartTimeLocal,

        /// <summary>
        /// The start date and time for the dataset, in utc.
        /// </summary>
        StartDateTime,

        /// <summary>
        /// The start date and time for the dataset, in local time.
        /// </summary>
        StartDateTimeLocal,

        /// <summary>
        /// The size of the dataset.
        /// </summary>
        Size,

        /// <summary>
        /// The bytes-per-hour throughput of the dataset.
        /// </summary>
        DataThroughputPerHour,

        /// <summary>
        /// The bytes-per-minute throughput of the dataset.
        /// </summary>
        DataThroughputPerMinute,

        /// <summary>
        /// The bytes-per-second throughput of the dataset.
        /// </summary>
        DataThroughputPerSecond,

        /// <summary>
        /// The number of streams.
        /// </summary>
        StreamCount,
    }

    /// <summary>
    /// Defines types of auxiliary session information to display.
    /// </summary>
    public enum AuxiliarySessionInfo
    {
        /// <summary>
        /// No auxiliary session info.
        /// </summary>
        None,

        /// <summary>
        /// The session duration.
        /// </summary>
        Duration,

        /// <summary>
        /// The start date for the session, in utc.
        /// </summary>
        StartDate,

        /// <summary>
        /// The start date for the session, in local time.
        /// </summary>
        StartDateLocal,

        /// <summary>
        /// The start time of day for the session, in utc.
        /// </summary>
        StartTime,

        /// <summary>
        /// The start time of day for the session, in local time.
        /// </summary>
        StartTimeLocal,

        /// <summary>
        /// The start date and time for the session, in utc.
        /// </summary>
        StartDateTime,

        /// <summary>
        /// The start date and time for the session, in local time.
        /// </summary>
        StartDateTimeLocal,

        /// <summary>
        /// The size of the session.
        /// </summary>
        Size,

        /// <summary>
        /// The bytes-per-hour throughput of the session.
        /// </summary>
        DataThroughputPerHour,

        /// <summary>
        /// The bytes-per-minute throughput of the session.
        /// </summary>
        DataThroughputPerMinute,

        /// <summary>
        /// The bytes-per-second throughput of the session.
        /// </summary>
        DataThroughputPerSecond,

        /// <summary>
        /// The number of streams.
        /// </summary>
        StreamCount,
    }

    /// <summary>
    /// Defines types of auxiliary partition information to display.
    /// </summary>
    public enum AuxiliaryPartitionInfo
    {
        /// <summary>
        /// No auxiliary partition info.
        /// </summary>
        None,

        /// <summary>
        /// The partition duration.
        /// </summary>
        Duration,

        /// <summary>
        /// The start date for the partition, in utc.
        /// </summary>
        StartDate,

        /// <summary>
        /// The start date for the partition, in local time.
        /// </summary>
        StartDateLocal,

        /// <summary>
        /// The start time of day for the partition, in utc.
        /// </summary>
        StartTime,

        /// <summary>
        /// The start time of day for the partition, in local time.
        /// </summary>
        StartTimeLocal,

        /// <summary>
        /// The start date and time for the partition, in utc.
        /// </summary>
        StartDateTime,

        /// <summary>
        /// The start date and time for the partition, in local time.
        /// </summary>
        StartDateTimeLocal,

        /// <summary>
        /// The size of the partition.
        /// </summary>
        Size,

        /// <summary>
        /// The bytes-per-hour throughput of the partition.
        /// </summary>
        DataThroughputPerHour,

        /// <summary>
        /// The bytes-per-minute throughput of the partition.
        /// </summary>
        DataThroughputPerMinute,

        /// <summary>
        /// The bytes-per-second throughput of the partition.
        /// </summary>
        DataThroughputPerSecond,

        /// <summary>
        /// The number of streams.
        /// </summary>
        StreamCount,
    }

    /// <summary>
    /// Defines types of auxiliary stream information to display.
    /// </summary>
    public enum AuxiliaryStreamInfo
    {
        /// <summary>
        /// No auxiliary stream info.
        /// </summary>
        None,

        /// <summary>
        /// The size of the stream.
        /// </summary>
        Size,

        /// <summary>
        /// The bytes-per-hour throughput of the stream.
        /// </summary>
        DataThroughputPerHour,

        /// <summary>
        /// The bytes-per-minute throughput of the stream.
        /// </summary>
        DataThroughputPerMinute,

        /// <summary>
        /// The bytes-per-second throughput of the stream.
        /// </summary>
        DataThroughputPerSecond,

        /// <summary>
        /// The messages (per hour) throughput of the stream.
        /// </summary>
        MessageCountThroughputPerHour,

        /// <summary>
        /// The messages (per minute) throughput of the stream.
        /// </summary>
        MessageCountThroughputPerMinute,

        /// <summary>
        /// The messages (per second) throughput of the stream.
        /// </summary>
        MessageCountThroughputPerSecond,

        /// <summary>
        /// The number of messages.
        /// </summary>
        MessageCount,

        /// <summary>
        /// The average message latency, in milliseconds.
        /// </summary>
        AverageMessageLatencyMs,

        /// <summary>
        /// The average message size.
        /// </summary>
        AverageMessageSize,
    }

    /// <summary>
    /// Represents a view model of a dataset.
    /// </summary>
    public class DatasetViewModel : ObservableTreeNodeObject
    {
        private readonly ObservableCollection<SessionViewModel> internalSessionViewModels;
        private readonly ReadOnlyObservableCollection<SessionViewModel> sessionViewModels;

        private Dataset dataset;
        private string filename;
        private SessionViewModel currentSessionViewModel = null;

        private AuxiliaryDatasetInfo showAuxiliaryDatasetInfo = AuxiliaryDatasetInfo.None;
        private AuxiliarySessionInfo showAuxiliarySessionInfo = AuxiliarySessionInfo.None;
        private AuxiliaryPartitionInfo showAuxiliaryPartitionInfo = AuxiliaryPartitionInfo.None;
        private AuxiliaryStreamInfo showAuxiliaryStreamInfo = AuxiliaryStreamInfo.None;

        private string auxiliaryInfo = string.Empty;

        private RelayCommand createSessionCommand;
        private RelayCommand addSessionFromFileCommand;
        private RelayCommand addSessionFromFolderCommand;
        private RelayCommand addMultipleSessionsFromFolderCommand;
        private RelayCommand<Grid> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetViewModel"/> class.
        /// </summary>
        /// <param name="dataset">The dataset for which to create the view model.</param>
        public DatasetViewModel(Dataset dataset)
        {
            this.PropertyChanged += this.OnPropertyChanged;

            this.dataset = dataset;
            this.internalSessionViewModels = new ObservableCollection<SessionViewModel>();
            this.sessionViewModels = new ReadOnlyObservableCollection<SessionViewModel>(this.internalSessionViewModels);
            foreach (var item in this.dataset.Sessions)
            {
                this.internalSessionViewModels.Add(new SessionViewModel(this, item));
            }

            this.currentSessionViewModel = this.internalSessionViewModels.FirstOrDefault();

            if (this.currentSessionViewModel != null && this.dataset.Sessions.Count == 1)
            {
                this.currentSessionViewModel.IsTreeNodeExpanded = true;
            }

            this.IsTreeNodeExpanded = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetViewModel"/> class.
        /// </summary>
        public DatasetViewModel()
            : this(new Dataset())
        {
        }

        /// <summary>
        /// Gets the underlying dataset.
        /// </summary>
        [Browsable(false)]
        public Dataset Dataset => this.dataset;

        /// <summary>
        /// Gets the collection of sessions in this dataset.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<SessionViewModel> SessionViewModels => this.sessionViewModels;

        /// <summary>
        /// Gets the current session view model for this dataset view model.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel CurrentSessionViewModel => this.currentSessionViewModel;

        /// <summary>
        /// Gets a value indicating whether any partitions in the dataset are invalid.
        /// </summary>
        [Browsable(false)]
        public bool ContainsInvalidPartitions => this.SessionViewModels.Any(s => s.ContainsInvalidPartitions);

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval =>
            TimeInterval.Coverage(
                this.sessionViewModels
                    .Where(s => s.OriginatingTimeInterval.Left > DateTime.MinValue && s.OriginatingTimeInterval.Right < DateTime.MaxValue)
                    .Select(s => s.OriginatingTimeInterval));

        /// <summary>
        /// Gets the total duration of the dataset.
        /// </summary>
        [Browsable(false)]
        public TimeSpan TotalDuration => TimeSpan.FromTicks(this.sessionViewModels.Sum(svm => svm.OriginatingTimeInterval.Span.Ticks));

        /// <summary>
        /// Gets the command to create a session.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateSessionCommand =>
            this.createSessionCommand ??= new RelayCommand(() => this.AddEmptySession());

        /// <summary>
        /// Gets the command to add a session from a user-specified file.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddSessionFromFileCommand =>
            this.addSessionFromFileCommand ??= new RelayCommand(async () => await this.AddSessionFromFileAsync());

        /// <summary>
        /// Gets the command to add a session from a user-specified folder.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddSessionFromFolderCommand =>
            this.addSessionFromFolderCommand ??= new RelayCommand(async () => await this.AddSessionFromFolderAsync());

        /// <summary>
        /// Gets the command to add multiple sessions from a user-specified folder.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AddMultipleSessionsFromFolderCommand =>
            this.addMultipleSessionsFromFolderCommand ??= new RelayCommand(async () => await this.AddMultipleSessionsFromFolderAsync());

        /// <summary>
        /// Gets the command that executes when opening the dataset context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<Grid> ContextMenuOpeningCommand =>
            this.contextMenuOpeningCommand ??= new RelayCommand<Grid>(panel => panel.ContextMenu = this.CreateContextMenu());

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
        /// Gets or sets the name of this dataset.
        /// </summary>
        [DisplayName("Dataset Name")]
        [Description("The name of the dataset.")]
        public string Name
        {
            get => this.dataset.Name;
            set
            {
                if (this.dataset.Name != value)
                {
                    this.RaisePropertyChanging(nameof(this.Name));
                    this.dataset.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets the filename of the underlying dataset.
        /// </summary>
        [DisplayName("Filename")]
        [Description("The full path to the dataset.")]
        public string FileName
        {
            get => this.filename;
            private set => this.Set(nameof(this.FileName), ref this.filename, value);
        }

        /// <summary>
        /// Gets or sets the type of auxiliary dataset information to display.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AuxiliaryDatasetInfo ShowAuxiliaryDatasetInfo
        {
            get => this.showAuxiliaryDatasetInfo;
            set => this.Set(nameof(this.ShowAuxiliaryDatasetInfo), ref this.showAuxiliaryDatasetInfo, value);
        }

        /// <summary>
        /// Gets or sets the type of auxiliary session information to display.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AuxiliarySessionInfo ShowAuxiliarySessionInfo
        {
            get => this.showAuxiliarySessionInfo;
            set => this.Set(nameof(this.ShowAuxiliarySessionInfo), ref this.showAuxiliarySessionInfo, value);
        }

        /// <summary>
        /// Gets or sets the type of auxiliary partition information to display.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AuxiliaryPartitionInfo ShowAuxiliaryPartitionInfo
        {
            get => this.showAuxiliaryPartitionInfo;
            set => this.Set(nameof(this.ShowAuxiliaryPartitionInfo), ref this.showAuxiliaryPartitionInfo, value);
        }

        /// <summary>
        /// Gets or sets the type of auxiliary stream information to display.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AuxiliaryStreamInfo ShowAuxiliaryStreamInfo
        {
            get => this.showAuxiliaryStreamInfo;
            set => this.Set(nameof(this.ShowAuxiliaryStreamInfo), ref this.showAuxiliaryStreamInfo, value);
        }

        /// <summary>
        /// Loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <param name="autoSave">A value to indicate whether to enable the autosave feature.</param>
        /// <returns>The newly loaded dataset view model.</returns>
        public static DatasetViewModel Load(string filename, bool autoSave = false) =>
            new (Dataset.Load(filename, autoSave))
            {
                FileName = filename,
            };

        /// <summary>
        /// Asynchronously loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <param name="autoSave">A value to indicate whether to enable the dataset's autosave feature.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the TResult parameter
        /// contains the newly loaded dataset view model.
        /// </returns>
        public static Task<DatasetViewModel> LoadAsync(string filename, bool autoSave = false) =>
            Task.Run(() => Load(filename, autoSave));

        /// <summary>
        /// Creates a new, empty dataset view model.
        /// </summary>
        /// <returns>An empty dataset video model.</returns>
        public static DatasetViewModel Create()
            => new (new Dataset());

        /// <summary>
        /// Creates a new dataset from an existing data store.
        /// </summary>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly created dataset view model.</returns>
        public static async Task<DatasetViewModel> CreateFromStoreAsync(IStreamReader streamReader, string partitionName = null)
            => new (await Dataset.CreateAsync(streamReader, partitionName));

        /// <summary>
        /// Updates the dataset view model based on the latest version of the dataset.
        /// </summary>
        /// <param name="dataset">The new dataset.</param>
        public void Update(Dataset dataset)
        {
            this.dataset = dataset;

            var oldSessions = new HashSet<SessionViewModel>();
            foreach (var existingSession in this.internalSessionViewModels)
            {
                oldSessions.Add(existingSession);
            }

            foreach (var session in this.dataset.Sessions)
            {
                var existingSession = this.internalSessionViewModels.FirstOrDefault(s => s.Name == session.Name);
                if (existingSession != null)
                {
                    existingSession.Update(session);
                    oldSessions.Remove(existingSession);
                }
                else
                {
                    this.internalSessionViewModels.Add(new SessionViewModel(this, session));
                }
            }

            // The sessions remaining in oldSessions at this point are the ones that need to be removed.
            // If the current session happens to be among them, change it to the first session.
            if (oldSessions.Contains(this.currentSessionViewModel))
            {
                this.currentSessionViewModel = null;
            }

            // Now remove all the old sessions that are no longer in the dataset
            foreach (var session in oldSessions)
            {
                this.internalSessionViewModels.Remove(session);
            }

            // now set the current session if null
            this.currentSessionViewModel ??= this.internalSessionViewModels.FirstOrDefault();
        }

        /// <summary>
        /// Sets a session to be the current session being visualized.
        /// </summary>
        /// <param name="sessionViewModel">The SessionViewModel to visualize.</param>
        public void VisualizeSession(SessionViewModel sessionViewModel)
        {
            VisualizationContainer visualizationContainer = VisualizationContext.Instance.VisualizationContainer;

            // Make sure we unlock the cursor
            visualizationContainer.Navigator.CursorFollowsMouse = true;

            // We need to ensure we're not in Live or Playback mode before we unbind
            visualizationContainer.Navigator.SetManualCursorMode();

            // Update the current session
            this.Set(nameof(this.CurrentSessionViewModel), ref this.currentSessionViewModel, sessionViewModel);

            if (this.CurrentSessionViewModel != null)
            {
                // Get the session extents
                TimeInterval sessionExtents = this.CurrentSessionViewModel.OriginatingTimeInterval;

                // Update the bindings on all sessions
                visualizationContainer.UpdateStreamSources(this.CurrentSessionViewModel);

                // Update the navigator with the session extents
                visualizationContainer.Navigator.DataRange.Set(sessionExtents);
                visualizationContainer.Navigator.ViewRange.Set(sessionExtents);
                visualizationContainer.Navigator.Cursor = sessionExtents.Left;

                // If the current session has live data, switch to live mode
                if (this.CurrentSessionViewModel.ContainsLivePartitions)
                {
                    visualizationContainer.Navigator.SetLiveCursorMode();
                }
            }
            else
            {
                // There is no current session, so unbind everything
                visualizationContainer.UpdateStreamSources(null);
            }
        }

        /// <summary>
        /// Add a new, empty session within the dataset.
        /// </summary>
        public void AddEmptySession()
        {
            string sessionName = this.EnsureUniqueSessionName(Session.DefaultName);
            this.AddSession(this.dataset.AddEmptySession(sessionName));
        }

        /// <summary>
        /// Adds a new session containing a single partition from a file specified by the user.
        /// </summary>
        /// <returns>The task for adding a new session containing a single partition from a store specified by the user.</returns>
        public async Task<SessionViewModel> AddSessionFromFileAsync()
        {
            var formats = VisualizationContext.Instance.PluginMap.GetStreamReaderExtensions();
            var openFileDialog = new Win32.OpenFileDialog
            {
                DefaultExt = ".psi",
                Filter = string.Join("|", formats.Select(f => $"{f.Name}|*{f.Extensions}")),
            };

            bool? result = openFileDialog.ShowDialog(Application.Current.MainWindow);
            if (result == true)
            {
                var fileInfo = new FileInfo(openFileDialog.FileName);
                var sessionName = fileInfo.Directory.Name;
                var storeName = fileInfo.Name.Split('.')[0];
                var storePath = fileInfo.DirectoryName;
                var readerType = VisualizationContext.Instance.PluginMap.GetStreamReaderType(fileInfo.Extension);
                var streamReader = Psi.Data.StreamReader.Create(storeName, storePath, readerType);
                var sessionViewModel = await this.AddSessionAsync(streamReader, sessionName);

                // if this is the only session, set it to visualize
                if (this.dataset.Sessions.Count == 1)
                {
                    this.VisualizeSession(sessionViewModel);
                }

                return sessionViewModel;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Adds a session containing a single partition based on the specified stream reader.
        /// </summary>
        /// <param name="streamReader">The stream reader.</param>
        /// <param name="sessionName">The name of the session.</param>
        /// <param name="partitionName">An optional name for the partition (default to the stream reader name).</param>
        /// <returns>The task for adding a session containing a single partition based on the specified stream reader..</returns>
        public async Task<SessionViewModel> AddSessionAsync(IStreamReader streamReader, string sessionName, string partitionName = null)
        {
            sessionName = this.EnsureUniqueSessionName(sessionName);
            var session = await this.dataset.AddSessionAsync(streamReader, sessionName, partitionName ?? streamReader.Name);
            return this.AddSession(session);
        }

        /// <summary>
        /// Adds a session containing multiple psi store partitions from a user-specified folder.
        /// </summary>
        /// <returns>The task for adding a session containing multiple psi store partitions from a user-specified folder.</returns>
        public async Task<SessionViewModel> AddSessionFromFolderAsync()
        {
            var selectFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder containing the partitions to be added as a new session.",
                ShowNewFolderButton = false,
            };

            // Get the dataset directory name to prepopulate the folder select path
            var datasetDirectoryName = string.IsNullOrEmpty(this.dataset.Filename) ? default : new FileInfo(this.dataset.Filename).DirectoryName;
            if (datasetDirectoryName != default)
            {
                selectFolderDialog.SelectedPath = datasetDirectoryName;
            }

            var result = selectFolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // get the folder
                var selectedFolder = selectFolderDialog.SelectedPath;

                // set the session name to be the directory name
                var sessionName = new FileInfo(selectedFolder).Name;
                var session = this.dataset.AddEmptySession(sessionName);
                var sessionViewModel = this.AddSession(session);

                await sessionViewModel.AddMultiplePsiStorePartitionsFromFolderAsync(selectedFolder);

                // if this is the only session, set it to visualize
                if (this.dataset.Sessions.Count == 1)
                {
                    this.VisualizeSession(sessionViewModel);
                }

                return sessionViewModel;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Adds multiple sessions from a user-specified folder.
        /// </summary>
        /// <returns>The task for adding multiple sessions from a user-specified folder.</returns>
        public async Task AddMultipleSessionsFromFolderAsync()
        {
            var selectFolderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder containing the sessions to be added to the dataset.",
                ShowNewFolderButton = false,
            };

            // Get the dataset directory name to prepopulate the folder select path
            var datasetDirectoryName = string.IsNullOrEmpty(this.dataset.Filename) ? default : new FileInfo(this.dataset.Filename).DirectoryName;
            if (datasetDirectoryName != default)
            {
                selectFolderDialog.SelectedPath = datasetDirectoryName;
            }

            var result = selectFolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                (var existingSessions, var existingPartitions) = await ProgressWindow.RunWithProgressAsync(
                    $"Adding multiple sessions from {selectFolderDialog.SelectedPath} ...",
                    async progress => await this.AddMultipleSessionsFromFolderAsync(selectFolderDialog.SelectedPath, progress));

                if (existingSessions.Count > 0 || existingPartitions.Count > 0)
                {
                    var message = default(string);
                    if (existingSessions.Count > 0)
                    {
                        if (existingPartitions.Count > 0)
                        {
                            message = $"{existingSessions.Count} session(s) and {existingPartitions.Count} partition(s) already existed in the dataset and were not added.";
                        }
                        else
                        {
                            message = $"{existingSessions.Count} session(s) already existed in the dataset and were not added.";
                        }
                    }
                    else if (existingPartitions.Count > 0)
                    {
                        message = $"{existingPartitions} partition(s) already existed in the dataset and were not added.";
                    }

                    // Inform the user of partitions that were already present in the session
                    new MessageBoxWindow(Application.Current.MainWindow, "Existing Sessions or Partitions", message, "Close", null)
                        .ShowDialog();
                }
            }
        }

        /// <summary>
        /// Adds multiple sessions from a specified folder.
        /// </summary>
        /// <param name="folderName">The folder to add sessions from.</param>
        /// <param name="progress">An optional progress updates receiver.</param>
        /// <returns>The task for adding multiple sessions from a specified folder.</returns>
        public async Task<(List<Session> ExistingSessions, List<IPartition> ExistingPartition)> AddMultipleSessionsFromFolderAsync(
            string folderName,
            IProgress<(string, double)> progress = null)
        {
            var existingSessions = new List<Session>();
            var existingPartitions = new List<IPartition>();

            // go through the subdirectories
            var sessionFolders = Directory.GetDirectories(folderName);
            for (int i = 0; i < sessionFolders.Length; i++)
            {
                // set the session name to be the directory name
                var sessionName = new FileInfo(sessionFolders[i]).Name;

                // check if the dataset already contains a session by that name
                var existingSession = this.dataset.Sessions.FirstOrDefault(s => s.Name == sessionName);
                if (existingSession != null)
                {
                    // add it to the list of existing session
                    existingSessions.Add(existingSession);
                }
                else
                {
                    this.AddSession(this.dataset.AddEmptySession(sessionName));
                }

                // get the view model
                var sessionViewModel = this.sessionViewModels.First(s => s.Name == sessionName);

                // add partitions from the folder
                progress?.Report(($"Loading psi store partitions from {sessionFolders[i]}", i / (double)sessionFolders.Length));
                int j = i;
                var existingPartitionsInSession = await sessionViewModel.AddMultiplePsiStorePartitionsFromFolderAsync(
                    sessionFolders[i],
                    new Progress<(string, double)>(t => progress?.Report(
                        ($"Loading psi store partitions from {sessionFolders[j]}\n{t.Item1}",
                        (j + t.Item2) / sessionFolders.Length))));

                // and add any existing partitions to the range.
                existingPartitions.AddRange(existingPartitionsInSession);
            }

            progress?.Report((string.Empty, 1));

            return (existingSessions, existingPartitions);
        }

        /// <summary>
        /// Saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataset into.</param>
        public void SaveAs(string filename)
        {
            this.dataset.SaveAs(filename);
            this.FileName = filename;
        }

        /// <summary>
        /// Asynchronously saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataset into.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SaveAsAsync(string filename) =>
            Task.Run(() => this.SaveAs(filename));

        /// <summary>
        /// Removes the specified session from the underlying dataset.
        /// </summary>
        /// <param name="sessionViewModel">The view model of the session to remove.</param>
        public void RemoveSession(SessionViewModel sessionViewModel)
        {
            // remove the session
            this.dataset.RemoveSession(sessionViewModel.Session);
            this.internalSessionViewModels.Remove(sessionViewModel);

            // If we're removing the current session, find another session to visualize
            if (this.CurrentSessionViewModel == sessionViewModel)
            {
                this.VisualizeSession(this.internalSessionViewModels.FirstOrDefault());
            }
        }

        /// <summary>
        /// Removes a partition specified by name from all sessions.
        /// </summary>
        /// <param name="partitionName">The partition name.</param>
        public void RemovePartitionFromAllSessions(string partitionName)
        {
            var count = this.sessionViewModels.Count(svm => svm.PartitionViewModels.Any(pvm => pvm.Name == partitionName));
            if (count > 1)
            {
                var result = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Are you sure?",
                    $"The partition named {partitionName} appears in {count} sessions. Are you sure you want to remove it from all these sessions?",
                    "Close",
                    null)
                    .ShowDialog();

                if (result == null || !result.Value)
                {
                    return;
                }
            }

            foreach (var sessionViewModel in this.sessionViewModels)
            {
                var partitionViewModel = sessionViewModel.PartitionViewModels.FirstOrDefault(p => p.Name == partitionName);
                if (partitionViewModel != null)
                {
                    if (partitionViewModel.PromptSaveChangesAndContinue())
                    {
                        sessionViewModel.RemovePartition(partitionViewModel);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes a partition specified by name from all sessions.
        /// </summary>
        /// <param name="partitionName">The partition name.</param>
        public void DeletePartitionFromAllSessions(string partitionName)
        {
            var count = this.sessionViewModels.Count(svm => svm.PartitionViewModels.Any(pvm => pvm.Name == partitionName));
            if (count > 1)
            {
                var result = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Are you sure?",
                    $"The partition named {partitionName} appears in {count} sessions. Are you sure you want to delete it from all these sessions? This will permanently delete the partitions from disk.",
                    "Yes",
                    "Cancel")
                    .ShowDialog();

                if (result == null || !result.Value)
                {
                    return;
                }
            }

            var livePartitions = new List<string>();
            var errorsWhileDeletingPartitions = new List<string>();
            var nonPsiPartitions = new List<string>();
            int deletedCount = 0;
            int notFoundCount = 0;

            foreach (var sessionViewModel in this.sessionViewModels)
            {
                var partitionViewModel = sessionViewModel.PartitionViewModels.FirstOrDefault(p => p.Name == partitionName);
                if (partitionViewModel == null)
                {
                    notFoundCount++;
                }
                else if (partitionViewModel.IsLivePartition)
                {
                    livePartitions.Add(sessionViewModel.Name);
                }
                else if (partitionViewModel.IsPsiPartition)
                {
                    try
                    {
                        PsiStore.Delete((partitionViewModel.StoreName, partitionViewModel.StorePath), true);
                        sessionViewModel.RemovePartition(partitionViewModel);
                        deletedCount++;
                    }
                    catch
                    {
                        errorsWhileDeletingPartitions.Add(sessionViewModel.Name);
                    }
                }
                else
                {
                    nonPsiPartitions.Add(partitionViewModel.Name);
                }
            }

            if (livePartitions.Any() || errorsWhileDeletingPartitions.Any())
            {
                var report =
                    $"The partition {partitionName} was deleted from {deletedCount} sessions." + Environment.NewLine +
                    (livePartitions.Any() ? $"The partition was live and therefore not deleted in {livePartitions.Count} sessions: {string.Join(",", livePartitions)}" + Environment.NewLine : string.Empty) +
                    (nonPsiPartitions.Any() ? $"The partition is not a \\psi partition and therefore not deleted in {nonPsiPartitions.Count} sessions: {string.Join(",", nonPsiPartitions)}" + Environment.NewLine : string.Empty) +
                    (errorsWhileDeletingPartitions.Any() ? $"An error occured while trying to delete the partition and therefore the partition was not deleted in {errorsWhileDeletingPartitions.Count} sessions: {string.Join(",", errorsWhileDeletingPartitions)}" + Environment.NewLine : string.Empty) +
                    (notFoundCount > 0 ? $"The partition was not found in {notFoundCount} sessions." + Environment.NewLine : string.Empty);

                new MessageBoxWindow(Application.Current.MainWindow, "Delete Partition Report", report, "Close", null).ShowDialog();
            }
        }

        /// <inheritdoc/>
        public override string ToString() => "Dataset: " + this.Name;

        /// <summary>
        /// Checks all partitions in the session to determine whether they have an active writer attached and updates their IsLivePartition property.
        /// </summary>
        internal void UpdateLivePartitionStatuses()
        {
            foreach (SessionViewModel sessionViewModel in this.SessionViewModels)
            {
                sessionViewModel.UpdateLivePartitionStatuses();
            }
        }

        private SessionViewModel AddSession(Session session)
        {
            var sessionViewModel = new SessionViewModel(this, session);
            this.internalSessionViewModels.Add(sessionViewModel);
            return sessionViewModel;
        }

        private string EnsureUniqueSessionName(string sessionName)
        {
            int suffix = 0;
            string sessionNamePrefix = sessionName;

            // ensure that session name is unique
            while (this.SessionViewModels.Any(svm => svm.Name == sessionName))
            {
                // append numeric suffix to ensure uniqueness
                sessionName = $"{sessionNamePrefix}_{++suffix}";
            }

            return sessionName;
        }

        private ContextMenu CreateContextMenu()
        {
            var contextMenu = new ContextMenu();

            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.SessionCreate,
                    "Create Session",
                    this.CreateSessionCommand));
            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.SessionAddFromFile,
                    "Add Session from Store ...",
                    this.AddSessionFromFileCommand));
            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.SessionAddFromFolder,
                    "Add Session from Folder ...",
                    this.AddSessionFromFolderCommand));
            contextMenu.Items.Add(
                MenuItemHelper.CreateMenuItem(
                    IconSourcePath.MultipleSessionsAddFromFolder,
                    "Add Multiple Sessions from Folder ...",
                    this.AddMultipleSessionsFromFolderCommand));

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
                            new VisualizationCommand<BatchProcessingTaskMetadata>(async bpt => await VisualizationContext.Instance.RunDatasetBatchProcessingTaskAsync(this, bpt)),
                            tag: batchProcessingTask,
                            isEnabled: true,
                            commandParameter: batchProcessingTask));
                }

                runTasksMenuItem.Items.Add(namespaceMenuItem);
            }

            contextMenu.Items.Add(runTasksMenuItem);

            contextMenu.Items.Add(new Separator());

            // Add show dataset info menu
            var showDatasetInfoMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Show Dataset Info", null);
            foreach (var auxiliaryDatasetInfo in Enum.GetValues(typeof(AuxiliaryDatasetInfo)))
            {
                var auxiliaryDatasetInfoValue = (AuxiliaryDatasetInfo)auxiliaryDatasetInfo;
                var auxiliaryDatasetInfoName = auxiliaryDatasetInfoValue switch
                {
                    AuxiliaryDatasetInfo.None => "None",
                    AuxiliaryDatasetInfo.Extent => "Extent",
                    AuxiliaryDatasetInfo.TotalDuration => "Total Duration",
                    AuxiliaryDatasetInfo.StartDate => "Start Date (UTC)",
                    AuxiliaryDatasetInfo.StartDateLocal => "Start Date (Local)",
                    AuxiliaryDatasetInfo.StartTime => "Start Time (UTC)",
                    AuxiliaryDatasetInfo.StartTimeLocal => "Start Time (Local)",
                    AuxiliaryDatasetInfo.StartDateTime => "Start DateTime (UTC)",
                    AuxiliaryDatasetInfo.StartDateTimeLocal => "Start DateTime (Local)",
                    AuxiliaryDatasetInfo.Size => "Size",
                    AuxiliaryDatasetInfo.DataThroughputPerHour => "Throughput (bytes per hour)",
                    AuxiliaryDatasetInfo.DataThroughputPerMinute => "Throughput (bytes per minute)",
                    AuxiliaryDatasetInfo.DataThroughputPerSecond => "Throughput (bytes per second)",
                    AuxiliaryDatasetInfo.StreamCount => "Number of Streams",
                    _ => throw new NotImplementedException(),
                };

                showDatasetInfoMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        this.ShowAuxiliaryDatasetInfo == auxiliaryDatasetInfoValue ? IconSourcePath.Checkmark : null,
                        auxiliaryDatasetInfoName,
                        new VisualizationCommand<AuxiliaryDatasetInfo>(adi => this.ShowAuxiliaryDatasetInfo = adi),
                        commandParameter: auxiliaryDatasetInfoValue));
            }

            contextMenu.Items.Add(showDatasetInfoMenuItem);

            return contextMenu;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ShowAuxiliaryDatasetInfo))
            {
                this.UpdateAuxiliaryInfo();
            }
        }

        private void UpdateAuxiliaryInfo()
        {
            switch (this.ShowAuxiliaryDatasetInfo)
            {
                case AuxiliaryDatasetInfo.None:
                    this.AuxiliaryInfo = string.Empty;
                    break;
                case AuxiliaryDatasetInfo.Extent:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Span.ToString(@"d\.hh\:mm\:ss");
                    break;
                case AuxiliaryDatasetInfo.TotalDuration:
                    this.AuxiliaryInfo = this.TotalDuration.ToString(@"d\.hh\:mm\:ss");
                    break;
                case AuxiliaryDatasetInfo.StartDate:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToShortDateString();
                    break;
                case AuxiliaryDatasetInfo.StartDateLocal:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToLocalTime().ToShortDateString();
                    break;
                case AuxiliaryDatasetInfo.StartTime:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToShortTimeString();
                    break;
                case AuxiliaryDatasetInfo.StartTimeLocal:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToLocalTime().ToShortTimeString();
                    break;
                case AuxiliaryDatasetInfo.StartDateTime:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToString();
                    break;
                case AuxiliaryDatasetInfo.StartDateTimeLocal:
                    this.AuxiliaryInfo = this.OriginatingTimeInterval.Left.ToLocalTime().ToString();
                    break;
                case AuxiliaryDatasetInfo.DataThroughputPerHour:
                    this.AuxiliaryInfo = this.Dataset.Size.HasValue ? SizeHelper.FormatThroughput(this.Dataset.Size.Value / this.TotalDuration.TotalHours, "hour") : "?";
                    break;
                case AuxiliaryDatasetInfo.DataThroughputPerMinute:
                    this.AuxiliaryInfo = this.Dataset.Size.HasValue ? SizeHelper.FormatThroughput(this.Dataset.Size.Value / this.TotalDuration.TotalMinutes, "min") : "?";
                    break;
                case AuxiliaryDatasetInfo.DataThroughputPerSecond:
                    this.AuxiliaryInfo = this.Dataset.Size.HasValue ? SizeHelper.FormatThroughput(this.Dataset.Size.Value / this.TotalDuration.TotalSeconds, "sec") : "?";
                    break;
                case AuxiliaryDatasetInfo.Size:
                    this.AuxiliaryInfo = this.Dataset.Size.HasValue ? SizeHelper.FormatSize(this.Dataset.Size.Value) : "?";
                    break;
                case AuxiliaryDatasetInfo.StreamCount:
                    this.AuxiliaryInfo = this.Dataset.StreamCount.HasValue ? (this.Dataset.StreamCount == 0 ? "0" : $"{this.Dataset.StreamCount.Value:0,0.}") : "?";
                    break;
                default:
                    break;
            }
        }
    }
}
