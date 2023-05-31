// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements numeric series range summarization.
    /// </summary>
    internal static class NumericSeriesRangeSummarizer
    {
        /// <summary>
        /// Summarizes an enumerable of double messages into summarized doubles.
        /// </summary>
        /// <typeparam name="TKey">The type of the series key.</typeparam>
        /// <typeparam name="T">The type of messages to summarize.</typeparam>
        /// <param name="messages">Enumerable of double messages.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>List of summarized doubles.</returns>
        internal static List<IntervalData<Dictionary<TKey, T>>> SeriesSummarizer<TKey, T>(IEnumerable<Message<Dictionary<TKey, T>>> messages, TimeSpan interval)
        {
            return messages
                .OrderBy(msg => msg.OriginatingTime)
                .GroupBy(msg => Summarizer<T, T>.GetIntervalStartTime(msg.OriginatingTime, interval))
                .Select(
                    group =>
                    {
                        var firstMessage = group.First();
                        var lastMessage = group.Last();
                        var min = new Dictionary<TKey, T>();
                        var max = new Dictionary<TKey, T>();

                        foreach (var message in group)
                        {
                            foreach (var kvp in message.Data)
                            {
                                // Update min
                                if (!min.ContainsKey(kvp.Key))
                                {
                                    min.Add(kvp.Key, kvp.Value);
                                }
                                else if (kvp.Value is IComparable<T> comparable)
                                {
                                    if (comparable.CompareTo(min[kvp.Key]) == -1)
                                    {
                                        min[kvp.Key] = kvp.Value;
                                    }
                                }
                                else if (kvp.Value is IComparable untypedComparable)
                                {
                                    if (untypedComparable.CompareTo(min[kvp.Key]) == -1)
                                    {
                                        min[kvp.Key] = kvp.Value;
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException("Cannot summarize over values that are not comparable.");
                                }

                                // Update max
                                if (!max.ContainsKey(kvp.Key))
                                {
                                    max.Add(kvp.Key, kvp.Value);
                                }
                                else if (kvp.Value is IComparable<T> comparable)
                                {
                                    if (comparable.CompareTo(max[kvp.Key]) == 1)
                                    {
                                        max[kvp.Key] = kvp.Value;
                                    }
                                }
                                else if (kvp.Value is IComparable untypedComparable)
                                {
                                    if (untypedComparable.CompareTo(max[kvp.Key]) == 1)
                                    {
                                        max[kvp.Key] = kvp.Value;
                                    }
                                }
                                else
                                {
                                    throw new InvalidOperationException("Cannot summarize over values that are not comparable.");
                                }
                            }
                        }

                        // Use the last value as representative for summarization, with the first message
                        // originating time.
                        return IntervalData.Create(
                            value: lastMessage.Data,
                            minimum: min,
                            maximum: max,
                            originatingTime: firstMessage.OriginatingTime,
                            interval: lastMessage.OriginatingTime - firstMessage.OriginatingTime);
                    }).ToList();
        }

        /// <summary>
        /// Combines two numeric series interval data values.
        /// </summary>
        /// <typeparam name="TKey">The type of the series key.</typeparam>
        /// <typeparam name="T">The type of messages to summarize.</typeparam>
        /// <param name="left">The first value to combine.</param>
        /// <param name="right">The second value to combine.</param>
        /// <returns>The combined value.</returns>
        internal static IntervalData<Dictionary<TKey, T>> SeriesCombiner<TKey, T>(IntervalData<Dictionary<TKey, T>> left, IntervalData<Dictionary<TKey, T>> right)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            var min = new Dictionary<TKey, T>();
            foreach (var key in left.Minimum.Keys.Union(right.Minimum.Keys).Distinct())
            {
                if (left.Minimum.ContainsKey(key))
                {
                    if (right.Minimum.ContainsKey(key))
                    {
                        min.Add(key, comparer.Compare(left.Minimum[key], right.Minimum[key]) < 0 ? left.Minimum[key] : right.Minimum[key]);
                    }
                    else
                    {
                        min.Add(key, left.Minimum[key]);
                    }
                }
                else
                {
                    min.Add(key, right.Minimum[key]);
                }
            }

            var max = new Dictionary<TKey, T>();
            foreach (var key in left.Maximum.Keys.Union(right.Maximum.Keys).Distinct())
            {
                if (left.Maximum.ContainsKey(key))
                {
                    if (right.Maximum.ContainsKey(key))
                    {
                        max.Add(key, comparer.Compare(left.Maximum[key], right.Maximum[key]) > 0 ? left.Maximum[key] : right.Maximum[key]);
                    }
                    else
                    {
                        max.Add(key, left.Maximum[key]);
                    }
                }
                else
                {
                    max.Add(key, right.Maximum[key]);
                }
            }

            Dictionary<TKey, T> value;
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
    }
}
