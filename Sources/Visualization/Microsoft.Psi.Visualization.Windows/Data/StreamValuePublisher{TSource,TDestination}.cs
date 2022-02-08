// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Adapters;

    /// <summary>
    /// Implements a publisher that pushes adapted data to stream value subscribers.
    /// Data of type TSource supplied to the provider is converted to data of type
    /// TDestination by the stream adapter and then routed to all stream value
    /// subscribers.
    /// </summary>
    /// <typeparam name="TSource">The type of source data.</typeparam>
    /// <typeparam name="TDestination">The type of destination data.</typeparam>
    public class StreamValuePublisher<TSource, TDestination> : IStreamValuePublisher<TSource>
    {
        /// <summary>
        /// The collection of targets, indexed by the targets' subscriber id.
        /// </summary>
        private readonly Dictionary<Guid, Action<bool, TDestination, DateTime, DateTime>> targets;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamValuePublisher{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="streamAdapter">The stream adapter that will convert the incoming data to the data that the targets require.</param>
        public StreamValuePublisher(IStreamAdapter streamAdapter)
        {
            this.targets = new Dictionary<Guid, Action<bool, TDestination, DateTime, DateTime>>();
            this.StreamAdapter = (streamAdapter ?? new PassthroughAdapter<TDestination>()) as StreamAdapter<TSource, TDestination>;
        }

        /// <inheritdoc/>
        public IStreamAdapter StreamAdapter { get; }

        /// <summary>
        /// Gets a value indicating whether any targets are registered with the provider.
        /// </summary>
        public bool HasSubscribers => this.targets.Count > 0;

        /// <summary>
        /// Registers a subscriber.
        /// </summary>
        /// <param name="target">The target action to call for the subscriber.</param>
        /// <returns>A subscriber id that can be user to unregister.</returns>
        public Guid RegisterSubscriber(Action<bool, TDestination, DateTime, DateTime> target)
        {
            // Add the target to the collection
            lock (this.targets)
            {
                var guid = Guid.NewGuid();
                this.targets[guid] = target;
                return guid;
            }
        }

        /// <inheritdoc/>
        public bool HasSubscriber(Guid subscriberId)
        {
            lock (this.targets)
            {
                return this.targets.ContainsKey(subscriberId);
            }
        }

        /// <inheritdoc/>
        public void UnregisterSubscriber(Guid subscriberId)
        {
            lock (this.targets)
            {
                if (this.targets.ContainsKey(subscriberId))
                {
                    this.targets.Remove(subscriberId);
                }
            }
        }

        /// <inheritdoc/>
        public void PublishValue(bool dataIsAvailable, TSource data, DateTime originatingTime, DateTime creationTime)
        {
            // Adapt the data to the type required by target. In the process, the stream
            // adapter may simply select a portion of the sourceData, or might allocate
            // new objects. A dispose method is called on the stream adapter after the
            // data is passed to the visualization objects, so any allocations that
            // might have been created by the stream adapter are disposed (this correctly
            // handles cases which involve Shared<T> objects.
            var adaptedData = (this.StreamAdapter as StreamAdapter<TSource, TDestination>).GetAdaptedValue(data, default);

            // Call each of the targets with the adapted data (the targets will make
            // copies of the data if they need to hold on to it)
            foreach (var target in this.targets.Values.ToList())
            {
                target.Invoke(dataIsAvailable, adaptedData, originatingTime, creationTime);
            }

            // Call the stream adapter to dispose any data it might have allocated.
            (this.StreamAdapter as StreamAdapter<TSource, TDestination>).Dispose(adaptedData);
        }
    }
}
