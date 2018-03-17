// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents the summarized data of a stream generated from a specified summarizer type.
    /// Incoming data is summarized over a fixed interval and the resulting summarized values
    /// are stored in the cache as <see cref="IntervalData"/> items.
    /// </summary>
    public interface IStreamSummary : IDisposable
    {
        /// <summary>
        /// Gets the time interval over which summary <see cref="IntervalData"/> values are calculated.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        /// Gets an optional list of parameters that may be passed to the summarizer on creation. These
        /// typically control and/or alter the way the summarizer computes the summary values, and
        /// will be specific to the summarizer class specified in <see cref="SummarizerType"/>.
        /// </summary>
        object[] Parameters { get; }

        /// <summary>
        /// Gets the class type of the summarizer that will be used to generate the cached values.
        /// </summary>
        Type SummarizerType { get; }

        /// <summary>
        /// Method to dispatch summarized data computed in the background to the observable cache.
        /// </summary>
        void DispatchData();

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="viewMode">The view mode, which may be either fixed or live data.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="tailCount">Not yet supported and should be set to zero.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange);

        /// <summary>
        /// Searches the summary data for an interval that includes the specified time point. The supplied search mode
        /// defines whether an exact match is required, or whether the next or previous interval is returned should
        /// there be no interval that contains the specified time point.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="time">The time to search for.</param>
        /// <param name="mode">
        /// The search mode which determines whether to require an exact match, or to return the previous
        /// or next adjacent <see cref="IntervalData{TItem}"/> if no exact match was found.
        /// </param>
        /// <returns>An <see cref="IntervalData"/> that matches the search for <paramref name="time"/>.</returns>
        IntervalData<T> Search<T>(DateTime time, StreamSummarySearchMode mode);
    }
}
