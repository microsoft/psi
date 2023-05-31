// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a set of overlapping time-interval annotations that belong to separate tracks but end at the same time.
    /// </summary>
    /// <remarks>
    /// This data structure provides the basis for persisting overlapping time interval annotations
    /// in \psi streams. It captures a set of overlapping time interval annotations that are on
    /// different tracks but end at the same time, captured by <see cref="EndTime"/>.
    /// When persisted to a stream, the originating time of the <see cref="Message{TimeIntervalAnnotationSet}"/>
    /// should correspond to the <see cref="EndTime"/>.
    /// </remarks>
    public class TimeIntervalAnnotationSet
    {
        private readonly Dictionary<string, TimeIntervalAnnotation> data = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationSet"/> class.
        /// </summary>
        /// <param name="timeIntervalAnnotation">The time interval annotation.</param>
        public TimeIntervalAnnotationSet(TimeIntervalAnnotation timeIntervalAnnotation)
        {
            this.data.Add(timeIntervalAnnotation.Track, timeIntervalAnnotation);
        }

        /// <summary>
        /// Gets the end time for the annotation set.
        /// </summary>
        public DateTime EndTime => this.data.Values.First().Interval.Right;

        /// <summary>
        /// Gets the set of tracks spanned by these time interval annotations.
        /// </summary>
        public IEnumerable<string> Tracks => this.data.Keys;

        /// <summary>
        /// Gets the time interval annotation for a specified track name.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>The corresponding time interval annotation.</returns>
        public TimeIntervalAnnotation this[string track] => this.data[track];

        /// <summary>
        /// Adds a specified time interval annotation.
        /// </summary>
        /// <param name="timeIntervalAnnotation">The time interval annotation to add.</param>
        public void AddAnnotation(TimeIntervalAnnotation timeIntervalAnnotation)
        {
            if (timeIntervalAnnotation.Interval.Right != this.EndTime)
            {
                throw new ArgumentException("Cannot add a time interval annotation with a different end time to a time interval annotation set.");
            }

            this.data.Add(timeIntervalAnnotation.Track, timeIntervalAnnotation);
        }

        /// <summary>
        /// Removes an annotation specified by a track name.
        /// </summary>
        /// <param name="track">The track name for the annotation to remove.</param>
        public void RemoveAnnotation(string track)
        {
            if (this.data.Count() == 1)
            {
                throw new InvalidOperationException("Cannot remove the last time interval annotation from a time interval annotation set.");
            }

            this.data.Remove(track);
        }

        /// <summary>
        /// Gets a value indicating whether the annotation set contains an annotation for the specified track.
        /// </summary>
        /// <param name="track">The track name.</param>
        /// <returns>True if the annotation set contains an annotation for the specified track, otherwise false.</returns>
        public bool ContainsTrack(string track) => this.data.ContainsKey(track);
    }
}
