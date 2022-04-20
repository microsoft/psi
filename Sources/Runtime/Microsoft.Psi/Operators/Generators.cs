// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Factory methods for constructing finite stream generators.
    /// </summary>
    public static class Generators
    {
        /// <summary>
        /// Generates a finite stream of values published at a regular interval from a user-provided function.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="generateNext">The function that generates a new value based on the previous value.</param>
        /// <param name="count">The number of messages to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="keepOpen">Indicates whether the stream should be kept open after all messages in the sequence have been posted.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, T initialValue, Func<T, T> generateNext, int count, TimeSpan interval, DateTime? alignmentDateTime = null, bool keepOpen = false, string name = nameof(Sequence))
            => Sequence(pipeline, Enumerate(initialValue, generateNext, count), interval, alignmentDateTime, keepOpen, name);

        /// <summary>
        /// Generates an infinite stream of values published at a regular interval from a user-provided function.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="generateNext">The function that generates a new value based on the previous value.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, T initialValue, Func<T, T> generateNext, TimeSpan interval, DateTime? alignmentDateTime = null, string name = nameof(Sequence))
            => Sequence(pipeline, Enumerate(initialValue, generateNext), interval, alignmentDateTime, keepOpen: true, name);

        /// <summary>
        /// Generates a stream of values published at a regular interval from a specified enumerable.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="enumerable">The sequence to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="keepOpen">Indicates whether the stream should be kept open after all messages in the sequence have been posted.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerable<T> enumerable, TimeSpan interval, DateTime? alignmentDateTime = null, bool keepOpen = false, string name = nameof(Sequence))
            => new Generator<T>(pipeline, enumerable.GetEnumerator(), interval, alignmentDateTime, isInfiniteSource: keepOpen);

        /// <summary>
        /// Generates a stream of values from a specified enumerable that provides the values and corresponding originating times.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="enumerable">An enumerable sequence of (data, originating time) pairs.</param>
        /// <param name="startTime">The explicit start time of the data in the enumeration. Supply this parameter when the enumeration contains
        /// data values with absolute originating times (e.g. [value, time] pairs read from a file), and you want to propose a pipeline replay
        /// time to take this into account. Otherwise, pipeline playback will be determined by the prevailing replay descriptor (taking into
        /// account any other components in the pipeline which may have proposed replay times).</param>
        /// <param name="keepOpen">Indicates whether the stream should be kept open after all the messages in the enumerable have been posted.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerable<(T, DateTime)> enumerable, DateTime? startTime = null, bool keepOpen = false, string name = nameof(Sequence))
            => new Generator<T>(pipeline, enumerable.GetEnumerator(), startTime, isInfiniteSource: keepOpen, name);

        /// <summary>
        /// Generates stream containing a single message, and keeps the stream open afterwards.
        /// </summary>
        /// <typeparam name="T">The type of value to publish.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>The generated stream stays open until the pipeline is shut down.</remarks>
        public static IProducer<T> Once<T>(Pipeline pipeline, T value, string name = nameof(Once))
            => Sequence(pipeline, new[] { value }, default, null, keepOpen: true, name);

        /// <summary>
        /// Generates stream containing a single message, and closes the stream afterwards.
        /// </summary>
        /// <typeparam name="T">The type of value to publish.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream containing one value of type T.</returns>
        /// <remarks>The generated stream closes after the message is published.</remarks>
        public static IProducer<T> Return<T>(Pipeline pipeline, T value, string name = nameof(Return))
            => Sequence(pipeline, new[] { value }, default, null, keepOpen: false, name);

        /// <summary>
        /// Generates a finite stream of constant values published at a regular interval.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="count">The number of messages to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="keepOpen">Indicates whether the stream should be kept open after the specified number of messages have been posted.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline. The generated stream closes once the specified number of messages has been published.</remarks>
        public static IProducer<T> Repeat<T>(Pipeline pipeline, T value, int count, TimeSpan interval, DateTime? alignmentDateTime = null, bool keepOpen = false, string name = nameof(Repeat))
            => Sequence(pipeline, Enumerable.Repeat(value, count), interval, alignmentDateTime, keepOpen, name);

        /// <summary>
        /// Generates an infinite stream of constant values published at a regular interval.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <returns>A stream of values of type T.</returns>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<T> Repeat<T>(Pipeline pipeline, T value, TimeSpan interval, DateTime? alignmentDateTime = null, string name = nameof(Repeat))
            => Sequence(pipeline, Enumerate(value, x => x), interval, alignmentDateTime, keepOpen: true, name);

        /// <summary>
        /// Generates a stream of a finite range of integer values published at a regular interval.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="start">The starting value.</param>
        /// <param name="count">The number of messages to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults to 1 tick.</param>
        /// <param name="alignDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the parameter
        /// is non-null, the messages will have originating times that align with the specified time.</param>
        /// <returns>A stream of consecutive integers.</returns>
        /// <param name="keepOpen">Indicates whether the stream should be kept open after the specified number of messages have been posted.</param>
        /// <param name="name">An optional name for the stream generator.</param>
        /// <remarks>When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.</remarks>
        public static IProducer<int> Range(Pipeline pipeline, int start, int count, TimeSpan interval, DateTime? alignDateTime = null, bool keepOpen = false, string name = nameof(Range))
            => Sequence(pipeline, Enumerable.Range(start, count), interval, alignDateTime, keepOpen, name);

        internal static IEnumerable<TResult> Enumerate<TResult>(TResult initialValue, Func<TResult, TResult> generateNext, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("The count parameter has to be positive.");
            }

            yield return initialValue;
            var value = initialValue;
            for (int i = 1; i < count; i++)
            {
                value = generateNext(value);
                yield return value;
            }
        }

        internal static IEnumerable<TResult> Enumerate<TResult>(TResult initialValue, Func<TResult, TResult> generateNext)
        {
            yield return initialValue;
            var value = initialValue;
            while (true)
            {
                value = generateNext(value);
                yield return value;
            }
        }
    }
}