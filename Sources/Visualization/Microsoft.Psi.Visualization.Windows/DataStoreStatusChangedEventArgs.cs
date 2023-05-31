// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;

    /// <summary>
    /// Represents the event args passed by the DataStoreStatusChanged event.
    /// </summary>
    public class DataStoreStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreStatusChangedEventArgs"/> class.
        /// </summary>
        /// <param name="storeName">The name of the store whose status has changed.</param>
        /// <param name="isDirty">True if the store is now dirty, otherwise false.</param>
        /// <param name="streamNames">The list of streams whose status has changed.</param>
        public DataStoreStatusChangedEventArgs(string storeName, bool isDirty, params string[] streamNames)
        {
            this.StoreName = storeName;
            this.IsDirty = isDirty;
            this.StreamNames = streamNames;
        }

        /// <summary>
        /// Gets the name of the store whose status has changed.
        /// </summary>
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the store is now dirty (unsaved changes) or clean (no unsaved changes).
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Gets the list of streams whose status has changed.
        /// </summary>
        public string[] StreamNames { get; private set; }
    }
}