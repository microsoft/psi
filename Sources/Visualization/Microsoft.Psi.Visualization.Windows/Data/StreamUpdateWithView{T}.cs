// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents a stream update with an attached view of the cache.  The view covers only the stream update itself and exists to
    /// ensure that the data in the data cache retains a reference to ensure it does not get deleted during a purge of the data cache.
    /// </summary>
    /// <typeparam name="T">The type of messages in the stream.</typeparam>
    public class StreamUpdateWithView<T> : StreamUpdate<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamUpdateWithView{T}"/> class.
        /// </summary>
        /// <param name="streamUpdate">An existing stream update to convert to a stream update with view.</param>
        /// <param name="view">The view of the data cache spanning the duration of the update.</param>
        public StreamUpdateWithView(StreamUpdate<T> streamUpdate, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView view)
            : base(streamUpdate.UpdateType, streamUpdate.Message)
        {
            this.View = view;
        }

        /// <summary>
        /// Gets a view of the data cache spanning the duration of the update.
        /// </summary>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView View { get; private set; }
    }
}
