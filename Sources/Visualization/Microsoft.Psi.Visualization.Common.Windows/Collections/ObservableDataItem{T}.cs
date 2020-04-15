// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Collections
{
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Object with a data field that notifies listeners when changed.
    /// </summary>
    /// <typeparam name="T">The type of the data field.</typeparam>
    public class ObservableDataItem<T> : ObservableObject
    {
        private T data;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableDataItem{T}"/> class.
        /// </summary>
        /// <param name="data">The initiali data value.</param>
        public ObservableDataItem(T data)
        {
            this.data = data;
        }

        /// <summary>
        /// Gets or sets the data field.
        /// </summary>
        public T Data
        {
            get { return this.data; }
            set { this.Set(nameof(this.Data), ref this.data, value); }
        }
    }
}
