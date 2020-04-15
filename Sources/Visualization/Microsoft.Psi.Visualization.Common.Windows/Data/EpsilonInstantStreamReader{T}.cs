// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
        /// Flag indicating whether type paramamter T is Shared{} or not.
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
                    // If the data provider now has no targets to call, remove it from the collecction
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
        /// <param name="reader">The simple reader that will read the data.</param>
        /// <param name="cursorTime">The cursor time at which to read the data.</param>
        /// <param name="indexCache">The stream reader's index cache.</param>
        public void ReadInstantData(ISimpleReader reader, DateTime cursorTime, ObservableKeyedCache<DateTime, IndexEntry> indexCache)
        {
            // Get the index of the data, given the cursor time
            int index = IndexHelper.GetIndexForTime(cursorTime, indexCache?.Count ?? 0, (idx) => indexCache[idx].OriginatingTime, this.CursorEpsilon);

            T data = default;
            IndexEntry indexEntry = default;
            if (index >= 0)
            {
                // Get the index entry
                indexEntry = indexCache[index];

                // Read the data
                data = reader.Read<T>(indexEntry);
            }

            // Notify all registered adapting data providers of the new data.  If the data is Shared<T> then perform a deep clone
            // (which resolves to an AddRef() for this type) for each provider we call.  The providers are responsible for releasing
            // their reference to the data once they're done with it.
            if (this.isSharedType && data != null)
            {
                Parallel.ForEach(this.dataProviders.ToList(), provider => provider.PushData(data.DeepClone<T>(), indexEntry));

                // Release the reference to the local copy of the data
                (data as IDisposable).Dispose();
            }
            else
            {
                Parallel.ForEach(this.dataProviders.ToList(), provider => provider.PushData(data, indexEntry));
            }
        }
    }
}
