// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Represents a class that monitors live stores.
    /// </summary>
    public static class PsiStoreMonitor
    {
        /// <summary>
        /// The frequency with which we check whether the live marker file for a store
        /// exists and cannot be read from because the writer holds an exclusive share.
        /// </summary>
        private const int UpdatePeriodMs = 5000;

        /// <summary>
        /// The timeout period during which no client asks for status of a monitored live
        /// store before it gets removed from the the collection of monitored live stores.
        /// </summary>
        private const int AccessPeriodTimeoutMs = 30000;

        /// <summary>
        /// The collection of stores whose live marker files are currently being tracked.
        /// </summary>
        private static Dictionary<(string storeName, string storePath), MarkerFileInfo> monitoredStores = new Dictionary<(string storeName, string storePath), MarkerFileInfo>();

        /// <summary>
        /// A lock to ensure the above collection is not modified while it's being iterated.
        /// </summary>
        private static object collectionLock = new object();

        /// <summary>
        /// The timer that controls when we check whether the live marker files exist
        /// and can be read from.
        /// </summary>
        private static Timer updateTime = new Timer(OnUpdateTimer, null, UpdatePeriodMs, UpdatePeriodMs);

        /// <summary>
        /// Gets the name of the marker file that can be used to determine if a store is currently live.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="storePath">The path to the store.</param>
        /// <returns>True if the store is live, otherwise false..</returns>
        public static bool IsStoreLive(string storeName, string storePath)
        {
            // Check if we're tracking the marker file yet.
            if (monitoredStores.ContainsKey((storeName, storePath)))
            {
                MarkerFileInfo markerFileInfo = monitoredStores[(storeName, storePath)];
                markerFileInfo.LastAccessTime = DateTime.UtcNow;
                return markerFileInfo.IsLive;
            }
            else
            {
                // Create a record for the live marker file and update its initial status.
                MarkerFileInfo markerFileInfo = new MarkerFileInfo(storeName, storePath);
                UpdateMarkerFileInfo(markerFileInfo);

                lock (collectionLock)
                {
                    monitoredStores[(storeName, storePath)] = markerFileInfo;
                }

                return markerFileInfo.IsLive;
            }
        }

        /// <summary>
        /// Gets the path to the live marker file.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="storePath">The path to the store.</param>
        /// <returns>The full path to the live marker file.</returns>
        public static string GetLiveMarkerFileName(string storeName, string storePath)
        {
            // Virtual (in-memory) stores have no path. For such stores
            // we place the live marker file into the user's temp folder.
            string liveMarkerFilePath = storePath;
            if (liveMarkerFilePath == null)
            {
                liveMarkerFilePath = System.IO.Path.Combine(Path.GetTempPath(), "PsiStoreLiveMarkers");
                if (!Directory.Exists(liveMarkerFilePath))
                {
                    Directory.CreateDirectory(liveMarkerFilePath);
                }
            }

            return Path.Combine(liveMarkerFilePath, PsiStoreCommon.GetLivePsiStoreFileName(storeName));
        }

        private static void OnUpdateTimer(object state)
        {
            lock (collectionLock)
            {
                // Remove any monitored stores where no client has asked for its status in a while
                monitoredStores = monitoredStores
                    .Where(ms => ms.Value.LastAccessTime.AddMilliseconds(AccessPeriodTimeoutMs) > DateTime.UtcNow)
                    .ToDictionary(ms => ms.Key, ms => ms.Value);

                // Check the status of all stores that are still showing as live
                foreach (var markerFileInfo in monitoredStores.Values)
                {
                    if (markerFileInfo.IsLive)
                    {
                        UpdateMarkerFileInfo(markerFileInfo);
                    }
                }
            }
        }

        private static void UpdateMarkerFileInfo(MarkerFileInfo markerFileInfo)
        {
            // A non-live store will never become live again
            if (markerFileInfo.IsLive)
            {
                // Check if the live marker file exists
                if (File.Exists(markerFileInfo.FilePath))
                {
                    markerFileInfo.IsLive = !Platform.Specific.CanOpenFile(markerFileInfo.FilePath);
                }
                else
                {
                    // The marker file does not exist, which means the writer finished and deleted it
                    markerFileInfo.IsLive = false;
                }
            }
        }

        private class MarkerFileInfo
        {
            public MarkerFileInfo(string storeName, string storePath)
            {
                this.FilePath = GetLiveMarkerFileName(storeName, storePath);
                this.IsLive = true;
                this.LastAccessTime = DateTime.UtcNow;
            }

            /// <summary>
            /// Gets or sets the path to the live marker file.
            /// </summary>
            public string FilePath { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the store is currently live.
            /// </summary>
            public bool IsLive { get; set; }

            /// <summary>
            /// Gets or sets the last time a client checked the status of the marker file.
            /// </summary>
            public DateTime LastAccessTime { get; set; }
        }
    }
}
