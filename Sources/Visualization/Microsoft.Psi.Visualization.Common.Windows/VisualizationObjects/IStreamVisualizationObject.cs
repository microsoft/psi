// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Data;

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
        /// Gets the stream binding for this visualizationobject.
        /// </summary>
        StreamBinding StreamBinding { get; }

        /// <summary>
        /// Updates the binding between a stream visualization object and a data source.
        /// </summary>
        /// <param name="session">The current session.</param>
        void UpdateStreamBinding(Session session);

        /// <summary>
        /// Gets a snapped time based on a given time.
        /// </summary>
        /// <param name="time">The input time.</param>
        /// <returns>The snapped time.</returns>
        DateTime? GetSnappedTime(DateTime time);
    }
}
