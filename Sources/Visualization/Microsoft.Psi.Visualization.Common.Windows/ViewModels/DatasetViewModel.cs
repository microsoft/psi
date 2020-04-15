// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a view model of a dataset.
    /// </summary>
    public class DatasetViewModel : ObservableTreeNodeObject
    {
        private Dataset dataset;
        private string filename;
        private SessionViewModel currentSessionViewModel = null;
        private ObservableCollection<SessionViewModel> internalSessionViewModels;
        private ReadOnlyObservableCollection<SessionViewModel> sessionViewModels;

        private RelayCommand createSessionCommand;
        private RelayCommand createSessionFromExistingStoreCommand;
        private RelayCommand closeDatasetCommand;

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
        public RelayCommand CreateSessionCommand
        {
            get
            {
                if (this.createSessionCommand == null)
                {
                    this.createSessionCommand = new RelayCommand(() => this.CreateSession());
                }

                return this.createSessionCommand;
            }
        }

        /// <summary>
        /// Gets the create session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateSessionFromExistingStoreCommand
        {
            get
            {
                if (this.createSessionFromExistingStoreCommand == null)
                {
                    this.createSessionFromExistingStoreCommand = new RelayCommand(
                        () =>
                        {
                            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
                            dlg.DefaultExt = ".psi";
                            dlg.Filter = "Psi Store (.psi)|*.psi";

                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                var fileInfo = new FileInfo(dlg.FileName);
                                var name = fileInfo.Name.Split('.')[0];
                                this.CreateSessionFromExistingStore(name, name, fileInfo.DirectoryName);
                            }
                        });
                }

                return this.createSessionFromExistingStoreCommand;
            }
        }

        /// <summary>
        /// Gets the close dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CloseDatasetCommand
        {
            get
            {
                if (this.closeDatasetCommand == null)
                {
                    this.closeDatasetCommand = new RelayCommand(() => { });
                }

                return this.closeDatasetCommand;
            }
        }

        /// <summary>
        /// Loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <returns>The newly loaded dataset view model.</returns>
        public static DatasetViewModel Load(string filename)
        {
            var viewModel = new DatasetViewModel(Dataset.Load(filename));
            viewModel.FileName = filename;
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
        /// Creates a new dataset from an exising data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly created dataset view model.</returns>
        public static DatasetViewModel CreateFromExistingStore(string storeName, string storePath, string partitionName = null)
        {
            return new DatasetViewModel(Dataset.CreateFromExistingStore(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Asynchronously creates a new dataset from an exising data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the TResult parameter
        /// contains the newly created dataset view model.
        /// </returns>
        public static Task<DatasetViewModel> CreateFromExistingStoreAsync(string storeName, string storePath, string partitionName = null)
        {
            // Wrapping synchronous CreateFromExistingStore method in a Task for now. Eventually we should
            // plumb this all the way down into the Dataset and implement progressive loading.
            return Task.Run(() => CreateFromExistingStore(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Sets a session to be the currrent session being visualized.
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
                visualizationContainer.Navigator.SelectionRange.SetRange(sessionExtents);

                // Update the bindings on all sessions
                visualizationContainer.UpdateStreamBindings(this.CurrentSessionViewModel.Session);

                // If the current session has live data, switch to live mode
                if (this.CurrentSessionViewModel.ContainsLivePartitions)
                {
                    visualizationContainer.Navigator.SetCursorMode(CursorMode.Live);
                }
            }
            else
            {
                // There is no current session, so unbind everything
                visualizationContainer.UpdateStreamBindings(null);
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
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="sessionName">The name of the session.</param>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        public void CreateSessionFromExistingStore(string sessionName, string storeName, string storePath, string partitionName = null)
        {
            sessionName = this.EnsureUniqueSessionName(sessionName);
            this.AddSession(this.dataset.AddSessionFromExistingStore(sessionName, storeName, storePath, partitionName));
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
    }
}
