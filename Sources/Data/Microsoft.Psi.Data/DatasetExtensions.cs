// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    /// <summary>
    /// Represents helper methods for working with \psi store datasets.
    /// </summary>
    public static class DatasetExtensions
    {
        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="dataset">This dataset.</param>
        /// <param name="storeName">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="storePath">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="sessionName">The name of the session (optional, defaults to streamReader.Name).</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly added session.</returns>
        public static Session AddSessionFromPsiStore(this Dataset dataset, string storeName, string storePath, string sessionName = null, string partitionName = null)
        {
            return dataset.AddSessionFromStore(new PsiStoreStreamReader(storeName, storePath), sessionName, partitionName);
        }

        /// <summary>
        /// Creates and adds a data partition from an existing \psi data store.
        /// </summary>
        /// <param name="session">This session.</param>
        /// <param name="storeName">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="storePath">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="partitionName">The partition name. Default is stream reader name.</param>
        /// <returns>The newly added data partition.</returns>
        public static Partition<PsiStoreStreamReader> AddPsiStorePartition(this Session session, string storeName, string storePath, string partitionName = null)
        {
            return session.AddStorePartition(new PsiStoreStreamReader(storeName, storePath), partitionName);
        }
    }
}
