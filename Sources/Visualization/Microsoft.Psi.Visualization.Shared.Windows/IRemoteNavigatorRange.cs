// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if COM_SERVER
namespace Microsoft.Psi.Visualization.Server
#elif COM_CLIENT
namespace Microsoft.Psi.Visualization.Client
#endif
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a remote navigator range.
    /// </summary>
    [Guid(Guids.IRemoteNavigatorRangeIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteNavigatorRange
    {
        /// <summary>
        /// Gets the range duration.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// Gets the range start time.
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets the range end time.
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        /// Sets the range.
        /// </summary>
        /// <param name="startTime">Start time of the range.</param>
        /// <param name="endTime">End time of the range.</param>
        void SetRange(DateTime startTime, DateTime endTime);
    }
}
