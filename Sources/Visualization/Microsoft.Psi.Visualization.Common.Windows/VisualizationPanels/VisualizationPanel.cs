// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Data;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents the base class that visualization panels derive from.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class VisualizationPanel : ObservableObject
    {
        private RelayCommand<VisualizationObject> deleteVisualizationCommand;

        /// <summary>
        /// The current visualization object.
        /// </summary>
        private VisualizationObject currentVisualizationObject;

        /// <summary>
        /// multithreaded collection lock.
        /// </summary>
        private object visualizationObjectsLock;

        /// <summary>
        /// Determines whether the visualization panel is initially expanded when shown in a tree view.
        /// </summary>
        private bool isTreeNodeExpanded = true;

        /// <summary>
        /// The template to use when creating the view for this panel.
        /// </summary>
        private DataTemplate defaultViewTemplate = null;

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

        /// <summary>
        /// Gets the default view template.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public DataTemplate DefaultViewTemplate
        {
            get
            {
                if (this.defaultViewTemplate == null)
                {
                    this.defaultViewTemplate = this.CreateDefaultViewTemplate();
                }

                return this.defaultViewTemplate;
            }
        }

        /// <summary>
        /// Gets the height of the panel.
        /// </summary>
        [IgnoreDataMember]
        public abstract double Height { get; }

        /// <summary>
        /// Gets a value indicating whether or not this is the current panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsCurrentPanel => this.Container.CurrentPanel == this;

        /// <summary>
        /// Gets the navigator associated with this panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Navigator Navigator => this.Container.Navigator;

        /// <summary>
        /// Gets the collection of data stream visualization objects.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public ObservableCollection<VisualizationObject> VisualizationObjects { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the visualization panel is initially expanded when shown in a tree view.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsTreeNodeExpanded
        {
            get => this.isTreeNodeExpanded;
            set => this.Set(nameof(this.IsTreeNodeExpanded), ref this.isTreeNodeExpanded, value);
        }

        /// <summary>
        /// Gets the delete visualization command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<VisualizationObject> DeleteVisualizationCommand
        {
            get
            {
                if (this.deleteVisualizationCommand == null)
                {
                    this.deleteVisualizationCommand = new RelayCommand<VisualizationObject>(
                        o =>
                        {
                            this.RemoveVisualizationObject(o);
                        });
                }

                return this.deleteVisualizationCommand;
            }
        }

        /// <summary>
        /// Gets the width of the panel.
        /// </summary>
        [IgnoreDataMember]
        public abstract double Width { get; }

        /// <summary>
        /// Add a visualization object to the panel.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be added.</param>
        public void AddVisualizationObject(VisualizationObject visualizationObject)
        {
            visualizationObject.AddToPanel(this);
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

        /// <summary>
        /// Brings a visualization object to the front.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to bring to front.</param>
        public void BringToFront(VisualizationObject visualizationObject)
        {
            int oldIndex = this.VisualizationObjects.IndexOf(visualizationObject);
            if (oldIndex != this.VisualizationObjects.Count - 1)
            {
                this.VisualizationObjects.Move(oldIndex, this.VisualizationObjects.Count - 1);
            }
        }

        /// <summary>
        /// Clears the visualization panel.
        /// </summary>
        public void Clear()
        {
            while (this.VisualizationObjects.Count > 0)
            {
                this.RemoveVisualizationObject(this.VisualizationObjects[0]);
            }
        }

        /// <summary>
        /// Removes a visualization object specified by a view model.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be removed.</param>
        public void RemoveVisualizationObject(VisualizationObject visualizationObject)
        {
            // change the current visualization object
            if (this.currentVisualizationObject == visualizationObject)
            {
                this.currentVisualizationObject = null;
            }

            // If the visualization object being deleted is the stream being snapped to, then reset the snap to stream object
            if (visualizationObject == this.Container.SnapToVisualizationObject)
            {
                this.Container.SnapToVisualizationObject = null;
            }

            visualizationObject.RemoveFromPanel();
            this.VisualizationObjects.Remove(visualizationObject);

            if ((this.currentVisualizationObject == null) && (this.VisualizationObjects.Count > 0))
            {
                this.CurrentVisualizationObject = this.VisualizationObjects.Last();
            }
        }

        /// <summary>
        /// Sends a visualization object to the back.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to bring to front.</param>
        public void SendToBack(VisualizationObject visualizationObject)
        {
            int oldIndex = this.VisualizationObjects.IndexOf(visualizationObject);
            if (oldIndex != 0)
            {
                this.VisualizationObjects.Move(oldIndex, 0);
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
                visualizationObject.AddToPanel(this);
            }
        }

        /// <summary>
        /// Initializes a new visualization panel. Called by ctor and contract serializer.
        /// </summary>
        protected virtual void InitNew()
        {
            this.VisualizationObjects = new ObservableCollection<VisualizationObject>();
            this.visualizationObjectsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.VisualizationObjects, this.visualizationObjectsLock);
        }

        /// <summary>
        /// Creates the view template.
        /// </summary>
        /// <returns>The template for the view.</returns>
        protected abstract DataTemplate CreateDefaultViewTemplate();

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever the Configuration property has changed.
        /// </summary>
        protected virtual void OnConfigurationChanged()
        {
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever a property on the Configuration property has changed.
        /// </summary>
        /// <param name="sender">The object that triggered the configuration property change event.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
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
