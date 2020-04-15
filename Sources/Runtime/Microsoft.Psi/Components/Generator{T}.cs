// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Generates messages by lazily enumerating a sequence of data,
    /// at the pace dictated by the pipeline.
    /// </summary>
    /// <typeparam name="T">The output type.</typeparam>
    /// <remarks>
    /// The static functions provided by the <see cref="Generators"/> wrap <see cref="Generator{T}"/>
    /// and are designed to make the common cases easier.
    /// </remarks>
    public class Generator<T> : Generator, IProducer<T>
    {
        private readonly Enumerator enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="enumerator">A lazy enumerator of data.</param>
        /// <param name="interval">The interval used to increment time on each generated message.</param>
        /// <param name="alignDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the paramater
        /// is non-null, the messages will have originating times that align with the specified time.</param>
        /// <param name="isInfiniteSource">If true, mark this Generator instance as representing an infinite source (e.g., a live-running sensor).
        /// If false (default), it represents a finite source (e.g., Generating messages based on a finite file or IEnumerable).</param>
        public Generator(Pipeline pipeline, IEnumerator<T> enumerator, TimeSpan interval, DateTime? alignDateTime = null, bool isInfiniteSource = false)
            : this(pipeline, CreateEnumerator(pipeline, enumerator, interval, alignDateTime), null, isInfiniteSource)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="enumerator">A lazy enumerator of data.</param>
        /// <param name="startTime">The explicit start time of the data in the enumeration. Supply this parameter when the enumeration contains
        /// data values with absolute originating times (e.g. [value, time] pairs read from a file), and you want to propose a pipeline replay
        /// time to take this into account. Otherwise, pipeline playback will be determined by the prevailing replay descriptor (taking into
        /// account any other components in the pipeline which may have proposed replay times.</param>
        /// <param name="isInfiniteSource">If true, mark this Generator instance as representing an infinite source (e.g., a live-running sensor).
        /// If false (default), it represents a finite source (e.g., Generating messages based on a finite file or IEnumerable).</param>
        public Generator(Pipeline pipeline, IEnumerator<(T, DateTime)> enumerator, DateTime? startTime = null, bool isInfiniteSource = false)
            : base(pipeline, isInfiniteSource)
        {
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.enumerator = new Enumerator(enumerator);

            // if data has a defined start time, use this to propose a replay time
            if (startTime != null)
            {
                var interval = TimeInterval.LeftBounded(startTime.Value);
                pipeline.ProposeReplayTime(interval);
            }
        }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        public Emitter<T> Out { get; }

        /// <summary>
        /// Called to generate the next value.
        /// </summary>
        /// <param name="currentTime">The originating time that triggered the current call.</param>
        /// <returns>The originating time at which to generate the next value.</returns>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            if (!this.enumerator.MoveNext())
            {
                return DateTime.MaxValue; // no more data
            }

            this.Out.Post(this.enumerator.Current.value, this.enumerator.Current.time);

            // ensure that the originating times in the enumerated sequence are strictly increasing
            if (this.enumerator.Next.time <= this.enumerator.Current.time)
            {
                throw new InvalidOperationException("The generated sequence contains timestamps that are out of order. Originating times in the enumerated data must be strictly increasing.");
            }

            return this.enumerator.Next.time;
        }

        private static IEnumerator<(T value, DateTime time)> CreateEnumerator(Pipeline pipeline, IEnumerator<T> enumerator, TimeSpan interval, DateTime? alignDateTime)
        {
            // Use the pipeline start time as the origin time for the data. This assumes that the pipeline is
            // already running, so we should not access the enumerator before the pipeline starts running.
            DateTime startTime = pipeline.StartTime;

            if (alignDateTime.HasValue)
            {
                if (alignDateTime.Value > startTime)
                {
                    startTime += TimeSpan.FromTicks((alignDateTime.Value - startTime).Ticks % interval.Ticks);
                }
                else
                {
                    startTime += TimeSpan.FromTicks(interval.Ticks - (((startTime - alignDateTime.Value).Ticks - 1) % interval.Ticks) - 1);
                }
            }

            // Ensure that generated messages remain within the pipeline replay descriptor.
            // An infinite replay descriptor will have an end time of DateTime.MaxValue.
            DateTime endTime = pipeline.ReplayDescriptor.End;
            DateTime nextTime = startTime;

            while (enumerator.MoveNext() && nextTime <= endTime)
            {
                yield return (enumerator.Current, nextTime);
                nextTime += interval;
            }
        }

        /// <summary>
        /// Wraps an enumerator and provides the ability to look-ahead to the next value.
        /// </summary>
        internal class Enumerator : IEnumerator<(T value, DateTime time)>
        {
            private static (T, DateTime) end = (default, DateTime.MaxValue);
            private readonly IEnumerator<(T, DateTime)> enumerator;
            private (T, DateTime) current;
            private bool onNext;
            private bool atEnd;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> class.
            /// </summary>
            /// <param name="enumerator">The underlying enumerator of values.</param>
            public Enumerator(IEnumerator<(T, DateTime)> enumerator)
            {
                this.enumerator = enumerator;
            }

            /// <inheritdoc/>
            public (T value, DateTime time) Current
            {
                get
                {
                    if (this.onNext)
                    {
                        // if the enumerator is pointing to the next value, return the cached current value
                        return this.current;
                    }

                    // otherwise return the enumerator's current value, or the sentinel value if we have reached the end
                    return this.atEnd ? end : this.enumerator.Current;
                }
            }

            /// <summary>
            /// Gets the next value in the enumeration.
            /// </summary>
            public (T value, DateTime time) Next
            {
                get
                {
                    if (!this.onNext)
                    {
                        // cache the current value, then advance the enumerator to the next value
                        this.current = this.enumerator.Current;
                        this.atEnd = !this.enumerator.MoveNext();
                        this.onNext = true;
                    }

                    // return the enumerator's current value, or the sentinel value if we have reached the end
                    return this.atEnd ? end : this.enumerator.Current;
                }
            }

            /// <inheritdoc/>
            object IEnumerator.Current => this.Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (this.onNext)
                {
                    // since the enumerator is already on the next value, we don't need to move it - just clear the flag
                    this.onNext = false;
                }
                else
                {
                    this.atEnd = !this.enumerator.MoveNext();
                }

                return !this.atEnd;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                this.enumerator.Reset();
                this.onNext = false;
                this.atEnd = false;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                this.enumerator.Dispose();
            }
        }
    }
}
