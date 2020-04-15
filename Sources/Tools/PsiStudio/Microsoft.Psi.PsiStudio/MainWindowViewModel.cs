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
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.PsiStudio.Windows;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Microsoft.Win32;

    /// <summary>
    /// Represents the view model for the main window of the psi studio application.
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        private readonly string newLayoutName = "<New Layout>";
        private List<LayoutInfo> availableLayouts = new List<LayoutInfo>();
        private LayoutInfo currentLayout = null;

        /// <summary>
        /// The currently selected node in the Datasets tree view.
        /// </summary>
        private object selectedDatasetObject;

        /// <summary>
        /// The currently selected node in the Visualizations tree view.
        /// </summary>
        private object selectedVisualization;

        /// <summary>
        /// Flag indicating if the visualizer map has initialized itself yet.
        /// </summary>
        private bool isInitialized = false;

        /// <summary>
        /// The object whose properties are currently being displayed in the Properties view.
        /// This is always either the selectedDatasetObject or the selectedVisualization.
        /// </summary>
        private object selectedPropertiesObject;

        private RelayCommand playPauseCommand;
        private RelayCommand toggleCursorFollowsMouseComand;
        private RelayCommand openStoreCommand;
        private RelayCommand openDatasetCommand;
        private RelayCommand saveDatasetCommand;
        private RelayCommand insertTimelinePanelCommand;
        private RelayCommand insert1CellInstantPanelCommand;
        private RelayCommand insert2CellInstantPanelCommand;
        private RelayCommand insert3CellInstantPanelCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand moveToSelectionStartCommand;
        private RelayCommand togglePlayRepeatCommand;
        private RelayCommand moveToSelectionEndCommand;
        private RelayCommand increasePlaySpeedCommand;
        private RelayCommand decreasePlaySpeedCommand;
        private RelayCommand toggleLiveModeCommand;
        private RelayCommand saveLayoutCommand;
        private RelayCommand saveLayoutAsCommand;
        private RelayCommand expandDatasetsTreeCommand;
        private RelayCommand collapseDatasetsTreeCommand;
        private RelayCommand expandVisualizationsTreeCommand;
        private RelayCommand collapseVisualizationsTreeCommand;
        private RelayCommand synchronizeTreesCommand;
        private RelayCommand<RoutedPropertyChangedEventArgs<object>> selectedVisualizationChangedCommand;
        private RelayCommand<RoutedPropertyChangedEventArgs<object>> selectedDatasetChangedCommand;
        private RelayCommand<string> treeSelectedCommand;
        private RelayCommand closedCommand;
        private RelayCommand exitCommand;

        ////private RelayCommand insertAnnotationCommand;
        ////private RelayCommand showSettingsWindowComand;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            // Create and load the settings
            this.AppSettings = PsiStudioSettings.Load(Path.Combine(PsiStudioDocumentsPath, "PsiStudioSettings.xml"));

            // Wait until the main window is visible before initializing the visualizer
            // map as we may need to display some message boxes during this process.
            Application.Current.MainWindow.ContentRendered += this.MainWindow_Activated;

            // Listen for property change events from the visualization context (specifically when the visualization container changes)
            VisualizationContext.Instance.PropertyChanged += this.VisualizationContext_PropertyChanged;

            // Load the available layouts
            this.UpdateLayoutList();

            // Set the current layout if it's in the available layouts, otherwise make "new layout" the current layout
            LayoutInfo lastLayout = this.AvailableLayouts.FirstOrDefault(l => l.Name == this.AppSettings.CurrentLayoutName);
            this.currentLayout = lastLayout ?? this.AvailableLayouts[0];
        }

        /// <summary>
        /// Gets the name of this application for use when constructing paths etc.
        /// </summary>
        public static string ApplicationName => "PsiStudio";

        /// <summary>
        /// Gets the path to the PsiStudio data in the MyDocuments folder.
        /// </summary>
        public static string PsiStudioDocumentsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ApplicationName);

        /// <summary>
        /// Gets the application settings.
        /// </summary>
        public PsiStudioSettings AppSettings { get; private set; }

        /// <summary>
        /// Gets the visualization container.
        /// </summary>
        public VisualizationContainer VisualizationContainer => VisualizationContext.Instance.VisualizationContainer;

        /// <summary>
        /// Gets the collection of dataset view models.
        /// </summary>
        public ObservableCollection<DatasetViewModel> DatasetViewModels => VisualizationContext.Instance.DatasetViewModels;

        /// <summary>
        /// Gets or sets the current object shown in the properties window.
        /// </summary>
        public object SelectedPropertiesObject
        {
            get => this.selectedPropertiesObject;
            set => this.Set(nameof(this.SelectedPropertiesObject), ref this.selectedPropertiesObject, value);
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
                        () => VisualizationContext.Instance.PlayOrPause(),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.playPauseCommand;
            }
        }

        /// <summary>
        /// Gets the toggle cursor follows mouse command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleCursorFollowsMouseComand
        {
            get
            {
                if (this.toggleCursorFollowsMouseComand == null)
                {
                    this.toggleCursorFollowsMouseComand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.CursorFollowsMouse = !this.VisualizationContainer.Navigator.CursorFollowsMouse);
                }

                return this.toggleCursorFollowsMouseComand;
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
                                Filter = "Psi Store (.psi)|*.psi",
                            };

                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                await VisualizationContext.Instance.OpenDatasetAsync(filename);
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

                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                await VisualizationContext.Instance.OpenDatasetAsync(filename);
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

                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                string filename = dlg.FileName;

                                // this should be a relatively quick operation so no need to show progress
                                await VisualizationContext.Instance.DatasetViewModel.SaveAsync(filename);
                            }
                        });
                }

                return this.saveDatasetCommand;
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
                    this.insertTimelinePanelCommand = new RelayCommand(() => VisualizationContext.Instance.VisualizationContainer.AddPanel(new TimelineVisualizationPanel()));
                }

                return this.insertTimelinePanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert 1 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert1CellInstantPanelCommand
        {
            get
            {
                if (this.insert1CellInstantPanelCommand == null)
                {
                    this.insert1CellInstantPanelCommand = new RelayCommand(() => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(1)));
                }

                return this.insert1CellInstantPanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert 2 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert2CellInstantPanelCommand
        {
            get
            {
                if (this.insert2CellInstantPanelCommand == null)
                {
                    this.insert2CellInstantPanelCommand = new RelayCommand(() => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(2)));
                }

                return this.insert2CellInstantPanelCommand;
            }
        }

        /// <summary>
        /// Gets the insert 3 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert3CellInstantPanelCommand
        {
            get
            {
                if (this.insert3CellInstantPanelCommand == null)
                {
                    this.insert3CellInstantPanelCommand = new RelayCommand(() => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(3)));
                }

                return this.insert3CellInstantPanelCommand;
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.moveToSelectionStartCommand;
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
                        () => VisualizationContext.Instance.IsDatasetLoaded());
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
                        () => VisualizationContext.Instance.ToggleLiveMode(),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == true);
                }

                return this.toggleLiveModeCommand;
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
        /// Gets the expand all command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExpandDatasetsTreeCommand
        {
            get
            {
                if (this.expandDatasetsTreeCommand == null)
                {
                    this.expandDatasetsTreeCommand = new RelayCommand(() => this.ExpandDatasetsTree());
                }

                return this.expandDatasetsTreeCommand;
            }
        }

        /// <summary>
        /// Gets the collapse all command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CollapseDatasetsTreeCommand
        {
            get
            {
                if (this.collapseDatasetsTreeCommand == null)
                {
                    this.collapseDatasetsTreeCommand = new RelayCommand(() => this.CollapseDatasetsTree());
                }

                return this.collapseDatasetsTreeCommand;
            }
        }

        /// <summary>
        /// Gets the expand visualizations tree command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExpandVisualizationsTreeCommand
        {
            get
            {
                if (this.expandVisualizationsTreeCommand == null)
                {
                    this.expandVisualizationsTreeCommand = new RelayCommand(() => this.ExpandVisualizationsTree());
                }

                return this.expandVisualizationsTreeCommand;
            }
        }

        /// <summary>
        /// Gets the collapse visualizations tree command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CollapseVisualizationsTreeCommand
        {
            get
            {
                if (this.collapseVisualizationsTreeCommand == null)
                {
                    this.collapseVisualizationsTreeCommand = new RelayCommand(() => this.CollapseVisualizationsTree());
                }

                return this.collapseVisualizationsTreeCommand;
            }
        }

        /// <summary>
        /// Gets the synchronize trees command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SynchronizeTreesCommand
        {
            get
            {
                if (this.synchronizeTreesCommand == null)
                {
                    this.synchronizeTreesCommand = new RelayCommand(() => this.SynchronizeDatasetsTreeToVisualizationsTree());
                }

                return this.synchronizeTreesCommand;
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
        /// Gets the command that executes after the user clicks on either the datasets or the visualizations tree views.
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
        /// Gets or sets the collection of available layouts.
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
        /// Gets or sets the current layout.
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
                    this.OpenCurrentLayout();
                }

                this.RaisePropertyChanged(nameof(this.CurrentLayout));
            }
        }

        private string LayoutsDirectory => Path.Combine(PsiStudioDocumentsPath, "Layouts");

        /// <summary>
        /// Called when the main application window is closing.
        /// </summary>
        public void OnClosing()
        {
            // Put the current state of the timeing buttons into the settings object
            this.AppSettings.ShowAbsoluteTiming = this.VisualizationContainer.Navigator.ShowAbsoluteTiming;
            this.AppSettings.ShowTimingRelativeToSessionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart;
            this.AppSettings.ShowTimingRelativeToSelectionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart;
        }

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
                panel.Name = dlg.PartitionName;
                panel.Height = 22;
                this.VisualizationContainer.AddPanel(panel);

                // create a new annotated event visualization object and add to the panel
                var annotations = new AnnotatedEventVisualizationObject();
                annotations.Name = dlg.AnnotationName;
                panel.AddVisualizationObject(annotations);

                // create a new annotation definition and store
                var definition = new AnnotatedEventDefinition(dlg.StreamName);
                definition.AddSchema(dlg.AnnotationSchema);
                this.DatasetViewModel.CurrentSessionViewModel.CreateAnnotationPartition(dlg.StoreName, dlg.StorePath, definition);

                // open the stream for visualization (NOTE: if the selection extents were MinTime/MaxTime, no event will be created)
                annotations.OpenStream(new StreamBinding(dlg.StreamName, dlg.PartitionName, dlg.StoreName, dlg.StorePath, typeof(AnnotationSimpleReader)));
            }
        }*/

        private void OpenCurrentLayout()
        {
            // Attempt to open the current layout
            bool success = VisualizationContext.Instance.OpenLayout(this.CurrentLayout.Path, this.CurrentLayout.Name);

            // If the load failed, load the default layout instead.  This method
            // may have been initially called by the SelectedItemChanged handler
            // from the Layouts combobox, and it's bound to CurrentLayout, so
            // we need to kick off a task to change its value back rather than
            // set it directly here.
            /*if (!success)
            {
                Task.Run(() => Application.Current?.Dispatcher.InvokeAsync(() => this.CurrentLayout = this.AvailableLayouts[0]));
            }*/
        }

        private async void MainWindow_Activated(object sender, EventArgs e)
        {
            if (!this.isInitialized)
            {
                this.isInitialized = true;

                // Initialize the visualizer map
                this.InitializeVisualizerMap();

                // Open the current layout
                this.OpenCurrentLayout();

                // Check if the name of a psi store was specified on the command line, and if so, load the store.
                // First arg is this exe's filename, second arg (if it exists) is the store to open
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    await VisualizationContext.Instance.OpenDatasetAsync(args[1]);
                }
            }
        }

        private void InitializeVisualizerMap()
        {
            // The list of additional assemblies PsiStudio will load.
            List<string> additionalAssemblies = new List<string>();

            // If we have any additional assemblies to search for visualization
            // classes, display the security warning before proceeding.
            if ((this.AppSettings.AdditionalAssembliesList != null) && (this.AppSettings.AdditionalAssembliesList.Count > 0))
            {
                AdditionalAssembliesWindow dlg = new AdditionalAssembliesWindow(Application.Current.MainWindow, this.AppSettings.AdditionalAssembliesList);

                if (dlg.ShowDialog() == true)
                {
                    additionalAssemblies.AddRange(this.AppSettings.AdditionalAssembliesList);
                }
            }

            // Initialize the visualizer map
            VisualizationContext.Instance.VisualizerMap.Initialize(additionalAssemblies, Path.Combine(PsiStudioDocumentsPath, "VisualizersLog.txt"));
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

        private void SaveLayoutAs()
        {
            LayoutNameWindow dlg = new LayoutNameWindow(Application.Current.MainWindow);

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

        private void ExpandDatasetsTree()
        {
            this.UpdateDatasetsTreeView(true);
        }

        private void CollapseDatasetsTree()
        {
            this.UpdateDatasetsTreeView(false);
        }

        private void UpdateDatasetsTreeView(bool expand)
        {
            foreach (DatasetViewModel datasetViewModel in VisualizationContext.Instance.DatasetViewModels)
            {
                foreach (SessionViewModel sessionViewModel in datasetViewModel.SessionViewModels)
                {
                    foreach (PartitionViewModel partitionViewModel in sessionViewModel.PartitionViewModels)
                    {
                        if (expand)
                        {
                            partitionViewModel.StreamTreeRoot.ExpandAll();
                        }
                        else
                        {
                            partitionViewModel.StreamTreeRoot.CollapseAll();
                        }

                        partitionViewModel.IsTreeNodeExpanded = expand;
                    }

                    sessionViewModel.IsTreeNodeExpanded = expand;
                }

                datasetViewModel.IsTreeNodeExpanded = expand;
            }
        }

        private void ExpandVisualizationsTree()
        {
            this.UpdateVisualizationTreeView(true);
        }

        private void CollapseVisualizationsTree()
        {
            this.UpdateVisualizationTreeView(false);
        }

        private void UpdateVisualizationTreeView(bool expand)
        {
            foreach (VisualizationPanel visualizationPanel in this.VisualizationContainer.Panels)
            {
                visualizationPanel.IsTreeNodeExpanded = expand;
            }
        }

        private void SynchronizeDatasetsTreeToVisualizationsTree()
        {
            if (VisualizationContext.Instance.DatasetViewModel != null)
            {
                IStreamVisualizationObject streamVisualizationObject = this.selectedVisualization as IStreamVisualizationObject;
                if (streamVisualizationObject != null)
                {
                    StreamBinding streamBinding = streamVisualizationObject.StreamBinding;
                    foreach (SessionViewModel sessionViewModel in VisualizationContext.Instance.DatasetViewModel.SessionViewModels)
                    {
                        PartitionViewModel partitionViewModel = sessionViewModel.PartitionViewModels.FirstOrDefault(p => p.StorePath == streamBinding.StorePath);
                        if (partitionViewModel != null)
                        {
                            if (partitionViewModel.SelectStream(streamBinding.StreamName))
                            {
                                sessionViewModel.IsTreeNodeExpanded = true;
                                VisualizationContext.Instance.DatasetViewModel.IsTreeNodeExpanded = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void VisualizationContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.VisualizationContainer))
            {
                this.RaisePropertyChanged(nameof(this.VisualizationContainer));
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
    }
}
