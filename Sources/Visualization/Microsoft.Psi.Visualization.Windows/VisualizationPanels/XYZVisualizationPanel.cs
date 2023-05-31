// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Media3D;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that 3D visualizers can be rendered in.
    /// </summary>
    public class XYZVisualizationPanel : InstantVisualizationPanel
    {
        private double majorDistance = 5;
        private double minorDistance = 5;
        private double thickness = 0.01;

        private Point3D mouseRightButtonDownCameraPosition;
        private RelayCommand<MouseButtonEventArgs> previewMouseRightButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonUpCommand;

        private Dictionary<string, Timeline> cameraAnimation;
        private Point3D cameraPosition = new (15, 15, 15);
        private Vector3D cameraLookDirection = new (-15, -15, -15);
        private Vector3D cameraUpDirection = new (0, 0, 1);
        private double cameraFieldOfView = 45;
        private bool rotateAroundMouseDownPoint = true;
        private bool zoomAroundMouseDownPoint = true;
        private double rotationSensitivity = 1;
        private double zoomSensitivity = 1;

        private bool showCameraInfo = false;
        private bool showCameraTarget = true;
        private bool showCoordinateSystem = false;
        private bool showFieldOfView = false;
        private bool showFrameRate = false;
        private bool showTriangleCountInfo = false;
        private bool showViewCube = true;

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
        /// Gets the preview mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual RelayCommand<MouseButtonEventArgs> PreviewMouseRightButtonDownCommand
        {
            get
            {
                if (this.previewMouseRightButtonDownCommand == null)
                {
                    this.previewMouseRightButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            // Helix viewport dows not mark the right mouse button up event as handled
                            // as a user has finished manipulating the camera position with the right
                            // mouse button.  As a result, when the user finishes manipulating the
                            // camera position, this event will propagate up to the visualization container
                            // view which will then think the user wishes to launch the context menu.  Note
                            // however that the Helix viewport DOES swallow the right mouse button down
                            // event.
                            //
                            // So, we need to instead handle the preview mouse right button down
                            // event and remember the current camera position.  When the subsequent
                            // right mouse button up event arrives, if the camera position has not
                            // changed it means the user was NOT trying to move the camera position
                            // so we will let that event propagate to the visualization container
                            // which will cause the context menu to be displayed.
                            this.mouseRightButtonDownCameraPosition = this.cameraPosition;
                        });
                }

                return this.previewMouseRightButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets the mouse right button up command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public virtual RelayCommand<MouseButtonEventArgs> MouseRightButtonUpCommand
        {
            get
            {
                if (this.mouseRightButtonUpCommand == null)
                {
                    this.mouseRightButtonUpCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            if (this.mouseRightButtonDownCameraPosition != this.cameraPosition)
                            {
                                // The camera position was moved by the user with the right mouse
                                // button, so do not allow this event to propagate to the visualization
                                // container and cause the context menu to be launched.
                                e.Handled = true;
                            }
                        });
                }

                return this.mouseRightButtonUpCommand;
            }
        }

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
        /// Gets or sets the major distance.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Major Distance")]
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
        [DisplayName("Minor Distance")]
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
        [DisplayName("Thickness")]
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
        [DisplayName("Camera Position")]
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
        [DisplayName("Camera Look Direction")]
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
        [DisplayName("Camera Up Direction")]
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
        [DisplayName("Camera Field of View")]
        [Description("The view camera field of view.")]
        public double CameraFieldOfView
        {
            get { return this.cameraFieldOfView; }
            set { this.Set(nameof(this.CameraFieldOfView), ref this.cameraFieldOfView, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to rotate around the mouse down point.
        /// </summary>
        [DataMember]
        [DisplayName("Rotate Around Mouse Point")]
        [Description("Indicates whether to rotate around the mouse down point.")]
        public bool RotateAroundMouseDownPoint
        {
            get { return this.rotateAroundMouseDownPoint; }
            set { this.Set(nameof(this.RotateAroundMouseDownPoint), ref this.rotateAroundMouseDownPoint, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to zoom around the mouse down point.
        /// </summary>
        [DataMember]
        [DisplayName("Zoom Around Mouse Point")]
        [Description("Indicates whether to zoom around the mouse down point.")]
        public bool ZoomAroundMouseDownPoint
        {
            get { return this.zoomAroundMouseDownPoint; }
            set { this.Set(nameof(this.ZoomAroundMouseDownPoint), ref this.zoomAroundMouseDownPoint, value); }
        }

        /// <summary>
        /// Gets or sets the rotation sensitivity.
        /// </summary>
        [DataMember]
        [DisplayName("Rotation Sensitivity")]
        [Description("The rotation sensitivity.")]
        public double RotationSensitivity
        {
            get { return this.rotationSensitivity; }
            set { this.Set(nameof(this.RotationSensitivity), ref this.rotationSensitivity, value); }
        }

        /// <summary>
        /// Gets or sets the zoom sensitivity.
        /// </summary>
        [DataMember]
        [DisplayName("Zoom Sensitivity")]
        [Description("The zoom sensitivity.")]
        public double ZoomSensitivity
        {
            get { return this.zoomSensitivity; }
            set { this.Set(nameof(this.ZoomSensitivity), ref this.zoomSensitivity, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show camera info.
        /// </summary>
        [DataMember]
        [DisplayName("Show Camera Info")]
        [Description("Indicates whether to show camera info.")]
        public bool ShowCameraInfo
        {
            get { return this.showCameraInfo; }
            set { this.Set(nameof(this.ShowCameraInfo), ref this.showCameraInfo, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the camera target.
        /// </summary>
        [DataMember]
        [DisplayName("Show Camera Target")]
        [Description("Indicates whether to show the camera target.")]
        public bool ShowCameraTarget
        {
            get { return this.showCameraTarget; }
            set { this.Set(nameof(this.ShowCameraTarget), ref this.showCameraTarget, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the coordinate system.
        /// </summary>
        [DataMember]
        [DisplayName("Show Coordinate System")]
        [Description("Indicates whether to show the coordinate system.")]
        public bool ShowCoordinateSystem
        {
            get { return this.showCoordinateSystem; }
            set { this.Set(nameof(this.ShowCoordinateSystem), ref this.showCoordinateSystem, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the field of view.
        /// </summary>
        [DataMember]
        [DisplayName("Show Field of View")]
        [Description("Indicates whether to show the field of view.")]
        public bool ShowFieldOfView
        {
            get { return this.showFieldOfView; }
            set { this.Set(nameof(this.ShowFieldOfView), ref this.showFieldOfView, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the frame rate.
        /// </summary>
        [DataMember]
        [DisplayName("Show Frame Rate")]
        [Description("Indicates whether to show the frame rate.")]
        public bool ShowFrameRate
        {
            get { return this.showFrameRate; }
            set { this.Set(nameof(this.ShowFrameRate), ref this.showFrameRate, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the triangle count info.
        /// </summary>
        [DataMember]
        [DisplayName("Show Triangle Count")]
        [Description("Indicates whether to show triangle count info.")]
        public bool ShowTriangleCountInfo
        {
            get { return this.showTriangleCountInfo; }
            set { this.Set(nameof(this.ShowTriangleCountInfo), ref this.showTriangleCountInfo, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the view cube.
        /// </summary>
        [DataMember]
        [DisplayName("Show View Cube")]
        [Description("Indicates whether to show the view cube.")]
        public bool ShowViewCube
        {
            get { return this.showViewCube; }
            set { this.Set(nameof(this.ShowViewCube), ref this.showViewCube, value); }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new List<VisualizationPanelType>() { VisualizationPanelType.XYZ };

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