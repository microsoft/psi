// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for XYZVisualizationPanelView.xaml.
    /// </summary>
    public partial class XYZVisualizationPanelView : VisualizationPanelViewBase
    {
        private AnimatedModelVisual cameraLocation;
        private bool follow = false;
        private Storyboard cameraStoryboard;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZVisualizationPanelView"/> class.
        /// </summary>
        public XYZVisualizationPanelView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.XYZVisualizationPanelView_DataContextChanged;
            CompositionTarget.Rendering += this.CompositionTarget_Rendering;

            // Register the view camera's name in the namescope so that it can be referenced in storyboard timelines
            NameScope.SetNameScope(this, new NameScope());
            this.RegisterName(nameof(this.ViewCamera), this.ViewCamera);

            this.ViewPort3D.CameraChanged += this.ViewPort3D_CameraChanged;

            // Add a handler for the storyboard completed event.
            this.cameraStoryboard = this.FindResource("CameraStoryboard") as Storyboard;
            this.cameraStoryboard.Completed += this.CameraStoryboard_Completed;
        }

        /// <summary>
        /// Gets ths visualization panel.
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

        private void ViewPort3D_CameraChanged(object sender, RoutedEventArgs e)
        {
            this.UpdateCameraInfoInPanel();
        }

        private void UpdateCameraInfoInPanel()
        {
            // Update the camera position info in the visualization panel for display in the property browser
            this.VisualizationPanel.CameraPosition = this.ViewPort3D.Camera.Position;
            this.VisualizationPanel.CameraLookDirection = this.ViewPort3D.Camera.LookDirection;
            this.VisualizationPanel.CameraUpDirection = this.ViewPort3D.Camera.UpDirection;
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
                this.UpdateCameraInfoInPanel();
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

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (this.cameraLocation != null && this.follow)
            {
                var cameraController = this.ViewPort3D.CameraController;
                var up = this.cameraLocation.TransformToAncestor(this.Root).Transform(this.cameraLocation.CameraTransform.Transform(new Point3D(0, 0, 1)));
                var lookAt = this.cameraLocation.TransformToAncestor(this.Root).Transform(this.cameraLocation.CameraTransform.Transform(new Point3D(1, 0, 0)));
                var lookFrom = this.cameraLocation.TransformToAncestor(this.Root).Transform(this.cameraLocation.CameraTransform.Transform(new Point3D(0, 0, 0)));
                cameraController.CameraPosition = lookFrom;
                cameraController.CameraLookDirection = lookAt - cameraController.CameraPosition;
                this.ViewPort3D.Camera.UpDirection = new Vector3D(0, 0, 1);
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            // Follow mode
            if (e.Key == Key.F)
            {
                this.follow = !this.follow;
                this.cameraLocation = this.FindCameraLocation(this.Root);
            }
        }

        private AnimatedModelVisual FindCameraLocation(ModelVisual3D modelVisual3D)
        {
            if (modelVisual3D is AnimatedModelVisual model && model.IsCameraLocation)
            {
                return model;
            }

            var children = modelVisual3D.Children;
            foreach (var child in children)
            {
                if (child is ModelVisual3D)
                {
                    var first = this.FindCameraLocation((ModelVisual3D)child);
                    if (first != null)
                    {
                        return first;
                    }
                }
            }

            return null;
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
            this.VisualizationPanel.CameraStoryboardCompleted();
        }
    }
}
