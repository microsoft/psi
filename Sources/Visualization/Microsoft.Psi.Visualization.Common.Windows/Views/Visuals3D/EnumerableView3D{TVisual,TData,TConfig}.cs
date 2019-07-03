// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Represents a collection of model visual 3D views.
    /// </summary>
    /// <typeparam name="TVisual">The type of visual being collected.</typeparam>
    /// <typeparam name="TData">The underlying data being visualized inside the collection.</typeparam>
    /// <typeparam name="TConfig">The configuration associated with TData.</typeparam>
    public class EnumerableView3D<TVisual, TData, TConfig> : ModelVisual3D, IView3D<IEnumerable<TData>, TConfig>
        where TVisual : ModelVisual3D, IView3D<TData, TConfig>, new()
        where TConfig : Instant3DVisualizationObjectConfiguration, new()
    {
        private readonly List<TVisual> currentVisuals = new List<TVisual>();
        private TConfig currentConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableView3D{TVisual,TData,TConfig}"/> class.
        /// </summary>
        public EnumerableView3D()
        {
            this.currentConfiguration = new TConfig();
        }

        /// <inheritdoc/>
        public void UpdateData(IEnumerable<TData> dataCollection, DateTime originatingTime)
        {
            int dataIndex = 0;
            foreach (var data in dataCollection)
            {
                if (dataIndex >= this.currentVisuals.Count)
                {
                    var newVisual = new TVisual();
                    newVisual.UpdateConfiguration(this.currentConfiguration);
                    newVisual.UpdateData(data, originatingTime);
                    this.currentVisuals.Add(newVisual);
                    this.Children.Add(newVisual);
                }
                else
                {
                    this.currentVisuals[dataIndex].UpdateData(data, originatingTime);
                }

                dataIndex++;
            }

            this.currentVisuals.RemoveRange(dataIndex, this.currentVisuals.Count - dataIndex);
        }

        /// <inheritdoc/>
        public void UpdateConfiguration(TConfig newConfiguration)
        {
            this.currentConfiguration = newConfiguration;
            foreach (var currentVisual in this.currentVisuals)
            {
                currentVisual.UpdateConfiguration(this.currentConfiguration);
            }
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            foreach (var currentVisual in this.currentVisuals)
            {
                currentVisual.ClearAll();
            }

            this.currentVisuals.Clear();
            this.Children.Clear();
        }
    }
}
