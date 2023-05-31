// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
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
    using System.Windows.Data;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Serialization;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the container where all visualization panels are hosted. The is the root UI element for visualizations.
    /// </summary>
    public class VisualizationContainer : ObservableObject, IContextMenuItemsSource, IDisposable
    {
        // Property names used in the layout (*.plo) files
        private const string LayoutPropertyName = "Layout";
        private const string LayoutVersionPropertyName = "LayoutVersion";

        // Current Visualization Container version
        private const double CurrentVisualizationContainerVersion = 5.0d;

        private RelayCommand goToTimeCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand clearSelectionCommand;
        private RelayCommand<VisualizationPanel> deleteVisualizationPanelCommand;

        /// <summary>
        /// The time navigator view model.
        /// </summary>
        private Navigator navigator;

        /// <summary>
        /// The collection of visualization Panels.
        /// </summary>
        private ObservableCollection<VisualizationPanel> panels;

        /// <summary>
        /// Multithreaded collection lock.
        /// </summary>
        private object panelsLock;

        /// <summary>
        /// The current visualization panel.
        /// </summary>
        private VisualizationPanel currentPanel;

        /// <summary>
        /// The current visualization object (if any) currently being snapped to.
        /// </summary>
        private VisualizationObject snapToVisualizationObject;

        /// <summary>
        /// True if the navigator should be visible, otherwise false.
        /// </summary>
        private bool showNavigator = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationContainer"/> class.
        /// </summary>
        public VisualizationContainer()
        {
            this.navigator = new Navigator();
            this.panels = new ObservableCollection<VisualizationPanel>();
            this.InitNew();
        }

        /// <summary>
        /// Gets the zoom to selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSelectionCommand
            => this.zoomToSelectionCommand ??= new RelayCommand(
                () => this.Navigator.ZoomToSelection(),
                () => this.Navigator.CanZoomToSelection());

        /// <summary>
        /// Gets the clear selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearSelectionCommand
            => this.clearSelectionCommand ??= new RelayCommand(
                () => this.Navigator.ClearSelection(),
                () => this.Navigator.CanClearSelection());

        /// <summary>
        /// Gets the zoom to session extents command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSessionExtentsCommand
            => this.zoomToSessionExtentsCommand ??= new RelayCommand(
                () => this.Navigator.ZoomToDataRange(),
                () => VisualizationContext.Instance.IsDatasetLoaded() && this.Navigator.CursorMode != CursorMode.Live);

        /// <summary>
        /// Gets the go to time command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand GoToTimeCommand
            => this.goToTimeCommand ??= new RelayCommand(() => this.GoToTime());

        /// <summary>
        /// Gets or sets the current visualization panel.
        /// </summary>
        [IgnoreDataMember]
        public VisualizationPanel CurrentPanel
        {
            get { return this.currentPanel; }

            set
            {
                if (this.currentPanel != value)
                {
                    this.Set(nameof(this.CurrentPanel), ref this.currentPanel, value);
                }
            }
        }

        /// <summary>
        /// Gets the current navigator.
        /// </summary>
        [IgnoreDataMember]
        public Navigator Navigator
        {
            get { return this.navigator; }
            private set { this.Set(nameof(this.Navigator), ref this.navigator, (Navigator)value); }
        }

        /// <summary>
        /// Gets the visualization Panels.
        /// </summary>
        [DataMember]
        public ObservableCollection<VisualizationPanel> Panels
        {
            get { return this.panels; }
            private set { this.Set(nameof(this.Panels), ref this.panels, value); }
        }

        /// <summary>
        /// Gets or sets the visualization object that the mouse pointer currently snaps to.
        /// </summary>
        [DataMember]
        public VisualizationObject SnapToVisualizationObject
        {
            get { return this.snapToVisualizationObject; }

            set
            {
                this.RaisePropertyChanging(nameof(this.SnapToVisualizationObject));
                this.snapToVisualizationObject = value;
                this.RaisePropertyChanged(nameof(this.SnapToVisualizationObject));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the navigator should be displayed.
        /// </summary>
        public bool ShowNavigator
        {
            get => this.showNavigator;
            set => this.Set(nameof(this.ShowNavigator), ref this.showNavigator, value);
        }

        /// <summary>
        /// Gets the delete visualization panel command.
        /// </summary>
        [IgnoreDataMember]
        public RelayCommand<VisualizationPanel> DeleteVisualizationPanelCommand
            => this.deleteVisualizationPanelCommand ??= new RelayCommand<VisualizationPanel>(o => this.RemovePanel(o));

        /// <summary>
        /// Loads a visualization layout from the specified file.
        /// </summary>
        /// <param name="filename">File to load visualization layout.</param>
        /// <param name="layoutName">The name of the layout.</param>
        /// <param name="currentVisualizationContainer">The current visualization container that will be replaced with the newly loaded one.</param>
        /// <returns>The new visualization container.</returns>
        public static VisualizationContainer Load(string filename, string layoutName, VisualizationContainer currentVisualizationContainer)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                });

            System.IO.StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(filename);
                using var jsonReader = new JsonTextReader(jsonFile);
                jsonFile = null;

                // Get the layout file version
                double layoutFileVersion = GetLayoutFileVersion(jsonReader);

                // Make sure it's a version we know how to deserialize (currently only version 3.0)
                if (layoutFileVersion != CurrentVisualizationContainerVersion)
                {
                    throw new ApplicationException("The layout could not be loaded because the file is invalid or is an unsupported version.");
                }

                // Find the "Layout" node
                if (SeekToLayoutElement(jsonReader))
                {
                    // Deserialize the visualization container
                    var visualizationContainer = serializer.Deserialize<VisualizationContainer>(jsonReader);

                    // Copy the settings from the current navigator to the new one.
                    if (currentVisualizationContainer != null)
                    {
                        visualizationContainer.Navigator.CopyFrom(currentVisualizationContainer.Navigator);
                    }

                    return visualizationContainer;
                }
                else
                {
                    throw new ApplicationException("No Layout element was found in the file");
                }
            }
            catch (Exception ex)
            {
                // Show the error message
                new MessageBoxWindow(Application.Current.MainWindow, "Error Loading Layout", CreateLayoutLoadErrorText(layoutName, ex), "Close", null).ShowDialog();
                return null;
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Ensure playback is stopped
            this.Navigator.SetCursorMode(CursorMode.Manual);

            // Clear container to give all panels a chance to clean up
            this.Clear();
        }

        /// <inheritdoc/>
        public List<ContextMenuItemInfo> ContextMenuItemsInfo()
            => new ()
            {
                new ContextMenuItemInfo(IconSourcePath.ZoomToSelection, "Zoom to Selection", this.ZoomToSelectionCommand),
                new ContextMenuItemInfo(IconSourcePath.ClearSelection, "Clear Selection", this.ClearSelectionCommand),
                new ContextMenuItemInfo(IconSourcePath.ZoomToSession, "Zoom to Session Extents", this.ZoomToSessionExtentsCommand),
            };

        /// <summary>
        /// Adds a new panel to the container.
        /// </summary>
        /// <param name="panel">The panel to be added to the container.</param>
        /// <param name="isRootChild">Flag indicating whether panel is root child.</param>
        public void AddPanel(VisualizationPanel panel, bool isRootChild = true)
        {
            panel.SetParentContainer(this);
            if (isRootChild)
            {
                this.Panels.Add(panel);
            }

            // Select the new panel, or its first child panel if the panel is an instant visualization container.
            if (panel is IInstantVisualizationContainer instantVisulizationContainer)
            {
                instantVisulizationContainer.Panels[0].IsTreeNodeSelected = true;
                this.CurrentPanel = instantVisulizationContainer.Panels[0];
            }
            else
            {
                panel.IsTreeNodeSelected = true;
                this.CurrentPanel = panel;
            }
        }

        /// <summary>
        /// Removes all Panels from the container.
        /// </summary>
        public void Clear()
        {
            foreach (var panel in this.Panels)
            {
                panel.Clear();
            }

            this.Panels.Clear();
            this.CurrentPanel = null;
        }

        /// <summary>
        /// Toggles the visibility of the indicated panel.
        /// </summary>
        /// <param name="panel">The panel for which to toggle visibility.</param>
        public void TogglePanelVisibility(VisualizationPanel panel)
        {
            // If we're about to make the current panel invisible, then set the current panel to null
            if (panel.Visible && this.CurrentPanel == panel)
            {
                this.CurrentPanel = null;
            }

            // Manage the parent if it's an instant visualization container
            if (panel.ParentPanel is InstantVisualizationContainer instantVisualizationContainer)
            {
                // If the parent container is visible, figure out if we need to toggle it.
                if (instantVisualizationContainer.Visible)
                {
                    // We need to toggle it we're about to make the current panel invisible and
                    // all the other panels are invisible
                    if (panel.Visible && instantVisualizationContainer.Panels.All(p => !p.Visible || p == panel))
                    {
                        this.TogglePanelVisibility(instantVisualizationContainer);
                    }
                }
                else
                {
                    // O/w the parent container is not visible, figure out if we need to toggle it.
                    // We need to toggle it we're about to make the current panel visible
                    if (!panel.Visible)
                    {
                        this.TogglePanelVisibility(instantVisualizationContainer);
                    }
                }
            }

            // Toggle the visibility
            panel.Visible = !panel.Visible;

            // if we have no selection, select the last panel
            if ((this.CurrentPanel == null) && (this.Panels.Count > 0))
            {
                this.CurrentPanel = this.Panels.Last();
            }
        }

        /// <summary>
        /// Removes the indicated panel.
        /// </summary>
        /// <param name="panel">The panel to be removed from the container.</param>
        public void RemovePanel(VisualizationPanel panel)
        {
            // change the current panel
            if (this.CurrentPanel == panel)
            {
                this.CurrentPanel = null;
            }

            // If the panel being deleted contains the stream currently being snapped to, then reset the snap to stream object
            if ((this.snapToVisualizationObject != null) && panel.VisualizationObjects.Contains(this.snapToVisualizationObject))
            {
                this.SnapToVisualizationObject = null;
            }

            // Remove all visualizations from the panel
            panel.Clear();

            // If the panel to be removed is the child of an instant visualization container
            // then ask the parent to remove it, otherwise remove it directly from this container.
            if (panel.ParentPanel is InstantVisualizationContainer instantVisualizationContainer)
            {
                instantVisualizationContainer.RemoveCell(panel);
                if (!instantVisualizationContainer.Panels.Any())
                {
                    this.Panels.Remove(instantVisualizationContainer);
                }
            }
            else
            {
                this.Panels.Remove(panel);
            }

            if ((this.CurrentPanel == null) && (this.Panels.Count > 0))
            {
                this.CurrentPanel = this.Panels.Last();
            }
        }

        /// <summary>
        /// Gets all of the visualization objects that visualize a derived stream, rather than a raw stream.
        /// </summary>
        /// <returns>The collection of visualization objects that visualize a derived stream.</returns>
        public List<IStreamVisualizationObject> GetDerivedStreamVisualizationObjects()
        {
            var derivedStreamVisualizationObjects = new List<IStreamVisualizationObject>();

            foreach (var visualizationPanel in this.Panels)
            {
                derivedStreamVisualizationObjects.AddRange(visualizationPanel.GetDerivedStreamVisualizationObjects());
            }

            return derivedStreamVisualizationObjects;
        }

        /// <summary>
        /// Saves the current layout to the specified file.
        /// </summary>
        /// <param name="filename">The file to save the layout too.</param>
        public void Save(string filename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    ContractResolver = new XYZValueVisualizationObjectContractResolver(),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(filename);
                using var jsonWriter = new JsonTextWriter(jsonFile);
                jsonFile = null;

                // Open the json
                jsonWriter.WriteStartObject();

                // Write the layout version
                jsonWriter.WritePropertyName(LayoutVersionPropertyName);
                jsonWriter.WriteValue(CurrentVisualizationContainerVersion);

                // Write the layout
                jsonWriter.WritePropertyName(LayoutPropertyName);
                serializer.Serialize(jsonWriter, this);

                // Close the json
                jsonWriter.WriteEndObject();
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Unbinds any visualization objects currently bound to a store.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="storePath">The path to the store.</param>
        /// <param name="partitionName">The partition name of the instance to unbind, or null to unbind all instances.</param>
        public void UnbindVisualizationObjectsFromStore(string storeName, string storePath, string partitionName)
        {
            foreach (var panel in this.Panels)
            {
                panel.UnbindVisualizationObjectsFromStore(storeName, storePath, partitionName);
            }
        }

        /// <summary>
        /// Update the stream sources for the visualization objects in this container.
        /// </summary>
        /// <param name="sessionViewModel">The currently active session view model.</param>
        public void UpdateStreamSources(SessionViewModel sessionViewModel)
        {
            // First, ensure any required derived stream tree nodes exist.
            sessionViewModel?.EnsureDerivedStreamTreeNodesExist(this);

            foreach (var panel in this.Panels)
            {
                // Update the stream sources for the panel
                panel.UpdateStreamSources(sessionViewModel);
            }
        }

        /// <summary>
        /// Zoom to the specified time interval.
        /// </summary>
        /// <param name="timeInterval">Time interval to zoom to.</param>
        public void ZoomToRange(TimeInterval timeInterval)
        {
            this.Navigator.ViewRange.Set(timeInterval.Left, timeInterval.Right);
            this.Navigator.Cursor = timeInterval.Left;
        }

        /// <summary>
        /// Goes to a time specified by the user.
        /// </summary>
        public void GoToTime()
        {
            var getTime = new GetParameterWindow(
                Application.Current.MainWindow,
                "Go To Time",
                "Time",
                string.Empty,
                value =>
                {
                    if (DateTime.TryParse(value, out var dateTime))
                    {
                        if (dateTime >= this.Navigator.DataRange.StartTime &&
                            dateTime <= this.Navigator.DataRange.EndTime)
                        {
                            return (true, string.Empty);
                        }
                        else
                        {
                            return (false, "The specified date-time is outside the range of the current session.");
                        }
                    }
                    else if (long.TryParse(value, out var ticks))
                    {
                        var ticksDateTime = new DateTime(ticks);
                        if (ticksDateTime >= this.Navigator.DataRange.StartTime &&
                            ticksDateTime <= this.Navigator.DataRange.EndTime)
                        {
                            return (true, string.Empty);
                        }
                        else
                        {
                            return (false, "The specified date-time is outside the range of the current session.");
                        }
                    }
                    else
                    {
                        return (false, "Cannot convert the specified time to a valid date time.");
                    }
                });

            if (getTime.ShowDialog() == true)
            {
                var cursor = default(DateTime);
                if (DateTime.TryParse(getTime.ParameterValue, out var dateTime))
                {
                    cursor = dateTime;
                }
                else if (long.TryParse(getTime.ParameterValue, out var ticks))
                {
                    cursor = new DateTime(ticks);
                }

                // If the cursor falls outside the current view range, shift the view range
                if (cursor <= this.Navigator.ViewRange.StartTime)
                {
                    var viewDurationTicks = this.Navigator.ViewRange.Duration.Ticks;
                    var viewStartTime = cursor - TimeSpan.FromTicks(viewDurationTicks / 2);
                    if (viewStartTime < this.Navigator.DataRange.StartTime)
                    {
                        viewStartTime = this.Navigator.DataRange.StartTime;
                    }

                    var viewEndTime = viewStartTime + this.Navigator.ViewRange.Duration;
                    this.Navigator.ViewRange.Set(viewStartTime, viewEndTime);
                }
                else if (cursor >= this.Navigator.ViewRange.EndTime)
                {
                    var viewDurationTicks = this.Navigator.ViewRange.Duration.Ticks;
                    var viewEndTime = cursor + TimeSpan.FromTicks(viewDurationTicks / 2);
                    if (viewEndTime > this.Navigator.DataRange.EndTime)
                    {
                        viewEndTime = this.Navigator.DataRange.EndTime;
                    }

                    var viewStartTime = viewEndTime - this.Navigator.ViewRange.Duration;
                    this.Navigator.ViewRange.Set(viewStartTime, viewEndTime);
                }

                this.Navigator.CursorFollowsMouse = true;
                this.Navigator.Cursor = cursor;
                this.Navigator.CursorFollowsMouse = false;
            }
        }

        private static bool SeekToLayoutElement(JsonTextReader jsonReader)
        {
            // Move the json text reader to the "Layout" node in the document
            try
            {
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.ValueType == typeof(string) && jsonReader.Value as string == LayoutPropertyName)
                    {
                        // Jump to the property value (start object)
                        jsonReader.Read();

                        // Success.
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static double GetLayoutFileVersion(JsonTextReader jsonReader)
        {
            // Get the layout version (if it exists) from the layout file being opened
            while (jsonReader.Read())
            {
                if ((jsonReader.TokenType == JsonToken.PropertyName) && (jsonReader.ValueType == typeof(string)) && (jsonReader.Value.ToString() == LayoutVersionPropertyName))
                {
                    if (jsonReader.Read() && jsonReader.TokenType == JsonToken.Float && jsonReader.ValueType == typeof(double))
                    {
                        return Convert.ToDouble(jsonReader.Value);
                    }
                }
            }

            throw new ApplicationException("No LayoutVersion element could be found in the file.");
        }

        private static string CreateLayoutLoadErrorText(string layoutName, Exception ex)
        {
            var text = new StringBuilder();
            text.AppendLine(string.Format("The layout {0} could not be loaded because of the following errors:", layoutName));
            text.AppendLine();

            while (ex != null)
            {
                text.AppendLine(ex.Message);
                ex = ex.InnerException;
            }

            text.AppendLine();
            text.AppendLine("The default layout will be loaded instead.");

            return text.ToString();
        }

        private void InitNew()
        {
            this.panelsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.panels, this.panelsLock);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
            foreach (var panel in this.Panels)
            {
                panel.SetParentContainer(this);
            }
        }
    }
}