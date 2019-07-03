// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Interface for all 3D visuals in Psi.
    /// </summary>
    /// <typeparam name="TData">The underlying data visualized by this 3d view.</typeparam>
    /// <typeparam name="TConfig">The configuration type associated with this 3d view.</typeparam>
    public interface IView3D<TData, TConfig>
        where TConfig : Instant3DVisualizationObjectConfiguration, new()
    {
        /// <summary>
        /// Updates the view with new configuration values.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        void UpdateConfiguration(TConfig newConfig);

        /// <summary>
        /// Update the view with new data to visualize.
        /// </summary>
        /// <param name="newData">The new data to visualize.</param>
        /// <param name="originatingTime">Originating time for the data.</param>
        void UpdateData(TData newData, DateTime originatingTime);

        /// <summary>
        /// Clear the view completely.
        /// </summary>
        void ClearAll();
    }
}
