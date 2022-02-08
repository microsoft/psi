// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;

    /// <summary>
    /// Provides data for editing a time interval annotation value.
    /// </summary>
    public class TimeIntervalAnnotationEditEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationEditEventArgs"/> class.
        /// </summary>
        /// <param name="displayData">The annotation display object to edit.</param>
        /// <param name="attributeIndex">The index of the attribute to edit.</param>
        public TimeIntervalAnnotationEditEventArgs(TimeIntervalAnnotationDisplayData displayData, int attributeIndex)
        {
            this.DisplayData = displayData;
            this.AttributeIndex = attributeIndex;
        }

        /// <summary>
        /// Gets the annotation to edit.
        /// </summary>
        public TimeIntervalAnnotationDisplayData DisplayData { get; private set; }

        /// <summary>
        /// Gets the index of the attribute to edit.
        /// </summary>
        public int AttributeIndex { get; private set; }
    }
}
