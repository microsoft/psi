// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Psi.Persistence;

    /// <summary>
    /// Represents a data provider that can provide adapted data to instant data targets.
    /// Data of type TSrc supplied to the provider is converted to data of type TDest
    /// by the stream adapter and then routed to all instant data targets.
    /// </summary>
    /// <typeparam name="TSrc">The type of source data.</typeparam>
    /// <typeparam name="TDest">The type of destination data.</typeparam>
    public class AdaptingInstantDataProvider<TSrc, TDest> : IAdaptingInstantDataProvider<TSrc>
    {
        /// <summary>
        /// Flag indicating whether type paramamter TSrc is Shared{} or not.
        /// </summary>
        private readonly bool adaptedDataIsSharedType = typeof(TDest).IsGenericType && typeof(TDest).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// The stream adapter that adapts the incoming data to the type required by the instant data targets.
        /// </summary>
        private StreamAdapter<TSrc, TDest> streamAdapter;

        /// <summary>
        /// The collection of instant data targets, indexed by the targets' registration token.
        /// </summary>
        private Dictionary<Guid, InstantDataTarget> targets;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptingInstantDataProvider{TSrc, TDest}"/> class.
        /// </summary>
        /// <param name="streamAdapter">The stream adapter that will convert the incoming data to the data that the targets require.</param>
        public AdaptingInstantDataProvider(IStreamAdapter streamAdapter)
        {
            if (streamAdapter == null)
            {
                throw new ArgumentNullException(nameof(streamAdapter));
            }

            this.streamAdapter = streamAdapter as StreamAdapter<TSrc, TDest>;
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
        public void PushData(TSrc sourceData, IndexEntry indexEntry)
        {
            // Adapt the data to the type required by target.  The data
            // adapter will release the reference to the source data automatically.
            TDest adaptedData = this.streamAdapter.AdaptData(sourceData);

            // Create a non-volatile copy of the list of targets
            List<InstantDataTarget> targetList;
            lock (this.targets)
            {
                targetList = this.targets.Values.ToList();
            }

            // Call each of the targets with the new data.  If the adapted data is shared,
            // then do a deep clone of each item before calling the callback and release the
            // reference to the adapted data once we're done.
            bool createClone = this.adaptedDataIsSharedType && adaptedData != null;

            foreach (InstantDataTarget callbackTarget in targetList)
            {
                this.RunPushTask(callbackTarget, createClone ? adaptedData.DeepClone<TDest>() : adaptedData, indexEntry);
            }

            if (createClone)
            {
                (adaptedData as IDisposable).Dispose();
            }
        }

        private void RunPushTask(InstantDataTarget target, TDest data, IndexEntry indexEntry)
        {
            Task.Run(() =>
            {
                target.Callback.Invoke(data, indexEntry);
            });
        }
    }
}
