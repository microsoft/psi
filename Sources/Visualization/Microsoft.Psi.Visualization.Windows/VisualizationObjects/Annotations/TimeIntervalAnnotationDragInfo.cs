// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Represents the metadata for dragging the edge of a time interval annotation and possibly also dragging an abutting time interval annotation.
    /// </summary>
    internal class TimeIntervalAnnotationDragInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationDragInfo"/> class.
        /// </summary>
        /// <param name="leftAnnotationMessage">The message that contains the left annotation being dragged (or null).</param>
        /// <param name="rightAnnotationMessage">The message that contains the right annotation being dragged (or null).</param>
        /// <param name="minimumTime">The minimum time the edge can be dragged to.</param>
        /// <param name="maximumTime">The maximum time the edge can be dragged to.</param>
        public TimeIntervalAnnotationDragInfo(Message<TimeIntervalAnnotation>? leftAnnotationMessage, Message<TimeIntervalAnnotation>? rightAnnotationMessage, DateTime minimumTime, DateTime maximumTime)
        {
            this.LeftAnnotationMessage = leftAnnotationMessage;
            this.RightAnnotationMessage = rightAnnotationMessage;
            this.MinimumTime = minimumTime;
            this.MaximumTime = maximumTime;
        }

        /// <summary>
        /// Gets the message that contains the left annotation.
        /// </summary>
        public Message<TimeIntervalAnnotation>? LeftAnnotationMessage { get; private set; }

        /// <summary>
        /// Gets the message that contains the right annotation.
        /// </summary>
        public Message<TimeIntervalAnnotation>? RightAnnotationMessage { get; private set; }

        /// <summary>
        /// Gets the annotation on the left, or null.
        /// </summary>
        public TimeIntervalAnnotation LeftAnnotation => this.LeftAnnotationMessage.HasValue ? this.LeftAnnotationMessage.Value.Data : null;

        /// <summary>
        /// Gets the annotation on the right, or null.
        /// </summary>
        public TimeIntervalAnnotation RightAnnotation => this.RightAnnotationMessage.HasValue ? this.RightAnnotationMessage.Value.Data : null;

        /// <summary>
        /// Gets the minimum time that the edge can be dragged to.
        /// </summary>
        public DateTime MinimumTime { get; private set; }

        /// <summary>
        /// Gets the maximum time that the edge can be dragged to.
        /// </summary>
        public DateTime MaximumTime { get; private set; }
    }
}
