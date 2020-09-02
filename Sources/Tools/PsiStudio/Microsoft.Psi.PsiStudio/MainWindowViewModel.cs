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
    using System.Threading.Tasks;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.PsiStudio.Windows;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
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
        /// <summary>
        /// The path to the PsiStudio data in the MyDocuments folder.
        /// </summary>
        private static string psiStudioDocumentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ApplicationName);

        /// <summary>
        /// The path to the layouts directory.
        /// </summary>
        private static string layoutsPath = Path.Combine(psiStudioDocumentsPath, "Layouts");

        /// <summary>
        /// The path to the annotations definitions directory.
        /// </summary>
        private static string annotationDefinitionsPath = Path.Combine(psiStudioDocumentsPath, "AnnotationDefinitions");

        private readonly TimeSpan nudgeTimeSpan = TimeSpan.FromSeconds(1 / 30.0);
        private readonly TimeSpan jumpTimeSpan = TimeSpan.FromSeconds(1 / 6.0);
        private readonly string newLayoutName = "<New>";
        private List<LayoutInfo> availableLayouts = new List<LayoutInfo>();
        private List<AnnotationDefinition> annotationDefinitions;
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
        private RelayCommand nudgeRightCommand;
        private RelayCommand nudgeLeftCommand;
        private RelayCommand jumpRightCommand;
        private RelayCommand jumpLeftCommand;
        private RelayCommand openStoreCommand;
        private RelayCommand openDatasetCommand;
        private RelayCommand saveDatasetCommand;
        private RelayCommand insertTimelinePanelCommand;
        private RelayCommand insert1CellInstantPanelCommand;
        private RelayCommand insert2CellInstantPanelCommand;
        private RelayCommand insert3CellInstantPanelCommand;
        private RelayCommand createAnnotationStreamCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand clearSelectionCommand;
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

        ////private RelayCommand showSettingsWindowComand;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            // Create and load the settings
            this.AppSettings = PsiStudioSettings.Load(Path.Combine(psiStudioDocumentsPath, "PsiStudioSettings.xml"));

            // Wait until the main window is visible before initializing the visualizer
            // map as we may need to display some message boxes during this process.
            Application.Current.MainWindow.ContentRendered += this.MainWindow_Activated;

            // Listen for property change events from the visualization context (specifically when the visualization container changes)
            VisualizationContext.Instance.PropertyChanging += this.VisualizationContext_PropertyChanging;
            VisualizationContext.Instance.PropertyChanged += this.VisualizationContext_PropertyChanged;

            // Listen for events that occur when some part of a visualization object requests to have its properties displayed in the property browser.
            VisualizationContext.Instance.RequestDisplayObjectProperties += (sender, e) => this.SelectedPropertiesObject = e.Object;

            // Listen for events that occur when a store/stream becomes dirty or clean
            DataManager.Instance.DataStoreStatusChanged += this.DataStoreStatusChanged;

            // Load the available layouts
            this.UpdateLayoutList();
        }

        /// <summary>
        /// Gets the name of this application for use when constructing paths etc.
        /// </summary>
        public static string ApplicationName => "PsiStudio";

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
        /// Gets the text to display in the application's titlebar.
        /// </summary>
        public string TitleText
        {
            get
            {
                StringBuilder text = new StringBuilder("Platform for Situated Intelligence Studio");
                if (VisualizationContext.Instance.DatasetViewModel != null)
                {
                    text.Append(" - ");
                    text.Append(VisualizationContext.Instance.DatasetViewModel.Name);

                    if (VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject != null)
                    {
                        text.Append(" [cursor snaps to ");
                        text.Append(VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject.Name);
                        text.Append(" stream]");
                    }
                }

                return text.ToString();
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
                        () => VisualizationContext.Instance.PlayOrPause(),
                        () => this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
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
        /// Gets the nudge cursor right command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NudgeRightCommand
        {
            get
            {
                if (this.nudgeRightCommand == null)
                {
                    this.nudgeRightCommand = new RelayCommand(
                        () => this.MoveCursorBy(this.nudgeTimeSpan, SnappingBehavior.Next),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.nudgeRightCommand;
            }
        }

        /// <summary>
        /// Gets the nudge cursor left command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NudgeLeftCommand
        {
            get
            {
                if (this.nudgeLeftCommand == null)
                {
                    this.nudgeLeftCommand = new RelayCommand(
                        () => this.MoveCursorBy(-this.nudgeTimeSpan, SnappingBehavior.Previous),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.nudgeLeftCommand;
            }
        }

        /// <summary>
        /// Gets the jump cursor right command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand JumpRightCommand
        {
            get
            {
                if (this.jumpRightCommand == null)
                {
                    this.jumpRightCommand = new RelayCommand(
                        () => this.MoveCursorBy(this.jumpTimeSpan, SnappingBehavior.Next),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.jumpRightCommand;
            }
        }

        /// <summary>
        /// Gets the jump cursor left command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand JumpLeftCommand
        {
            get
            {
                if (this.jumpLeftCommand == null)
                {
                    this.jumpLeftCommand = new RelayCommand(
                        () => this.MoveCursorBy(-this.jumpTimeSpan, SnappingBehavior.Previous),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);
                }

                return this.jumpLeftCommand;
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
                            var formats = VisualizationContext.Instance.PluginMap.GetStreamReaderExtensions();
                            OpenFileDialog dlg = new OpenFileDialog
                            {
                                DefaultExt = ".psi",
                                Filter = string.Join("|", formats.Select(f => $"{f.Name}|*{f.Extensions}")),
                            };

                            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
                            if (result == true)
                            {
                                string filename = dlg.FileName;
                                await VisualizationContext.Instance.OpenDatasetAsync(filename);
                                this.EnsureStreamMemberNodesVisible();
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
                                this.EnsureStreamMemberNodesVisible();
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
                        () => this.VisualizationContainer.Navigator.CanZoomToSelection());
                }

                return this.zoomToSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the clear selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearSelectionCommand
        {
            get
            {
                if (this.clearSelectionCommand == null)
                {
                    this.clearSelectionCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.ClearSelection(),
                        () => this.VisualizationContainer.Navigator.CanClearSelection());
                }

                return this.clearSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the command to move the cursor to the selection start.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveCursorToSelectionStartCommand
        {
            get
            {
                if (this.moveToSelectionStartCommand == null)
                {
                    this.moveToSelectionStartCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.MoveCursorToSelectionStart(),
                        () => this.VisualizationContainer.Navigator.CanMoveCursorToSelectionStart());
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
        /// Gets the command to move the cursor to the selection end.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveCursorToSelectionEndCommand
        {
            get
            {
                if (this.moveToSelectionEndCommand == null)
                {
                    this.moveToSelectionEndCommand = new RelayCommand(
                        () => this.VisualizationContainer.Navigator.MoveCursorToSelectionEnd(),
                        () => this.VisualizationContainer.Navigator.CanMoveCursorToSelectionEnd());
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
        /// Gets the selected visualization changed command.
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

        /// <summary>
        /// Gets the create annotation stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateAnnotationStreamCommand
        {
            get
            {
                if (this.createAnnotationStreamCommand == null)
                {
                    this.createAnnotationStreamCommand = new RelayCommand(() => this.CreateAnnotationStream());
                }

                return this.createAnnotationStreamCommand;
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

        /// <summary>
        /// Called when the main application window is closing.
        /// </summary>
        /// <returns>True if the application should continue closing, or false if closing has been cancelled by the user.</returns>
        public bool OnClosing()
        {
            // If there's any unsaved partitions, prompt the user to save the changes first.
            SessionViewModel currentSession = VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel;
            if (currentSession != null)
            {
                foreach (PartitionViewModel partitionViewModel in currentSession.PartitionViewModels)
                {
                    if (!partitionViewModel.PromptSaveChangesAndContinue())
                    {
                        return false;
                    }
                }
            }

            // Put the current state of the timing buttons into the settings object
            this.AppSettings.ShowAbsoluteTiming = this.VisualizationContainer.Navigator.ShowAbsoluteTiming;
            this.AppSettings.ShowTimingRelativeToSessionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart;
            this.AppSettings.ShowTimingRelativeToSelectionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart;

            // Save the settings
            this.AppSettings.Save();

            return true;
        }

        /// <summary>
        /// Creates a new annotation stream in a partition.
        /// </summary>
        public void CreateAnnotationStream()
        {
            // Ensure there is a current session.
            SessionViewModel currentSession = VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel;
            if (currentSession == null)
            {
                return;
            }

            CreateAnnotationStreamWindow dlg = new CreateAnnotationStreamWindow(currentSession.PartitionViewModels, this.annotationDefinitions, Application.Current.MainWindow);
            if (dlg.ShowDialog() == true)
            {
                AnnotationDefinition annotationDefinition = dlg.SelectedAnnotationDefinition;
                string streamName = dlg.StreamName;
                string storeName;
                string storePath;

                if (dlg.UseExistingPartition)
                {
                    // Get the partition that the stream will be created in
                    PartitionViewModel partitionViewModel = currentSession.PartitionViewModels.FirstOrDefault(p => p.Name == dlg.ExistingPartitionName);

                    // Make note of the partition's name and path so we can reload it later
                    storeName = partitionViewModel.StoreName;
                    storePath = partitionViewModel.StorePath;

                    // Attempt to remove the partition from the session.  If the partition contains unsaved changes, then the user will be
                    // prompted to save the changes first.  The user may elect to cancel the entire operation, in which case we cannot continue.
                    if (!partitionViewModel.RemovePartition())
                    {
                        return;
                    }

                    // Close the existing partition's store
                    DataManager.Instance.CloseStore(storeName, storePath);
                }
                else
                {
                    // Make note of the new partition's name and path
                    storeName = dlg.StoreName;
                    storePath = dlg.StorePath;
                }

                // Create the progress window
                var progressWindow = new ProgressWindow(Application.Current.MainWindow, $"Creating annotations stream {dlg.StreamName}");
                var progress = new Progress<double>(p =>
                {
                    progressWindow.Progress = p;

                    if (p == 1.0)
                    {
                        // close the status window when the task reports completion
                        progressWindow.Close();
                    }
                });

                // Run the task to add the stream to the existing or new partition
                if (dlg.UseExistingPartition)
                {
                    // Add the empty annotations stream to the existing partition
                    Task.Run(() => PsiStore.AddStreamInPlace<TimeIntervalAnnotation, AnnotationDefinition>((storeName, storePath), streamName, annotationDefinition, true, progress));
                }
                else
                {
                    // Create the new partition with the empty annotations stream
                    Task.Run(() => PsiStore.CreateWithStream<TimeIntervalAnnotation, AnnotationDefinition>(storeName, storePath, streamName, annotationDefinition, progress));
                }

                // Show the modal progress window.  If the task has already completed then it will have
                // closed the progress window and an invalid operation exception will be thrown.
                try
                {
                    progressWindow.ShowDialog();
                }
                catch (InvalidOperationException)
                {
                }

                // Add the partition to the session
                currentSession.AddStorePartition(new PsiStoreStreamReader(storeName, storePath));

                // Update the source bindings for all visualization objects in the current session
                this.VisualizationContainer.UpdateStreamSources(currentSession);
            }
        }

        private void MoveCursorBy(TimeSpan timeSpan, SnappingBehavior snappingBehavior)
        {
            var visContainer = this.VisualizationContainer;
            var nav = visContainer.Navigator;
            var time = nav.Cursor + timeSpan;
            if (visContainer.SnapToVisualizationObject is IStreamVisualizationObject vo)
            {
                nav.MoveCursorTo(vo.GetSnappedTime(time, snappingBehavior) ?? time);
            }
            else
            {
                nav.MoveCursorTo(time);
            }
        }

        private void OpenCurrentLayout()
        {
            // Check if the current layout is the default, empty layout
            if (this.CurrentLayout.Name == this.newLayoutName)
            {
                VisualizationContext.Instance.ClearLayout();
            }
            else
            {
                // Attempt to open the current layout
                bool success = VisualizationContext.Instance.OpenLayout(this.CurrentLayout.Path, this.CurrentLayout.Name);
                if (success)
                {
                    this.EnsureStreamMemberNodesVisible();
                }
                else
                {
                    // If the load failed, load the default layout instead.  This method
                    // may have been initially called by the SelectedItemChanged handler
                    // from the Layouts combobox, and it's bound to CurrentLayout, so
                    // we need to asynchronously dispatch a message to change its value
                    // back rather than set it directly here.
                    Application.Current?.Dispatcher.InvokeAsync(() => this.CurrentLayout = this.AvailableLayouts[0]);
                }
            }
        }

        private async void MainWindow_Activated(object sender, EventArgs e)
        {
            if (!this.isInitialized)
            {
                this.isInitialized = true;

                // Initialize the visualizer map
                this.InitializeVisualizerMap();

                // Load the available annotation definitions
                this.LoadAnnotationDefinitions();

                // Open the current layout
                this.OpenCurrentLayout();

                // Check if the name of a psi store was specified on the command line, and if so, load the store.
                // First arg is this exe's filename, second arg (if it exists) is the store to open
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    await VisualizationContext.Instance.OpenDatasetAsync(args[1]);
                    this.EnsureStreamMemberNodesVisible();
                }
            }
        }

        private void EnsureStreamMemberNodesVisible()
        {
            if (VisualizationContext.Instance.DatasetViewModel != null)
            {
                // Check if the visualization container contains any stream member visualizers.
                List<IStreamVisualizationObject> memberVisualizers = this.VisualizationContainer.GetStreamMemberVisualizers();
                if (memberVisualizers.Any())
                {
                    // Get the current session
                    SessionViewModel currentSessionViewModel = VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel;

                    foreach (IStreamVisualizationObject streamMemberVisualizer in memberVisualizers)
                    {
                        // Get the stream tree node for stream being used by the stream member visualizer.
                        StreamTreeNode streamTreeNode = currentSessionViewModel.FindStream(streamMemberVisualizer.StreamBinding.PartitionName, streamMemberVisualizer.StreamBinding.StreamName);

                        // If the session contains the stream, ensure its member children have been created.
                        if (streamTreeNode != null)
                        {
                            streamTreeNode.EnsureMemberChildExists(streamMemberVisualizer.StreamBinding.StreamAdapterArguments[0] as string);
                        }
                    }
                }
            }
        }

        private void DataStoreStatusChanged(object sender, DataStoreStatusChangedEventArgs e)
        {
            if (VisualizationContext.Instance.DatasetViewModel != null)
            {
                SessionViewModel currentSessionViewModel = VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel;
                if (currentSessionViewModel != null)
                {
                    PartitionViewModel partitionViewModel = currentSessionViewModel.PartitionViewModels.FirstOrDefault(p => p.Name == e.StoreName);
                    if (partitionViewModel != default)
                    {
                        partitionViewModel.ChangeStoreStatus(e.IsDirty, e.StreamNames);
                    }
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
            VisualizationContext.Instance.PluginMap.Initialize(additionalAssemblies, Path.Combine(psiStudioDocumentsPath, "VisualizersLog.txt"));
        }

        private void UpdateLayoutList()
        {
            // Create a new collection of layouts
            List<LayoutInfo> layouts = new List<LayoutInfo>();

            // Add the default/new layout
            layouts.Add(new LayoutInfo(this.newLayoutName, null));

            // Create the layouts directory if it doesn't already exist
            DirectoryInfo directoryInfo = new DirectoryInfo(layoutsPath);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(layoutsPath);
            }

            // Find all the layout files and add them to the list of available layouts
            FileInfo[] files = directoryInfo.GetFiles("*.plo");
            foreach (FileInfo fileInfo in files)
            {
                layouts.Add(new LayoutInfo(Path.GetFileNameWithoutExtension(fileInfo.FullName), fileInfo.FullName));
            }

            // Set the list of available layouts
            this.RaisePropertyChanging(nameof(this.AvailableLayouts));
            this.AvailableLayouts = layouts;
            this.RaisePropertyChanged(nameof(this.AvailableLayouts));

            // Set the current layout if it's in the available layouts, otherwise make "new layout" the current layout
            LayoutInfo lastLayout = this.AvailableLayouts.FirstOrDefault(l => l.Name == this.AppSettings.CurrentLayoutName);
            this.CurrentLayout = lastLayout ?? this.AvailableLayouts[0];
        }

        private void SaveLayoutAs()
        {
            LayoutNameWindow dlg = new LayoutNameWindow(Application.Current.MainWindow, layoutsPath);

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string fileName = Path.Combine(layoutsPath, dlg.LayoutName);

                // Save the layout
                this.VisualizationContainer.Save(fileName);

                // Recreate the layout list
                this.UpdateLayoutList();

                // Set the current layout
                this.CurrentLayout = this.AvailableLayouts.First(l => l.Path == fileName);
            }
        }

        private void LoadAnnotationDefinitions()
        {
            this.annotationDefinitions = new List<AnnotationDefinition>();

            // Create the annotations definitions directory if it doesn't already exist
            DirectoryInfo directoryInfo = new DirectoryInfo(annotationDefinitionsPath);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(annotationDefinitionsPath);
            }

            // Keep a list of annotation definitions that failed to load
            List<string> annotationDefinitionLoadFailures = new List<string>();

            // Find all the annotations definitions and add them to the list
            FileInfo[] files = directoryInfo.GetFiles("*.pad");
            foreach (FileInfo fileInfo in files)
            {
                AnnotationDefinition annotationDefinition = AnnotationDefinition.Load(fileInfo.FullName);
                if (annotationDefinition != null)
                {
                    this.annotationDefinitions.Add(annotationDefinition);
                }
                else
                {
                    annotationDefinitionLoadFailures.Add(fileInfo.FullName);
                }
            }

            if (annotationDefinitionLoadFailures.Count > 0)
            {
                this.ReportAnnotationDefinitionLoadFailures(annotationDefinitionLoadFailures);
            }
        }

        private void ReportAnnotationDefinitionLoadFailures(List<string> annotationDefinitionLoadFailures)
        {
            StringBuilder errorMessage = new StringBuilder();
            errorMessage.AppendLine("The following Annotation Definitions could not be loaded because they contain unknown types:");
            errorMessage.AppendLine();
            foreach (string annotationDefinitionLoadFailure in annotationDefinitionLoadFailures)
            {
                FileInfo fileInfo = new FileInfo(annotationDefinitionLoadFailure);
                errorMessage.AppendLine(fileInfo.Name);
            }

            new MessageBoxWindow(Application.Current.MainWindow, "Annotation Definition Load Error", errorMessage.ToString(), "Close", null).ShowDialog();
        }

        private void ExpandDatasetsTree()
        {
            this.ExpandOrCollapseDatasetsTreeView(true);
        }

        private void CollapseDatasetsTree()
        {
            this.ExpandOrCollapseDatasetsTreeView(false);
        }

        private void ExpandOrCollapseDatasetsTreeView(bool expand)
        {
            foreach (var datasetViewModel in VisualizationContext.Instance.DatasetViewModels)
            {
                foreach (var sessionViewModel in datasetViewModel.SessionViewModels)
                {
                    foreach (var partitionViewModel in sessionViewModel.PartitionViewModels)
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

                // for the dataset level, we only expand. When we collapse, the dataset level does not
                // collapse, since that provides no useful information - the user most likely wants to
                // see the sessions. If for some reason they need to be hidden, that collapse can be
                // done manually.
                if (expand)
                {
                    datasetViewModel.IsTreeNodeExpanded = expand;
                }
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
                        PartitionViewModel partitionViewModel = sessionViewModel.PartitionViewModels.FirstOrDefault(p => p.Name == streamBinding.PartitionName);
                        if (partitionViewModel != null)
                        {
                            // Get the name of the node to select.  If there are stream adapter arguments then assume
                            // the stream adapter is a stream member adapter and append the path to the member.
                            string nodeName = streamBinding.StreamName;
                            if ((streamBinding.StreamAdapterArguments != null) && streamBinding.StreamAdapterArguments.Any())
                            {
                                nodeName += "." + streamBinding.StreamAdapterArguments[0] as string;
                            }

                            if (partitionViewModel.SelectNode(nodeName))
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

        private void VisualizationContext_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.VisualizationContainer))
            {
                // Unhook property changed events from old visualization container
                if (VisualizationContext.Instance.VisualizationContainer != null)
                {
                    VisualizationContext.Instance.VisualizationContainer.PropertyChanged -= this.VisualizationContainer_PropertyChanged;
                }

                this.RaisePropertyChanging(nameof(this.VisualizationContainer));
            }
            else if (e.PropertyName == nameof(VisualizationContext.DatasetViewModel))
            {
                // Unhook property changed events from old dataset view model
                if (VisualizationContext.Instance.DatasetViewModel != null)
                {
                    VisualizationContext.Instance.DatasetViewModel.PropertyChanged -= this.DatasetViewModel_PropertyChanged;
                }

                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void VisualizationContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.VisualizationContainer))
            {
                // Hook property changed events to new visualization container
                if (VisualizationContext.Instance.VisualizationContainer != null)
                {
                    VisualizationContext.Instance.VisualizationContainer.PropertyChanged += this.VisualizationContainer_PropertyChanged;
                }

                this.RaisePropertyChanged(nameof(this.VisualizationContainer));
            }
            else if (e.PropertyName == nameof(VisualizationContext.DatasetViewModel))
            {
                // Hook property changed events to new dataset view model
                if (VisualizationContext.Instance.DatasetViewModel != null)
                {
                    VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.DatasetViewModel_PropertyChanged;
                }

                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void DatasetViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DatasetViewModel.Name))
            {
                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void VisualizationContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject))
            {
                this.RaisePropertyChanged(nameof(this.TitleText));
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
