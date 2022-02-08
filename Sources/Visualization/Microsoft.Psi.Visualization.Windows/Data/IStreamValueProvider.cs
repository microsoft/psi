// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Defines a stream value provider, i.e. an object that provides stream values around a specified time position.
    /// </summary>
    /// <remarks>
    /// A stream value provider allows stream value subscribers to register and unregister.
    /// When the <see cref="IStreamValueProvider.ReadAndPublishStreamValue(IStreamReader, DateTime)"/>
    /// method is called, the provider reads and publishes the stream value at a specified time to
    /// all registered subscribers.
    /// Finally, the <see cref="IStreamValueProvider.SetCacheInterval(TimeInterval)"/>
    /// method allows the stream value provider to be notified when the interval of interest
    /// (in which subsequent requests to read might arise) might change.
    /// </remarks>
    public interface IStreamValueProvider : IStreamDataProvider
    {
        /// <summary>
        /// Registers a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="streamAdapter">The stream adapter used to convert the raw stream data to the type required by the subscriber.</param>
        /// <param name="epsilonTimeInterval">The epsilon interval to use when retrieving stream values.</param>
        /// <param name="callback">The method to call to deliver data to the stream value subscriber.</param>
        /// <returns>A subscriber id which can be used to unregister the subscriber.</returns>
        Guid RegisterStreamValueSubscriber<TData>(IStreamAdapter streamAdapter, RelativeTimeInterval epsilonTimeInterval, Action<bool, TData, DateTime, DateTime> callback);

        /// <summary>
        /// Unregisters a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="subscriberId">The subscriber id that the subscriber was given when it was initially registered.</param>
        void UnregisterStreamValueSubscriber<TData>(Guid subscriberId);

        /// <summary>
        /// Reads data from the stream at the specified time and publishes it to all registered stream value subscribers.
        /// </summary>
        /// <param name="streamReader">The reader to use when reading data.</param>
        /// <param name="dateTime">The time for the value to read and publish.</param>
        void ReadAndPublishStreamValue(IStreamReader streamReader, DateTime dateTime);

        /// <summary>
        /// Sets the caching interval for the stream value provider.
        /// </summary>
        /// <param name="cacheInterval">The time interval to cache.</param>
        void SetCacheInterval(TimeInterval cacheInterval);
    }
}
