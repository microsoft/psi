// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display a list of point clouds.
    /// </summary>
    [VisualizationObject("3D Point Clouds")]
    public class PointCloud3DListVisualizationObject : ModelVisual3DListVisualizationObject<PointCloud3DVisualizationObject, PointCloud3D>
    {
        private readonly Random random = new ();
        private readonly Dictionary<int, Color> pointCloudColors = new ();

        /// <inheritdoc />
        public override void UpdateVisual3D()
        {
            base.UpdateVisual3D();
            for (int i = 0; i < this.Items.Count(); i++)
            {
                this.Items.ElementAt(i).Color = this.GetPointCloudColor(i);
            }
        }

        private Color GetPointCloudColor(int key)
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