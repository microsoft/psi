// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Serialization;
    using Microsoft.Psi.Visualization.Server;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the container where all visualization panels are hosted. The is the root UI element for visualizations.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteVisualizationContainerCLSIDString)]
    [ComVisible(false)]
    public class VisualizationContainer : ReferenceCountedObject, IRemoteVisualizationContainer
    {
        /// <summary>
        /// The name of the container
        /// </summary>
        private string name;

        /// <summary>
        /// The time navigator view model
        /// </summary>
        private Navigator navigator;

        /// <summary>
        /// The collection of visualization Panels
        /// </summary>
        private ObservableCollection<VisualizationPanel> panels;

        /// <summary>
        /// multithreaded collection lock
        /// </summary>
        private object panelsLock;

        /// <summary>
        /// The current visualization panel
        /// </summary>
        private VisualizationPanel currentPanel;

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
        /// Gets or sets the current visualization panel
        /// </summary>
        [IgnoreDataMember]
        public VisualizationPanel CurrentPanel
        {
            get { return this.currentPanel; }
            set { this.Set(nameof(this.CurrentPanel), ref this.currentPanel, value); }
        }

        /// <summary>
        /// Gets or sets the name of the container
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets the current navigator
        /// </summary>
        [DataMember]
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

        /// <inheritdoc />
        IRemoteVisualizationPanel IRemoteVisualizationContainer.CurrentPanel
        {
            get => this.CurrentPanel;
            set => this.CurrentPanel = (VisualizationPanel)value;
        }

        /// <inheritdoc />
        IRemoteNavigator IRemoteVisualizationContainer.Navigator => this.Navigator;

        /// <summary>
        /// Loads a visualization layout from the specified file.
        /// </summary>
        /// <param name="filename">File to load visualization layout.</param>
        /// <returns>The new visualization container.</returns>
        public static VisualizationContainer Load(string filename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                });

            StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(filename);
                using (var jsonReader = new JsonTextReader(jsonFile))
                {
                    jsonFile = null;
                    VisualizationContainer container = serializer.Deserialize<VisualizationContainer>(jsonReader);
                    return container;
                }
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <inheritdoc />
        public IRemoteVisualizationPanel AddPanel(string type)
        {
            var t = Type.GetType(type);
            if (t == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                t = assembly.GetType(type);
            }

            VisualizationPanel panel = (VisualizationPanel)Activator.CreateInstance(t);
            this.AddPanel(panel);
            return panel;
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
                if (this.CurrentPanel != null)
                {
                    this.Panels.Insert(this.Panels.IndexOf(this.CurrentPanel) + 1, panel);
                }
                else
                {
                    this.Panels.Add(panel);
                }
            }

            this.CurrentPanel = panel;
        }

        /// <summary>
        /// Creates and adds a new panel to the container.
        /// </summary>
        /// <typeparam name="T">The type of panel to add.</typeparam>
        /// <returns>The newly added panel.</returns>
        public T AddPanel<T>()
            where T : VisualizationPanel, new()
        {
            T panel = new T();
            this.AddPanel(panel);
            return panel;
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
        /// Close all streams in all visualization objects.
        /// </summary>
        public void CloseStreams()
        {
            foreach (var panel in this.Panels)
            {
                foreach (var vo in panel.VisualizationObjects)
                {
                    var svo = vo as IStreamVisualizationObject;
                    svo?.CloseStream();
                }
            }
        }

        /// <inheritdoc />
        public void RemovePanel(IRemoteVisualizationPanel panel)
        {
            this.RemovePanel((VisualizationPanel)panel);
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

            panel.Clear();
            this.Panels.Remove(panel);

            if ((this.CurrentPanel == null) && (this.Panels.Count > 0))
            {
                this.CurrentPanel = this.Panels.Last();
            }
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
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(filename);
                using (var jsonWriter = new JsonTextWriter(jsonFile))
                {
                    jsonFile = null;
                    serializer.Serialize(jsonWriter, this);
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
        /// <param name="partitions">Partitions to use in updating store bindings.</param>
        public void UpdateStoreBindings(IEnumerable<PartitionViewModel> partitions)
        {
            foreach (var panel in this.Panels)
            {
                foreach (var vo in ((VisualizationPanel)panel).VisualizationObjects)
                {
                    var svo = vo as IStreamVisualizationObject;
                    svo?.UpdateStoreBindings(partitions);
                }
            }
        }

        /// <summary>
        /// Zoom to the spcified time interval.
        /// </summary>
        /// <param name="timeInterval">Time interval to zoom to.</param>
        public void ZoomToRange(TimeInterval timeInterval)
        {
            this.Navigator.SelectionRange.SetRange(timeInterval.Left, timeInterval.Right);
            this.Navigator.ViewRange.SetRange(timeInterval.Left, timeInterval.Right);
            this.Navigator.Cursor = timeInterval.Left;
        }

        private void InitNew()
        {
            ((Navigator)this.navigator).NavigationModeChanged += this.Navigator_NavigationModeChanged;
            this.panelsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.panels, this.panelsLock);
        }

        private void Navigator_NavigationModeChanged(object sender, NavigatorModeChangedEventArgs e)
        {
            this.Clear();
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