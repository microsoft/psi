// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Spatial.Euclidean.Visualization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for the <see cref="Object3DTrackingResults"/>.
    /// </summary>
    [VisualizationObject("Object 3D Tracking Results")]
    public class Object3DTrackingResultsVisualizationObject : ModelVisual3DValueVisualizationObject<Object3DTrackingResults>
    {
        private readonly Dictionary<(string Class, string InstanceId), (BillboardTextVisualizationObject BillboardVisual, PointCloud3DVisualizationObject PointCloudVisual)> visuals = new ();
        private readonly Dictionary<string, Color> pointCloudColors = new ();
        private readonly Random random = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="Object3DTrackingResultsVisualizationObject"/> class.
        /// </summary>
        public Object3DTrackingResultsVisualizationObject()
        {
        }

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            // Make all visuals that no longer have corresponding detections invisible
            foreach (var key in this.visuals.Keys)
            {
                if (this.CurrentData == null || !this.CurrentData.Detections.Any(t => t.Class == key.Class && t.InstanceId == key.InstanceId))
                {
                    this.visuals[key].BillboardVisual.Visible = false;
                    this.visuals[key].PointCloudVisual.Visible = false;
                }
            }

            // Add new visuals for detections that don't have a corresponding visual
            if (this.CurrentData != null)
            {
                foreach (var entry in this.CurrentData.Detections)
                {
                    var entryColor = this.GetPointCloudColor(entry.Class);
                    var key = (entry.Class, entry.InstanceId);

                    if (!this.visuals.Keys.Any(k => k.Class == entry.Class && k.InstanceId == entry.InstanceId))
                    {
                        this.visuals.Add(key, (new (), new ()));
                    }

                    this.visuals[key].BillboardVisual.BillboardFontSize = 20;
                    this.visuals[key].BillboardVisual.BillboardBorderThickness = 2;
                    this.visuals[key].BillboardVisual.BillboardBorderColor = entryColor;
                    var centroid = entry.PointCloud.Centroid;
                    var billboardValue = Tuple.Create(new Point3D(centroid.X, centroid.Y, centroid.Z + 0.1), entry.Class);
                    this.visuals[key].BillboardVisual.SetCurrentValue(this.SynthesizeMessage(billboardValue));
                    this.visuals[key].BillboardVisual.Visible = true;

                    this.visuals[key].PointCloudVisual.Color = entryColor;
                    this.visuals[key].PointCloudVisual.PointSize = 3;
                    this.visuals[key].PointCloudVisual.SetCurrentValue(this.SynthesizeMessage(entry.PointCloud));
                    this.visuals[key].PointCloudVisual.Visible = true;
                }
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
            foreach (var visual in this.visuals.Values)
            {
                this.UpdateChildVisibility(visual.BillboardVisual.Visual3D, this.Visible && visual.BillboardVisual.Visible);
                this.UpdateChildVisibility(visual.PointCloudVisual.Visual3D, this.Visible && visual.PointCloudVisual.Visible);
            }
        }

        private Color GetPointCloudColor(string key)
        {
            byte RandomChannelValue()
            {
                return (byte)this.random.Next(64, 192);
            }

            if (!this.pointCloudColors.ContainsKey(key))
            {
                this.pointCloudColors.Add(
                    key,
                    Color.FromRgb(
                        RandomChannelValue(),
                        RandomChannelValue(),
                        RandomChannelValue()));
            }

            return this.pointCloudColors[key];
        }
    }
}
