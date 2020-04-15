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
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Helpers;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a view model of a session.
    /// </summary>
    public class SessionViewModel : ObservableTreeNodeObject
    {
        private Session session;
        private DatasetViewModel datasetViewModel;
        private ObservableCollection<PartitionViewModel> internalPartitionViewModels;
        private ReadOnlyObservableCollection<PartitionViewModel> partitionViewModels;
        private bool containsLivePartitions = false;

        private RelayCommand addPartitionCommand;
        private RelayCommand removeSessionCommand;
        private RelayCommand visualizeSessionCommand;

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
        /// Gets the orginating time interval (earliest to latest) of the messages in this session.
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
        public RelayCommand AddPartitionCommand
        {
            get
            {
                if (this.addPartitionCommand == null)
                {
                    this.addPartitionCommand = new RelayCommand(
                        () =>
                        {
                            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
                            dlg.DefaultExt = ".psi";
                            dlg.Filter = "Psi Store (.psi)|*.psi|Psi Annotation Store (.pas)|*.pas";
                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                var fileInfo = new FileInfo(dlg.FileName);
                                var name = fileInfo.Name.Split('.')[0];

                                if (fileInfo.Extension == ".psi")
                                {
                                    this.AddStorePartition(name, fileInfo.DirectoryName);
                                }
                                else if (fileInfo.Extension == ".pas")
                                {
                                    this.AddAnnotationPartition(name, fileInfo.DirectoryName);
                                }
                                else
                                {
                                    throw new ApplicationException("Invalid file type selected when adding partition.");
                                }
                            }
                        });
                }

                return this.addPartitionCommand;
            }
        }

        /// <summary>
        /// Gets the remove session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemoveSessionCommand
        {
            get
            {
                if (this.removeSessionCommand == null)
                {
                    this.removeSessionCommand = new RelayCommand(() => this.RemoveSession());
                }

                return this.removeSessionCommand;
            }
        }

        /// <summary>
        /// Gets the visualize session command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand VisualizeSessionCommand
        {
            get
            {
                if (this.visualizeSessionCommand == null)
                {
                    this.visualizeSessionCommand = new RelayCommand(
                        () =>
                        {
                            this.DatasetViewModel.VisualizeSession(this);
                        });
                }

                return this.visualizeSessionCommand;
            }
        }

        /// <summary>
        /// Gets the underlying session.
        /// </summary>
        public Session Session => this.session;

        /// <summary>
        /// Creates and adds an annotation partition from an existing annotation store.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        public void AddAnnotationPartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            this.AddPartition(this.session.AddAnnotationPartition(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Creates and adds a data partition from an existing data store.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        public void AddStorePartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            this.AddPartition(this.session.AddStorePartition(storeName, storePath, partitionName));
        }

        /// <summary>
        /// Creates and adds an new annotation partition.
        /// </summary>
        /// <param name="storeName">The name of the annotation store.</param>
        /// <param name="storePath">The path of the annotation store.</param>
        /// <param name="definition">The annotated event definition to use when creating new annoted events in the newly created annotation partition.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        public void CreateAnnotationPartition(string storeName, string storePath, AnnotatedEventDefinition definition, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            this.AddPartition(this.session.CreateAnnotationPartition(storeName, storePath, definition, partitionName));
        }

        /// <summary>
        /// Creates and adds a new data partition.
        /// </summary>
        /// <param name="storeName">The name of the data store.</param>
        /// <param name="storePath">The path of the data store.</param>
        /// <param name="partitionName">The partition name. Default is null.</param>
        public void CreateStorePartition(string storeName, string storePath, string partitionName = null)
        {
            partitionName = this.EnsureUniquePartitionName(partitionName ?? storeName);
            this.AddPartition(this.session.CreateStorePartition(storeName, storePath, partitionName));
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
            this.datasetViewModel.RemoveSession(this);
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
    }
}
