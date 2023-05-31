// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Provides an abstract base class for visualization objects.
    /// </summary>
    public abstract class VisualizationObject : ObservableTreeNodeObject, IContextMenuItemsSource
    {
        /// <summary>
        /// The name of the visualization object.
        /// </summary>
        private string name;

        /// <summary>
        /// Indicates whether the visualization object should be visible.
        /// </summary>
        private bool visible = true;

        /// <summary>
        /// The visualization panel this visualization object is parented under.
        /// </summary>
        private VisualizationPanel panel;

        /// <summary>
        /// The positive magnitude of the cursor epsilon (this value can be modified in the properties window).
        /// </summary>
        private int cursorEpsilonPosMs;

        /// <summary>
        /// The negative magnitude of the cursor epsilon (this value can be modified in the properties window).
        /// </summary>
        private int cursorEpsilonNegMs;

        /// <summary>
        /// Gets or sets the epsilon interval around the cursor used when reading data.
        /// </summary>
        private RelativeTimeInterval cursorEpsilon;

        /// <summary>
        /// The toggle visualization command.
        /// </summary>
        private RelayCommand toggleVisibilityCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObject"/> class.
        /// </summary>
        public VisualizationObject()
        {
            this.PropertyChanging += this.OnPropertyChanging;
            this.PropertyChanged += this.OnPropertyChanged;
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
        /// Gets the type name of the visualization object.
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Visualizer Type")]
        [Description("The type of the visualization object.")]
        public string VisualizerTypeName => TypeSpec.Simplify(this.GetType().Name);

        /// <summary>
        /// Gets or sets a value indicating whether the visualization object should be visible.
        /// </summary>
        [DataMember]
        [Description("Indicates whether the visualization object should be visible.")]
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
        /// Gets the cursor epsilon.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelativeTimeInterval CursorEpsilon
        {
            get => this.cursorEpsilon;
            private set
            {
                this.Set(nameof(this.CursorEpsilon), ref this.cursorEpsilon, value);
            }
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        [DisplayName("Cursor Epsilon Future (ms)")]
        [Description("The epsilon future duration relative to the cursor (in milliseconds) to consider when finding messages to visualize.")]
        public int CursorEpsilonPosMs
        {
            get { return this.cursorEpsilonPosMs; }

            set
            {
                this.cursorEpsilonPosMs = value;
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.cursorEpsilonNegMs), TimeSpan.FromMilliseconds(this.cursorEpsilonPosMs));
            }
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        [DisplayName("Cursor Epsilon Past (ms)")]
        [Description("The epsilon past duration relative to the cursor (in milliseconds) to consider when finding messages to visualize.")]
        public int CursorEpsilonNegMs
        {
            get { return this.cursorEpsilonNegMs; }

            set
            {
                this.cursorEpsilonNegMs = value;
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.cursorEpsilonNegMs), TimeSpan.FromMilliseconds(this.cursorEpsilonPosMs));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the visualization object is shown.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public bool IsShown => this.Visible && this.panel != null && this.panel.IsShown;

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
        /// Gets the toggle visualization command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ToggleVisibilityCommand
        {
            get
            {
                if (this.toggleVisibilityCommand == null)
                {
                    this.toggleVisibilityCommand = new RelayCommand(() => this.Panel.ToggleVisualizationObjectVisibility(this));
                }

                return this.toggleVisibilityCommand;
            }
        }

        /// <summary>
        /// Gets the contract resolver. Default is null.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        protected virtual IContractResolver ContractResolver => null;

        /// <inheritdoc/>
        public virtual List<ContextMenuItemInfo> ContextMenuItemsInfo()
            => new ()
            {
                // Add the show/hide command
                new ContextMenuItemInfo(
                    IconSourcePath.ToggleVisibility,
                    this.Visible ? "Hide Visualizer" : "Show Visualizer",
                    this.ToggleVisibilityCommand),

                // Add the remove from panel command
                new ContextMenuItemInfo(
                    IconSourcePath.RemovePanel,
                    "Remove Visualizer",
                    this.Panel.DeleteVisualizationCommand,
                    commandParameter: this),
            };

        /// <summary>
        /// Snaps or unsnaps the navigation cursor to the visualization object.
        /// </summary>
        public virtual void ToggleSnapToStream()
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
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
        /// Implements a response to a notification that a property of the visualization panel is changing.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnPanelPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
        }

        /// <summary>
        /// Implements a response to a notification that a property of the visualization panel has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VisualizationPanel.Visible))
            {
                this.RaisePropertyChanged(nameof(this.IsShown));
            }
        }

        /// <summary>
        /// Implements a response to a notification the visualization object has been added to a panel.
        /// </summary>
        protected virtual void OnAddToPanel()
        {
            this.Navigator.CursorChanged += this.OnCursorChanged;
            this.Navigator.CursorModeChanged += this.OnCursorModeChanged;
            this.Panel.PropertyChanging += this.OnPanelPropertyChanging;
            this.Panel.PropertyChanged += this.OnPanelPropertyChanged;
        }

        /// <summary>
        /// Implements a response to a notification the visualization object has been removed from the panel.
        /// </summary>
        protected virtual void OnRemoveFromPanel()
        {
            this.Navigator.CursorChanged -= this.OnCursorChanged;
            this.Navigator.CursorModeChanged -= this.OnCursorModeChanged;
            this.Panel.PropertyChanging -= this.OnPanelPropertyChanging;
            this.Panel.PropertyChanged -= this.OnPanelPropertyChanged;
        }
    }
}