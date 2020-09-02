// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a data provider that can provide adapted data to instant data targets.
    /// Data of type TSrc supplied to the provider is converted to data of type TDest
    /// by the stream adapter and then routed to all instant data targets.
    /// </summary>
    /// <typeparam name="TSource">The type of input data for provider.</typeparam>
    public interface IAdaptingInstantDataProvider<TSource>
    {
        /// <summary>
        /// Gets the name of the stream adapter associated with adapting data provider.
        /// </summary>
        string StreamAdapterName { get; }

        /// <summary>
        /// Gets a value indicating whether any targets are registered with the provider.
        /// </summary>
        bool HasRegisteredTargets { get; }

        /// <summary>
        /// Registers an instant data target to receive data from the provider.
        /// </summary>
        /// <param name="target">The instant data target to register.</param>
        void RegisterInstantDataTarget(InstantDataTarget target);

        /// <summary>
        /// Unregisters an instant data target from receiving data from he provider.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        /// <returns>An instant data target representing the target that was unregistered, or null if no target with
        /// the specified registration token is registered with the provider.</returns>
        InstantDataTarget UnregisterInstantDataTarget(Guid registrationToken);

        /// <summary>
        /// Pushes data to all of the registered instant data targets.
        /// </summary>
        /// <param name="sourceData">The source data.</param>
        /// <param name="streamCacheEntry">The stream cache entry with which to read the source data.</param>
        void PushData(TSource sourceData, StreamCacheEntry streamCacheEntry);
    }
}