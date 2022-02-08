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
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey>(
            this IProducer<Dictionary<TKey, bool>> source,
            Func<TKey, (string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, bool>> deliveryPolicy = null)
            => source.ToTimeIntervalAnnotations(
                dict => dict.Where(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                (k, _) => annotationConstructor(k),
                deliveryPolicy);

        /// <summary>
        /// Converts a stream of dictionaries into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <typeparam name="TValue">The type of values in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="annotationConstructor">A function that, given a key and value, produces a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A time interval annotation stream.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TKey, TValue>(
            this IProducer<Dictionary<TKey, TValue>> source,
            Func<TKey, TValue, (string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<Dictionary<TKey, TValue>> deliveryPolicy = null)
            => source.ToTimeIntervalAnnotations(
                _ => _,
                annotationConstructor,
                deliveryPolicy);

        /// <summary>
        /// Converts a stream into a corresponding stream of time interval annotations.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TKey">The type of key in the source stream.</typeparam>
        /// <typeparam name="TValue">The type of values in the source stream.</typeparam>
        /// <param name="source">The source stream.</param>
        /// <param name="selector">A function that, given an input message produces a dictionary of key-values that generates the annotation set.</param>
        /// <param name="annotationConstructor">A function that, given a key and value, produces a track name and set of attribute values for the annotation.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A time interval annotation stream.</returns>
        private static IProducer<TimeIntervalAnnotationSet> ToTimeIntervalAnnotations<TInput, TKey, TValue>(
            this IProducer<TInput> source,
            Func<TInput, Dictionary<TKey, TValue>> selector,
            Func<TKey, TValue, (string Track, Dictionary<string, IAnnotationValue> AttributeValues)> annotationConstructor,
            DeliveryPolicy<TInput> deliveryPolicy = null)
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
                            (var annotationTrack, var attributeValues) = annotationConstructor(key, intervals[key].Value);
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
                            (var annotationTrack, var attributeValues) = annotationConstructor(key, intervals[key].Value);
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

                        // Post the value
                        emitter.Post(timeIntervalAnnotationSet, closingTime);
                    }
                });

            return source.PipeTo(processor, deliveryPolicy);
        }
    }
}
