// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents helper methods for working with WAVE file datasets.
    /// </summary>
    public static class DatasetExtensions
    {
        /// <summary>
        /// Creates and adds a session to this dataset using the specified parameters.
        /// </summary>
        /// <param name="dataset">This dataset.</param>
        /// <param name="storeName">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="storePath">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="startTime">Starting time for streams of data..</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer in milliseconds.</param>
        /// <param name="sessionName">The name of the session (optional, defaults to streamReader.Name).</param>
        /// <param name="partitionName">The partition name.</param>
        /// <returns>The newly added session.</returns>
        public static Session AddSessionFromWaveFileStore(this Dataset dataset, string storeName, string storePath, DateTime startTime, int audioBufferSizeMs = WaveFileStreamReader.DefaultAudioBufferSizeMs, string sessionName = null, string partitionName = null)
        {
            return dataset.AddSessionFromStore(new WaveFileStreamReader(storeName, storePath, startTime, audioBufferSizeMs), sessionName, partitionName);
        }

        /// <summary>
        /// Creates and adds a data partition from an existing WAVE file data store.
        /// </summary>
        /// <param name="session">This session.</param>
        /// <param name="storeName">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="storePath">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="startTime">Starting time for streams of data..</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer in milliseconds.</param>
        /// <param name="partitionName">The partition name. Default is stream reader name.</param>
        /// <returns>The newly added data partition.</returns>
        public static Partition<WaveFileStreamReader> AddWaveFileStorePartition(this Session session, string storeName, string storePath, DateTime startTime, int audioBufferSizeMs = WaveFileStreamReader.DefaultAudioBufferSizeMs, string partitionName = null)
        {
            return session.AddStorePartition(new WaveFileStreamReader(storeName, storePath, startTime, audioBufferSizeMs), partitionName);
        }
    }
}
