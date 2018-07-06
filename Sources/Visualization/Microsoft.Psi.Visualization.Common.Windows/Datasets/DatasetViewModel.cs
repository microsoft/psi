// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Datasets
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a view model of a dataset.
    /// </summary>
    public class DatasetViewModel : ObservableObject
    {
        private Dataset dataset;
        private string filename;
        private SessionViewModel currentSessionViewModel;
        private ObservableCollection<SessionViewModel> internalSessionViewModels;
        private ReadOnlyObservableCollection<SessionViewModel> sessionViewModels;

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
        public string Name
        {
            get => this.dataset.Name;
            set
            {
                if (this.dataset.Name != value)
                {
                    this.dataset.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current session view model for this dataset view model.
        /// </summary>
        [Browsable(false)]
        public SessionViewModel CurrentSessionViewModel
        {
            get { return this.currentSessionViewModel; }
            set { this.Set(nameof(this.CurrentSessionViewModel), ref this.currentSessionViewModel, value); }
        }

        /// <summary>
        /// Gets the filename of the underlying dataset.
        /// </summary>
        public string FileName
        {
            get { return this.filename; }
            private set { this.Set(nameof(this.filename), ref this.filename, value); }
        }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this dataset.
        /// </summary>
        [Browsable(false)]
        public TimeInterval OriginatingTimeInterval => this.dataset.OriginatingTimeInterval;

        /// <summary>
        /// Gets the collection of sessions in this dataset.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<SessionViewModel> SessionViewModels => this.sessionViewModels;

        /// <summary>
        /// Loads a dataset from the specified file.
        /// </summary>
        /// <param name="filename">The name of the file that contains the dataset to be loaded.</param>
        /// <returns>The newly loaded dataset.</returns>
        public static DatasetViewModel Load(string filename)
        {
            var viewModel = new DatasetViewModel(Dataset.Load(filename));
            viewModel.FileName = filename;
            return viewModel;
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
        /// Creates a new session within the dataset.
        /// </summary>
        /// <returns>The newly created session.</returns>
        public SessionViewModel CreateSession()
        {
            string sessionName = this.EnsureUniqueSessionName(Session.DefaultName);
            return this.AddSession(this.dataset.CreateSession(sessionName));
        }

        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="sessionName">The name of the session.</param>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly added session.</returns>
        public SessionViewModel AddSessionFromExistingStore(string sessionName, string storeName, string storePath, string partitionName = null)
        {
            sessionName = this.EnsureUniqueSessionName(sessionName);
            return this.AddSession(this.dataset.AddSessionFromExistingStore(sessionName, storeName, storePath, partitionName));
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
        /// Removes the specified session from the underlying dataset.
        /// </summary>
        /// <param name="sessionViewModel">The view model of the session to remove.</param>
        public void RemoveSession(SessionViewModel sessionViewModel)
        {
            this.dataset.RemoveSession(sessionViewModel.Session);
            this.internalSessionViewModels.Remove(sessionViewModel);
        }

        private SessionViewModel AddSession(Session session)
        {
            var sessionViewModel = new SessionViewModel(this, session);
            this.internalSessionViewModels.Add(sessionViewModel);
            this.CurrentSessionViewModel = sessionViewModel;
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
    }
}
