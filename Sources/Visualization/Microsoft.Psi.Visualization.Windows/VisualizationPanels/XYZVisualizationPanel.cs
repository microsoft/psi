// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that 3D visualizers can be rendered in.
    /// </summary>
    public class XYZVisualizationPanel : VisualizationPanel
    {
        private int relativeWidth = 100;
        private double majorDistance = 5;
        private double minorDistance = 5;
        private double thickness = 0.01;

        /// <summary>
        /// The current plan for moving the camera.
        /// </summary>
        private Dictionary<string, Timeline> cameraAnimation;

        /// <summary>
        /// The current camera position.
        /// </summary>
        private Point3D cameraPosition = new Point3D(15, 15, 15);

        /// <summary>
        /// The current camera look direction.
        /// </summary>
        private Vector3D cameraLookDirection = new Vector3D(-15, -15, -15);

        /// <summary>
        /// The current camera up direction.
        /// </summary>
        private Vector3D cameraUpDirection = new Vector3D(0, 0, 1);

        /// <summary>
        /// The current camera field of view.
        /// </summary>
        private double cameraFieldOfView = 45;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZVisualizationPanel"/> class.
        /// </summary>
        public XYZVisualizationPanel()
        {
            this.Name = "3D Panel";
        }

        /// <summary>
        /// Gets the camera animation completed event.
        /// </summary>
        public event EventHandler CameraAnimationCompleted;

        /// <summary>
        /// Gets or sets the current camera animation.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Dictionary<string, Timeline> CameraAnimation
        {
            get { return this.cameraAnimation; }
            set { this.Set(nameof(this.CameraAnimation), ref this.cameraAnimation, value); }
        }

        /// <summary>
        /// Gets or sets the name of the relative width for the panel.
        /// </summary>
        [DataMember]
        [Description("The relative width for the panel.")]
        public int RelativeWidth
        {
            get { return this.relativeWidth; }
            set { this.Set(nameof(this.RelativeWidth), ref this.relativeWidth, value); }
        }

        /// <summary>
        /// Gets or sets the major distance.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [Description("The major distance for the grid.")]
        public double MajorDistance
        {
            get { return this.majorDistance; }
            set { this.Set(nameof(this.MajorDistance), ref this.majorDistance, value); }
        }

        /// <summary>
        /// Gets or sets the minor distance.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [Description("The minor distance for the grid.")]
        public double MinorDistance
        {
            get { return this.minorDistance; }
            set { this.Set(nameof(this.MinorDistance), ref this.minorDistance, value); }
        }

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [Description("The thickness of the gridlines.")]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }

        /// <summary>
        /// Gets or sets the view's camera position.
        /// </summary>
        [DataMember]
        [ExpandableObject]
        [Description("The view camera position.")]
        public Point3D CameraPosition
        {
            get { return this.cameraPosition; }
            set { this.Set(nameof(this.CameraPosition), ref this.cameraPosition, value); }
        }

        /// <summary>
        /// Gets or sets the view's camera look direction.
        /// </summary>
        [DataMember]
        [ExpandableObject]
        [Description("The view camera look direction.")]
        public Vector3D CameraLookDirection
        {
            get { return this.cameraLookDirection; }
            set { this.Set(nameof(this.CameraLookDirection), ref this.cameraLookDirection, value); }
        }

        /// <summary>
        /// Gets or sets the view's camera up direction.
        /// </summary>
        [DataMember]
        [ExpandableObject]
        [Description("The view camera up direction.")]
        public Vector3D CameraUpDirection
        {
            get { return this.cameraUpDirection; }
            set { this.Set(nameof(this.CameraUpDirection), ref this.cameraUpDirection, value); }
        }

        /// <summary>
        /// Gets or sets the view's camera field of view (in degrees).
        /// </summary>
        [DataMember]
        [ExpandableObject]
        [Description("The view camera field of view.")]
        public double CameraFieldOfView
        {
            get { return this.cameraFieldOfView; }
            set { this.Set(nameof(this.CameraFieldOfView), ref this.cameraFieldOfView, value); }
        }

        /// <summary>
        /// Called when the views's current camera animation has completed.
        /// </summary>
        public void NotifyCameraAnimationCompleted()
        {
            this.CameraAnimationCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(XYZVisualizationPanelView));
        }
    }
}