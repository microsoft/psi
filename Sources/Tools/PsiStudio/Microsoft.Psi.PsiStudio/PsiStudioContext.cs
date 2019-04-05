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
    using System.Windows.Threading;
    using GalaSoft.MvvmLight.CommandWpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.PsiStudio.ViewModels;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Annotations;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Microsoft.Win32;

    /// <summary>
    /// Data context for PsiStudio.
    /// </summary>
    public class PsiStudioContext : ObservableObject
    {
        private readonly string newLayoutName = "<New Layout>";

        private VisualizationContainer visualizationContainer;
        private DatasetViewModel datasetViewModel;
        private DispatcherTimer liveStatusTimer = null;

        private RelayCommand closedCommand;
        private RelayCommand openStoreCommand;
        private RelayCommand openDatasetCommand;
        private RelayCommand saveDatasetCommand;
        private RelayCommand saveLayoutCommand;
        private RelayCommand saveLayoutAsCommand;
        private RelayCommand insertTimelinePanelCommand;
        private RelayCommand insert2DPanelCommand;
        private RelayCommand insert3DPanelCommand;
        ////private RelayCommand insertAnnotationCommand;
        private RelayCommand absoluteTimingCommand;
        private RelayCommand timingRelativeToSessionStartCommand;
        private RelayCommand timingRelativeToSelectionStartCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand moveToSelectionStartCommand;
        private RelayCommand playPauseCommand;
        private RelayCommand togglePlayRepeatCommand;
        private RelayCommand moveToSelectionEndCommand;
        private RelayCommand increasePlaySpeedCommand;
        private RelayCommand decreasePlaySpeedCommand;
        private RelayCommand toggleLiveModeCommand;
        ////private RelayCommand showSettingsWindowComand;
        private RelayCommand exitCommand;

        private RelayCommand<RoutedPropertyChangedEventArgs<object>> selectedVisualizationChangedCommand;
        private RelayCommand<RoutedPropertyChangedEventArgs<object>> selectedDatasetChangedCommand;
        private RelayCommand<string> treeSelectedCommand;

        private List<TypeKeyedActionCommand> typeVisualizerActions = new List<TypeKeyedActionCommand>();

        private List<LayoutInfo> availableLayouts = new List<LayoutInfo>();
        private LayoutInfo currentLayout = null;

        /// <summary>
        /// The currently selected node in the Datasets tree view
        /// </summary>
        private object selectedDatasetObject;

        /// <summary>
        /// The currently selected node in the Visualizations tree view
        /// </summary>
        private object selectedVisualization;

        /// <summary>
        /// The object whose properties are currently being displayed in the Properties view.
        /// This is always either the selectedDatasetObject or the selectedVisualization
        /// </summary>
        private object selectedPropertiesObject;

        static PsiStudioContext()
        {
            PsiStudioContext.Instance = new PsiStudioContext();
        }

        private PsiStudioContext()
        {
            this.InitVisualizeStreamCommands();

            // Load the application settings
            this.AppSettings = new PsiSettings();

            var booleanSchema = new AnnotationSchema("Boolean");
            booleanSchema.AddSchemaValue(null, System.Drawing.Color.Gray);
            booleanSchema.AddSchemaValue("false", System.Drawing.Color.Red);
            booleanSchema.AddSchemaValue("true", System.Drawing.Color.Green);
            AnnotationSchemaRegistry.Default.Register(booleanSchema);

            this.DatasetViewModel = new DatasetViewModel();
            this.DatasetViewModels = new ObservableCollection<DatasetViewModel> { this.datasetViewModel };

            // Load the available layouts
            this.UpdateLayoutList();

            // Set the current layout if it's in the available layouts, otherwise make "new layout" the current layout
            LayoutInfo lastLayout = this.AvailableLayouts.FirstOrDefault(l => l.Name == this.AppSettings.CurrentLayoutName);
            this.currentLayout = lastLayout ?? this.AvailableLayouts[0];

            // Periodically check if there's any live partitions in the dataset
            this.liveStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Normal, new EventHandler(this.OnLiveStatusTimer), Application.Current.Dispatcher);
            this.liveStatusTimer.Start();
        }

        /// <summary>
        /// Gets the name of this application for use when constructing paths etc
        /// </summary>
        public static string ApplicationName => "PsiStudio";

        /// <summary>
        /// Gets the path to the PsiStudio data in the MyDocuments folder
        /// </summary>
        public static string PsiStudioDocumentsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ApplicationName);

        /// <summary>
        /// Gets the PsiStudioContext singleton.
        /// </summary>
        public static PsiStudioContext Instance { get; private set; }

        /// <summary>
        /// Gets the settings for the application
        /// </summary>
        public PsiSettings AppSettings { get; private set; }

        /// <summary>
        /// Gets or sets the thing
        /// </summary>
        public List<LayoutInfo> AvailableLayouts
        {
            get
            {
                return this.availableLayouts;
            }

            set
            {
                this.availableLayouts = value;
            }
        }

        /// <summary>
        /// Gets or sets the current layout
        /// </summary>
        public LayoutInfo CurrentLayout
        {
            get
            {
                return this.currentLayout;
            }

            set
            {
                this.RaisePropertyChanging(nameof(this.CurrentLayout));

                this.currentLayout = value;
                if (this.currentLayout == null || this.currentLayout.Name == this.newLayoutName)
                {
                    this.AppSettings.CurrentLayoutName = null;
                }
                else
                {
                    this.AppSettings.CurrentLayoutName = this.currentLayout.Name;
                }

                if (this.currentLayout != null)
                {
                    this.OpenLayout(this.currentLayout);
                }

                this.RaisePropertyChanged(nameof(this.CurrentLayout));
            }
        }

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
            set
            {
                this.Set(nameof(this.VisualizationContainer), ref this.visualizationContainer, value);
                this.visualizationContainer.Navigator.PropertyChanged += this.Navigator_PropertyChanged;
            }
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
                            this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);

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
                            OpenFileDialog dlg = new OpenFileDialog
                            {
                                DefaultExt = ".psi",
                                Filter = "Psi Store (.psi)|*.psi"
                            };

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
                            if (this.CurrentLayout.Name == this.newLayoutName)
                            {
                                this.SaveLayoutAs();
                            }
                            else
                            {
                                this.VisualizationContainer.Save(this.CurrentLayout.Path);
                            }
                        });
                }

                return this.saveLayoutCommand;
            }
        }

        /// <summary>
        /// Gets the save layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveLayoutAsCommand
        {
            get
            {
                if (this.saveLayoutAsCommand == null)
                {
                    this.saveLayoutAsCommand = new RelayCommand(
                        () =>
                        {
                            this.SaveLayoutAs();
                        });
                }

                return this.saveLayoutAsCommand;
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

        /*/// <summary>
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
        }*/

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
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.zoomToSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the move to selection start command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveToSelectionStartCommand
        {
            get
            {
                if (this.moveToSelectionStartCommand == null)
                {
                    this.moveToSelectionStartCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.MoveToSelectionStart(),
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.moveToSelectionStartCommand;
            }
        }

        /// <summary>
        /// Gets the play/pause command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlayPauseCommand
        {
            get
            {
                if (this.playPauseCommand == null)
                {
                    this.playPauseCommand = new RelayCommand(
                        () => this.PlayOrPause(),
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.playPauseCommand;
            }
        }

        /// <summary>
        /// Gets the toggle play repeat command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand TogglePlayRepeatCommand
        {
            get
            {
                if (this.togglePlayRepeatCommand == null)
                {
                    this.togglePlayRepeatCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.RepeatPlayback = !this.VisualizationContainer.Navigator.RepeatPlayback,
                        () => this.IsDatasetLoaded());
                }

                return this.togglePlayRepeatCommand;
            }
        }

        /// <summary>
        /// Gets the move to selection end command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveToSelectionEndCommand
        {
            get
            {
                if (this.moveToSelectionEndCommand == null)
                {
                    this.moveToSelectionEndCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.MoveToSelectionEnd(),
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.moveToSelectionEndCommand;
            }
        }

        /// <summary>
        /// Gets the increase play speed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand IncreasePlaySpeedCommand
        {
            get
            {
                if (this.increasePlaySpeedCommand == null)
                {
                    this.increasePlaySpeedCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.PlaySpeed++,
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.increasePlaySpeedCommand;
            }
        }

        /// <summary>
        /// Gets the decrease play speed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DecreasePlaySpeedCommand
        {
            get
            {
                if (this.decreasePlaySpeedCommand == null)
                {
                    this.decreasePlaySpeedCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.PlaySpeed--,
                        () => this.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.decreasePlaySpeedCommand;
            }
        }

        /// <summary>
        /// Gets the toggle live mode command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleLiveModeCommand
        {
            get
            {
                if (this.toggleLiveModeCommand == null)
                {
                    this.toggleLiveModeCommand = new RelayCommand(
                        () => this.ToggleLiveMode(),
                        () => this.IsDatasetLoaded() && this.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == true);
                }

                return this.toggleLiveModeCommand;
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
                            this.SelectedPropertiesObject = e.NewValue;
                            e.Handled = true;
                        });
                }

                return this.selectedVisualizationChangedCommand;
            }
        }

        /// <summary>
        /// Gets the selected dataset changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedPropertyChangedEventArgs<object>> SelectedDatasetChangedCommand
        {
            get
            {
                if (this.selectedDatasetChangedCommand == null)
                {
                    this.selectedDatasetChangedCommand = new RelayCommand<RoutedPropertyChangedEventArgs<object>>(
                        e =>
                        {
                            this.selectedDatasetObject = e.NewValue;
                            this.SelectedPropertiesObject = e.NewValue;
                            e.Handled = true;
                        });
                }

                return this.selectedDatasetChangedCommand;
            }
        }

        /// <summary>
        /// Gets the command that executes after the user clicks on either the datasets or the visualizations tree views
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<string> TreeSelectedCommand
        {
            get
            {
                if (this.treeSelectedCommand == null)
                {
                    this.treeSelectedCommand = new RelayCommand<string>(
                        e =>
                        {
                            // Update the properties view to show the properties
                            // of the selected item in the appropriate tree view
                            if (e == "VisualizationTreeView")
                            {
                                this.SelectedPropertiesObject = this.selectedVisualization;
                            }
                            else
                            {
                                this.SelectedPropertiesObject = this.selectedDatasetObject;
                            }
                        });
                }

                return this.treeSelectedCommand;
            }
        }

        /*/// <summary>
        /// Gets the show settings window command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ShowSettingsWindowComand
        {
            get
            {
                if (this.showSettingsWindowComand == null)
                {
                    this.showSettingsWindowComand = new RelayCommand(() => this.ShowSettingsWindow());
                }

                return this.showSettingsWindowComand;
            }
        }*/

        /// <summary>
        /// Gets the exit command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExitCommand
        {
            get
            {
                if (this.exitCommand == null)
                {
                    this.exitCommand = new RelayCommand(() => Application.Current.Shutdown());
                }

                return this.exitCommand;
            }
        }

        /// <summary>
        /// Gets or sets the current object shown in the properties window
        /// </summary>
        public object SelectedPropertiesObject
        {
            get => this.selectedPropertiesObject;
            set => this.Set(nameof(this.SelectedPropertiesObject), ref this.selectedPropertiesObject, value);
        }

        /// <summary>
        /// Gets the  image to display on the Play/Pause button
        /// </summary>
        public string PlayPauseButtonImage => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Icons\stop_x4.png" : @"Icons\play_x4.png";

        /// <summary>
        /// Gets the  image to display on the Play/Pause button
        /// </summary>
        public string PlayPauseButtonToolTip => this.VisualizationContainer.Navigator.IsCursorModePlayback ? @"Stop" : @"Play";

        private string LayoutsDirectory => Path.Combine(PsiStudioDocumentsPath, "Layouts");

        /*/// <summary>
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
        }*/

        /// <summary>
        /// Opens a previously persisted layout file.
        /// </summary>
        /// <param name="layoutInfo">The layout to open</param>
        public void OpenLayout(LayoutInfo layoutInfo)
        {
            // Clear the current layout
            this.VisualizationContainer.Clear();

            // Load the new layout
            if (!string.IsNullOrWhiteSpace(layoutInfo.Path))
            {
                this.VisualizationContainer = VisualizationContainer.Load(layoutInfo.Path);

                // zoom into the current session if there is one
                SessionViewModel sessionViewModel = this.DatasetViewModel.CurrentSessionViewModel;
                if (sessionViewModel != null)
                {
                    // Zoom to the current session extents
                    this.VisualizationContainer.ZoomToRange(sessionViewModel.OriginatingTimeInterval);

                    // set the data range to the dataset
                    this.VisualizationContainer.Navigator.DataRange.SetRange(this.DatasetViewModel.OriginatingTimeInterval);
                }

                // update store bindings
                this.VisualizationContainer.UpdateStreamBindings(sessionViewModel?.Session);
            }
        }

        /// <summary>
        /// Gets the message type for a stream
        /// </summary>
        /// <param name="streamTreeNode">The stream tree node</param>
        /// <returns>The type of messages in the stream</returns>
        public Type GetStreamType(IStreamTreeNode streamTreeNode)
        {
            return Type.GetType(streamTreeNode.TypeName, this.AssemblyResolver, null) ?? Type.GetType(streamTreeNode.TypeName.Split(',')[0], this.AssemblyResolver, null);
        }

        /// <summary>
        /// Asynchronously opens a previously persisted dataset.
        /// </summary>
        /// <param name="filename">Fully qualified path to dataset file.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        internal async Task OpenDatasetAsync(string filename)
        {
            // Window that will be used to indicate that an open operation is in progress.
            // Progress notification and cancellation are not yet fully supported.
            var statusWindow = new LoadingDatasetWindow(App.Current.MainWindow, filename);

            // progress reporter for the load dataset task
            var progress = new Progress<(string s, double p)>(t =>
            {
                statusWindow.Status = t.s;
                if (t.p == 1.0)
                {
                    // close the status window when the task reports completion
                    statusWindow.Close();
                }
            });

            // start the load dataset task
            var loadDatasetTask = this.LoadDatasetOrStoreAsync(filename, progress);

            try
            {
                // show the modal status window, which will be closed once the load dataset operation completes
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
                await loadDatasetTask;

                this.DatasetViewModels.Clear();
                this.DatasetViewModels.Add(this.DatasetViewModel);

                // Check for live partitions
                this.DatasetViewModel.UpdateLivePartitionStatuses();

                // The first session (if there is one) will already have been selected in the dataset, so visualize it.
                this.DatasetViewModel.VisualizeSession(this.DatasetViewModel.CurrentSessionViewModel);
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
                var streamType = this.GetStreamType(streamTreeNode);
                if (streamType != null)
                {
                    // Get the list of commands
                    result.AddRange(this.typeVisualizerActions.Where(a => a.TypeKey.AssemblyQualifiedName == streamType.AssemblyQualifiedName));

                    // generate generic Visualize Latency
                    var genericPlotLatency = typeof(PsiStudioContext).GetMethod(nameof(this.VisualizeLatency), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var visualizeLatencyAction = new Action<IStreamTreeNode>(s => genericPlotLatency.Invoke(this, new object[] { s, false }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { ContextMenuName.VisualizeLatency, visualizeLatencyAction }) as TypeKeyedActionCommand);

                    // generate generic Visulize Latency in New Panel
                    var visualizeLatencyInNewPanelAction = new Action<IStreamTreeNode>(s => genericPlotLatency.Invoke(this, new object[] { s, true }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { ContextMenuName.VisualizeLatencyInNewPanel, visualizeLatencyInNewPanelAction }) as TypeKeyedActionCommand);

                    // generate generic Visualize Messages
                    var genericPlotMessages = typeof(PsiStudioContext).GetMethod(nameof(this.VisualizeMessages), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var visualizeMessagesAction = new Action<IStreamTreeNode>(s => genericPlotMessages.Invoke(this, new object[] { s, false }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { ContextMenuName.VisualizeMessages, visualizeMessagesAction }) as TypeKeyedActionCommand);

                    // generate generic Visualize Messages in New Panel
                    var visualizeMessagesInNewPanelAction = new Action<IStreamTreeNode>(s => genericPlotMessages.Invoke(this, new object[] { s, true }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { ContextMenuName.VisualizeMessagesInNewPanel, visualizeMessagesInNewPanelAction }) as TypeKeyedActionCommand);

                    var zoomToStreamExtents = typeof(PsiStudioContext).GetMethod("ZoomToStreamExtents", BindingFlags.NonPublic | BindingFlags.Instance);
                    var zoomToStreamExtentsAction = new Action<IStreamTreeNode>(s => zoomToStreamExtents.Invoke(this, new object[] { s }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { ContextMenuName.ZoomToStreamExtents, zoomToStreamExtentsAction }) as TypeKeyedActionCommand);
                }
            }

            return result;
        }

        /// <summary>
        /// Pause or resume playback of streams.
        /// </summary>
        private void PlayOrPause()
        {
            switch (this.VisualizationContainer.Navigator.CursorMode)
            {
                case CursorMode.Playback:
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
                    break;
                case CursorMode.Manual:
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Playback);
                    break;
            }
        }

        /// <summary>
        /// Toggle into or out of live mode
        /// </summary>
        private void ToggleLiveMode()
        {
            // Only enter live mode if the current session contains live partitions
            if (this.DatasetViewModel.CurrentSessionViewModel.ContainsLivePartitions && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live)
            {
                this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Live);
            }
            else
            {
                this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
            }
        }

        private async Task LoadDatasetOrStoreAsync(string filename, IProgress<(string, double)> progress = null)
        {
            try
            {
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Extension == ".psi")
                {
                    var name = fileInfo.Name.Substring(0, Path.GetFileNameWithoutExtension(filename).LastIndexOf('.'));

                    progress?.Report(("Opening store...", 0));

                    // If the store is not closed, and nobody's holding a reference to it, assume it was closed improperly and needs to be repaired.
                    if (!Store.IsClosed(name, fileInfo.DirectoryName) && !StoreReader.IsStoreLive(name, fileInfo.DirectoryName))
                    {
                        progress?.Report(("Repairing store...", 0.5));
                        await Task.Run(() => Store.Repair(name, fileInfo.DirectoryName));
                    }

                    progress?.Report(("Loading store...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.CreateFromExistingStoreAsync(name, fileInfo.DirectoryName);
                }
                else
                {
                    progress?.Report(("Loading dataset...", 0.5));
                    this.DatasetViewModel = await DatasetViewModel.LoadAsync(filename);
                }
            }
            finally
            {
                // report completion
                progress?.Report(("Done", 1.0));
            }
        }

        /*/// <summary>
        /// Display the settings dialog.
        /// </summary>
        private void ShowSettingsWindow()
        {
            SettingsWindow dlg = new SettingsWindow();
            dlg.Owner = App.Current.MainWindow;
            dlg.LayoutsDirectory = this.AppSettings.LayoutsDirectory;
            if (dlg.ShowDialog() == true)
            {
                this.AppSettings.LayoutsDirectory = dlg.LayoutsDirectory;
                this.UpdateLayoutList();

                // Make "new layout" the current layout
                this.CurrentLayout = this.AvailableLayouts[0];
            }
        }*/

        private void SaveLayoutAs()
        {
            LayoutNameWindow dlg = new LayoutNameWindow();

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string fileName = Path.Combine(this.LayoutsDirectory, dlg.LayoutName);

                // Save the layout
                this.VisualizationContainer.Save(fileName);

                // Add this layout to the list of available layouts and make it current
                this.RaisePropertyChanging(nameof(this.AvailableLayouts));
                this.RaisePropertyChanging(nameof(this.CurrentLayout));
                LayoutInfo newLayout = this.AddLayoutToAvailableLayouts(fileName);
                this.CurrentLayout = newLayout;
                this.RaisePropertyChanged(nameof(this.AvailableLayouts));
                this.RaisePropertyChanged(nameof(this.CurrentLayout));
            }
        }

        private void UpdateLayoutList()
        {
            this.RaisePropertyChanging(nameof(this.AvailableLayouts));

            this.availableLayouts = new List<LayoutInfo>();
            this.availableLayouts.Add(new LayoutInfo(this.newLayoutName, null));

            // Create the layouts directory if it doesn't already exist
            DirectoryInfo directoryInfo = new DirectoryInfo(this.LayoutsDirectory);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(this.LayoutsDirectory);
            }

            // Find all the layout files and add them to the the list of available layouts
            FileInfo[] files = directoryInfo.GetFiles("*.plo");
            foreach (FileInfo fileInfo in files)
            {
                this.AddLayoutToAvailableLayouts(fileInfo.FullName);
            }

            this.RaisePropertyChanged(nameof(this.AvailableLayouts));
        }

        private LayoutInfo AddLayoutToAvailableLayouts(string fileName)
        {
            LayoutInfo layoutInfo = new LayoutInfo(Path.GetFileNameWithoutExtension(fileName), fileName);
            this.availableLayouts.Add(layoutInfo);
            return layoutInfo;
        }

        private void OnLiveStatusTimer(object sender, EventArgs e)
        {
            if (this.DatasetViewModel != null)
            {
                // Update the list of live partitions
                this.DatasetViewModel.UpdateLivePartitionStatuses();

                // If the're no longer any live partitions, exit live mode
                if ((this.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == false) && (this.VisualizationContainer.Navigator.CursorMode == CursorMode.Live))
                {
                    this.VisualizationContainer.Navigator.SetCursorMode(CursorMode.Manual);
                }
            }
        }

        private void InitVisualizeStreamCommands()
        {
            KnownSerializers.Default.Register<MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage<double>>(null);

            ////this.AddVisualizeStreamCommand<AnnotatedEvent>(ContextMenuName.Visualize, (s) => this.ShowAnnotations(s, false));
            this.AddVisualizeStreamCommand<double>(ContextMenuName.Plot, (s) => this.PlotDouble(s, false));
            this.AddVisualizeStreamCommand<double>(ContextMenuName.PlotInNewPanel, (s) => this.PlotDouble(s, true));
            this.AddVisualizeStreamCommand<float>(ContextMenuName.Plot, (s) => this.PlotFloat(s, false));
            this.AddVisualizeStreamCommand<float>(ContextMenuName.PlotInNewPanel, (s) => this.PlotFloat(s, true));
            this.AddVisualizeStreamCommand<TimeSpan>(ContextMenuName.PlotAsMilliseconds, (s) => this.PlotTimeSpan(s, false));
            this.AddVisualizeStreamCommand<TimeSpan>(ContextMenuName.PlotAsMillisecondsInNewPanel, (s) => this.PlotTimeSpan(s, true));
            this.AddVisualizeStreamCommand<int>(ContextMenuName.Plot, (s) => this.PlotInt(s, false));
            this.AddVisualizeStreamCommand<int>(ContextMenuName.PlotInNewPanel, (s) => this.PlotInt(s, true));
            this.AddVisualizeStreamCommand<bool>(ContextMenuName.Plot, (s) => this.PlotBool(s, false));
            this.AddVisualizeStreamCommand<bool>(ContextMenuName.PlotInNewPanel, (s) => this.PlotBool(s, true));
            this.AddVisualizeStreamCommand<Shared<Image>>(ContextMenuName.Visualize, (s) => this.Show2D<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<Shared<EncodedImage>>(ContextMenuName.Visualize, (s) => this.Show2D<EncodedImageVisualizationObject, Shared<EncodedImage>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<IStreamingSpeechRecognitionResult>(ContextMenuName.Visualize, (s) => this.Show<SpeechRecognitionVisualizationObject, IStreamingSpeechRecognitionResult, SpeechRecognitionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>(ContextMenuName.Visualize, (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>(ContextMenuName.VisualizeAsPlanarDirection, (s) => this.Show3D<ScatterPlanarDirectionVisualizationObject, List<CoordinateSystem>, ScatterPlanarDirectionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<CoordinateSystem>(ContextMenuName.Visualize, (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false, typeof(CoordinateSystemAdapter)));
            this.AddVisualizeStreamCommand<Point[]>(ContextMenuName.Visualize, (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(PointArrayToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<List<Tuple<Point, string>>>(ContextMenuName.Visualize, (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<Point2D?>(ContextMenuName.Visualize, (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(NullablePoint2DToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<Point3D?>(ContextMenuName.Visualize, (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(NullablePoint3DAdapter)));
            this.AddVisualizeStreamCommand<List<Point3D>>(ContextMenuName.Visualize, (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(ListPoint3DAdapter)));
            this.AddVisualizeStreamCommand<byte[]>(ContextMenuName.VisualizeAs3DDepth, this.ShowDepth3D);
            this.AddVisualizeStreamCommand<byte[]>(ContextMenuName.VisualizeAs2DDepth, this.ShowDepth2D);
            this.AddVisualizeStreamCommand<AudioBuffer>(ContextMenuName.Visualize, this.PlotAudio);
            this.AddVisualizeStreamCommand<List<Tuple<System.Drawing.Rectangle, string>>>(ContextMenuName.Visualize, (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<System.Drawing.Rectangle>>(ContextMenuName.Visualize, (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false, typeof(ListRectangleAdapter)));
            this.AddVisualizeStreamCommand<List<KinectBody>>(ContextMenuName.Visualize, (s) => this.Show3D<KinectBodies3DVisualizationObject, List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<(CoordinateSystem, System.Windows.Media.Media3D.Rect3D)>>(ContextMenuName.Visualize, (s) => this.Show3D<ScatterRect3DVisualizationObject, List<(CoordinateSystem, System.Windows.Media.Media3D.Rect3D)>, ScatterRect3DVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<TimeIntervalHistory>(ContextMenuName.Visualize, (s) => this.Show<TimeIntervalHistoryVisualizationObject, TimeIntervalHistory, TimeIntervalHistoryVisualizationObjectConfiguration>(s, false));
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

        private void Show<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType = null)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<TimelineVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private TVisObj Show<TPanel, TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType = null, Type summarizerType = null, params object[] summarizerArgs)
            where TPanel : VisualizationPanel, new()
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            var visObj = new TVisObj();
            visObj.Configuration.Name = streamTreeNode.StreamName;

            this.EnsureCurrentPanel<TPanel>(newPanel);
            this.VisualizationContainer.CurrentPanel.AddVisualizationObject(visObj);

            visObj.Configuration.StreamBinding = new StreamBinding(streamTreeNode.StreamName, streamTreeNode.Partition.Name, typeof(SimpleReader), streamAdapterType, summarizerType, summarizerArgs);
            visObj.UpdateStreamBinding(this.DatasetViewModel.CurrentSessionViewModel.Session);

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

        /*private AnnotatedEventVisualizationObject ShowAnnotations(IStreamTreeNode streamTreeNode, bool newPanel)
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
        }*/

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

        private void VisualizeLatency<TData>(IStreamTreeNode streamTreeNode, bool newPanel = false)
        {
            var visObj = this.Show<TimelineVisualizationPanel, TimeIntervalVisualizationObject, Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(LatencyAdapter<TData>), typeof(TimeIntervalSummarizer));
            visObj.Configuration.Name = streamTreeNode.StreamName + " (Latency)";
        }

        private void VisualizeMessages<TData>(IStreamTreeNode streamTreeNode, bool newPanel = false)
        {
            var visObj = this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(MessageAdapter<TData>), typeof(RangeSummarizer));
            visObj.Configuration.MarkerSize = 4;
            visObj.Configuration.MarkerStyle = Visualization.Common.MarkerStyle.Circle;
            visObj.Configuration.Name = streamTreeNode.StreamName + " (Messages)";
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

        private void Navigator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.visualizationContainer.Navigator.IsCursorModePlayback))
            {
                this.RaisePropertyChanged(nameof(this.PlayPauseButtonImage));
                this.RaisePropertyChanged(nameof(this.PlayPauseButtonToolTip));
            }
        }
    }
}
