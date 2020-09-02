// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using System.Windows.Data;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Data.Helpers;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Serialization;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the container where all visualization panels are hosted. The is the root UI element for visualizations.
    /// </summary>
    public class VisualizationContainer : ObservableObject
    {
        // Property names used in the layout (*.plo) files
        private const string LayoutPropertyName = "Layout";
        private const string LayoutVersionPropertyName = "LayoutVersion";

        // Current Visualization Container version
        private const double CurrentVisualizationContainerVersion = 5.0d;

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

                    // Display the properties of the panel
                    if (this.currentPanel != null)
                    {
                        this.currentPanel.IsTreeNodeSelected = true;
                    }
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
        [IgnoreDataMember]
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
        {
            get
            {
                if (this.deleteVisualizationPanelCommand == null)
                {
                    this.deleteVisualizationPanelCommand = new RelayCommand<VisualizationPanel>(
                        o =>
                        {
                            this.RemovePanel(o);
                        });
                }

                return this.deleteVisualizationPanelCommand;
            }
        }

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
                });

            System.IO.StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(filename);
                using (var jsonReader = new JsonTextReader(jsonFile))
                {
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
                        VisualizationContainer visualizationContainer = serializer.Deserialize<VisualizationContainer>(jsonReader);

                        // Copy the settings from the current navigator to the new one.
                        if (currentVisualizationContainer != null)
                        {
                            visualizationContainer.Navigator.Initialize(currentVisualizationContainer.Navigator);
                        }

                        return visualizationContainer;
                    }
                    else
                    {
                        throw new ApplicationException("No Layout element was found in the file");
                    }
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

            this.CurrentPanel = panel is IInstantVisualizationContainer instantContainer ? instantContainer.Panels[0] : panel;
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

            // If the panel to be removed is the child of a container panel (such as instant visualization panel
            // matrix panel) then ask the parent to remove it, otherwise remove it directly from this container.
            if (panel.ParentPanel is VisualizationPanel containerPanel)
            {
                containerPanel.RemoveChildPanel(panel);
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
        /// Gets all of the visualization objects that visualize a stream member rather than a stream.
        /// </summary>
        /// <returns>The collection ov visualization objects that visualize a stream member rathan a stream.</returns>
        public List<IStreamVisualizationObject> GetStreamMemberVisualizers()
        {
            List<IStreamVisualizationObject> streamMemberVisualizers = new List<IStreamVisualizationObject>();

            foreach (VisualizationPanel visualizationPanel in this.Panels)
            {
                streamMemberVisualizers.AddRange(visualizationPanel.GetStreamMemberVisualizers());
            }

            return streamMemberVisualizers;
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
                    ContractResolver = new Instant3DVisualizationObjectContractResolver(),
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(filename);
                using (var jsonWriter = new JsonTextWriter(jsonFile))
                {
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
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Update the store bindings with the specified enumeration of partitions.
        /// </summary>
        /// <param name="sessionViewModel">The currently active session view model.</param>
        public void UpdateStreamSources(SessionViewModel sessionViewModel)
        {
            foreach (VisualizationPanel panel in this.Panels)
            {
                // Update the stream sources for the panel
                UpdateVisualizationPanelStreamSources(panel, sessionViewModel);

                // If the panel is an instant visualization container, then do the same for all the panels that it contains
                if (panel is IInstantVisualizationContainer instantVisualizationContainer)
                {
                    foreach (VisualizationPanel instantVisualizationPanel in instantVisualizationContainer.Panels)
                    {
                        UpdateVisualizationPanelStreamSources(instantVisualizationPanel, sessionViewModel);
                    }
                }
            }
        }

        /// <summary>
        /// Zoom to the specified time interval.
        /// </summary>
        /// <param name="timeInterval">Time interval to zoom to.</param>
        public void ZoomToRange(TimeInterval timeInterval)
        {
            this.Navigator.ViewRange.SetRange(timeInterval.Left, timeInterval.Right);
            this.Navigator.Cursor = timeInterval.Left;
        }

        private static void UpdateVisualizationPanelStreamSources(VisualizationPanel panel, SessionViewModel sessionViewModel)
        {
            foreach (VisualizationObject visualizationObject in panel.VisualizationObjects)
            {
                IStreamVisualizationObject streamVisualizationObject = visualizationObject as IStreamVisualizationObject;
                streamVisualizationObject?.UpdateStreamSource(sessionViewModel);
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
            StringBuilder text = new StringBuilder();
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