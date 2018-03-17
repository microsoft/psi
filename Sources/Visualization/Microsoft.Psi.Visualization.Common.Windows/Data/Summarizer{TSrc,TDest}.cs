// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base class for summarizers that perform interval-based data summarization over a series of data values.
    /// </summary>
    /// <typeparam name="TSrc">The source data type.</typeparam>
    /// <typeparam name="TDest">The summarized data type.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class Summarizer<TSrc, TDest> : ISummarizer<TSrc, TDest>
    {
        private Func<IEnumerable<Message<TSrc>>, TimeSpan, List<IntervalData<TDest>>> summarizer;
        private Func<IntervalData<TDest>, IntervalData<TDest>, IntervalData<TDest>> combiner;

        /// <summary>
        /// Initializes a new instance of the <see cref="Summarizer{TSrc, TDest}"/> class.
        /// </summary>
        /// <param name="summarizer">The summarizer function.</param>
        /// <param name="combiner">The combiner function.</param>
        protected Summarizer(
            Func<IEnumerable<Message<TSrc>>, TimeSpan, List<IntervalData<TDest>>> summarizer,
            Func<IntervalData<TDest>, IntervalData<TDest>, IntervalData<TDest>> combiner = null)
        {
            this.summarizer = summarizer ?? throw new ArgumentNullException(nameof(summarizer));

            // When null, use the default combine method
            this.combiner = combiner ?? DefaultCombiner;

            this.SourceType = typeof(TSrc);
            this.DestinationType = typeof(TDest);
        }

        /// <summary>
        /// Gets the destination data type.
        /// </summary>
        public Type DestinationType { get; private set; }

        /// <summary>
        /// Gets the source data type.
        /// </summary>
        public Type SourceType { get; private set; }

        /// <summary>
        /// Default method for combining two <see cref="IntervalData{TDest}"/> values.
        /// </summary>
        /// <param name="left">The first value to combine.</param>
        /// <param name="right">The second value to combine.</param>
        /// <returns>The combined value.</returns>
        public static IntervalData<TDest> DefaultCombiner(IntervalData<TDest> left, IntervalData<TDest> right)
        {
            Comparer<TDest> comparer = Comparer<TDest>.Default;
            TDest min = comparer.Compare(left.Minimum, right.Minimum) < 0 ? left.Minimum : right.Minimum;
            TDest max = comparer.Compare(left.Maximum, right.Maximum) > 0 ? left.Maximum : right.Maximum;
            TDest value;
            DateTime originatingTime;

            // Take the value which occurs last, and the time which occurs first
            if (left.OriginatingTime <= right.OriginatingTime)
            {
                value = right.Value;
                originatingTime = left.OriginatingTime;
            }
            else
            {
                value = left.Value;
                originatingTime = right.OriginatingTime;
            }

            // Take the whichever end time occurs last and use it to find the interval
            TimeSpan interval = (right.EndTime > left.EndTime ? right.EndTime : left.EndTime) - originatingTime;
            return IntervalData.Create(value, min, max, originatingTime, interval);
        }

        /// <summary>
        /// Gets the start time of the bucketized interval which contains the supplied time point.
        /// Intervals are bucketized based on their tick count.
        /// </summary>
        /// <param name="time">The time point to lookup.</param>
        /// <param name="interval">The interval duration.</param>
        /// <returns>The start time of the bucketized interval.</returns>
        public static DateTime GetIntervalStartTime(DateTime time, TimeSpan interval)
        {
            if (interval == TimeSpan.Zero)
            {
                // Zero interval means take the time value as-is
                return time;
            }

            // Snap to the start time of the interval that time falls into
            return new DateTime((time.Ticks / interval.Ticks) * interval.Ticks);
        }

        /// <inheritdoc />
        public IntervalData<TDest> Combine(IntervalData<TDest> left, IntervalData<TDest> right)
        {
            return this.combiner(left, right);
        }

        /// <inheritdoc />
        public List<IntervalData<TDest>> Summarize(IEnumerable<Message<TSrc>> items, TimeSpan interval)
        {
            return this.summarizer(items, interval);
        }
    }
}
