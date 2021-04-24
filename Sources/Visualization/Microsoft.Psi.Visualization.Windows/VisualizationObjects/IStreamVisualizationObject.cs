// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Represents a stream visualization object.
    /// </summary>
    public interface IStreamVisualizationObject
    {
        /// <summary>
        /// Gets a value indicating whether the source of the stream visualization object is live.
        /// </summary>
        bool IsLive { get; }

        /// <summary>
        /// Gets or sets the name of the stream visualization object.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the stream binding for this visualizationobject.
        /// </summary>
        StreamBinding StreamBinding { get; set; }

        /// <summary>
        /// Gets the source for the stream data, or null if the visualization object is not currently bound to a source.
        /// </summary>
        StreamSource StreamSource { get; }

        /// <summary>
        /// Gets the extents of the visualization object from first message originating time to last message originating time.
        /// </summary>
        TimeInterval StreamExtents { get; }

        /// <summary>
        /// Gets a value indicating whether the visualization object requires the stream's supplemental metadata to be readable and valid.
        /// </summary>
        public bool RequiresSupplementalMetadata { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this object is the selected one when it is in a visual collection.
        /// </summary>
        bool IsTreeNodeSelected { get; set; }

        /// <summary>
        /// Gets a value indicating whether the visualization object is currently bound to a datasource.
        /// </summary>
        bool IsBound { get; }

        /// <summary>
        /// Updates the binding between a stream visualization object and a data source.
        /// </summary>
        /// <param name="currentSession">The current active session view model.</param>
        void UpdateStreamSource(SessionViewModel currentSession);
    }
}
