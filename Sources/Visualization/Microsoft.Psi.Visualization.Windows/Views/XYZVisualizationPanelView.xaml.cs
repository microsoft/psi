// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for XYZVisualizationPanelView.xaml.
    /// </summary>
    public partial class XYZVisualizationPanelView : InstantVisualizationPanelView
    {
        private Storyboard cameraStoryboard;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZVisualizationPanelView"/> class.
        /// </summary>
        public XYZVisualizationPanelView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.XYZVisualizationPanelView_DataContextChanged;

            // Add a handler for the storyboard completed event.
            this.cameraStoryboard = this.FindResource("CameraStoryboard") as Storyboard;
            this.cameraStoryboard.Completed += this.CameraStoryboard_Completed;
        }

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        protected XYZVisualizationPanel VisualizationPanel => this.DataContext as XYZVisualizationPanel;

        private void AddVisualForVisualizationObject(VisualizationObject visualizationObject)
        {
            Visual3D visual = ((I3DVisualizationObject)visualizationObject).Visual3D;
            this.SortingVisualRoot.Children.Add(visual);
        }

        private void RemoveVisualForVisualizationObject(VisualizationObject visualizationObject)
        {
            Visual3D visual = ((I3DVisualizationObject)visualizationObject).Visual3D;
            this.SortingVisualRoot.Children.Remove(visual);
        }

        private void XYZVisualizationPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.VisualizationPanel != null)
            {
                this.VisualizationPanel.VisualizationObjects.CollectionChanged += this.VisualizationObjects_CollectionChanged;
                foreach (var visualizationObject in this.VisualizationPanel.VisualizationObjects)
                {
                    this.AddVisualForVisualizationObject(visualizationObject);
                }

                this.VisualizationPanel.PropertyChanged += this.VisualizationPanel_PropertyChanged;
            }
        }

        private void VisualizationPanel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYZVisualizationPanel.CameraAnimation))
            {
                this.AnimateCamera();
            }
        }

        private void VisualizationObjects_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var visualizationObject = item as VisualizationObject;
                    this.AddVisualForVisualizationObject(visualizationObject);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                throw new NotImplementedException("Unexpected collectionChanged action.");
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    var visualizationObject = item as VisualizationObject;
                    this.RemoveVisualForVisualizationObject(visualizationObject);
                    this.InvalidateVisual();
                }
            }
            else
            {
                throw new NotImplementedException("Unexpected collectionChanged action.");
            }
        }

        private void AnimateCamera()
        {
            // Stop any existing storyboard that's executing.
            this.cameraStoryboard.Stop();
            this.cameraStoryboard.Children.Clear();

            // For each timeline in the animation, set the view
            // camera as the target and set the target property.
            foreach (KeyValuePair<string, Timeline> timeline in this.VisualizationPanel.CameraAnimation)
            {
                Storyboard.SetTargetName(timeline.Value, nameof(this.ViewCamera));
                Storyboard.SetTargetProperty(timeline.Value, new PropertyPath(timeline.Key));
                this.cameraStoryboard.Children.Add(timeline.Value);
            }

            // Run the storyboard.
            this.cameraStoryboard.Begin();
        }

        private void CameraStoryboard_Completed(object sender, EventArgs e)
        {
            this.VisualizationPanel.NotifyCameraAnimationCompleted();
        }
    }
}
