// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    public interface IStreamVisualizationObject
    {
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
