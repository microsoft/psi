// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Point3D = MathNet.Spatial.Euclidean.Point3D;

    /// <summary>
    /// Implements a visualization object that can display a stream of Point3D from an interval as a point cloud.
    /// </summary>
    [VisualizationObject("3D Point (interval) as Point Cloud")]
    public class Point3DIntervalAsPointCloudVisualizationObject : ModelVisual3DIntervalVisualizationObject<Point3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Point3DIntervalAsPointCloudVisualizationObject"/> class.
        /// </summary>
        public Point3DIntervalAsPointCloudVisualizationObject()
            : base()
        {
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets the visualization object for the point cloud.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [DisplayName("Point Cloud")]
        [Description("The point cloud properties.")]
        public PointCloud3DVisualizationObject PointCloud { get; } = new ();

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            if (this.Data != null)
            {
                var pointCloud = new PointCloud3D(this.Data.Where(m => m.Data.HasValue).Select(m => m.Data.Value));
                this.PointCloud.SetCurrentValue(this.SynthesizeMessage(pointCloud));
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.PointCloud.ModelVisual3D, this.Visible && this.Data != null);
        }
    }
}