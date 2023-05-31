// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents the base class that visualization panels derive from.
    /// </summary>
    public abstract class VisualizationPanel : ObservableTreeNodeObject, IContextMenuItemsSource
    {
        // The minimum height of a Visualization Panel
        private const double MinHeight = 10;

        private RelayCommand toggleAllVisualizersVisibilityCommand;
        private RelayCommand toggleVisibilityCommand;
        private RelayCommand removePanelCommand;
        private RelayCommand clearPanelCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<DragDeltaEventArgs> resizePanelCommand;

        /// <summary>
        /// The height of the panel.
        /// </summary>
        private double height = 400;

        /// <summary>
        /// The height of the panel.
        /// </summary>
        private double width = 400;

        /// <summary>
        /// The background color for the panel [DarkBackgroundColor in PsiStudioDark.xml].
        /// </summary>
        private Color backgroundColor = new () { R = 0x25, G = 0x25, B = 0x26, A = 0xFF };

        /// <summary>
        /// The name of the visualization panel.
        /// </summary>
        private string name = "Visualization Panel";

        /// <summary>
        /// Indicates whether the visualization panel is visible.
        /// </summary>
        private bool visible = true;

        /// <summary>
        /// The zoom to panel command.
        /// </summary>
        private RelayCommand zoomToPanelCommand;

        /// <summary>
        /// The delete visualization command.
        /// </summary>
        private RelayCommand<VisualizationObject> deleteVisualizationCommand;

        /// <summary>
        /// The current visualization object.
        /// </summary>
        private VisualizationObject currentVisualizationObject;

        /// <summary>
        /// Multithreaded collection lock.
        /// </summary>
        private object visualizationObjectsLock;

        /// <summary>
        /// The margin to use for this panel's view.
        /// </summary>
        private Thickness visualMargin;

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
            this.IsTreeNodeExpanded = true;
        }

        /// <summary>
        /// Gets the command for toggling the visibility of all visualizers.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleAllVisualizersVisibilityCommand
            => this.toggleAllVisualizersVisibilityCommand ??= new RelayCommand(() => this.ToggleAllVisualizationObjectsVisibility());

        /// <summary>
        /// Gets the toggle visibility command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleVisibilityCommand
            => this.toggleVisibilityCommand ??= new RelayCommand(() => this.Container.TogglePanelVisibility(this));

        /// <summary>
        /// Gets the remove panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemovePanelCommand
            => this.removePanelCommand ??= new RelayCommand(() => this.Container.RemovePanel(this));

        /// <summary>
        /// Gets the clear panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearPanelCommand
            => this.clearPanelCommand ??= new RelayCommand(() => this.Clear());

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
            => this.mouseLeftButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(
                e =>
                {
                    // Set the current panel on click
                    if (!this.IsCurrentPanel)
                    {
                        // Set the current panel to this panel
                        this.IsTreeNodeSelected = true;
                        this.Container.CurrentPanel = this;

                        // If the panel contains any visualization objects, set the first one as selected.
                        if (this.VisualizationObjects.Any())
                        {
                            this.VisualizationObjects[0].IsTreeNodeSelected = true;
                        }
                    }
                });

        /// <summary>
        /// Gets the resize panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<DragDeltaEventArgs> ResizePanelCommand
            => this.resizePanelCommand ??= new RelayCommand<DragDeltaEventArgs>(o => this.Height = Math.Max(this.Height + o.VerticalChange, MinHeight));

        /// <summary>
        /// Gets or sets the name of the visualization panel name.
        /// </summary>
        [DataMember]
        [PropertyOrder(0)]
        [Description("The name of the visualization panel.")]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the visualization panel is visible.
        /// </summary>
        [DataMember]
        [Description("The visibility of the visualization panel.")]
        public bool Visible
        {
            get { return this.visible; }

            set
            {
                this.Set(nameof(this.Visible), ref this.visible, value);
                this.RaisePropertyChanged(nameof(this.IsShown));
            }
        }

        /// <summary>
        /// Gets or sets the height of the panel.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public double Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the background color for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Background Color")]
        [Description("The background color for the visualization panel.")]
        public Color BackgroundColor
        {
            get { return this.backgroundColor; }
            set { this.Set(nameof(this.BackgroundColor), ref this.backgroundColor, value); }
        }

        /// <summary>
        /// Gets or sets the width of the panel.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public double Width
        {
            get { return this.width; }
            set { this.Set(nameof(this.Width), ref this.width, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the visualization panel is shown.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsShown => this.Visible && (this.ParentPanel == null || this.ParentPanel.IsShown);

        /// <summary>
        /// Gets the visualization panel container that contains this panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationContainer Container { get; private set; }

        /// <summary>
        /// Gets the parent visualization panel (if this panel is a child of another panel).
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationPanel ParentPanel { get; private set; }

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
        public DataTemplate DefaultViewTemplate => this.defaultViewTemplate ??= this.CreateDefaultViewTemplate();

        /// <summary>
        /// Gets or sets the visual margin for the panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Thickness VisualMargin
        {
            get => this.visualMargin;
            set { this.Set(nameof(this.VisualMargin), ref this.visualMargin, value); }
        }

        /// <summary>
        /// Gets a value indicating whether or not this is the current panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsCurrentPanel => this.Container?.CurrentPanel == this;

        /// <summary>
        /// Gets a value indicating whether we should display the zoom to panel menuitem.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool ShowZoomToPanelMenuItem => false;

        /// <summary>
        /// Gets a value indicating whether or not this panel can currently be zoomed to.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool CanZoomToPanel => false;

        /// <summary>
        /// Gets the navigator associated with this panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Navigator Navigator => this.Container?.Navigator;

        /// <summary>
        /// Gets the collection of data stream visualization objects.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public ObservableCollection<VisualizationObject> VisualizationObjects { get; internal set; }

        /// <summary>
        /// Gets the list of compatible panel types.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public abstract List<VisualizationPanelType> CompatiblePanelTypes { get; }

        /// <summary>
        /// Gets the zoom to panel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToPanelCommand
            => this.zoomToPanelCommand ??= new RelayCommand(() => this.ZoomToPanel());

        /// <summary>
        /// Gets the delete visualization command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<VisualizationObject> DeleteVisualizationCommand
            => this.deleteVisualizationCommand ??= new RelayCommand<VisualizationObject>(o => this.RemoveVisualizationObject(o));

        /// <inheritdoc/>
        public virtual List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var commands = new List<ContextMenuItemInfo>();

            if (this.VisualizationObjects.Count > 0)
            {
                var visible = this.VisualizationObjects.Any(vo => vo.Visible);
                commands.Add(
                    new ContextMenuItemInfo(
                        IconSourcePath.ToggleVisibility,
                        visible ? "Hide All Visualizers" : "Show All Visualizers",
                        this.ToggleAllVisualizersVisibilityCommand,
                        isEnabled: true));
            }

            commands.Add(
                new ContextMenuItemInfo(
                    IconSourcePath.ClearPanel,
                    $"Remove All Visualizers",
                    this.ClearPanelCommand,
                    isEnabled: this.VisualizationObjects.Count > 0));

            // Add copy to clipboard menu with sub-menu items
            var copyToClipboardCommands = new ContextMenuItemInfo("Copy to Clipboard");

            copyToClipboardCommands.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Cursor Time",
                    this.Navigator.CopyToClipboardCommand,
                    isEnabled: true,
                    commandParameter: this.Navigator.Cursor.ToString("M/d/yyyy HH:mm:ss.ffff")));
            copyToClipboardCommands.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Cursor Time (as Ticks)",
                    this.Navigator.CopyToClipboardCommand,
                    isEnabled: true,
                    commandParameter: this.Navigator.Cursor.Ticks.ToString()));
            copyToClipboardCommands.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Session Name",
                    this.Navigator.CopyToClipboardCommand,
                    isEnabled: VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel != null,
                    commandParameter: VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel.Name.ToString()));
            copyToClipboardCommands.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Session Name & Cursor Time",
                    this.Navigator.CopyToClipboardCommand,
                    isEnabled: VisualizationContext.Instance.DatasetViewModel?.CurrentSessionViewModel != null,
                    commandParameter: VisualizationContext.Instance.DatasetViewModel.CurrentSessionViewModel.Name.ToString() + "@" + this.Navigator.Cursor.ToString("M/d/yyyy HH:mm:ss.ffff")));

            commands.Add(copyToClipboardCommands);

            commands.Add(
                new ContextMenuItemInfo(
                    null,
                    $"Go To Time ...",
                    this.Container.GoToTimeCommand,
                    isEnabled: true));

            return commands;
        }

        /// <summary>
        /// Add a visualization object to the panel.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be added.</param>
        public virtual void AddVisualizationObject(VisualizationObject visualizationObject)
        {
            visualizationObject.AddToPanel(this);
            this.VisualizationObjects.Add(visualizationObject);
            this.CurrentVisualizationObject = visualizationObject;
            this.RaisePropertyChanged(nameof(this.VisualizationObjects));
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
        public virtual void Clear()
        {
            while (this.VisualizationObjects.Count > 0)
            {
                this.RemoveVisualizationObject(this.VisualizationObjects[0]);
            }
        }

        /// <summary>
        /// Toggles the visibility of all visualization objects.
        /// </summary>
        public void ToggleAllVisualizationObjectsVisibility()
        {
            var anyVisible = this.VisualizationObjects.Any(vo => vo.Visible);
            foreach (var visualizationObject in this.VisualizationObjects)
            {
                visualizationObject.Visible = !anyVisible;
            }
        }

        /// <summary>
        /// Toggles the visibility for a specified visualization object.
        /// </summary>
        /// <param name="visualizationObject">The specified visualization object.</param>
        public void ToggleVisualizationObjectVisibility(VisualizationObject visualizationObject)
        {
            // If we are about to make the visualization object visible and the panel is
            // set to not visible
            if (this.Visible == false)
            {
                // Then toggle the panel to visible
                this.Container.TogglePanelVisibility(this);

                // And the set the visualization object to visible
                visualizationObject.Visible = true;
            }
            else
            {
                // O/w just toggle the visualization object
                visualizationObject.Visible = !visualizationObject.Visible;
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
        public virtual void SetParentContainer(VisualizationContainer container)
        {
            if (this.Container != null)
            {
                this.Container.PropertyChanged -= this.OnContainerPropertyChanged;
            }

            this.Container = container;
            if (this.Container != null)
            {
                this.Container.PropertyChanged += this.OnContainerPropertyChanged;
            }

            foreach (var visualizationObject in this.VisualizationObjects)
            {
                visualizationObject.AddToPanel(this);
            }
        }

        /// <summary>
        /// Sets the parent panel.
        /// </summary>
        /// <param name="parentPanel">The new parent panel.</param>
        public virtual void SetParentPanel(VisualizationPanel parentPanel)
        {
            if (this.ParentPanel != null)
            {
                this.ParentPanel.PropertyChanged -= this.OnParentPanelPropertyChanged;
            }

            this.ParentPanel = parentPanel;

            if (this.ParentPanel != null)
            {
                this.ParentPanel.PropertyChanged += this.OnParentPanelPropertyChanged;
            }
        }

        /// <summary>
        /// Gets all of the visualization objects that visualize a derived stream, rather than a raw stream.
        /// </summary>
        /// <returns>The collection of visualization objects that visualize a derived stream.</returns>
        public virtual List<IStreamVisualizationObject> GetDerivedStreamVisualizationObjects()
        {
            var derivedStreamVisualizationObjects = new List<IStreamVisualizationObject>();

            foreach (var visualizationObject in this.VisualizationObjects.OfType<IStreamVisualizationObject>())
            {
                if (visualizationObject.StreamBinding.IsBindingToDerivedStream)
                {
                    derivedStreamVisualizationObjects.Add(visualizationObject);
                }
            }

            return derivedStreamVisualizationObjects;
        }

        /// <summary>
        /// Unbinds any visualization objects currently bound to a store.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="storePath">The path to the store.</param>
        /// <param name="partitionName">The partition name of the instance to unbind, or null to unbind all instances.</param>
        public void UnbindVisualizationObjectsFromStore(string storeName, string storePath, string partitionName)
        {
            foreach (var streamVisualizationObject in this.VisualizationObjects.OfType<IStreamVisualizationObject>())
            {
                if (streamVisualizationObject.StreamSource != null
                    && streamVisualizationObject.StreamSource.StoreName == storeName
                    && streamVisualizationObject.StreamSource.StorePath == storePath
                    && (partitionName == null || streamVisualizationObject.StreamBinding.PartitionName == partitionName))
                {
                    streamVisualizationObject.UpdateStreamSource(null);
                }
            }
        }

        /// <summary>
        /// Update the stream sources for the visualization objects in this panel.
        /// </summary>
        /// <param name="sessionViewModel">The currently active session view model.</param>
        public virtual void UpdateStreamSources(SessionViewModel sessionViewModel)
        {
            foreach (var streamVisualizationObject in this.VisualizationObjects.OfType<IStreamVisualizationObject>())
            {
                streamVisualizationObject?.UpdateStreamSource(sessionViewModel);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.Name;

        /// <summary>
        /// Initializes a new visualization panel. Called by ctor and contract serializer.
        /// </summary>
        protected virtual void InitNew()
        {
            this.VisualizationObjects = new ObservableCollection<VisualizationObject>();
            this.visualizationObjectsLock = new object();
            BindingOperations.EnableCollectionSynchronization(this.VisualizationObjects, this.visualizationObjectsLock);
            this.VisualizationObjects.CollectionChanged += this.OnVisualizationObjectsCollectionChanged;
        }

        /// <summary>
        /// Creates the view template.
        /// </summary>
        /// <returns>The template for the view.</returns>
        protected abstract DataTemplate CreateDefaultViewTemplate();

        /// <summary>
        /// Called when the collection of child visualization objects has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnVisualizationObjectsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var visualizationObject in e.OldItems)
                {
                    if (visualizationObject is IXValueRangeProvider xValueRangeProvider)
                    {
                        xValueRangeProvider.XValueRangeChanged -= this.OnVisualizationObjectXValueRangeChanged;
                    }

                    if (visualizationObject is IYValueRangeProvider yValueRangeProvider)
                    {
                        yValueRangeProvider.YValueRangeChanged -= this.OnVisualizationObjectYValueRangeChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var visualizationObject in e.NewItems)
                {
                    if (visualizationObject is IXValueRangeProvider xValueRangeProvider)
                    {
                        xValueRangeProvider.XValueRangeChanged += this.OnVisualizationObjectXValueRangeChanged;
                    }

                    if (visualizationObject is IYValueRangeProvider yValueRangeProvider)
                    {
                        yValueRangeProvider.YValueRangeChanged += this.OnVisualizationObjectYValueRangeChanged;
                    }
                }
            }

            this.RaisePropertyChanged(nameof(this.CanZoomToPanel));
        }

        /// <summary>
        /// Called when the X value range of a child visualization object has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnVisualizationObjectXValueRangeChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Called when the Y value range of a child visualization object has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnVisualizationObjectYValueRangeChanged(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Called when a property of the container has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnContainerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Container.CurrentPanel))
            {
                this.RaisePropertyChanged(nameof(this.IsCurrentPanel));
            }
        }

        /// <summary>
        /// Called when a property of the parent panel has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnParentPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.Visible))
            {
                this.RaisePropertyChanged(nameof(this.IsShown));
                this.RaisePropertyChanged(nameof(this.Visible));
            }
        }

        private void ZoomToPanel()
        {
            // Get a list of time intervals for all stream visualization objects in this panel
            var streamIntervals = new List<TimeInterval>();
            foreach (var streamVisualizationObject in this.VisualizationObjects.OfType<IStreamVisualizationObject>())
            {
                if (streamVisualizationObject.StreamExtents != TimeInterval.Empty)
                {
                    streamIntervals.Add(streamVisualizationObject.StreamExtents);
                }
            }

            // Zoom to the coverage of the stream visualization objects
            if (streamIntervals.Count > 0)
            {
                TimeInterval panelInterval = TimeInterval.Coverage(streamIntervals);
                this.Container.Navigator.Zoom(panelInterval.Left, panelInterval.Right);
            }
        }

        /// <summary>
        /// Removes a visualization object specified by a view model.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to be removed.</param>
        private void RemoveVisualizationObject(VisualizationObject visualizationObject)
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

            this.RaisePropertyChanged(nameof(this.VisualizationObjects));
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.InitNew();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // After the panel has been deserialized from the layout file, it most likely will contain
            // some visualization objects that will need their property changed handlers hooked up.
            foreach (var yValueRangeProvider in this.VisualizationObjects.OfType<IYValueRangeProvider>())
            {
                yValueRangeProvider.YValueRangeChanged += this.OnVisualizationObjectYValueRangeChanged;
            }
        }
    }
}
