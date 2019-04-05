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

    /// <summary>
    /// Base class for visualization objects
    /// </summary>
    public abstract class VisualizationObject : ObservableObject
    {
        /// <summary>
        /// The visualization panel this visualization object is parented under.
        /// </summary>
        private VisualizationPanel panel;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObject"/> class.
        /// </summary>
        public VisualizationObject()
        {
            this.InitNew();
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
        /// Gets the path to the visualization object's icon
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual string IconSource => IconSourcePath.Stream;

        /// <summary>
        /// Gets a value indicating whether this visualization object can use the snap to stream functionality
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool CanSnapToStream => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is currently the one being snapped to
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool IsSnappedToStream => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is an audio stream
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual bool IsAudioStream => false;

        /// <summary>
        /// Gets a value indicating whether this visualization object is connected to a panel
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsConnected => this.panel != null;

        /// <summary>
        /// Snaps or unsnaps the parent container to this stream
        /// </summary>
        /// <param name="snapToStream">true if this object should be snapped, otherwise false</param>
        [Browsable(false)]
        public virtual void SnapToStream(bool snapToStream)
        {
        }

        /// <summary>
        /// Disconnect from the panel.
        /// </summary>
        internal void Disconnect()
        {
            // if this visualization object has already been disconnected, throw an exception
            if (this.Panel == null)
            {
                throw new InvalidOperationException("This visualization object is already disconnected.");
            }

            this.OnDisconnect();
            this.Panel = null;
        }

        /// <summary>
        /// Connect to the panel.
        /// </summary>
        /// <param name="panel">Panel to connect this visualization object to.</param>
        internal void ConnectToPanel(VisualizationPanel panel)
        {
            // if this visualization object is already connected to a different panel, throw an exception
            if (this.panel != null)
            {
                throw new InvalidOperationException("This visualization object is already connected into a panel.");
            }

            this.Panel = panel;
            this.OnConnect();
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
        /// Notifies the visualization object that the cursor mode has changed
        /// </summary>
        /// <param name="sender">The object that triggered the change event.</param>
        /// <param name="cursorModeEventArgs">The old and new cursor modes.</param>
        protected virtual void OnCursorModeChanged(object sender, CursorModeChangedEventArgs cursorModeEventArgs)
        {
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
        }

        /// <summary>
        /// Overridable method to allow derived VisualizationObject to react whenever the Configuration property has changed.
        /// </summary>
        protected virtual void OnConfigurationChanged()
        {
        }

        /// <summary>
        /// Overridable method to allow derived VisualizationObject to react whenever a property on the Configuration property has changed.
        /// </summary>
        /// <param name="sender">The sender that triggered the change event.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to being loaded.
        /// </summary>
        protected virtual void OnConnect()
        {
            this.Navigator.CursorChanged += this.OnCursorChanged;
            this.Navigator.CursorModeChanged += this.OnCursorModeChanged;
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to being unloaded.
        /// </summary>
        protected virtual void OnDisconnect()
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