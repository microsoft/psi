// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
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
        /// <summary>
        /// The path to the settings directiory.
        /// </summary>
        private static readonly string PsiStudioSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ApplicationName,
            "Settings",
            Environment.MachineName);

        /// <summary>
        /// The path to the logs directiory.
        /// </summary>
        private static readonly string PsiStudioLogsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ApplicationName,
            "Logs",
            Environment.MachineName);

        /// <summary>
        /// The path to the layouts directory.
        /// </summary>
        private static readonly string PsiStudioLayoutsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ApplicationName,
            "Layouts");

        /// <summary>
        /// The path to the annotations schemas directory.
        /// </summary>
        private static readonly string PsiStudioAnnotationSchemasPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ApplicationName,
            "AnnotationSchemas");

        /// <summary>
        /// The path to the batch processing task configurations directory.
        /// </summary>
        private static readonly string PsiStudioBatchProcessingTaskConfigurationsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            ApplicationName,
            "BatchProcessingTaskConfigurations");

        private readonly TimeSpan nudgeTimeSpan = TimeSpan.FromSeconds(1 / 30.0);
        private readonly TimeSpan jumpTimeSpan = TimeSpan.FromSeconds(1 / 6.0);
        private readonly string newLayoutName = "<New>";
        private readonly Dictionary<string, bool> userConsentObtained = new ();
        private List<LayoutInfo> availableLayouts = new ();
        private List<AnnotationSchema> annotationSchemas;
        private LayoutInfo currentLayout = null;
        private bool currentLayoutUpdating = false;

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

        private RelayCommand playPauseFromCursorCommand;
        private RelayCommand playPauseFromSelectionStartCommand;
        private RelayCommand goToTimeCommand;
        private RelayCommand toggleCursorFollowsMouseCommand;
        private RelayCommand nudgeRightCommand;
        private RelayCommand nudgeLeftCommand;
        private RelayCommand jumpRightCommand;
        private RelayCommand jumpLeftCommand;
        private RelayCommand openStoreCommand;
        private RelayCommand openDatasetCommand;
        private RelayCommand<string> openRecentlyUsedDatasetCommand;
        private RelayCommand<string> openRecentlyUsedStoreCommand;
        private RelayCommand saveDatasetAsCommand;
        private RelayCommand insertTimelinePanelCommand;
        private RelayCommand insert1CellInstantPanelCommand;
        private RelayCommand insert2CellInstantPanelCommand;
        private RelayCommand insert3CellInstantPanelCommand;
        private RelayCommand createAnnotationStreamCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand moveSelectionLeftCommand;
        private RelayCommand moveSelectionRightCommand;
        private RelayCommand moveSelectionToPreviousAnnotationCommand;
        private RelayCommand moveSelectionToNextAnnotationCommand;
        private RelayCommand clearSelectionCommand;
        private RelayCommand moveToSelectionStartCommand;
        private RelayCommand togglePlayRepeatCommand;
        private RelayCommand moveToSelectionEndCommand;
        private RelayCommand increasePlaySpeedCommand;
        private RelayCommand decreasePlaySpeedCommand;
        private RelayCommand toggleLiveModeCommand;
        private RelayCommand saveLayoutCommand;
        private RelayCommand saveLayoutAsCommand;
        private RelayCommand clearLayoutCommand;
        private RelayCommand deleteLayoutCommand;
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
        private RelayCommand editSettingsCommand;
        private RelayCommand helpCommand;
        private RelayCommand viewAdditionalAssemblyLoadErrorLogCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            // Create the settings path if it doesn't exist.
            if (!Directory.Exists(PsiStudioSettingsPath))
            {
                Directory.CreateDirectory(PsiStudioSettingsPath);
            }

            if (!Directory.Exists(PsiStudioLogsPath))
            {
                Directory.CreateDirectory(PsiStudioLogsPath);
            }

            // Load the settings
            this.Settings = PsiStudioSettings.Load(Path.Combine(PsiStudioSettingsPath, "PsiStudioSettings.xml"));

            // Wait until the main window is visible before initializing the visualizer
            // map as we may need to display some message boxes during this process.
            Application.Current.MainWindow.ContentRendered += this.OnMainWindowContentRendered;

            // Listen for property change events from the visualization context (specifically when the visualization container changes)
            VisualizationContext.Instance.PropertyChanging += this.OnVisualizationContextPropertyChanging;
            VisualizationContext.Instance.PropertyChanged += this.OnVisualizationContextPropertyChanged;

            // Listen for events that occur when some part of a visualization object requests to have its properties displayed in the property browser.
            VisualizationContext.Instance.RequestDisplayObjectProperties += (sender, e) => this.SelectedPropertiesObject = e.Object;

            // Listen for events that occur when a store/stream becomes dirty or clean
            DataManager.Instance.DataStoreStatusChanged += this.DataStoreStatusChanged;

            // Load the available layouts
            this.UpdateLayoutList();

            // Listen for navigator property changes to capture in settings
            this.VisualizationContainer.Navigator.PropertyChanged += this.OnNavigatorPropertyChanged;
        }

        /// <summary>
        /// Gets the name of this application for use when constructing paths etc.
        /// </summary>
        public static string ApplicationName => "PsiStudio";

        /// <summary>
        /// Gets the application settings.
        /// </summary>
        public PsiStudioSettings Settings { get; }

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
                var text = new StringBuilder("Platform for Situated Intelligence Studio");
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
        /// Gets the play/pause from cursor command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlayPauseFromCursorCommand
            => this.playPauseFromCursorCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.PlayOrPause(false),
                () => this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the play/pause from selection start command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlayPauseFromSelectionStartCommand
            => this.playPauseFromSelectionStartCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.PlayOrPause(true),
                () => this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the go-to-time command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand GoToTimeCommand
            => this.goToTimeCommand ??= new RelayCommand(() => VisualizationContext.Instance.VisualizationContainer.GoToTime());

        /// <summary>
        /// Gets the toggle cursor follows mouse command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleCursorFollowsMouseCommand
            => this.toggleCursorFollowsMouseCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.CursorFollowsMouse = !this.VisualizationContainer.Navigator.CursorFollowsMouse);

        /// <summary>
        /// Gets the nudge cursor right command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NudgeRightCommand
            => this.nudgeRightCommand ??= new RelayCommand(
                () => this.MoveCursorBy(this.nudgeTimeSpan, NearestType.Next),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the nudge cursor left command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NudgeLeftCommand
            => this.nudgeLeftCommand ??= new RelayCommand(
                () => this.MoveCursorBy(-this.nudgeTimeSpan, NearestType.Previous),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the jump cursor right command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand JumpRightCommand
            => this.jumpRightCommand ??= new RelayCommand(
                () => this.MoveCursorBy(this.jumpTimeSpan, NearestType.Next),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the jump cursor left command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand JumpLeftCommand
            => this.jumpLeftCommand ??= new RelayCommand(
                () => this.MoveCursorBy(-this.jumpTimeSpan, NearestType.Previous),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the open store command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand OpenStoreCommand
            => this.openStoreCommand ??= new RelayCommand(
                async () =>
                {
                    var formats = VisualizationContext.Instance.PluginMap.GetStreamReaderExtensions();
                    var openFileDialog = new OpenFileDialog
                    {
                        DefaultExt = ".psi",
                        Filter = string.Join("|", formats.Select(f => $"{f.Name}|*{f.Extensions}")),
                    };

                    bool? result = openFileDialog.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        string filename = openFileDialog.FileName;
                        await VisualizationContext.Instance.OpenDatasetOrStoreAsync(filename, true, this.Settings.AutoSaveDatasets);
                        this.Settings.AddMostRecentlyUsedStoreFilename(filename);
                        this.Settings.Save();
                    }
                });

        /// <summary>
        /// Gets the open dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand OpenDatasetCommand
            => this.openDatasetCommand ??= new RelayCommand(
                async () =>
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        DefaultExt = ".pds",
                        Filter = "Psi Dataset (.pds)|*.pds",
                    };

                    bool? result = openFileDialog.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        string filename = openFileDialog.FileName;
                        await VisualizationContext.Instance.OpenDatasetOrStoreAsync(filename, true, this.Settings.AutoSaveDatasets);
                        this.Settings.AddMostRecentlyUsedDatasetFilename(filename);
                        this.Settings.Save();
                    }
                });

        /// <summary>
        /// Gets the open recently used dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<string> OpenRecentlyUsedDatasetCommand
            => this.openRecentlyUsedDatasetCommand ??= new RelayCommand<string>(
                async (filename) =>
                {
                    try
                    {
                        await VisualizationContext.Instance.OpenDatasetOrStoreAsync(filename, true, this.Settings.AutoSaveDatasets);
                        this.Settings.AddMostRecentlyUsedDatasetFilename(filename);
                    }
                    catch (Exception ex)
                    {
                        new MessageBoxWindow(Application.Current.MainWindow, "Error Opening Dataset", ex.Message, "Close", null).ShowDialog();

                        // Remove dataset from most recently used list if it failed to open
                        this.Settings.MostRecentlyUsedDatasetFilenames.Remove(filename);
                    }

                    // Save any changes to the most recently used dataset list
                    this.Settings.Save();
                });

        /// <summary>
        /// Gets the open recently used store command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<string> OpenRecentlyUsedStoreCommand
            => this.openRecentlyUsedStoreCommand ??= new RelayCommand<string>(
                async (filename) =>
                {
                    try
                    {
                        await VisualizationContext.Instance.OpenDatasetOrStoreAsync(filename, true, this.Settings.AutoSaveDatasets);
                        this.Settings.AddMostRecentlyUsedStoreFilename(filename);
                    }
                    catch (Exception ex)
                    {
                        new MessageBoxWindow(Application.Current.MainWindow, "Error Opening Store", ex.Message, "Close", null).ShowDialog();

                        // Remove store from most recently used list if it failed to open
                        this.Settings.MostRecentlyUsedStoreFilenames.Remove(filename);
                    }

                    // Save any changes to the most recently used store list
                    this.Settings.Save();
                });

        /// <summary>
        /// Gets the save dataset command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveDatasetAsCommand
            => this.saveDatasetAsCommand ??= new RelayCommand(
                async () =>
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        DefaultExt = ".pds",
                        Filter = "Psi Dataset (.pds)|*.pds",
                    };

                    bool? result = saveFileDialog.ShowDialog(Application.Current.MainWindow);
                    if (result == true)
                    {
                        string filename = saveFileDialog.FileName;

                        // this should be a relatively quick operation so no need to show progress
                        await VisualizationContext.Instance.DatasetViewModel.SaveAsAsync(filename);
                        this.Settings.AddMostRecentlyUsedDatasetFilename(filename);
                        this.Settings.Save();
                    }
                });

        /// <summary>
        /// Gets the insert timeline panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand InsertTimelinePanelCommand
            => this.insertTimelinePanelCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.VisualizationContainer.AddPanel(new TimelineVisualizationPanel()));

        /// <summary>
        /// Gets the insert 1 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert1CellInstantPanelCommand
            => this.insert1CellInstantPanelCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(1)));

        /// <summary>
        /// Gets the insert 2 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert2CellInstantPanelCommand
            => this.insert2CellInstantPanelCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(2)));

        /// <summary>
        /// Gets the insert 3 cell instant panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand Insert3CellInstantPanelCommand
            => this.insert3CellInstantPanelCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.VisualizationContainer.AddPanel(new InstantVisualizationContainer(3)));

        /// <summary>
        /// Gets the zoom to session extents command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSessionExtentsCommand
            => this.zoomToSessionExtentsCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.ZoomToDataRange(),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the zoom to selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSelectionCommand
            => this.zoomToSelectionCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.ZoomToSelection(),
                () => this.VisualizationContainer.Navigator.CanZoomToSelection());

        /// <summary>
        /// Gets the move selection left command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveSelectionLeftCommand
            => this.moveSelectionLeftCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveSelectionLeft(),
                () => this.VisualizationContainer.Navigator.CanMoveSelectionLeft());

        /// <summary>
        /// Gets the move selection right command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveSelectionRightCommand
            => this.moveSelectionRightCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveSelectionRight(),
                () => this.VisualizationContainer.Navigator.CanMoveSelectionRight());

        /// <summary>
        /// Gets the move selection to previous annotation command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveSelectionToPreviousAnnotationCommand
            => this.moveSelectionToPreviousAnnotationCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveSelectionToPreviousAnnotation(),
                () => this.VisualizationContainer.Navigator.CanMoveSelectionToPreviousAnnotation());

        /// <summary>
        /// Gets the move selection to next annotation command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveSelectionToNextAnnotationCommand
            => this.moveSelectionToNextAnnotationCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveSelectionToNextAnnotation(),
                () => this.VisualizationContainer.Navigator.CanMoveSelectionToNextAnnotation());

        /// <summary>
        /// Gets the clear selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearSelectionCommand
            => this.clearSelectionCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.ClearSelection(),
                () => this.VisualizationContainer.Navigator.CanClearSelection());

        /// <summary>
        /// Gets the command to move the cursor to the selection start.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveCursorToSelectionStartCommand
            => this.moveToSelectionStartCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveCursorToSelectionStart(),
                () => this.VisualizationContainer.Navigator.CanMoveCursorToSelectionStart());

        /// <summary>
        /// Gets the toggle play repeat command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand TogglePlayRepeatCommand
            => this.togglePlayRepeatCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.RepeatPlayback = !this.VisualizationContainer.Navigator.RepeatPlayback,
                () => VisualizationContext.Instance.IsDatasetLoaded());

        /// <summary>
        /// Gets the command to move the cursor to the selection end.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MoveCursorToSelectionEndCommand
            => this.moveToSelectionEndCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.MoveCursorToSelectionEnd(),
                () => this.VisualizationContainer.Navigator.CanMoveCursorToSelectionEnd());

        /// <summary>
        /// Gets the increase play speed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand IncreasePlaySpeedCommand
            => this.increasePlaySpeedCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.PlaySpeed *= 2,
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the decrease play speed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DecreasePlaySpeedCommand
            => this.decreasePlaySpeedCommand ??= new RelayCommand(
                () => this.VisualizationContainer.Navigator.PlaySpeed /= 2,
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.VisualizationContainer.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the toggle live mode command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleLiveModeCommand
            => this.toggleLiveModeCommand ??= new RelayCommand(
                () => VisualizationContext.Instance.ToggleLiveMode(),
                () => VisualizationContext.Instance.IsDatasetLoaded() && VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel?.ContainsLivePartitions == true);

        /// <summary>
        /// Gets the save layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveLayoutCommand
            => this.saveLayoutCommand ??= new RelayCommand(this.SaveLayout);

        /// <summary>
        /// Gets the save layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SaveLayoutAsCommand
            => this.saveLayoutAsCommand ??= new RelayCommand(this.SaveLayoutAs);

        /// <summary>
        /// Gets the clear layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearLayoutCommand
            => this.clearLayoutCommand ??= new RelayCommand(this.ClearLayout);

        /// <summary>
        /// Gets the delete layout command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand DeleteLayoutCommand
            => this.deleteLayoutCommand ??= new RelayCommand(this.DeleteLayout, () => this.CurrentLayout != this.AvailableLayouts[0]);

        /// <summary>
        /// Gets the expand all command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExpandDatasetsTreeCommand
            => this.expandDatasetsTreeCommand ??= new RelayCommand(this.ExpandDatasetsTree);

        /// <summary>
        /// Gets the collapse all command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CollapseDatasetsTreeCommand
            => this.collapseDatasetsTreeCommand ??= new RelayCommand(this.CollapseDatasetsTree);

        /// <summary>
        /// Gets the expand visualizations tree command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExpandVisualizationsTreeCommand
            => this.expandVisualizationsTreeCommand ??= new RelayCommand(this.ExpandVisualizationsTree);

        /// <summary>
        /// Gets the collapse visualizations tree command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CollapseVisualizationsTreeCommand
            => this.collapseVisualizationsTreeCommand ??= new RelayCommand(this.CollapseVisualizationsTree);

        /// <summary>
        /// Gets the synchronize trees command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SynchronizeTreesCommand
            => this.synchronizeTreesCommand ??= new RelayCommand(this.SynchronizeDatasetsTreeToVisualizationsTree);

        /// <summary>
        /// Gets the selected visualization changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedPropertyChangedEventArgs<object>> SelectedVisualizationChangedCommand
            => this.selectedVisualizationChangedCommand ??= new RelayCommand<RoutedPropertyChangedEventArgs<object>>(
                e =>
                {
                    if (e.NewValue is VisualizationPanel visualizationPanel)
                    {
                        this.VisualizationContainer.CurrentPanel = visualizationPanel;
                    }
                    else if (e.NewValue is VisualizationObject visualizationObject)
                    {
                        this.VisualizationContainer.CurrentPanel = visualizationObject.Panel;
                        visualizationObject.Panel.CurrentVisualizationObject = visualizationObject;
                    }

                    this.selectedVisualization = e.NewValue;
                    this.SelectedPropertiesObject = e.NewValue;
                    e.Handled = true;
                });

        /// <summary>
        /// Gets the selected dataset changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedPropertyChangedEventArgs<object>> SelectedDatasetChangedCommand
            => this.selectedDatasetChangedCommand ??= new RelayCommand<RoutedPropertyChangedEventArgs<object>>(
                e =>
                {
                    this.selectedDatasetObject = e.NewValue;
                    this.SelectedPropertiesObject = e.NewValue;
                    e.Handled = true;
                });

        /// <summary>
        /// Gets the command that executes after the user clicks on either the datasets or the visualizations tree views.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<string> TreeSelectedCommand
            => this.treeSelectedCommand ??= new RelayCommand<string>(
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

        /// <summary>
        /// Gets the closed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClosedCommand
            => this.closedCommand ??= new RelayCommand(
                () =>
                {
                    // Explicitly dispose the VisualizationContext to clean up resources before closing
                    VisualizationContext.Instance?.Dispose();

                    // Explicitly dispose so that DataManager doesn't keep the app running for a while longer.
                    DataManager.Instance?.Dispose();
                });

        /// <summary>
        /// Gets the exit command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ExitCommand
            => this.exitCommand ??= new RelayCommand(Application.Current.Shutdown);

        /// <summary>
        /// Gets the create annotation stream command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand CreateAnnotationStreamCommand
            => this.createAnnotationStreamCommand ??= new RelayCommand(this.CreateAnnotationStream);

        /// <summary>
        /// Gets the edit settings command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand EditSettingsCommand
            => this.editSettingsCommand ??= new RelayCommand(this.EditSettings);

        /// <summary>
        /// Gets the help command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand HelpCommand
            => this.helpCommand ??= new RelayCommand(this.Help);

        /// <summary>
        /// Gets the command for viewing the error log for additional assembly load.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ViewAdditionalAssemblyLoadErrorLogCommand
            => this.viewAdditionalAssemblyLoadErrorLogCommand ??= new RelayCommand(
                this.ViewAdditionalAssemblyLoadErrorLog,
                () => File.Exists(Path.Combine(PsiStudioLogsPath, "VisualizersLog.txt")));

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
                this.RaisePropertyChanging(nameof(this.AvailableLayouts));
                this.availableLayouts = value;
                this.RaisePropertyChanged(nameof(this.AvailableLayouts));
            }
        }

        /// <summary>
        /// Gets or sets the current layout.
        /// </summary>
        /// <remarks>
        /// Updates to CurrentLayout should be made via the SetCurrentLayout() method. The setter
        /// of this property is only intended to be called in response to changes in the view.
        /// </remarks>
        public LayoutInfo CurrentLayout
        {
            get => this.currentLayout;
            set
            {
                // Ignore re-entrant calls to set CurrentLayout which may occur if the list of
                // available layouts is modified by UpdateLayoutList() due to a new layout being
                // saved in the call to PromptSaveCurrentLayoutAndContinue() below.
                if (this.currentLayoutUpdating)
                {
                    return;
                }

                this.currentLayoutUpdating = true;

                // Do not update CurrentLayout if it is being set to null as this is a transitory
                // state caused by AvailableLayouts being updated in UpdateLayoutList(). Because
                // AvailableLayouts and CurrentLayout are bound to the ComboBox's ItemsSource and
                // SelectedItem properties respectively, SelectedItem will be set to null when
                // ItemsSource is updated and SelectedItem is no longer present in the new list.
                // Typically this happens when the current layout is deleted. In this case, the
                // view model will call SetCurrentLayout() to set CurrentLayout to the new value.
                if (value != null)
                {
                    // Save the current layout if necessary before switching to the new one. If the user
                    // cancels the save, reset the selected layout in the view to the current value.
                    if (this.PromptSaveCurrentLayoutAndContinue())
                    {
                        this.SetCurrentLayout(value);
                    }
                    else
                    {
                        // If user cancelled the save, reset the selected layout in the view to the current value
                        value = this.currentLayout;

                        // Since we're may already be handling the SelectedItem change, we need to invoke this change
                        // asynchronously as a separate operation to ensure that the UI is updated correctly.
                        Application.Current?.Dispatcher.InvokeAsync(
                            () =>
                            {
                                // In order to force the view to update the selected layout, we need to set it to null first
                                this.currentLayout = null;
                                this.RaisePropertyChanged(nameof(this.CurrentLayout));
                                this.currentLayout = value;
                                this.RaisePropertyChanged(nameof(this.CurrentLayout));
                            });
                    }
                }

                this.currentLayoutUpdating = false;
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

            // Prompt the user to save the current layout
            if (!this.PromptSaveCurrentLayoutAndContinue())
            {
                return false;
            }

            // Put the current state of the timing buttons into the settings object
            this.Settings.ShowAbsoluteTiming = this.VisualizationContainer.Navigator.ShowAbsoluteTiming;
            this.Settings.ShowTimingRelativeToSessionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart;
            this.Settings.ShowTimingRelativeToSelectionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart;

            // Save the settings
            this.Settings.Save();

            return true;
        }

        /// <summary>
        /// Creates a new annotation stream in a partition.
        /// </summary>
        public async void CreateAnnotationStream()
        {
            // Ensure there is a current session.
            var currentSession = VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel;
            if (currentSession == null)
            {
                return;
            }

            var createAnnotationStreamWindow = new CreateAnnotationStreamWindow(currentSession, this.annotationSchemas, Application.Current.MainWindow);
            if (createAnnotationStreamWindow.ShowDialog() == true)
            {
                var annotationSchema = createAnnotationStreamWindow.SelectedAnnotationSchema;
                string streamName = createAnnotationStreamWindow.StreamName;
                string storeName;
                string storePath;

                if (createAnnotationStreamWindow.UseExistingPartition)
                {
                    // Get the partition that the stream will be created in
                    var partitionViewModel = currentSession.PartitionViewModels.FirstOrDefault(p => p.Name == createAnnotationStreamWindow.ExistingPartitionName);

                    // Make note of the partition's name and path so we can reload it later
                    storeName = partitionViewModel.StoreName;
                    storePath = partitionViewModel.StorePath;

                    // Attempt to remove the partition from the session.  If the partition contains unsaved changes, then the user will be
                    // prompted to save the changes first.  The user may elect to cancel the entire operation, in which case we cannot continue.
                    if (!partitionViewModel.RemovePartition())
                    {
                        return;
                    }

                    // Unbind any visualization objects from the store.
                    this.VisualizationContainer.UnbindVisualizationObjectsFromStore(partitionViewModel.StoreName, partitionViewModel.StorePath, partitionViewModel.Name);
                }
                else
                {
                    // Make note of the new partition's name and path
                    storeName = createAnnotationStreamWindow.StoreName;
                    storePath = createAnnotationStreamWindow.StorePath;
                }

                await ProgressWindow.RunWithProgressAsync(
                    $"Creating annotations stream {createAnnotationStreamWindow.StreamName} ...",
                    async progress =>
                    {
                        if (createAnnotationStreamWindow.UseExistingPartition)
                        {
                            // Add the empty annotations stream to the existing partition
                            await Task.Run(() => PsiStore.AddStreamInPlace<TimeIntervalAnnotationSet, AnnotationSchema>(
                                (storeName, storePath),
                                streamName,
                                annotationSchema,
                                true,
                                new Progress<double>(p => progress.Report(("Adding stream to existing store ...", p * 0.95)))));
                        }
                        else
                        {
                            // Create the new partition with the empty annotations stream
                            await Task.Run(() => PsiStore.CreateWithStream<TimeIntervalAnnotationSet, AnnotationSchema>(
                                storeName,
                                storePath,
                                streamName,
                                annotationSchema,
                                new Progress<double>(p => progress.Report(("Creating \\psi store ...", p * 0.95)))));
                        }

                        // Add the partition to the session
                        await currentSession.AddPartitionAsync(
                            new PsiStoreStreamReader(storeName, storePath),
                            progress: new Progress<(string Status, double Value)>(p => progress.Report((p.Status, 0.95 + p.Value * 0.05))));
                    });

                // Update the source bindings for all visualization objects in the current session
                this.VisualizationContainer.UpdateStreamSources(currentSession);
            }
        }

        private void MoveCursorBy(TimeSpan timeSpan, NearestType nearestType)
        {
            var visContainer = this.VisualizationContainer;
            var nav = visContainer.Navigator;
            var time = nav.Cursor + timeSpan;
            if (visContainer.SnapToVisualizationObject is IStreamVisualizationObject vo)
            {
                nav.MoveCursorTo(DataManager.Instance.GetTimeOfNearestMessage(vo.StreamSource, time, nearestType) ?? time);
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
                // Attempt to open the current layout. User consent may be needed for layouts containing scripts.
                this.userConsentObtained.TryGetValue(this.CurrentLayout.Name, out bool userConsent);
                bool success = VisualizationContext.Instance.OpenLayout(this.CurrentLayout.Path, this.CurrentLayout.Name, ref userConsent);
                if (!success)
                {
                    // If the load failed, load the default layout instead.  This method
                    // may have been initially called by the SelectedItemChanged handler
                    // from the Layouts combobox, and it's bound to CurrentLayout, so
                    // we need to asynchronously dispatch a message to change its value
                    // back rather than set it directly here.
                    Application.Current?.Dispatcher.InvokeAsync(() => this.SetCurrentLayout(this.AvailableLayouts[0]));
                }
                else
                {
                    this.userConsentObtained[this.CurrentLayout.Name] = userConsent;
                }
            }
        }

        /// <summary>
        /// Sets the current layout to a new value without first prompting to save the current layout.
        /// </summary>
        /// <param name="layout">The new layout to set the current layout to.</param>
        private void SetCurrentLayout(LayoutInfo layout)
        {
            this.currentLayout = layout;

            if (this.currentLayout == null || this.currentLayout.Name == this.newLayoutName)
            {
                this.Settings.MostRecentlyUsedLayoutName = null;
            }
            else
            {
                this.Settings.MostRecentlyUsedLayoutName = this.currentLayout.Name;
            }

            if (this.currentLayout != null && this.isInitialized)
            {
                this.OpenCurrentLayout();

                // Save changes to the most recently used layout
                this.Settings.Save();
            }

            this.RaisePropertyChanged(nameof(this.CurrentLayout));
        }

        private bool PromptSaveCurrentLayoutAndContinue()
        {
            // Never save if the SaveLayoutBehavior setting is set to Manual
            if (this.currentLayout != null && this.Settings.SaveLayoutBehavior != SaveBehavior.Manual)
            {
                // Check for changes to the current layout compared to what's on disk
                if (this.currentLayout.Name != this.newLayoutName)
                {
                    try
                    {
                        string currentLayoutJson = this.VisualizationContainer.SerializeToJson();

                        // read all contents of CurrentLayout.Path
                        string savedLayoutJson = File.ReadAllText(this.currentLayout.Path);

                        if (currentLayoutJson == savedLayoutJson)
                        {
                            // No changes, so continue without saving
                            return true;
                        }
                    }
                    catch
                    {
                        // If there was an error comparing with the file, we'll just assume that the layout has changed
                    }
                }
                else
                {
                    // If the current layout is a new layout, then we only need to save if there are panels in the container
                    if (!this.VisualizationContainer.Panels.Any())
                    {
                        // Empty new layout, so continue without saving
                        return true;
                    }
                }

                // Always prompt to save if this is a new layout, otherwise prompt based on the SaveLayoutBehavior setting
                bool promptToSave = this.currentLayout.Name == this.newLayoutName || this.Settings.SaveLayoutBehavior == SaveBehavior.Prompt;

                if (promptToSave)
                {
                    var confirmSaveLayout = new ConfirmOperationWindow(
                        Application.Current.MainWindow,
                        "Save Current Layout",
                        "There are unsaved changes in the current layout. Do you wish to save these changes to disk before continuing?");

                    confirmSaveLayout.ShowDialog();
                    switch (confirmSaveLayout.UserSelection)
                    {
                        case ConfirmOperationWindow.ConfirmOperationResult.Yes:
                            this.SaveLayout();
                            break;

                        case ConfirmOperationWindow.ConfirmOperationResult.No:
                            // Continue without saving
                            break;

                        default:
                            // Cancel the operation by returning false
                            return false;
                    }
                }
                else
                {
                    // Save without prompting
                    this.SaveLayout();
                }
            }

            // Continue
            return true;
        }

        private async void OnMainWindowContentRendered(object sender, EventArgs e)
        {
            if (!this.isInitialized)
            {
                this.isInitialized = true;

                // Initialize the visualizer map
                this.InitializeVisualizerMap();

                // Load the available annotation schemas
                this.LoadAnnotationSchemas();

                // Open the current layout
                this.OpenCurrentLayout();

                // Check if the name of a psi store was specified on the command line, and if so, load the store.
                // First arg is this exe's filename, second arg (if it exists) is the store to open
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    await VisualizationContext.Instance.OpenDatasetOrStoreAsync(args[1], true, this.Settings.AutoSaveDatasets);
                }
                else if (this.Settings.AutoLoadMostRecentlyUsedFileOnStartUp)
                {
                    if (this.Settings.MostRecentlyUsedFileIsDataset &&
                        this.Settings.MostRecentlyUsedDatasetFilenames != null &&
                        this.Settings.MostRecentlyUsedDatasetFilenames.Any())
                    {
                        try
                        {
                            await VisualizationContext.Instance.OpenDatasetOrStoreAsync(
                                this.Settings.MostRecentlyUsedDatasetFilenames.First(),
                                true,
                                this.Settings.AutoSaveDatasets);
                        }
                        catch (Exception ex)
                        {
                            new MessageBoxWindow(Application.Current.MainWindow, "Error Opening Dataset", ex.Message, "Close", null).ShowDialog();

                            // Remove dataset from most recently used list if it failed to open
                            this.Settings.MostRecentlyUsedDatasetFilenames.RemoveAt(0);
                            this.Settings.Save();
                        }
                    }
                    else if (!this.Settings.MostRecentlyUsedFileIsDataset &&
                        this.Settings.MostRecentlyUsedStoreFilenames != null &&
                        this.Settings.MostRecentlyUsedStoreFilenames.Any())
                    {
                        try
                        {
                            await VisualizationContext.Instance.OpenDatasetOrStoreAsync(
                                this.Settings.MostRecentlyUsedStoreFilenames.First(),
                                true,
                                this.Settings.AutoSaveDatasets);
                        }
                        catch (Exception ex)
                        {
                            new MessageBoxWindow(Application.Current.MainWindow, "Error Opening Store", ex.Message, "Close", null).ShowDialog();

                            // Remove store from most recently used list if it failed to open
                            this.Settings.MostRecentlyUsedStoreFilenames.RemoveAt(0);
                            this.Settings.Save();
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
            var additionalAssemblies = new List<string>();

            // If we have any additional assemblies to search for visualization
            // classes, display the security warning before proceeding.
            if ((this.Settings.AdditionalAssemblies != null) && (this.Settings.AdditionalAssemblies.Count > 0))
            {
                if (!this.Settings.ShowSecurityWarningOnLoadingThirdPartyCode ||
                    new AdditionalAssembliesWindow(Application.Current.MainWindow, this.Settings.AdditionalAssemblies).ShowDialog() == true)
                {
                    additionalAssemblies.AddRange(this.Settings.AdditionalAssemblies);
                }
            }

            // Initialize the visualizer map
            VisualizationContext.Instance.PluginMap.Initialize(
                additionalAssemblies,
                this.Settings.TypeMappings,
                Path.Combine(PsiStudioLogsPath, "VisualizersLog.txt"),
                this.Settings.ShowErrorLogOnLoadingAdditionalAssemblies,
                PsiStudioBatchProcessingTaskConfigurationsPath);
        }

        private void UpdateLayoutList()
        {
            // Create a new collection of layouts
            var layouts = new List<LayoutInfo>
            {
                new (this.newLayoutName, null),
            };

            // Create the layouts directory if it doesn't already exist
            var directoryInfo = new DirectoryInfo(PsiStudioLayoutsPath);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(PsiStudioLayoutsPath);
            }

            // Find all the layout files and add them to the list of available layouts
            var files = directoryInfo.GetFiles("*.plo");
            foreach (FileInfo fileInfo in files)
            {
                layouts.Add(new LayoutInfo(Path.GetFileNameWithoutExtension(fileInfo.FullName), fileInfo.FullName));
            }

            // Set the list of available layouts
            this.AvailableLayouts = layouts;

            // Set the most recently used layout if it's in the available layouts, otherwise make "new layout" the current layout
            var mostRecentlyUsedLayout = this.AvailableLayouts.FirstOrDefault(l => l.Name == this.Settings.MostRecentlyUsedLayoutName);
            this.SetCurrentLayout(mostRecentlyUsedLayout ?? this.AvailableLayouts[0]);
        }

        private void SaveLayout()
        {
            if (this.CurrentLayout.Name == this.newLayoutName)
            {
                this.SaveLayoutAs();
            }
            else
            {
                this.VisualizationContainer.Save(this.CurrentLayout.Path);
            }
        }

        private void SaveLayoutAs()
        {
            var layoutNameWindow = new LayoutNameWindow(Application.Current.MainWindow, PsiStudioLayoutsPath);

            bool? result = layoutNameWindow.ShowDialog();
            if (result == true)
            {
                string fileName = Path.Combine(PsiStudioLayoutsPath, layoutNameWindow.LayoutName);

                // Save the layout
                this.VisualizationContainer.Save(fileName);

                // Recreate the layout list
                this.UpdateLayoutList();

                // Set the current layout
                this.SetCurrentLayout(this.AvailableLayouts.First(l => l.Path == fileName));
            }
        }

        private void ClearLayout()
        {
            // Clear the visualization container
            this.VisualizationContainer.Clear();
        }

        private void DeleteLayout()
        {
            var result = new MessageBoxWindow(
                Application.Current.MainWindow,
                "Are you sure?",
                $"Are you sure you want to delete the layout called \"{this.CurrentLayout.Name}\"? This will permanently delete it from disk.",
                "Yes",
                "Cancel").ShowDialog();

            if (result == true)
            {
                var layoutName = this.CurrentLayout.Name;
                File.Delete(Path.Combine(PsiStudioLayoutsPath, $"{layoutName}.plo"));
                this.UpdateLayoutList();
            }
        }

        private void LoadAnnotationSchemas()
        {
            this.annotationSchemas = new ();

            // Create the annotations definitions directory if it doesn't already exist
            var directoryInfo = new DirectoryInfo(PsiStudioAnnotationSchemasPath);
            if (!directoryInfo.Exists)
            {
                Directory.CreateDirectory(PsiStudioAnnotationSchemasPath);
            }

            // Keep a list of annotation schemas that failed to load
            var annotationSchemaLoadFailures = new List<string>();

            // Find all the annotation schema files and add them to the list
            var fileInfos = directoryInfo.GetFiles("*.schema.json");
            foreach (var fileInfo in fileInfos)
            {
                if (AnnotationSchema.TryLoadFrom(fileInfo.FullName, out var annotationSchema))
                {
                    this.annotationSchemas.Add(annotationSchema);
                }
                else
                {
                    annotationSchemaLoadFailures.Add(fileInfo.FullName);
                }
            }

            if (annotationSchemaLoadFailures.Count > 0)
            {
                this.ReportAnnotationSchemaLoadFailures(annotationSchemaLoadFailures);
            }
        }

        private void ReportAnnotationSchemaLoadFailures(List<string> annotationSchemaLoadFailures)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine("The following annotation schemas could not be loaded (please check that they are in the correct format and that all the required types are available to PsiStudio):");
            errorMessage.AppendLine();
            foreach (string annotationSchemaLoadFailure in annotationSchemaLoadFailures)
            {
                var fileInfo = new FileInfo(annotationSchemaLoadFailure);
                errorMessage.AppendLine(fileInfo.Name);
            }

            new MessageBoxWindow(Application.Current.MainWindow, "Annotation Schema Load Error", errorMessage.ToString(), "Close", null).ShowDialog();
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
                            partitionViewModel.RootStreamTreeNode.ExpandAll();
                        }
                        else
                        {
                            partitionViewModel.RootStreamTreeNode.CollapseAll();
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
                if (this.selectedVisualization is IStreamVisualizationObject streamVisualizationObject)
                {
                    var streamBinding = streamVisualizationObject.StreamBinding;
                    var partitionViewModel = VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel.PartitionViewModels.FirstOrDefault(p => p.Name == streamBinding.PartitionName);
                    if (partitionViewModel != null)
                    {
                        if (partitionViewModel.SelectStreamTreeNode(streamBinding.StreamName))
                        {
                            VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel.IsTreeNodeExpanded = true;
                            VisualizationContext.Instance.DatasetViewModel.IsTreeNodeExpanded = true;
                            return;
                        }
                    }
                }
            }
        }

        private void OnVisualizationContextPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.VisualizationContainer))
            {
                // Unhook property changed events from old visualization container
                if (VisualizationContext.Instance.VisualizationContainer != null)
                {
                    VisualizationContext.Instance.VisualizationContainer.PropertyChanged -= this.OnVisualizationContainerPropertyChanged;
                }

                this.RaisePropertyChanging(nameof(this.VisualizationContainer));
            }
            else if (e.PropertyName == nameof(VisualizationContext.DatasetViewModel))
            {
                // Unhook property changed events from old dataset view model
                if (VisualizationContext.Instance.DatasetViewModel != null)
                {
                    VisualizationContext.Instance.DatasetViewModel.PropertyChanged -= this.OnDatasetViewModelPropertyChanged;
                }

                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void OnVisualizationContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.VisualizationContainer))
            {
                // Hook property changed events to new visualization container
                if (VisualizationContext.Instance.VisualizationContainer != null)
                {
                    VisualizationContext.Instance.VisualizationContainer.PropertyChanged += this.OnVisualizationContainerPropertyChanged;

                    // Update the window title to reflect any change in the snap-to stream
                    this.RaisePropertyChanged(nameof(this.TitleText));
                }

                this.RaisePropertyChanged(nameof(this.VisualizationContainer));
            }
            else if (e.PropertyName == nameof(VisualizationContext.DatasetViewModel))
            {
                // Hook property changed events to new dataset view model
                if (VisualizationContext.Instance.DatasetViewModel != null)
                {
                    VisualizationContext.Instance.DatasetViewModel.PropertyChanged += this.OnDatasetViewModelPropertyChanged;
                }

                // Update the window title to reflect the new dataset
                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void OnNavigatorPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Navigator.ShowAbsoluteTiming))
            {
                this.Settings.ShowAbsoluteTiming = this.VisualizationContainer.Navigator.ShowAbsoluteTiming;
            }
            else if (e.PropertyName == nameof(Navigator.ShowTimingRelativeToSelectionStart))
            {
                this.Settings.ShowTimingRelativeToSelectionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart;
            }
            else if (e.PropertyName == nameof(Navigator.ShowTimingRelativeToSessionStart))
            {
                this.Settings.ShowTimingRelativeToSessionStart = this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart;
            }
        }

        private void OnDatasetViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DatasetViewModel.Name))
            {
                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void OnVisualizationContainerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject))
            {
                this.RaisePropertyChanged(nameof(this.TitleText));
            }
        }

        private void EditSettings()
        {
            var psiStudioSettingsWindow = new PsiStudioSettingsWindow(Application.Current.MainWindow)
            {
                SettingsViewModel = new PsiStudioSettingsViewModel(this.Settings),
            };

            if (psiStudioSettingsWindow.ShowDialog() == true)
            {
                bool requiresRestart = psiStudioSettingsWindow.SettingsViewModel.UpdateSettings(this.Settings);
                this.VisualizationContainer.Navigator.ShowAbsoluteTiming = this.Settings.ShowAbsoluteTiming;
                this.VisualizationContainer.Navigator.ShowTimingRelativeToSelectionStart = this.Settings.ShowTimingRelativeToSelectionStart;
                this.VisualizationContainer.Navigator.ShowTimingRelativeToSessionStart = this.Settings.ShowTimingRelativeToSessionStart;
                this.Settings.Save();

                if (requiresRestart)
                {
                    new MessageBoxWindow(
                        Application.Current.MainWindow,
                        "Information",
                        "Some of the changes you have made to the settings will only take effect on the next start of Platform for Situated Intelligence Studio.",
                        "OK",
                        null)
                        .ShowDialog();
                }
            }
        }

        private void Help()
        {
            var helpWindow = new HelpWindow(Application.Current.MainWindow);

            helpWindow.ShowDialog();
        }

        private void ViewAdditionalAssemblyLoadErrorLog()
        {
            string logFilePath = Path.Combine(PsiStudioLogsPath, "VisualizersLog.txt");
            if (File.Exists(logFilePath))
            {
                Process.Start("notepad.exe", logFilePath);
            }
        }
    }
}
