// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Server;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents the base class that visualization panels derive from.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteVisualizationPanelCLSIDString)]
    [ComVisible(false)]
    public abstract class VisualizationPanel : ReferenceCountedObject, IRemoteVisualizationPanel
    {
        private Dictionary<uint, INotifyRemoteConfigurationChanged> advises;
        private uint nextAdviseCookie;

        /// <summary>
        /// The current visualization object
        /// </summary>
        private VisualizationObject currentVisualizationObject;

        /// <summary>
        /// multithreaded collection lock
        /// </summary>
        private object visualizationObjectsLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationPanel"/> class.
        /// </summary>
        public VisualizationPanel()
        {
            this.InitNew();
        }

        /// <summary>
        /// Gets the visualization container that this panel is under.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationContainer Container { get; private set; }

        /// <inheritdoc />
        IRemoteVisualizationContainer IRemoteVisualizationPanel.Container => this.Container;

        /// <summary>
        /// Gets or sets the current visualization object.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationObject CurrentVisualizationObject
        {
            get { return this.currentVisualizationObject; }
            set { this.Set(nameof(this.CurrentVisualizationObject), ref this.currentVisualizationObject, value); }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        IRemoteVisualizationObject IRemoteVisualizationPanel.CurrentVisualizationObject
        {
            get => this.CurrentVisualizationObject;
            set => this.CurrentVisualizationObject = (VisualizationObject)value;
        }

        /// <summary>
        /// Gets the default view template.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public abstract DataTemplate DefaultViewTemplate { get; }

        /// <summary>
        /// Gets the height of the panel
        /// </summary>
        [IgnoreDataMember]
        public abstract double Height { get; }

        /// <summary>
        /// Gets a value indicating whether or not this is the current panel
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsCurrentPanel => this.Container.CurrentPanel == this;

        /// <summary>
        /// Gets the navigator associated with this panel
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Navigator Navigator => this.Container.Navigator;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        IRemoteNavigator IRemoteVisualizationPanel.Navigator => this.Navigator;

        /// <summary>
        /// Gets the collection of data stream visualization objects.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public ObservableCollection<VisualizationObject> VisualizationObjects { get; internal set; }

        /// <summary>
        /// Gets the width of the panel
        /// </summary>
        [IgnoreDataMember]
        public abstract double Width { get; }

        /// <inheritdoc />
        public IRemoteVisualizationObject AddVisualizationObject(string type)
        {
            var t = Type.GetType(type);
            if (t == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                t = assembly.GetType(type);
            }

            VisualizationObject visualizationObject = (VisualizationObject)Activator.CreateInstance(t);
            this.AddVisualizationObject(visualizationObject);
            return visualizationObject;
        }

        /// <inheritdoc />
        public IRemoteVisualizationObject AddVisualizationObject(string assemblyPath, string type)
        {
            var a = Assembly.LoadFrom(assemblyPath);
            var t = a.GetType(type);
            VisualizationObject visualizationObject = (VisualizationObject)Activator.CreateInstance(t);
            this.AddVisualizationObject(visualizationObject);
            return visualizationObject;
        }

        /// <summary>
        /// Add a visualization object to the panel
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be added.</param>
        public void AddVisualizationObject(VisualizationObject visualizationObject)
        {
            visualizationObject.SetParentPanel(this);
            this.VisualizationObjects.Add(visualizationObject);
            this.CurrentVisualizationObject = visualizationObject;
        }

        /// <summary>
        /// Creates and adds a visualization object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the visualization object to add.</typeparam>
        /// <returns>The newly created visualization object.</returns>
        public T AddVisualizationObject<T>()
            where T : VisualizationObject, new()
        {
            T vo = new T();
            this.AddVisualizationObject(vo);
            return vo;
        }

        /// <inheritdoc />
        public uint Advise(INotifyRemoteConfigurationChanged notifyVisualizationPanelChanged)
        {
            uint cookie = this.nextAdviseCookie++;
            this.advises.Add(cookie, notifyVisualizationPanelChanged);
            return cookie;
        }

        /// <inheritdoc />
        public void BringToFront(IRemoteVisualizationObject visualizationObject)
        {
            this.BringToFront((VisualizationObject)visualizationObject);
        }

        /// <summary>
        /// Brings a visualization object to the front.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to bring to front</param>
        public void BringToFront(VisualizationObject visualizationObject)
        {
            int oldIndex = this.VisualizationObjects.IndexOf(visualizationObject);
            if (oldIndex != this.VisualizationObjects.Count - 1)
            {
                this.VisualizationObjects.Move(oldIndex, this.VisualizationObjects.Count - 1);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            while (this.VisualizationObjects.Count > 0)
            {
                this.RemoveVisualizationObject(this.VisualizationObjects[0]);
            }
        }

        /// <inheritdoc />
        public abstract string GetConfiguration();

        /// <inheritdoc />
        public void RemoveVisualizationObject(IRemoteVisualizationObject visualizationObject)
        {
            this.RemoveVisualizationObject((VisualizationObject)visualizationObject);
        }

        /// <summary>
        /// Removes a visualization object specified by a view model
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be removed.</param>
        public void RemoveVisualizationObject(VisualizationObject visualizationObject)
        {
            // change the current visualization object
            if (this.currentVisualizationObject == visualizationObject)
            {
                this.currentVisualizationObject = null;
            }

            visualizationObject.SetParentPanel(null);
            this.VisualizationObjects.Remove(visualizationObject);

            if ((this.currentVisualizationObject == null) && (this.VisualizationObjects.Count > 0))
            {
                this.CurrentVisualizationObject = this.VisualizationObjects.Last();
            }
        }

        /// <inheritdoc />
        public void SendToBack(IRemoteVisualizationObject visualizationObject)
        {
            this.SendToBack((VisualizationObject)visualizationObject);
        }

        /// <summary>
        /// Sends a visualization object to the back.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to bring to front</param>
        public void SendToBack(VisualizationObject visualizationObject)
        {
            int oldIndex = this.VisualizationObjects.IndexOf(visualizationObject);
            if (oldIndex != 0)
            {
                this.VisualizationObjects.Move(oldIndex, 0);
            }
        }

        /// <inheritdoc />
        public abstract void SetConfiguration(string jsonConfiguration);

        /// <inheritdoc />
        public void Unadvise(uint cookie)
        {
            if (!this.advises.Remove(cookie))
            {
                Marshal.ThrowExceptionForHR(ComNative.E_POINTER);
            }
        }

        /// <summary>
        /// Called internally by the VisualizationContainer to connect the parent chain.
        /// </summary>
        /// <param name="container">Container to connect this visualization panel to.</param>
        internal void SetParentContainer(VisualizationContainer container)
        {
            if (this.Container != null)
            {
                this.Container.PropertyChanged -= this.Container_PropertyChanged;
            }

            this.Container = container;
            if (this.Container != null)
            {
                this.Container.PropertyChanged += this.Container_PropertyChanged;
            }

            foreach (var visualizationObject in this.VisualizationObjects)
            {
                visualizationObject.SetParentPanel(this);
            }
        }

        /// <summary>
        /// Initializes a new visualization panel. Called by ctor and contract serializer.
        /// </summary>
        protected virtual void InitNew()
        {
            this.advises = new Dictionary<uint, INotifyRemoteConfigurationChanged>();
            this.nextAdviseCookie = 1;

            this.VisualizationObjects = new ObservableCollection<VisualizationObject>();
            this.visualizationObjectsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.VisualizationObjects, this.visualizationObjectsLock);
        }

        /// <summary>
        /// Notifiy client application when configuration has changed.
        /// </summary>
        protected virtual void NotifyConfigurationChanged()
        {
            string jsonConfiguration = this.GetConfiguration();
            List<uint> orphanedAdvises = new List<uint>();
            foreach (var advise in this.advises)
            {
                try
                {
                    advise.Value.OnRemoteConfigurationChanged(jsonConfiguration);
                }
                catch (COMException)
                {
                    orphanedAdvises.Add(advise.Key);
                }
            }

            foreach (var orphanedAdvise in orphanedAdvises)
            {
                this.advises.Remove(orphanedAdvise);
            }
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever the Configuration property has changed.
        /// </summary>
        protected virtual void OnConfigurationChanged()
        {
            this.NotifyConfigurationChanged();
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever a property on the Configuration property has changed.
        /// </summary>
        /// <param name="propertyName">Name of property that has changed.</param>
        protected virtual void OnConfigurationPropertyChanged(string propertyName)
        {
        }

        private void Container_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Container.CurrentPanel))
            {
                this.RaisePropertyChanged(nameof(this.IsCurrentPanel));
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.InitNew();
        }
    }
}
