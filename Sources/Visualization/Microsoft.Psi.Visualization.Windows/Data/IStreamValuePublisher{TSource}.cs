// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents a publisher for stream values. A stream value publisher is used
    /// by a stream value provider of type TSource to adapt data via a stream adapter
    /// before publishing it to a stream value subscriber.
    /// </summary>
    /// <typeparam name="TSource">The type of the source data, from the stream value provider.</typeparam>
    public interface IStreamValuePublisher<TSource>
    {
        /// <summary>
        /// Gets the stream adapter used by this publisher.
        /// </summary>
        IStreamAdapter StreamAdapter { get; }

        /// <summary>
        /// Gets a value indicating whether any targets are registered with the provider.
        /// </summary>
        bool HasSubscribers { get; }

        /// <summary>
        /// Checks if a stream value publisher has a specified subscriber.
        /// </summary>
        /// <param name="subscriberId">The subscriber id that the target was given when it initially subscribed.</param>
        /// <returns>True if the publisher has the specified subscriber.</returns>
        bool HasSubscriber(Guid subscriberId);

        /// <summary>
        /// Unregisters a stream value subscriber.
        /// </summary>
        /// <param name="subscriberId">The subscriber id that the stream value subscriber was given when it initially subscribed.</param>
        void UnregisterSubscriber(Guid subscriberId);

        /// <summary>
        /// Pushes data to registered stream value subscribers.
        /// </summary>
        /// <param name="dataIsAvailable">Indicates whether data is valid, or whether no data was found at the specified read location.</param>
        /// <param name="data">The source data.</param>
        /// <param name="originatingTime">The originating time.</param>
        /// <param name="creationTime">The creation time.</param>
        void PublishValue(bool dataIsAvailable, TSource data, DateTime originatingTime, DateTime creationTime);
    }
}