// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides an abstract base class for visualization object views.
    /// </summary>
    public abstract class VisualizationObjectView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObjectView"/> class.
        /// </summary>
        public VisualizationObjectView()
        {
            this.DataContextChanged += this.OnDataContextChanged;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        /// <summary>
        /// Gets the visualization object.
        /// </summary>
        public VisualizationObject VisualizationObject { get; private set; }

        /// <summary>
        /// Gets the navigator for the visualization object.
        /// </summary>
        public Navigator Navigator => this.VisualizationObject.Navigator;

        /// <summary>
        /// Called when the data context is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The dependency property changed event arguments.</param>
        protected virtual void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.VisualizationObject = this.DataContext as VisualizationObject;

            // check that the visualization object is connected
            if (!this.VisualizationObject.IsConnected)
            {
                throw new Exception("Visualization object should be connected by the time the view is attached.");
            }

            this.VisualizationObject.PropertyChanging += this.DispatchVisualizationObjectPropertyChanging;
            this.VisualizationObject.PropertyChanged += this.DispatchVisualizationObjectPropertyChanged;
        }

        /// <summary>
        /// Called when the view is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the view is unloaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.VisualizationObject.PropertyChanging -= this.DispatchVisualizationObjectPropertyChanging;
            this.VisualizationObject.PropertyChanged -= this.DispatchVisualizationObjectPropertyChanged;
        }

        /// <summary>
        /// Called when a property of the visualization object is about to change.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.VisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is
                // being disconnected from the panel, so detach all handlers
                this.VisualizationObject.PropertyChanging -= this.DispatchVisualizationObjectPropertyChanging;
                this.VisualizationObject.PropertyChanged -= this.DispatchVisualizationObjectPropertyChanged;
            }
        }

        /// <summary>
        /// Called when a property of the visualization object has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>This method will be called on the application dispatcher thread.</remarks>
        protected virtual void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void DispatchVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
            => Application.Current.Dispatcher.Invoke(() => this.OnVisualizationObjectPropertyChanging(sender, e));

        private void DispatchVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
            => Application.Current.Dispatcher.Invoke(() => this.OnVisualizationObjectPropertyChanged(sender, e));
    }
}
