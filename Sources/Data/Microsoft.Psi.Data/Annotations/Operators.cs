// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Implements stream operators for manipulating annotations.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Converts a stream of dictionaries with boolean values into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="annotationConstructor">A function that, given a key, produces a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey>(
            this IProducer<Dictionary<TKey, bool>> source,
            Func<TKey, (string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, bool>> deliveryPolicy = null,
            string name = nameof(ToTimeIntervalAnnotations))
            => source.ToTimeIntervalAnnotations(
                dict => dict.Where(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                (k, _) =>
                {
                    var (track, attributeValues) = annotationConstructor(k);
                    return (true, track, attributeValues);
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Converts a stream of dictionaries with boolean values into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="annotationConstructor">A function that, given a key, produces a value indicating whether to create an annotation, a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey>(
            this IProducer<Dictionary<TKey, bool>> source,
            Func<TKey, (bool Create, string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, bool>> deliveryPolicy = null,
            string name = nameof(ToTimeIntervalAnnotations))
            => source.ToTimeIntervalAnnotations(
                dict => dict.Where(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                (k, _) => annotationConstructor(k),
                deliveryPolicy,
                name);

        /// <summary>
        /// Converts a stream of dictionaries into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <typeparam name="TValue">The type of values in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="annotationConstructor">A function that, given a key and value, produces a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey, TValue>(
            this IProducer<Dictionary<TKey, TValue>> source,
            Func<TKey, TValue, (string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, TValue>> deliveryPolicy = null,
            string name = nameof(ToTimeIntervalAnnotations))
            => source.ToTimeIntervalAnnotations(
                _ => _,
                (k, v) =>
                {
                    var (track, attributeValues) = annotationConstructor(k, v);
                    return (true, track, attributeValues);
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Converts a stream of dictionaries into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <typeparam name="TValue">The type of values in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="annotationConstructor">A function that, given a key and value, produces a value indicating whether to create an annotation, a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey, TValue>(
            this IProducer<Dictionary<TKey, TValue>> source,
            Func<TKey, TValue, (bool Create, string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, TValue>> deliveryPolicy = null,
            string name = nameof(ToTimeIntervalAnnotations))
            => source.ToTimeIntervalAnnotations(
                _ => _,
                annotationConstructor,
                deliveryPolicy,
                name);

        /// <summary>
        /// Determines whether any annotation on a specified track intersects with the specified time interval.
        /// </summary>
        /// <param name="annotations">The set of annotations.</param>
        /// <param name="track">The track name.</param>
        /// <param name="timeInterval">The time interval.</param>
        /// <param name="intersectingTimeInterval">An output parameter returning the time interval for the intersecting annotation.</param>
        /// <returns>True if any annotation on the specified track intersects with the specified time interval, otherwise false.</returns>
        public static bool GetAnnotationTimeIntervalOverlappingWith(
            this IEnumerable<TimeIntervalAnnotationSet> annotations,
            string track,
            TimeInterval timeInterval,
            out TimeInterval intersectingTimeInterval)
        {
            // set the intersectingTimeInterval to empty
            intersectingTimeInterval = null;

            // Start by building up a list of indices where the annotation set has an annotation on
            // the specified track. (We will need to search among the annotations for these indices)
            var annotationsOnTrack = annotations.GetTimeIntervalAnnotations(track);

            // If there's no annotations at all, we're done and there's no intersection.
            if (annotationsOnTrack.Count == 0)
            {
                return false;
            }

            // Find the nearest annotation to the left edge of the interval
            int index = IndexHelper.GetIndexForTime(timeInterval.Left, annotationsOnTrack.Count, i => annotationsOnTrack[i].Interval.Right, NearestType.Nearest);

            // Check if the annotation intersects with the interval, then keep walking to the right until
            // we find an annotation within the interval or we go past the right hand side of the interval.
            while (index < annotationsOnTrack.Count)
            {
                var annotation = annotationsOnTrack[index];

                // Check if the annotation intersects with the interval
                // NOTE: By default time intervals are inclusive of their endpoints, so abutting time intervals will
                // test as intersecting. Use a non-inclusive time interval so that we can let annotations abut.
                if (timeInterval.IntersectsWith(new TimeInterval(annotation.Interval.Left, false, annotation.Interval.Right, false)))
                {
                    intersectingTimeInterval = annotation.Interval;
                    return true;
                }

                // Check if the annotation is completely to the right of the interval
                if (timeInterval.Right <= annotation.Interval.Left)
                {
                    return false;
                }

                index++;
            }

            return false;
        }

        /// <summary>
        /// Gets the time interval annotation at a specified time on a specified track, or null if none exists.
        /// </summary>
        /// <param name="annotations">The collection of annotations.</param>
        /// <param name="time">The time.</param>
        /// <param name="track">The track name.</param>
        /// <returns>The time interval annotation at a specified time on a specified track, or null if none exists.</returns>
        public static TimeIntervalAnnotation GetTimeIntervalAnnotationAtTime(this IEnumerable<TimeIntervalAnnotationSet> annotations, DateTime time, string track)
        {
            if (track == null)
            {
                return null;
            }

            var annotationsOnTrack = annotations.GetTimeIntervalAnnotations(track);

            if (annotationsOnTrack.Count > 0)
            {
                var index = GetTimeIntervalItemIndexByTime(
                    time,
                    annotationsOnTrack.Count,
                    i => annotationsOnTrack[i].Interval.Left,
                    i => annotationsOnTrack[i].Interval.Right);
                return index == -1 ? null : annotationsOnTrack[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the list of time interval annotations on the specified track.
        /// </summary>
        /// <param name="annotations">The collection of annotations.</param>
        /// <param name="track">The track name.</param>
        /// <returns>The list of time interval annotations on the specified track.</returns>
        public static List<TimeIntervalAnnotation> GetTimeIntervalAnnotations(this IEnumerable<TimeIntervalAnnotationSet> annotations, string track)
            => annotations.Where(tias => tias.ContainsTrack(track)).Select(tias => tias[track]).ToList();

        /// <summary>
        /// Converts a stream into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <typeparam name="TValue">The type of values in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="selector">A function that, given an input message produces a dictionary of key-values that generates the annotation set.</param>
        /// <param name="annotationConstructor">A function that, given a key and value, produces a value indicating whether to create an annotation, a track name, and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A time interval annotation stream.</returns>
        private static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TInput, TKey, TValue>(
            this IProducer<TInput> source,
            Func<TInput, Dictionary<TKey, TValue>> selector,
            Func<TKey, TValue, (bool Create, string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<TInput> deliveryPolicy = null,
            string name = nameof(ToTimeIntervalAnnotations))
        {
            var intervals = new Dictionary<TKey, (TimeInterval TimeInterval, TValue Value)>();

            var processor = new Processor<TInput, TimeIntervalAnnotationSet>(
                source.Out.Pipeline,
                (input, e, emitter) =>
                {
                    var timeIntervalAnnotationSet = default(TimeIntervalAnnotationSet);

                    var dictionary = selector(input);

                    // Add incoming objects to state
                    foreach (var key in dictionary.Keys)
                    {
                        if (intervals.ContainsKey(key))
                        {
                            intervals[key] = (new TimeInterval(intervals[key].TimeInterval.Left, e.OriginatingTime), Serializer.DeepClone(dictionary[key]));
                        }
                        else
                        {
                            intervals.Add(key, (new TimeInterval(e.OriginatingTime, e.OriginatingTime), Serializer.DeepClone(dictionary[key])));
                        }
                    }

                    // For all ids no longer in the incoming message
                    var removeKeys = new List<TKey>();
                    foreach (var key in intervals.Keys)
                    {
                        if (!dictionary.ContainsKey(key))
                        {
                            // In this case we need to post the object
                            removeKeys.Add(key);
                            (var create, var annotationTrack, var attributeValues) = annotationConstructor(key, intervals[key].Value);
                            if (create)
                            {
                                var annotation = new TimeIntervalAnnotation(intervals[key].TimeInterval, annotationTrack, attributeValues);
                                if (timeIntervalAnnotationSet == null)
                                {
                                    timeIntervalAnnotationSet = new TimeIntervalAnnotationSet(annotation);
                                }
                                else
                                {
                                    timeIntervalAnnotationSet.AddAnnotation(annotation);
                                }
                            }
                        }
                    }

                    foreach (var id in removeKeys)
                    {
                        intervals.Remove(id);
                    }

                    if (timeIntervalAnnotationSet != null)
                    {
                        emitter.Post(timeIntervalAnnotationSet, timeIntervalAnnotationSet.EndTime);
                    }
                },
                (closingTime, emitter) =>
                {
                    // If we have any open interval
                    if (intervals.Any())
                    {
                        var timeIntervalAnnotationSet = default(TimeIntervalAnnotationSet);

                        // For each open interval
                        foreach (var key in intervals.Keys)
                        {
                            // Edit the end time to be the closing time
                            var newInterval = new TimeInterval(intervals[key].TimeInterval.Left, closingTime);

                            // Append to the annotation set
                            (var create, var annotationTrack, var attributeValues) = annotationConstructor(key, intervals[key].Value);
                            if (create)
                            {
                                var annotation = new TimeIntervalAnnotation(newInterval, annotationTrack, attributeValues);
                                if (timeIntervalAnnotationSet == null)
                                {
                                    timeIntervalAnnotationSet = new TimeIntervalAnnotationSet(annotation);
                                }
                                else
                                {
                                    timeIntervalAnnotationSet.AddAnnotation(annotation);
                                }
                            }
                        }

                        // Post the value
                        if (timeIntervalAnnotationSet != null)
                        {
                            emitter.Post(timeIntervalAnnotationSet, closingTime);
                        }
                    }
                },
                name: name);

            return source.PipeTo(processor, deliveryPolicy);
        }

        private static int GetTimeIntervalItemIndexByTime(DateTime time, int count, Func<int, DateTime> startTimeAtIndex, Func<int, DateTime> endTimeAtIndex)
        {
            if (count < 1)
            {
                return -1;
            }

            int lo = 0;
            int hi = count - 1;
            while ((lo != hi - 1) && (lo != hi))
            {
                var val = (lo + hi) / 2;
                if (endTimeAtIndex(val) < time)
                {
                    lo = val;
                }
                else if (startTimeAtIndex(val) > time)
                {
                    hi = val;
                }
                else
                {
                    return val;
                }
            }

            // If lo and hi differ by 1, then either of those value could be straddled by the first or last
            // annotation. If lo and hi are both 0 then there's only 1 element so we should test it as well.
            if (hi - lo <= 1)
            {
                if ((endTimeAtIndex(hi) >= time) && (startTimeAtIndex(hi) <= time))
                {
                    return hi;
                }

                if ((endTimeAtIndex(lo) >= time) && (startTimeAtIndex(lo) <= time))
                {
                    return lo;
                }
            }

            return -1;
        }
    }
}
