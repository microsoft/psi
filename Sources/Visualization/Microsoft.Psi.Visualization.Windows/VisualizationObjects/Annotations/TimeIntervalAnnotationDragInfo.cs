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
        /// <param name="track">The track for the annotation.</param>
        /// <param name="leftAnnotationSetMessage">The message that contains the left annotation being dragged (or null).</param>
        /// <param name="rightAnnotationSetMessage">The message that contains the right annotation being dragged (or null).</param>
        /// <param name="minimumTime">The minimum time the edge can be dragged to.</param>
        /// <param name="maximumTime">The maximum time the edge can be dragged to.</param>
        public TimeIntervalAnnotationDragInfo(
            string track,
            Message<TimeIntervalAnnotationSet>? leftAnnotationSetMessage,
            Message<TimeIntervalAnnotationSet>? rightAnnotationSetMessage,
            DateTime minimumTime,
            DateTime maximumTime)
        {
            this.Track = track;
            this.LeftAnnotationSetMessage = leftAnnotationSetMessage;
            this.RightAnnotationSetMessage = rightAnnotationSetMessage;
            this.MinimumTime = minimumTime;
            this.MaximumTime = maximumTime;
        }

        /// <summary>
        /// Gets the annotation track.
        /// </summary>
        public string Track { get; private set; }

        /// <summary>
        /// Gets the annotation set message that contains the left annotation.
        /// </summary>
        public Message<TimeIntervalAnnotationSet>? LeftAnnotationSetMessage { get; private set; }

        /// <summary>
        /// Gets the annotation set message that contains the right annotation.
        /// </summary>
        public Message<TimeIntervalAnnotationSet>? RightAnnotationSetMessage { get; private set; }

        /// <summary>
        /// Gets the annotation on the left, or null.
        /// </summary>
        public TimeIntervalAnnotation LeftAnnotation => this.LeftAnnotationSetMessage.HasValue ? this.LeftAnnotationSetMessage.Value.Data[this.Track] : null;

        /// <summary>
        /// Gets the annotation on the right, or null.
        /// </summary>
        public TimeIntervalAnnotation RightAnnotation => this.RightAnnotationSetMessage.HasValue ? this.RightAnnotationSetMessage.Value.Data[this.Track] : null;

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
