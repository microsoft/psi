// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Represents a stream reader which can read instant data based on a fixed cursor epsilon, and
    /// which then pushes the read data to a collection of adapting data providers.
    /// </summary>
    /// <typeparam name="T">The type of messages in the stream.</typeparam>
    public class EpsilonInstantStreamReader<T>
    {
        /// <summary>
        /// Flag indicating whether type parameter T is Shared{} or not.
        /// </summary>
        private readonly bool isSharedType = typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// The collection of adapting data providers that read data will be pushed to.
        /// </summary>
        private List<IAdaptingInstantDataProvider<T>> dataProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpsilonInstantStreamReader{T}"/> class.
        /// </summary>
        /// <param name="cursorEpsilon">The cursor epsilon to use when searching for messages around a cursor time.</param>
        public EpsilonInstantStreamReader(RelativeTimeInterval cursorEpsilon)
        {
            this.CursorEpsilon = cursorEpsilon;
            this.dataProviders = new List<IAdaptingInstantDataProvider<T>>();
        }

        /// <summary>
        /// Gets the value of the cursor epsilon.
        /// </summary>
        public RelativeTimeInterval CursorEpsilon { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instant stream reader has any adapting data providers.
        /// </summary>
        public bool HasAdaptingDataProviders => this.dataProviders.Count > 0;

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TTarget">The type of data the target requires.</typeparam>
        /// <param name="target">An instant data target specifying the properties of the target.</param>
        public void RegisterInstantDataTarget<TTarget>(InstantDataTarget target)
        {
            // Get the name of the stream adapter
            string streamAdapterName = target.StreamAdapter.GetType().FullName;

            // Check if we already have a data provider that uses this stream adapter
            IAdaptingInstantDataProvider<T> provider = this.dataProviders.FirstOrDefault(n => n.StreamAdapterName == streamAdapterName);
            if (provider == null)
            {
                // Create the adapting data provider
                provider = new AdaptingInstantDataProvider<T, TTarget>(target.StreamAdapter);

                // Add it to the collection
                lock (this.dataProviders)
                {
                    this.dataProviders.Add(provider);
                }
            }

            // Register the target with the adapting data provider
            provider.RegisterInstantDataTarget(target);
        }

        /// <summary>
        /// Unregisters an instant data target from data notification.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when the target was initially registered.</param>
        /// <returns>An instant data target representing the target that was unregistered.</returns>
        public InstantDataTarget UnregisterInstantDataTarget(Guid registrationToken)
        {
            for (int index = this.dataProviders.Count - 1; index >= 0; index--)
            {
                // Unregister the target from the data provider if it exists there
                InstantDataTarget target = this.dataProviders[index].UnregisterInstantDataTarget(registrationToken);

                if (target != null)
                {
                    // If the data provider now has no targets to call, remove it from the collection
                    if (!this.dataProviders[index].HasRegisteredTargets)
                    {
                        this.dataProviders.RemoveAt(index);
                    }

                    return target;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads instant data from the stream at the given cursor time and pushes it to all registered adapting data providers.
        /// </summary>
        /// <param name="streamReader">The stream reader that will read the data.</param>
        /// <param name="cursorTime">The cursor time at which to read the data.</param>
        /// <param name="streamCache">The stream reader's cache.</param>
        public void ReadInstantData(IStreamReader streamReader, DateTime cursorTime, ObservableKeyedCache<DateTime, StreamCacheEntry> streamCache)
        {
            // Get the index of the data, given the cursor time
            int index = IndexHelper.GetIndexForTime(cursorTime, streamCache?.Count ?? 0, (idx) => streamCache[idx].OriginatingTime, this.CursorEpsilon);

            T data = default;
            StreamCacheEntry cacheEntry = default;
            if (index >= 0)
            {
                // Get the index entry
                cacheEntry = streamCache[index];

                // Read the data
                data = cacheEntry.Read<T>(streamReader);
            }

            // Notify each adapting data provider of the new data
            foreach (IAdaptingInstantDataProvider<T> adaptingInstantDataProvider in this.dataProviders.ToList())
            {
                adaptingInstantDataProvider.PushData(data, cacheEntry);
            }

            // Release the reference to the local copy of the data if it's shared
            if (this.isSharedType && data != null)
            {
                (data as IDisposable).Dispose();
            }
        }
    }
}
