// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Newtonsoft.Json.Serialization;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Base class for visualization objects.
    /// </summary>
    public abstract class VisualizationObject : ObservableTreeNodeObject
    {
        /// <summary>
        /// The name of the visualization object.
        /// </summary>
        private string name;

        /// <summary>
        /// Indicated whether the visualization object is visible.
        /// </summary>
        private bool visible = true;

        /// <summary>
        /// The visualization panel this visualization object is parented under.
        /// </summary>
        private VisualizationPanel panel;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObject"/> class.
        /// </summary>
        public VisualizationObject()
        {
            this.PropertyChanging += this.OnPropertyChanging;
            this.PropertyChanged += this.OnPropertyChanged;
            this.InitNew();
        }

        /// <summary>
        /// Gets or sets the name of the visualization object.
        /// </summary>
        [DataMember]
        [Description("The name of the visualization object.")]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the visualization object is visible.
        /// </summary>
        [DataMember]
        [Description("The visibility of the visualization object.")]
        public bool Visible
        {
            get { return this.visible; }
            set { this.Set(nameof(this.Visible), ref this.visible, value); }
        }

        /// <summary>
        /// Gets the default DataTemplate that is used within a VisualizationPanel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public abstract DataTemplate DefaultViewTemplate { get; }

        /// <summary>
        /// Gets the parent VisualizationContainer.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationContainer Container => this.panel?.Container;

        /// <summary>
        /// Gets the parent VisualizationPanel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationPanel Panel
        {
            get => this.panel;
            private set
            {
                this.RaisePropertyChanging(nameof(this.IsConnected));
                this.panel = value;
                this.RaisePropertyChanged(nameof(this.IsConnected));
            }
        }

        /// <summary>
        /// Gets the navigator this object is bound to.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Navigator Navigator => this.panel?.Container.Navigator;

        /// <summary>
        /// Gets the path to the visualization object's icon.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual string IconSource => IconSourcePath.Stream;

        /// <summary>
        /// Gets a value indicating whether this visualization object can use the snap to stream functionality.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool CanSnapToStream => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is currently the one being snapped to.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool IsSnappedToStream => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is an audio stream.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool IsAudioStream => false;

        /// <summary>
        /// Gets a value indicating whether to display the zoom to stream menuitem for this stream.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool ShowZoomToStreamMenuItem => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is connected to a panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsConnected => this.panel != null;

        /// <summary>
        /// Gets the contract resolver. Default is null.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        protected virtual IContractResolver ContractResolver => null;

        /// <summary>
        /// Snaps or unsnaps the navigation cursor to the visualization object.
        /// </summary>
        [Browsable(false)]
        public virtual void ToggleSnapToStream()
        {
        }

        /// <summary>
        /// Removes the visualization object from the parent panel.
        /// </summary>
        internal void RemoveFromPanel()
        {
            // if this visualization object has already been disconnected, throw an exception
            if (this.Panel == null)
            {
                throw new InvalidOperationException("This visualization object is already disconnected.");
            }

            this.OnRemoveFromPanel();
            this.Panel = null;
        }

        /// <summary>
        /// Add the visualization object to a specified panel.
        /// </summary>
        /// <param name="panel">Panel to add this visualization object to.</param>
        internal void AddToPanel(VisualizationPanel panel)
        {
            // if this visualization object is already connected to a different panel, throw an exception
            if (this.panel != null)
            {
                throw new InvalidOperationException("This visualization object is already connected to a panel.");
            }

            this.Panel = panel;
            this.OnAddToPanel();
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to cursor changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains navigator changed event data.</param>
        protected virtual void OnCursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
        }

        /// <summary>
        /// Notifies the visualization object that the cursor mode has changed.
        /// </summary>
        /// <param name="sender">The object that triggered the change event.</param>
        /// <param name="cursorModeEventArgs">The old and new cursor modes.</param>
        protected virtual void OnCursorModeChanged(object sender, CursorModeChangedEventArgs cursorModeEventArgs)
        {
        }

        /// <summary>
        /// Initialize properties for visualization objects as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
        }

        /// <summary>
        /// Implements a response to a notification that a property of the visualization object is changing.
        /// </summary>
        /// <param name="sender">The sender of the notification.</param>
        /// <param name="e">The details of the notification.</param>
        protected virtual void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
        }

        /// <summary>
        /// Implements a response to a notification that a property of the visualization object has changed.
        /// </summary>
        /// <param name="sender">The sender of the notification.</param>
        /// <param name="e">The details of the notification.</param>
        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Implements a response to a notification the visualization object has been added to a panel.
        /// </summary>
        protected virtual void OnAddToPanel()
        {
            this.Navigator.CursorChanged += this.OnCursorChanged;
            this.Navigator.CursorModeChanged += this.OnCursorModeChanged;
        }

        /// <summary>
        /// Implements a response to a notification the visualization object has been removed from the panel.
        /// </summary>
        protected virtual void OnRemoveFromPanel()
        {
            this.Navigator.CursorChanged -= this.OnCursorChanged;
            this.Navigator.CursorModeChanged -= this.OnCursorModeChanged;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            this.InitNew();
        }
    }
}