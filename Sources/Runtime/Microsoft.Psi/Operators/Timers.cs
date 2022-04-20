// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Factory methods for instantiating timers.
    /// </summary>
    public static class Timers
    {
        /// <summary>
        /// Generates a stream by invoking a user-provided function at a regular time interval.
        /// Unlike the generators available in the <see cref="Generators"/> class,
        /// this operator relies on an OS timer. This guarantees that messages are emitted at regular wall-clock intervals regardless of pipeline load.
        /// When the pipeline is in replay mode, the originating times of the messages are derived from the virtual pipeline time,
        /// but if the pipeline slows down, the interval between messages might not appear constant.
        /// </summary>
        /// <typeparam name="T">The type of data in the sequence.</typeparam>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">The interval at which to generate messages.</param>
        /// <param name="generatorFn">The function generating the messages.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <returns>A stream of messages of type T.</returns>
        public static IProducer<T> Timer<T>(Pipeline pipeline, TimeSpan interval, Func<DateTime, TimeSpan, T> generatorFn, string name = nameof(Timer))
            => new Timer<T>(pipeline, (uint)interval.TotalMilliseconds, generatorFn, name);

        /// <summary>
        /// Generates a stream of <see cref="TimeSpan"/> messages indicating the time elapsed from the start of the pipeline.
        /// Unlike the generators available in the <see cref="Generators"/> class,
        /// this operator relies on an OS timer. This guarantees that messages are emitted at regular wall-clock intervals regardless of pipeline load.
        /// When the pipeline is in replay mode, the originating times of the messages are derived from the virtual pipeline time,
        /// but if the pipeline slows down, the interval between messages might not appear constant.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">The interval at which to generate messages.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <returns>A stream of messages representing time elapsed since the start of the pipeline.</returns>
        public static IProducer<TimeSpan> Timer(Pipeline pipeline, TimeSpan interval, string name = nameof(Timer))
            => Timer(pipeline, interval, (_, t) => t, name);
    }
}