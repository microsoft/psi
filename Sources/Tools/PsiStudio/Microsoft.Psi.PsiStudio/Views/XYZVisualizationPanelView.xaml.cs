// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Server;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for XYZVisualizationPanelView.xaml
    /// </summary>
    public partial class XYZVisualizationPanelView : UserControl
    {
        private AnimatedModelVisual cameraLocation;
        private bool follow = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZVisualizationPanelView"/> class.
        /// </summary>
        public XYZVisualizationPanelView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.XYZVisualizationPanelView_DataContextChanged;
            CompositionTarget.Rendering += this.CompositionTarget_Rendering;
        }

        /// <summary>
        /// Gets ths visualization panel.
        /// </summary>
        protected VisualizationPanel VisualizationPanel => this.DataContext as VisualizationPanel;

        private void AddVisualForVisualizationObject(IRemoteVisualizationObject visualizationObject)
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
            this.VisualizationPanel.VisualizationObjects.CollectionChanged += this.VisualizationObjects_CollectionChanged;
            foreach (var visualizationObject in this.VisualizationPanel.VisualizationObjects)
            {
                this.AddVisualForVisualizationObject(visualizationObject);
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

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Set the current panel on click
            if (!this.VisualizationPanel.IsCurrentPanel)
            {
                this.VisualizationPanel.Container.CurrentPanel = this.VisualizationPanel;
            }
        }

        private void RemovePanel_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Container.RemovePanel(this.VisualizationPanel);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.VisualizationPanel.Clear();
        }
    }
}
