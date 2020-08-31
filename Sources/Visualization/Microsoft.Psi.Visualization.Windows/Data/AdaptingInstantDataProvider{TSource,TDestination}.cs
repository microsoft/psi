// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a data provider that can provide adapted data to instant data targets.
    /// Data of type TSrc supplied to the provider is converted to data of type TDest
    /// by the stream adapter and then routed to all instant data targets.
    /// </summary>
    /// <typeparam name="TSource">The type of source data.</typeparam>
    /// <typeparam name="TDestination">The type of destination data.</typeparam>
    public class AdaptingInstantDataProvider<TSource, TDestination> : IAdaptingInstantDataProvider<TSource>
    {
        /// <summary>
        /// Flag indicating whether type parameter TDest is Shared{} or not.
        /// </summary>
        private readonly bool adaptedDataIsSharedType = typeof(TDestination).IsGenericType && typeof(TDestination).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// The stream adapter that adapts the incoming data to the type required by the instant data targets.
        /// </summary>
        private StreamAdapter<TSource, TDestination> streamAdapter;

        /// <summary>
        /// The collection of instant data targets, indexed by the targets' registration token.
        /// </summary>
        private Dictionary<Guid, InstantDataTarget> targets;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptingInstantDataProvider{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="streamAdapter">The stream adapter that will convert the incoming data to the data that the targets require.</param>
        public AdaptingInstantDataProvider(IStreamAdapter streamAdapter)
        {
            if (streamAdapter == null)
            {
                throw new ArgumentNullException(nameof(streamAdapter));
            }

            this.streamAdapter = streamAdapter as StreamAdapter<TSource, TDestination>;
            this.targets = new Dictionary<Guid, InstantDataTarget>();
        }

        /// <inheritdoc/>
        public string StreamAdapterName => this.streamAdapter.GetType().FullName;

        /// <inheritdoc/>
        public bool HasRegisteredTargets => this.targets.Count > 0;

        /// <inheritdoc/>
        public void RegisterInstantDataTarget(InstantDataTarget target)
        {
            // Add the target to the collection
            lock (this.targets)
            {
                this.targets[target.RegistrationToken] = target;
            }
        }

        /// <inheritdoc/>
        public InstantDataTarget UnregisterInstantDataTarget(Guid registrationToken)
        {
            InstantDataTarget target = null;

            lock (this.targets)
            {
                if (this.targets.ContainsKey(registrationToken))
                {
                    target = this.targets[registrationToken];
                    this.targets.Remove(registrationToken);
                }
            }

            return target;
        }

        /// <inheritdoc/>
        public void PushData(TSource sourceData, StreamCacheEntry streamCacheEntry)
        {
            // Adapt the data to the type required by target.
            TDestination adaptedData = this.streamAdapter.AdaptData(sourceData);

            // Call each of the targets with the adapted data, cloning it if it's shared.
            foreach (InstantDataTarget instantDataTarget in this.targets.Values.ToList())
            {
                instantDataTarget.Callback.Invoke(adaptedData, streamCacheEntry);
            }

            // We're done with the adapted data, so decrement its reference count if it's shared
            if (this.adaptedDataIsSharedType && adaptedData != null)
            {
                (adaptedData as IDisposable).Dispose();
            }
        }
    }
}
