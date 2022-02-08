// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;

    /// <summary>
    /// Implements a list of 3D points as a point cloud visualization object.
    /// </summary>
    [VisualizationObject("3D Point Cloud")]
    public class Point3DListAsPointCloudVisualizationObject : ModelVisual3DVisualizationObject<List<Point3D>>
    {
        private readonly PointsVisual3D pointsVisual3D;

        private Color color = Colors.Gray;
        private double pointSize = 1.0;
        private string numberOfPoints = "N/A";

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3DListAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public Point3DListAsPointCloudVisualizationObject()
        {
            this.pointsVisual3D = new PointsVisual3D()
            {
                Color = this.color,
                Size = this.pointSize,
            };
        }

        /// <summary>
        /// Gets or sets the point cloud color.
        /// </summary>
        [DataMember]
        [DisplayName("Color")]
        [Description("The color of the point cloud.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the point size.
        /// </summary>
        [DataMember]
        [DisplayName("Point Size")]
        [Description("The size of a point in the cloud.")]
        public double PointSize
        {
            get { return this.pointSize; }
            set { this.Set(nameof(this.PointSize), ref this.pointSize, value); }
        }

        /// <summary>
        /// Gets the number of points in the point-cloud.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(true)]
        [DisplayName("Number of points")]
        [Description("The number of points in the current point cloud.")]
        public string NumberOfPoints
        {
            get => this.numberOfPoints;
            private set => this.Set(nameof(this.NumberOfPoints), ref this.numberOfPoints, value);
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.NumberOfPoints = this.CurrentData != null ? this.CurrentData.Count.ToString() : "N/A";
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color))
            {
                this.pointsVisual3D.Color = this.Color;
            }
            else if (propertyName == nameof(this.PointSize))
            {
                this.pointsVisual3D.Size = this.PointSize;
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            if (this.CurrentData == null)
            {
                this.pointsVisual3D.Points.Clear();
            }
            else
            {
                this.pointsVisual3D.Points = new Point3DCollection(this.CurrentData);
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.pointsVisual3D, this.Visible && this.CurrentData != default);
        }
    }
}
