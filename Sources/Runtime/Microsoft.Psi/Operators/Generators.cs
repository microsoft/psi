// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Factory methods for instantiating generators and timers.
    /// </summary>
    public static class Generators
    {
        /// <summary>
        /// Generates a stream of values from a user-provided function, at a regular interval.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="generateNext">The function that generates a new value based on the previous value.</param>
        /// <param name="count">The count of values to generate. Use int.MaxValue if the generator should never stop.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults ot 1 tick.</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, T initialValue, Func<T, T> generateNext, int count, TimeSpan interval = default(TimeSpan))
        {
            return Sequence(pipeline, Enumerate(initialValue, generateNext, count), interval);
        }

        /// <summary>
        /// Transforms an enumerator into a stream of messages published at regular intervals.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="enumerator">The enumerator producing the values to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults ot 1 tick.</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerator<T> enumerator, TimeSpan interval = default(TimeSpan))
        {
            var g = new Generator<T>(pipeline, enumerator, interval);
            return g;
        }

        /// <summary>
        /// Transforms an enumerable sequence into a stream of messages published at regular intervals.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="enumerable">The sequence to publish.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults ot 1 tick.</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerable<T> enumerable, TimeSpan interval = default(TimeSpan))
        {
            var g = new Generator<T>(pipeline, enumerable.GetEnumerator(), interval);
            return g;
        }

        /// <summary>
        /// Generates a stream by enumerating a sequence of data and originating time pairs.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="enumerator">An enumerator of (data, originating time) pairs</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerator<(T, DateTime)> enumerator)
        {
            var g = new Generator<T>(pipeline, enumerator);
            return g;
        }

        /// <summary>
        /// Generates a stream by enumerating a sequence of data and originating time pairs.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="enumerable">An enumerable sequence of (data, originating time) pairs</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Sequence<T>(Pipeline pipeline, IEnumerable<(T, DateTime)> enumerable)
        {
            var g = new Generator<T>(pipeline, enumerable.GetEnumerator());
            return g;
        }

        /// <summary>
        /// Generates a single message containing the specified value.
        /// </summary>
        /// <typeparam name="T">The type of value to publish.</typeparam>
        /// <param name="pipeline">The pipeline to attach to</param>
        /// <param name="value">The value to publish.</param>
        /// <returns>A stream containing one value of type T</returns>
        public static IProducer<T> Return<T>(Pipeline pipeline, T value)
        {
            return Sequence(pipeline, new[] { value });
        }

        /// <summary>
        /// Generates a stream of messages containing the same value.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="value">The value to publish.</param>
        /// <param name="count">The count of values to generate. Use int.MaxValue if the generator should never stop.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults ot 1 tick.</param>
        /// <returns>A stream of values of type T</returns>
        public static IProducer<T> Repeat<T>(Pipeline pipeline, T value, int count, TimeSpan interval = default(TimeSpan))
        {
            return Sequence(pipeline, Enumerable.Repeat(value, count), interval);
        }

        /// <summary>
        /// Generates a stream of consecutive integer values, published at regular intervals.
        /// When the pipeline is in replay mode, the timing of the messages complies with the speed of the pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="start">The starting value.</param>
        /// <param name="count">The count of values to generate. Use int.MaxValue if the generator should never stop.</param>
        /// <param name="interval">The desired time interval between consecutive messages. Defaults ot 1 tick.</param>
        /// <returns>A stream of consecutive integers</returns>
        public static IProducer<int> Range(Pipeline pipeline, int start, int count, TimeSpan interval = default(TimeSpan))
        {
            return Sequence(pipeline, Enumerable.Range(start, count), interval);
        }

        /// <summary>
        /// Generates a stream by invoking a user-provided function at a regular time interval.
        /// Unlike <see cref="Sequence{T}(Pipeline, IEnumerable{T}, TimeSpan)"/>, <see cref="Repeat{T}(Pipeline, T, int, TimeSpan)"/> and <see cref="Range"/>
        /// this operator relies on an OS timer. This guarantees that messages are emitted at regular wall-clock intervals regardless of pipeline load.
        /// When the pipeline is in replay mode, the originating times of the messages are derived from the virtual pipeline time,
        /// but if the pipeline slows down, the interval between messages might not appear constant.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="interval">The interval at which to generate messages.</param>
        /// <param name="generatorFn">The function generating the messages.</param>
        /// <returns>A stream of messages of type T</returns>
        public static IProducer<T> Timer<T>(Pipeline pipeline, TimeSpan interval, Func<DateTime, TimeSpan, T> generatorFn)
        {
            return new Timer<T>(pipeline, (uint)interval.TotalMilliseconds, generatorFn);
        }

        /// <summary>
        /// Generates a stream of <see cref="TimeSpan"/> messages indicating the time elapsed from the start of the pipeline.
        /// Unlike <see cref="Sequence{T}(Pipeline, IEnumerable{T}, TimeSpan)"/>, <see cref="Repeat{T}(Pipeline, T, int, TimeSpan)"/> and <see cref="Range"/>
        /// this operator relies on an OS timer. This guarantees that messages are emitted at regular wall-clock intervals regardless of pipeline load.
        /// When the pipeline is in replay mode, the originating times of the messages are derived from the virtual pipeline time,
        /// but if the pipeline slows down, the interval between messages might not appear constant.
        /// </summary>
        /// <param name="pipeline">The pipeline that will run this generator.</param>
        /// <param name="interval">The interval at which to generate messages.</param>
        /// <returns>A stream of messages representing time elapsed since the start of the pipeline</returns>
        public static IProducer<TimeSpan> Timer(Pipeline pipeline, TimeSpan interval)
        {
            return Timer<TimeSpan>(pipeline, interval, (_, t) => t);
        }

        private static IEnumerable<TResult> Enumerate<TResult>(TResult initialValue, Func<TResult, TResult> generateNext, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException("count");
            }

            yield return initialValue;
            var value = initialValue;
            for (int i = 1; i < count; i++)
            {
                value = generateNext(value);
                yield return value;
            }
        }
    }
}