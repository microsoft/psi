// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Annotations;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Microsoft.Win32;

    /// <summary>
    /// Data context for PsiStudio.
    /// </summary>
    public class PsiStudioContext : ObservableObject
    {
        private VisualizationContainer visualizationContainer;
        private SimpleReader dataStore;
        private DatasetViewModel datasetViewModel;

        private Pipeline audioPlaybackPipeline;
        private string playbackSpeed = "1.0";
        private int tabControlIndex = (int)TabControlInicies.Visualizations;

        private RelayCommand closedCommand;
        private RelayCommand openStoreCommand;
        private RelayCommand openDatasetCommand;
        private RelayCommand saveDatasetCommand;
        private RelayCommand loadLayoutCommand;
        private RelayCommand saveLayoutCommand;
        private RelayCommand insertTimelinePanelCommand;
        private RelayCommand insert2DPanelCommand;
        private RelayCommand insert3DPanelCommand;
        private RelayCommand insertAnnotationCommand;
        private RelayCommand absoluteTimingCommand;
        private RelayCommand timingRelativeToSessionStartCommand;
        private RelayCommand timingRelativeToSelectionStartCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand playbackStartCommand;
        private RelayCommand playbackStopCommand;

        private RelayCommand deleteVisualizationCommand;
        private RelayCommand<RoutedPropertyChangedEventArgs<object>> selectedVisualizationChangedCommand;

        private List<TypeKeyedActionCommand> typeVisualizerActions = new List<TypeKeyedActionCommand>();
        private object selectedVisualization;

        static PsiStudioContext()
        {
            PsiStudioContext.Instance = new PsiStudioContext();
        }

        private PsiStudioContext()
        {
            this.InitVisualizeStreamCommands();

            var booleanSchema = new AnnotationSchema("Boolean");
            booleanSchema.AddSchemaValue(null, System.Drawing.Color.Gray);
            booleanSchema.AddSchemaValue("false", System.Drawing.Color.Red);
            booleanSchema.AddSchemaValue("true", System.Drawing.Color.Green);
            AnnotationSchemaRegistry.Default.Register(booleanSchema);

            this.DatasetViewModel = new DatasetViewModel();
            this.DatasetViewModels = new ObservableCollection<DatasetViewModel> { this.datasetViewModel };
        }

        private enum TabControlInicies : int
        {
            Visualizations = 0,
            Datasets = 1
        }

        /// <summary>
        /// Gets the PsiStudioContext singleton.
        /// </summary>
        public static PsiStudioContext Instance { get; private set; }

        /// <summary>
        /// Gets the annotation schema registry.
        /// </summary>
        public AnnotationSchemaRegistry AnnotationSchemaRegistry => AnnotationSchemaRegistry.Default;

        /// <summary>
        /// Gets the collection of dataset view models.
        /// </summary>
        public ObservableCollection<DatasetViewModel> DatasetViewModels { get; private set; }

        /// <summary>
        /// Gets or sets the current dataset view model.
        /// </summary>
        public DatasetViewModel DatasetViewModel
        {
            get => this.datasetViewModel;
            set => this.Set(nameof(this.DatasetViewModel), ref this.datasetViewModel, value);
        }

        /// <summary>
        /// Gets or sets the visualization container.
        /// </summary>
        public VisualizationContainer VisualizationContainer
        {
            get => this.visualizationContainer;
            set => this.Set(nameof(this.VisualizationContainer), ref this.visualizationContainer, value);
        }

        /// <summary>
        /// Gets the data stroe.
        /// </summary>
        public SimpleReader DataStore
        {
            get => this.dataStore;
            private set { this.Set(nameof(this.DataStore), ref this.dataStore, value); }
        }

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        public string PlaybackSpeed
        {
            get => this.playbackSpeed;
            set { this.Set(nameof(this.PlaybackSpeed), ref this.playbackSpeed, value); }
        }

        /// <summary>
        /// Gets or sets the current tab control index.
        /// </summary>
        public int TabControlIndex
        {
            get => this.tabControlIndex;
            set { this.Set(nameof(this.TabControlIndex), ref this.tabControlIndex, value); }
        }

        /// <summary>
        /// Gets the closed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClosedCommand
        {
            get
            {
                if (this.closedCommand == null)
                {
                    // Ensure playback is stopped before exiting
                    this.closedCommand = new RelayCommand(
                        () =>
                        {
                            this.PlaybackStop();

                            // Explicitly dispose so that DataManager doesn't keep the app running for a while longer.
                            DataManager.Instance?.Dispose();
                        });
                }

                return this.closedCommand;
            }
        }

        /// <summary>
        /// Gets the open store command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand OpenStoreCommand
        {
            get
            {
                if (this.openStoreCommand == null)
                {
                    this.openStoreCommand = new RelayCommand(
                        async () =>
                        {
                            OpenFileDialog dlg = new OpenFileDialog();
                            dlg.DefaultExt = ".psi";
                            dlg.Filter = "Psi Store (.psi)|*.psi";

                            bool? result = dlg.ShowDialog();
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                await this.OpenDatasetAsync(filename);
                            }
                        });
                }

                return this.openStoreCommand;
            }
        }

        /// <summary>
        /// Gets the open dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand OpenDatasetCommand
        {
            get
            {
                if (this.openDatasetCommand == null)
                {
                    this.openDatasetCommand = new RelayCommand(
                        async () =>
                        {
                            OpenFileDialog dlg = new OpenFileDialog();
                            dlg.DefaultExt = ".pds";
                            dlg.Filter = "Psi Dataset (.pds)|*.pds";

                            bool? result = dlg.ShowDialog();
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                await this.OpenDatasetAsync(filename);
                            }
                        });
                }

                return this.openDatasetCommand;
            }
        }

        /// <summary>
        /// Gets the save dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveDatasetCommand
        {
            get
            {
                if (this.saveDatasetCommand == null)
                {
                    this.saveDatasetCommand = new RelayCommand(
                        async () =>
                        {
                            SaveFileDialog dlg = new SaveFileDialog();
                            dlg.DefaultExt = ".pds";
                            dlg.Filter = "Psi Dataset (.pds)|*.pds";

                            bool? result = dlg.ShowDialog();
                            if (result == true)
                            {
                                string filename = dlg.FileName;

                                // this should be a relatively quick operation so no need to show progress
                                await this.DatasetViewModel.SaveAsync(filename);
                            }
                        });
                }

                return this.saveDatasetCommand;
            }
        }

        /// <summary>
        /// Gets the load layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand LoadLayoutCommand
        {
            get
            {
                if (this.loadLayoutCommand == null)
                {
                    this.loadLayoutCommand = new RelayCommand(
                        () =>
                        {
                            OpenFileDialog dlg = new OpenFileDialog();
                            dlg.DefaultExt = ".plo";
                            dlg.Filter = "Psi Layout (.plo)|*.plo";

                            bool? result = dlg.ShowDialog();
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                this.OpenLayout(filename);
                                this.TabControlIndex = (int)TabControlInicies.Visualizations;
                            }
                        });
                }

                return this.loadLayoutCommand;
            }
        }

        /// <summary>
        /// Gets the save layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveLayoutCommand
        {
            get
            {
                if (this.saveLayoutCommand == null)
                {
                    this.saveLayoutCommand = new RelayCommand(
                        () =>
                        {
                            SaveFileDialog dlg = new SaveFileDialog();
                            dlg.DefaultExt = ".plo";
                            dlg.Filter = "Psi Layout (.plo)|*.plo";

                            bool? result = dlg.ShowDialog();
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                this.VisualizationContainer.Save(filename);
                            }
                        });
                }

                return this.saveLayoutCommand;
            }
        }

        /// <summary>
        /// Gets the insert timeline panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand InsertTimelinePanelCommand
        {
            get
            {
                if (this.insertTimelinePanelCommand == null)
                {
                    this.insertTimelinePanelCommand = new RelayCommand(
                        () => this.VisualizationContainer.AddPanel(new TimelineVisualizationPanel()),
                        () => this.IsDatasetLoaded());
                }

                return this.insertTimelinePanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert 2D panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert2DPanelCommand
        {
            get
            {
                if (this.insert2DPanelCommand == null)
                {
                    this.insert2DPanelCommand = new RelayCommand(
                        () => this.VisualizationContainer.AddPanel(new XYVisualizationPanel()),
                        () => this.IsDatasetLoaded());
                }

                return this.insert2DPanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert 3D panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert3DPanelCommand
        {
            get
            {
                if (this.insert3DPanelCommand == null)
                {
                    this.insert3DPanelCommand = new RelayCommand(
                        () => this.VisualizationContainer.AddPanel(new XYZVisualizationPanel()),
                        () => this.IsDatasetLoaded());
                }

                return this.insert3DPanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert annotation command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand InsertAnnotationCommand
        {
            get
            {
                if (this.insertAnnotationCommand == null)
                {
                    this.insertAnnotationCommand = new RelayCommand(
                        () => this.AddAnnotation(App.Current.MainWindow),
                        () => this.IsDatasetLoaded());
                }

                return this.insertAnnotationCommand;
            }
        }

        /// <summary>
        /// Gets the absolute timing command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand AbsoluteTimingCommand
        {
            get
            {
                if (this.absoluteTimingCommand == null)
                {
                    this.absoluteTimingCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ShowAbsoluteTiming = !this.VisualizationContainer.Navigator.ShowAbsoluteTiming,
                        () => this.IsDatasetLoaded());
                }

                return this.absoluteTimingCommand;
            }
        }

        /// <summary>
        /// Gets the timing relative to session start command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand TimingRelativeToSessionStartCommand
        {
            get
            {
                if (this.timingRelativeToSessionStartCommand == null)
                {
                    this.timingRelativeToSessionStartCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart = !this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart,
                        () => this.IsDatasetLoaded());
                }

                return this.timingRelativeToSessionStartCommand;
            }
        }

        /// <summary>
        /// Gets the timing relative to selection start command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand TimingRelativeToSelectionStartCommand
        {
            get
            {
                if (this.timingRelativeToSelectionStartCommand == null)
                {
                    this.timingRelativeToSelectionStartCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart = !this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart,
                        () => this.IsDatasetLoaded());
                }

                return this.timingRelativeToSelectionStartCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to session extents command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSessionExtentsCommand
        {
            get
            {
                if (this.zoomToSessionExtentsCommand == null)
                {
                    this.zoomToSessionExtentsCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ZoomToDataRange(),
                        () => this.IsDatasetLoaded());
                }

                return this.zoomToSessionExtentsCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSelectionCommand
        {
            get
            {
                if (this.zoomToSelectionCommand == null)
                {
                    this.zoomToSelectionCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ZoomToSelection(),
                        () => this.IsDatasetLoaded());
                }

                return this.zoomToSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the playback start command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlaybackStartCommand
        {
            get
            {
                if (this.playbackStartCommand == null)
                {
                    this.playbackStartCommand = new RelayCommand(
                        () => this.PlaybackStart(),
                        () => this.VisualizationContainer.Navigator.NavigationMode != Visualization.Navigation.NavigationMode.Live && this.IsDatasetLoaded());
                }

                return this.playbackStartCommand;
            }
        }

        /// <summary>
        /// Gets the playback stop command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlaybackStopCommand
        {
            get
            {
                if (this.playbackStopCommand == null)
                {
                    this.playbackStopCommand = new RelayCommand(
                        () => this.PlaybackStop(),
                        () => this.VisualizationContainer.Navigator.NavigationMode != Visualization.Navigation.NavigationMode.Live && this.IsDatasetLoaded());
                }

                return this.playbackStopCommand;
            }
        }

        /// <summary>
        /// Gets the delete visualization command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DeleteVisualizationCommand
        {
            get
            {
                if (this.deleteVisualizationCommand == null)
                {
                    this.deleteVisualizationCommand = new RelayCommand(
                        () =>
                        {
                            if (this.selectedVisualization is VisualizationPanel)
                            {
                                var visualizationPanel = this.selectedVisualization as VisualizationPanel;
                                visualizationPanel.Container.RemovePanel(visualizationPanel);
                            }
                            else if (this.selectedVisualization is VisualizationObject)
                            {
                                var visualizationObject = this.selectedVisualization as VisualizationObject;
                                visualizationObject.Panel.RemoveVisualizationObject(visualizationObject);
                            }
                        },
                        () => this.selectedVisualization is VisualizationPanel || this.selectedVisualization is VisualizationObject);
                }

                return this.deleteVisualizationCommand;
            }
        }

        /// <summary>
        /// Gets the selected visualzation changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedPropertyChangedEventArgs<object>> SelectedVisualizationChangedCommand
        {
            get
            {
                if (this.selectedVisualizationChangedCommand == null)
                {
                    this.selectedVisualizationChangedCommand = new RelayCommand<RoutedPropertyChangedEventArgs<object>>(
                        e =>
                        {
                            if (e.NewValue is VisualizationPanel)
                            {
                                this.VisualizationContainer.CurrentPanel = e.NewValue as VisualizationPanel;
                            }
                            else if (e.NewValue is VisualizationObject)
                            {
                                var visualizationObject = e.NewValue as VisualizationObject;
                                this.VisualizationContainer.CurrentPanel = visualizationObject.Panel;
                                visualizationObject.Panel.CurrentVisualizationObject = visualizationObject;
                            }

                            this.selectedVisualization = e.NewValue;
                            e.Handled = true;
                        });
                }

                return this.selectedVisualizationChangedCommand;
            }
        }

        /// <summary>
        /// Display the add annotation dialog.
        /// </summary>
        /// <param name="owner">The window that will own this dialog.</param>
        public void AddAnnotation(Window owner)
        {
            AddAnnotationWindow dlg = new AddAnnotationWindow(AnnotationSchemaRegistryViewModel.Default.Schemas);
            dlg.Owner = owner;
            dlg.StorePath = string.IsNullOrWhiteSpace(this.DatasetViewModel.FileName) ? Environment.CurrentDirectory : Path.GetDirectoryName(this.DatasetViewModel.FileName);
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // test for overwrite
                var path = Path.Combine(dlg.StorePath, dlg.StoreName + ".pas");
                if (File.Exists(path))
                {
                    var overwrite = MessageBox.Show(
                        owner,
                        $"The annotation file ({dlg.StoreName + ".pas"}) already exists in {dlg.StorePath}. Overwrite?",
                        "Overwrite Annotation File",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Cancel);
                    if (overwrite == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                // create a new panel for the annotations - don't make it the current panel
                var panel = new TimelineVisualizationPanel();
                panel.Configuration.Name = dlg.PartitionName;
                panel.Configuration.Height = 22;
                this.VisualizationContainer.AddPanel(panel);

                // create a new annotated event visualization object and add to the panel
                var annotations = new AnnotatedEventVisualizationObject();
                annotations.Configuration.Name = dlg.AnnotationName;
                panel.AddVisualizationObject(annotations);

                // create a new annotation definition and store
                var definition = new AnnotatedEventDefinition(dlg.StreamName);
                definition.AddSchema(dlg.AnnotationSchema);
                this.DatasetViewModel.CurrentSessionViewModel.CreateAnnotationPartition(dlg.StoreName, dlg.StorePath, definition);

                // open the stream for visualization (NOTE: if the selection extents were MinTime/MaxTime, no event will be created)
                annotations.OpenStream(new StreamBinding(dlg.StreamName, dlg.PartitionName, dlg.StoreName, dlg.StorePath, typeof(AnnotationSimpleReader)));
            }
        }

        /// <summary>
        /// Opens a previously persisted layout file.
        /// </summary>
        /// <param name="filename">Fully qualified path to layout file.</param>
        public void OpenLayout(string filename)
        {
            this.VisualizationContainer.Clear();
            this.VisualizationContainer = VisualizationContainer.Load(filename);

            // zoom into the current session
            var session = this.DatasetViewModel.CurrentSessionViewModel;
            var timeInterval = session?.OriginatingTimeInterval;
            timeInterval = timeInterval ?? new TimeInterval(DateTime.MinValue, DateTime.MaxValue);
            this.VisualizationContainer.ZoomToRange(timeInterval);

            // set the data range to the dataset
            this.VisualizationContainer.Navigator.DataRange.SetRange(this.DatasetViewModel.OriginatingTimeInterval);

            // update store bindings
            this.VisualizationContainer.UpdateStoreBindings(session == null ? new List<PartitionViewModel>() : session.PartitionViewModels.ToList());
        }

        /// <summary>
        /// Start playback of audio stream.
        /// </summary>
        public void PlaybackStart()
        {
            double speed = 1.0;
            double.TryParse(this.playbackSpeed, out speed);
            ReplayDescriptor replayDescriptor = null;

            if (this.audioPlaybackPipeline != null)
            {
                this.audioPlaybackPipeline.Dispose();
                this.audioPlaybackPipeline = null;
            }

            this.audioPlaybackPipeline = Pipeline.Create("AudioPlayer");
            replayDescriptor = new ReplayDescriptor(this.VisualizationContainer.Navigator.SelectionRange.StartTime, this.VisualizationContainer.Navigator.SelectionRange.EndTime, false, true, (float)(1.0 / speed));
            var partition = this.DatasetViewModel.CurrentSessionViewModel.PartitionViewModels.First();
            var importer = Store.Open(this.audioPlaybackPipeline, partition.StoreName, partition.StorePath);

            // Find first stream that contains audio.
            string audioBufferTypeName = typeof(AudioBuffer).AssemblyQualifiedName;
            var audioStream = importer.AvailableStreams.FirstOrDefault(s => s.TypeName == audioBufferTypeName);

            // Play the audio stream, if found.
            if (audioStream != null)
            {
                var audioPlayer = new AudioPlayer(this.audioPlaybackPipeline, new AudioPlayerConfiguration());
                var stream = importer.OpenStream<AudioBuffer>(audioStream.Name);
                stream.PipeTo(audioPlayer.In);
                this.audioPlaybackPipeline.RunAsync(replayDescriptor);
            }

            this.VisualizationContainer.Navigator.Play(speed);
        }

        /// <summary>
        /// Stop playback of audio stream.
        /// </summary>
        public void PlaybackStop()
        {
            this.VisualizationContainer.Navigator.StopPlaying();
            if (this.audioPlaybackPipeline != null)
            {
                this.audioPlaybackPipeline.Dispose();
                this.audioPlaybackPipeline = null;
            }
        }

        /// <summary>
        /// Asynchronously opens a previously persisted dataset.
        /// </summary>
        /// <param name="filename">Fully qualified path to dataset file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal async Task OpenDatasetAsync(string filename)
        {
            // Window that will be used to indicate that an open operation is in progress.
            // Progress notification and cancellation are not yet supported.
            var statusWindow = new LoadingDatasetWindow(filename, App.Current.MainWindow);

            // Wrap the open dataset operation in a task that will close the modal status
            // window once the open dataset operation has completed.
            async Task openDatasetTask()
            {
                try
                {
                    var fileInfo = new FileInfo(filename);
                    if (fileInfo.Extension == ".psi")
                    {
                        var name = fileInfo.Name.Substring(0, Path.GetFileNameWithoutExtension(filename).LastIndexOf('.'));
                        this.DatasetViewModel = await DatasetViewModel.CreateFromExistingStoreAsync(name, fileInfo.DirectoryName);
                    }
                    else
                    {
                        this.DatasetViewModel = await DatasetViewModel.LoadAsync(filename);
                    }

                    this.DatasetViewModels.Clear();
                    this.DatasetViewModels.Add(this.DatasetViewModel);

                    // We may have previously been in Live mode, so explicitly switch to Playback mode
                    this.VisualizationContainer.Navigator.NavigationMode = NavigationMode.Playback;

                    // set the data range to the dataset
                    this.VisualizationContainer.Navigator.DataRange.SetRange(this.DatasetViewModel.OriginatingTimeInterval);

                    // zoom into the current session
                    var timeInterval = this.DatasetViewModel.CurrentSessionViewModel?.OriginatingTimeInterval;
                    timeInterval = timeInterval ?? new TimeInterval(DateTime.MinValue, DateTime.MaxValue);
                    this.VisualizationContainer.ZoomToRange(timeInterval);
                }
                finally
                {
                    // closes the modal status window
                    statusWindow.Close();
                }
            }

            // start the open dataset task
            var task = openDatasetTask();

            try
            {
                // show the modal status window, which will be closed once the open dataset operation completes
                statusWindow.ShowDialog();
            }
            catch (InvalidOperationException)
            {
                // This indicates that the window has already been closed in the async task,
                // which means the operation has already completed, so just ignore and continue.
            }

            try
            {
                // await completion of the open dataset task
                await task;

                // show the new dataset UI
                this.TabControlIndex = (int)TabControlInicies.Datasets;
            }
            catch (Exception e)
            {
                // catch and display any exceptions that occurred during the open dataset operation
                var exception = e.InnerException ?? e;
                MessageBox.Show(exception.Message, exception.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the list of visualization stream commands for a given stream tree node.
        /// </summary>
        /// <param name="streamTreeNode">Stream tree node.</param>
        /// <returns>List of visualization stream commands.</returns>
        internal List<TypeKeyedActionCommand> GetVisualizeStreamCommands(IStreamTreeNode streamTreeNode)
        {
            List<TypeKeyedActionCommand> result = new List<TypeKeyedActionCommand>();
            if (streamTreeNode != null && streamTreeNode.TypeName != null)
            {
                // Get the Type from the loaded assemblies that matches the stream type
                var streamType = Type.GetType(streamTreeNode.TypeName, this.AssemblyResolver, null) ?? Type.GetType(streamTreeNode.TypeName.Split(',')[0], this.AssemblyResolver, null);
                if (streamType != null)
                {
                    // Get the list of commands
                    result.AddRange(this.typeVisualizerActions.Where(a => a.TypeKey.AssemblyQualifiedName == streamType.AssemblyQualifiedName));

                    // generate generic Plot Latency
                    var genericPlotLatency = typeof(PsiStudioContext).GetMethod("PlotLatency", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var plotLatencyAction = new Action<IStreamTreeNode>(s => genericPlotLatency.Invoke(this, new object[] { s, false }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Plot Latency", plotLatencyAction }) as TypeKeyedActionCommand);

                    // generate generic View Messages
                    var genericPlotMessages = typeof(PsiStudioContext).GetMethod("PlotMessages", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var plotMessagesAction = new Action<IStreamTreeNode>(s => genericPlotMessages.Invoke(this, new object[] { s }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Visualize Messages", plotMessagesAction }) as TypeKeyedActionCommand);

                    var zoomToStreamExtents = typeof(PsiStudioContext).GetMethod("ZoomToStreamExtents", BindingFlags.NonPublic | BindingFlags.Instance);
                    var zoomToStreamExtentsAction = new Action<IStreamTreeNode>(s => zoomToStreamExtents.Invoke(this, new object[] { s }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Zoom to Stream Extents", zoomToStreamExtentsAction }) as TypeKeyedActionCommand);
                }
            }

            return result;
        }

        private void InitVisualizeStreamCommands()
        {
            KnownSerializers.Default.Register<MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage<double>>(null);

            this.AddVisualizeStreamCommand<AnnotatedEvent>("Visualize", (s) => this.ShowAnnotations(s, false));
            this.AddVisualizeStreamCommand<double>("Plot", (s) => this.PlotDouble(s, false));
            this.AddVisualizeStreamCommand<double>("Plot in New Panel", (s) => this.PlotDouble(s, true));
            this.AddVisualizeStreamCommand<float>("Plot", (s) => this.PlotFloat(s, false));
            this.AddVisualizeStreamCommand<float>("Plot in New Panel", (s) => this.PlotFloat(s, true));
            this.AddVisualizeStreamCommand<TimeSpan>("Plot (as ms)", (s) => this.PlotTimeSpan(s, false));
            this.AddVisualizeStreamCommand<TimeSpan>("Plot (as ms) in New Panel", (s) => this.PlotTimeSpan(s, true));
            this.AddVisualizeStreamCommand<int>("Plot", (s) => this.PlotInt(s, false));
            this.AddVisualizeStreamCommand<int>("Plot in New Panel", (s) => this.PlotInt(s, true));
            this.AddVisualizeStreamCommand<bool>("Plot", (s) => this.PlotBool(s, false));
            this.AddVisualizeStreamCommand<bool>("Plot in New Panel", (s) => this.PlotBool(s, true));
            this.AddVisualizeStreamCommand<Shared<Image>>("Visualize", (s) => this.Show2D<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<Shared<EncodedImage>>("Visualize", (s) => this.Show2D<EncodedImageVisualizationObject, Shared<EncodedImage>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<IStreamingSpeechRecognitionResult>("Visualize", (s) => this.Show<SpeechRecognitionVisualizationObject, IStreamingSpeechRecognitionResult, SpeechRecognitionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<KinectBody>>("Visualize", (s) => this.Show3D<KinectBodies3DVisualizationObject, List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>("Visualize ", (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>("Visualize as Planar Direction", (s) => this.Show3D<ScatterPlanarDirectionVisualizationObject, List<CoordinateSystem>, ScatterPlanarDirectionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<CoordinateSystem>("Visualize", (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false, typeof(CoordinateSystemAdapter)));
            this.AddVisualizeStreamCommand<Point[]>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(PointArrayToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<List<Tuple<Point, string>>>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<Point2D?>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(NullablePoint2DToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<Point3D?>("Visualize", (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(NullablePoint3DAdapter)));
            this.AddVisualizeStreamCommand<List<Point3D>>("Visualize", (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(ListPoint3DAdapter)));
            this.AddVisualizeStreamCommand<byte[]>("Visualize as 3D Depth", this.ShowDepth3D);
            this.AddVisualizeStreamCommand<byte[]>("Visualize as 2D Depth", this.ShowDepth2D);
            this.AddVisualizeStreamCommand<AudioBuffer>("Visualize", this.PlotAudio);
            this.AddVisualizeStreamCommand<List<Tuple<System.Drawing.Rectangle, string>>>("Visualize", (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<System.Drawing.Rectangle>>("Visualize", (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false, typeof(ListRectangleAdapter)));
            this.AddVisualizeStreamCommand<List<KinectBody>>("Visualize", (s) => this.Show3D<KinectBodies3DVisualizationObject, List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<(CoordinateSystem, System.Windows.Media.Media3D.Rect3D)>>("Visualize", (s) => this.Show3D<ScatterRect3DVisualizationObject, List<(CoordinateSystem, System.Windows.Media.Media3D.Rect3D)>, ScatterRect3DVisualizationObjectConfiguration>(s, false));
        }

        private void AddVisualizeStreamCommand<TKey>(string displayName, Action<IStreamTreeNode> action)
        {
            this.typeVisualizerActions.Add(new TypeKeyedActionCommand<TKey, IStreamTreeNode>(displayName, action));
        }

        private void EnsureCurrentPanel<T>(bool newPanel)
            where T : VisualizationPanel, new()
        {
            if (newPanel || this.VisualizationContainer.CurrentPanel == null || (this.VisualizationContainer.CurrentPanel as T) == null)
            {
                var panel = new T();
                this.VisualizationContainer.AddPanel(panel);
            }
        }

        private void Show<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<TimelineVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private TVisObj Show<TPanel, TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType = null, Type summarizerType = null, params object[] summarizerArgs)
            where TPanel : VisualizationPanel, new()
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            var partition = streamTreeNode.Partition;
            var visObj = new TVisObj();
            visObj.Configuration.Name = streamTreeNode.StreamName;

            this.EnsureCurrentPanel<TPanel>(newPanel);
            this.VisualizationContainer.CurrentPanel.AddVisualizationObject(visObj);

            var streamBinding = new StreamBinding(
                streamTreeNode.StreamName, partition.Name, partition.StoreName, partition.StorePath, typeof(SimpleReader), streamAdapterType, summarizerType, summarizerArgs);
            visObj.OpenStream(streamBinding);

            return visObj;
        }

        private void Show2D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private void Show2D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private void Show3D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYZVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private void Show3D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYZVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private AnnotatedEventVisualizationObject ShowAnnotations(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            var partition = streamTreeNode.Partition;
            var visObj = new AnnotatedEventVisualizationObject();
            visObj.Configuration.Name = streamTreeNode.StreamName;

            this.EnsureCurrentPanel<TimelineVisualizationPanel>(newPanel);
            this.VisualizationContainer.CurrentPanel.AddVisualizationObject(visObj);

            var streamBinding = new StreamBinding(streamTreeNode.StreamName, partition.Name, partition.StoreName, partition.StorePath, typeof(AnnotationSimpleReader));
            visObj.OpenStream(streamBinding);
            this.VisualizationContainer.ZoomToRange(streamTreeNode.Partition.OriginatingTimeInterval);

            return visObj;
        }

        private void ShowDepth2D(IStreamTreeNode streamTreeNode)
        {
            this.Show2D<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration>(streamTreeNode, false, typeof(CompressedImageAdapter));
        }

        private void ShowDepth3D(IStreamTreeNode streamTreeNode)
        {
            this.Show3D<KinectDepth3DVisualizationObject, Shared<Image>, KinectDepth3DVisualizationObjectConfiguration>(
                streamTreeNode, false, typeof(CompressedImageAdapter));
        }

        private void PlotBool(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(BoolAdapter), typeof(RangeSummarizer));
        }

        private void PlotAudio(IStreamTreeNode streamTreeNode)
        {
            var visObj = this.Show<TimelineVisualizationPanel, AudioVisualizationObject, double, AudioVisualizationObjectConfiguration>(
                streamTreeNode, false, null, typeof(AudioSummarizer), 0);
            visObj.Configuration.Name = streamTreeNode.StreamName;
            this.VisualizationContainer.ZoomToRange(streamTreeNode.Partition.OriginatingTimeInterval);
        }

        private void PlotDouble(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, null, typeof(RangeSummarizer));
        }

        private void PlotFloat(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(FloatAdapter), typeof(RangeSummarizer));
        }

        private void PlotInt(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, newPanel, typeof(IntAdapter), typeof(RangeSummarizer));
        }

        private void PlotLatency<TData>(IStreamTreeNode streamTreeNode, bool newPanel = false)
        {
            var visObj = this.Show<TimelineVisualizationPanel, TimeIntervalVisualizationObject, Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(LatencyAdapter<TData>), typeof(TimeIntervalSummarizer));
            visObj.Configuration.Color = System.Drawing.Color.Red;
            visObj.Configuration.Name = streamTreeNode.StreamName;
        }

        private void PlotMessages<TData>(IStreamTreeNode streamTreeNode)
        {
            var visObj = this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, false, typeof(MessageAdapter<TData>), typeof(RangeSummarizer));
            visObj.Configuration.MarkerSize = 4;
            visObj.Configuration.MarkerStyle = Visualization.Common.MarkerStyle.Circle;
            visObj.Configuration.Name = streamTreeNode.StreamName;
        }

        private void PlotTimeSpan(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, newPanel, typeof(TimeSpanAdapter), typeof(RangeSummarizer));
        }

        private void ZoomToStreamExtents(IStreamTreeNode streamTreeNode)
        {
            if (streamTreeNode.FirstMessageOriginatingTime.HasValue && streamTreeNode.LastMessageOriginatingTime.HasValue)
            {
                this.VisualizationContainer.Navigator.Zoom(streamTreeNode.FirstMessageOriginatingTime.Value, streamTreeNode.LastMessageOriginatingTime.Value);
            }
            else
            {
                this.VisualizationContainer.Navigator.ZoomToDataRange();
            }
        }

        private Assembly AssemblyResolver(AssemblyName assemblyName)
        {
            // Attempt to match by full name first
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().FullName == assemblyName.FullName);
            if (assembly != null)
            {
                return assembly;
            }

            // Otherwise try to match by simple name without version, culture or key
            assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));
            if (assembly != null)
            {
                return assembly;
            }

            return null;
        }

        private bool IsDatasetLoaded()
        {
            return this.DatasetViewModel?.CurrentSessionViewModel?.PartitionViewModels.FirstOrDefault() != null;
        }
    }
}
