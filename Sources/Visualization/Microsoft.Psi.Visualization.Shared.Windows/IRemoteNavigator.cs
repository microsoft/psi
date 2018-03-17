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
    /// Represents a remote navigator instance.
    /// </summary>
    [Guid(Guids.IRemoteNavigatorIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteNavigator
    {
        /// <summary>
        /// Gets or sets the cursor (position).
        /// </summary>
        DateTime Cursor { get; set; }

        /// <summary>
        /// Gets the data range.
        /// </summary>
        IRemoteNavigatorRange DataRange { get; }

        /// <summary>
        /// Gets or sets the navigation mode.
        /// </summary>
        RemoteNavigationMode NavigationMode { get; set; }

        /// <summary>
        /// Gets the selection range.
        /// </summary>
        IRemoteNavigatorRange SelectionRange { get; }

        /// <summary>
        /// Gets the view range.
        /// </summary>
        IRemoteNavigatorRange ViewRange { get; }

        /// <summary>
        /// Gets or sets the zoom range selection padding.
        /// </summary>
        double ZoomToSelectionPadding { get; set; }
    }
}