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
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Tasks;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a view model of a dataset.
    /// </summary>
    public class DatasetViewModel : ObservableTreeNodeObject
    {
        private readonly Dataset dataset;
        private readonly ObservableCollection<SessionViewModel> internalSessionViewModels;
        private readonly ReadOnlyObservableCollection<SessionViewModel> sessionViewModels;
        private string filename;
        private SessionViewModel currentSessionViewModel = null;

        private RelayCommand createSessionCommand;
        private RelayCommand createSessionFromStoreCommand;
        private RelayCommand<StackPanel> contextMenuOpeningCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetViewModel"/> class.
        /// </summary>
        /// <param name="dataset">The dataset for which to create the view model.</param>
        public DatasetViewModel(Dataset dataset)
        {
            this.dataset = dataset;
            this.internalSessionViewModels = new ObservableCollection<SessionViewModel>();
            this.sessionViewModels = new ReadOnlyObservableCollection<SessionViewModel>(this.internalSessionViewModels);
            foreach (var item in this.dataset.Sessions)
            {
                this.internalSessionViewModels.Add(new SessionViewModel(this, item));
            }

            this.currentSessionViewModel = this.internalSessionViewModels.FirstOrDefault();
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
        /// Gets or sets the name of this dataset.
        /// </summary>
        [PropertyOrder(0)]
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
        /// Gets the current session view model for this dataset view model.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel CurrentSessionViewModel
        {
            get => this.currentSessionViewModel;
        }

        /// <summary>
        /// Gets the filename of the underlying dataset.
        /// </summary>
        [PropertyOrder(1)]
        [Description("The full path to the dataset.")]
        public string FileName
        {
            get => this.filename;
            private set => this.Set(nameof(this.filename), ref this.filename, value);
        }

        /// <summary>
        /// Gets the collection of sessions in this dataset.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<SessionViewModel> SessionViewModels => this.sessionViewModels;

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
        /// Gets the create session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateSessionCommand => this.createSessionCommand ??= new RelayCommand(() => this.CreateSession());

        /// <summary>
        /// Gets the create session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateSessionFromStoreCommand => this.createSessionFromStoreCommand ??= new RelayCommand(() => this.CreateSessionFromStore());

        /// <summary>
        /// Gets the command that executes when opening the dataset context menu.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<StackPanel> ContextMenuOpeningCommand => this.contextMenuOpeningCommand ??= new RelayCommand<StackPanel>(panel => panel.ContextMenu = this.CreateContextMenu());

        /// <summary>
        /// Gets the underlying dataset.
        /// </summary>
        public Dataset Dataset => this.dataset;

        /// <summary>
        /// Loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <returns>The newly loaded dataset view model.</returns>
        public static DatasetViewModel Load(string filename)
        {
            var viewModel = new DatasetViewModel(Dataset.Load(filename))
            {
                FileName = filename,
            };
            return viewModel;
        }

        /// <summary>
        /// Asynchronously loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the TResult parameter
        /// contains the newly loaded dataset view model.
        /// </returns>
        public static Task<DatasetViewModel> LoadAsync(string filename)
        {
            // Wrapping synchronous Load method in a Task for now. Eventually we should plumb this all
            // the way down into the Dataset and implement progressive loading.
            return Task.Run(() => Load(filename));
        }

        /// <summary>
        /// Creates a new dataset from an existing data store.
        /// </summary>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly created dataset view model.</returns>
        public static DatasetViewModel CreateFromStore(IStreamReader streamReader, string partitionName = null)
        {
            return new DatasetViewModel(Dataset.CreateFromStore(streamReader, partitionName));
        }

        /// <summary>
        /// Asynchronously creates a new dataset from an existing data store.
        /// </summary>
        /// <param name="streamReader">The stream reader of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the TResult parameter
        /// contains the newly created dataset view model.
        /// </returns>
        public static Task<DatasetViewModel> CreateFromStoreAsync(IStreamReader streamReader, string partitionName = null)
        {
            // Wrapping synchronous CreateFromStore method in a Task for now. Eventually we should
            // plumb this all the way down into the Dataset and implement progressive loading.
            return Task.Run(() => CreateFromStore(streamReader, partitionName));
        }

        /// <summary>
        /// Updates the dataset view model based on the latest version of the dataset.
        /// </summary>
        public void Update()
        {
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
                    existingSession.Update();
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

            // We need to ensure we're not in Live or Playback mode before we unbind
            visualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);

            // Update the current session
            this.Set(nameof(this.CurrentSessionViewModel), ref this.currentSessionViewModel, sessionViewModel);

            if (this.CurrentSessionViewModel != null)
            {
                // Get the session extents
                TimeInterval sessionExtents = this.CurrentSessionViewModel.OriginatingTimeInterval;

                // Update the navigator with the session extents
                visualizationContainer.Navigator.DataRange.SetRange(sessionExtents);
                visualizationContainer.Navigator.ViewRange.SetRange(sessionExtents);

                // Update the bindings on all sessions
                visualizationContainer.UpdateStreamSources(this.CurrentSessionViewModel);

                // If the current session has live data, switch to live mode
                if (this.CurrentSessionViewModel.ContainsLivePartitions)
                {
                    visualizationContainer.Navigator.SetCursorMode(CursorMode.Live);
                }
            }
            else
            {
                // There is no current session, so unbind everything
                visualizationContainer.UpdateStreamSources(null);
            }
        }

        /// <summary>
        /// Creates a new session within the dataset.
        /// </summary>
        public void CreateSession()
        {
            string sessionName = this.EnsureUniqueSessionName(Session.DefaultName);
            this.AddSession(this.dataset.CreateSession(sessionName));
        }

        /// <summary>
        /// Creates a new session from a store.
        /// </summary>
        public void CreateSessionFromStore()
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
                var readerType = VisualizationContext.Instance.PluginMap.GetStreamReaderType(fileInfo.Extension);
                var streamReader = Psi.Data.StreamReader.Create(name, fileInfo.DirectoryName, readerType);
                this.CreateSessionFromStore(name, streamReader);
            }
        }

        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="sessionName">The name of the session.</param>
        /// <param name="streamReader">The stream reader for the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        public void CreateSessionFromStore(string sessionName, IStreamReader streamReader, string partitionName = null)
        {
            sessionName = this.EnsureUniqueSessionName(sessionName);
            this.AddSession(this.dataset.AddSessionFromStore(streamReader, sessionName, partitionName));
        }

        /// <summary>
        /// Saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataset into.</param>
        public void Save(string filename)
        {
            this.dataset.Save(filename);
            this.FileName = filename;
        }

        /// <summary>
        /// Asynchronously saves this dataset to the specified file.
        /// </summary>
        /// <param name="filename">The name of the file to save this dataset into.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SaveAsync(string filename)
        {
            // Wrapping synchronous Save method in a Task for now. Eventually we should plumb this all
            // the way down into the Dataset.
            return Task.Run(() => this.Save(filename));
        }

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

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Dataset: " + this.Name;
        }

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

        private void AddSession(Session session)
        {
            this.internalSessionViewModels.Add(new SessionViewModel(this, session));
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
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.SessionCreate, "Create Session", this.CreateSessionCommand));
            contextMenu.Items.Add(MenuItemHelper.CreateMenuItem(IconSourcePath.SessionCreateFromStore, "Create Session from Store ...", this.CreateSessionFromStoreCommand));

            contextMenu.Items.Add(new Separator());

            // Add run batch processing task menu
            var runTasksMenuItem = MenuItemHelper.CreateMenuItem(string.Empty, "Run Batch Processing Task", null);
            var batchProcessingTasks = VisualizationContext.Instance.PluginMap.GetDatasetCompatibleBatchProcessingTasks();
            runTasksMenuItem.IsEnabled = batchProcessingTasks.Any();
            foreach (var batchProcessingTask in batchProcessingTasks)
            {
                runTasksMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        batchProcessingTask.IconSourcePath,
                        batchProcessingTask.Name,
                        new VisualizationCommand<BatchProcessingTaskMetadata>(async s => await VisualizationContext.Instance.RunDatasetBatchProcessingTaskAsync(this, batchProcessingTask)),
                        batchProcessingTask));
            }

            contextMenu.Items.Add(runTasksMenuItem);

            return contextMenu;
        }
    }
}
