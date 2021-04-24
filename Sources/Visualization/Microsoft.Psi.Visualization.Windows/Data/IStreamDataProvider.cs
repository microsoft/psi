// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Defines an interfaces for providers of stream data.
    /// </summary>
    public interface IStreamDataProvider : IDisposable
    {
        /// <summary>
        /// Gets a list of outstanding read requests.
        /// </summary>
        IReadOnlyList<ReadRequest> ReadRequests { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        string StreamName { get; }

        /// <summary>
        /// Gets the time of the nearest message to a specified time.
        /// </summary>
        /// <param name="time">The time to find the nearest message to.</param>
        /// <param name="nearestMessageType">The type of nearest message to find.</param>
        /// <returns>The time of the nearest message, if one is found or null otherwise.</returns>
        DateTime? GetTimeOfNearestMessage(DateTime time, NearestMessageType nearestMessageType);

        /// <summary>
        /// Open stream given a reader.
        /// </summary>
        /// <param name="streamReader">Reader to open stream with.</param>
        void OpenStream(IStreamReader streamReader);

        /// <summary>
        /// Dispatches read data to subscribers. Called by <see cref="DataStoreReader"/> on the UI thread to populate data cache.
        /// </summary>
        void DispatchData();

        /// <summary>
        /// Remove read requests identified by the matching start and end times.
        /// </summary>
        /// <param name="startTime">Start time of read requests to complete.</param>
        /// <param name="endTime">End time of read requests to complete.</param>
        void RemoveReadRequest(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Stops the stream provider from publishing more data.
        /// </summary>
        void Stop();
    }
}