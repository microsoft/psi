// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    public interface IStreamVisualizationObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether the source of the stream visualization object is live.
        /// </summary>
        bool IsLive { get; set; }

        /// <summary>
        /// Gets or sets the name of the stream visualization object.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the stream binding for this visualizationobject.
        /// </summary>
        StreamBinding StreamBinding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this object is the selected one when it is in a visual collection.
        /// </summary>
        bool IsTreeNodeSelected { get; set; }

        /// <summary>
        /// Updates the binding between a stream visualization object and a data source.
        /// </summary>
        /// <param name="session">The current session.</param>
        void UpdateStreamBinding(Session session);

        /// <summary>
        /// Gets a snapped time based on a given time.
        /// </summary>
        /// <param name="time">The input time.</param>
        /// <param name="snappingBehavior">Timeline snapping behavior.</param>
        /// <returns>The snapped time.</returns>
        DateTime? GetSnappedTime(DateTime time, SnappingBehavior snappingBehavior = SnappingBehavior.Nearest);
    }
}
