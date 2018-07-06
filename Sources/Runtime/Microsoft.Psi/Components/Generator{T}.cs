// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Generates messages by lazily enumerating a sequence of data,
    /// at the pace dictated by the pipeline.
    /// </summary>
    /// <typeparam name="T">The output type</typeparam>
    /// <remarks>
    /// The static functions provided by the <see cref="Generators"/> wrap <see cref="Generator{T}"/>
    /// and are designed to make the common cases easier:
    /// <see cref="Generators.Sequence{T}(Pipeline, IEnumerable{ValueTuple{T, DateTime}})"/>
    /// <see cref="Generators.Sequence{T}(Pipeline, IEnumerable{T}, TimeSpan, DateTime?)"/>
    /// <see cref="Generators.Sequence{T}(Pipeline, IEnumerator{ValueTuple{T, DateTime}})"/>
    /// <see cref="Generators.Sequence{T}(Pipeline, IEnumerator{T}, TimeSpan, DateTime?)"/>
    /// </remarks>
    public class Generator<T> : Generator, IProducer<T>
    {
        private readonly IEnumerator<(T value, DateTime time)> enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="enumerator">A lazy enumerator of data.</param>
        /// <param name="interval">An optional timespan interval used to increment time on each generated message. Defaults to 1 tick.</param>
        /// <param name="alignDateTime">If non-null, this parameter specifies a time to align the generator messages with. If the paramater
        /// is non-null, the messages will have originating times that align with the specified time.</param>
        public Generator(Pipeline pipeline, IEnumerator<T> enumerator, TimeSpan interval = default(TimeSpan), DateTime? alignDateTime = null)
            : this(pipeline, CreateEnumerator(pipeline, enumerator, (interval == default(TimeSpan)) ? new TimeSpan(1) : interval, alignDateTime))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="enumerator">A lazy enumerator of data</param>
        public Generator(Pipeline pipeline, IEnumerator<ValueTuple<T, DateTime>> enumerator)
            : base(pipeline)
        {
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.enumerator = enumerator;
        }

        /// <summary>
        /// Gets the output stream
        /// </summary>
        public Emitter<T> Out { get; }

        /// <summary>
        /// Called to generate the next value.
        /// </summary>
        /// <param name="previous">The previous value</param>
        /// <returns>The time when to be called again</returns>
        protected override DateTime GenerateNext(DateTime previous)
        {
            if (!this.enumerator.MoveNext())
            {
                return DateTime.MaxValue; // no more data
            }

            this.Out.Post(this.enumerator.Current.value, this.enumerator.Current.time);
            return this.enumerator.Current.time;
        }

        private static IEnumerator<(T value, DateTime time)> CreateEnumerator(Pipeline pipeline, IEnumerator<T> enumerator, TimeSpan interval, DateTime? alignDateTime)
        {
            DateTime startTime;
            if (pipeline.ReplayDescriptor.Start == DateTime.MinValue)
            {
                startTime = pipeline.GetCurrentTime();
            }
            else
            {
                startTime = pipeline.ReplayDescriptor.Start;
            }

            if (alignDateTime.HasValue)
            {
                if (alignDateTime.Value > startTime)
                {
                    startTime += TimeSpan.FromTicks((alignDateTime.Value - startTime).Ticks % interval.Ticks);
                }
                else
                {
                    startTime += TimeSpan.FromTicks(interval.Ticks - ((startTime - alignDateTime.Value).Ticks % interval.Ticks));
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
    }
}
