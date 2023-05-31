// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object that can display a dictionary of point clouds.
    /// </summary>
    [VisualizationObject("3D Point Clouds")]
    public class PointCloud3DDictionaryVisualizationObject :
        ModelVisual3DDictionaryVisualizationObject<PointCloud3DVisualizationObject, int, PointCloud3D>
    {
        private readonly Random random = new ();
        private readonly Dictionary<int, Color> pointCloudColors = new ();

        /// <inheritdoc />
        public override void UpdateData()
        {
            base.UpdateData();
            this.BeginUpdate();

            foreach (var key in this.Keys)
            {
                this[key].Color = this.GetPointCloudColor(key);
            }

            this.EndUpdate();
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