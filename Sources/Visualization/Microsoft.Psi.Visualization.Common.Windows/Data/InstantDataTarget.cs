// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using Microsoft.Psi.Persistence;

    /// <summary>
    /// Represents an instant data target.
    /// </summary>
    public class InstantDataTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstantDataTarget"/> class.
        /// </summary>
        /// <param name="streamName">The name of the stream from which to receive updates when new data is available.</param>
        /// <param name="streamAdapter">The stream adapter to convert the raw stream data to the type required by the callback target.</param>
        /// <param name="cursorEpsilon">The cursor epsilon to use when searching for data.</param>
        /// <param name="callback">The method to call when new data is available.</param>
        public InstantDataTarget(string streamName, IStreamAdapter streamAdapter, RelativeTimeInterval cursorEpsilon, Action<object, IndexEntry> callback)
        {
            this.RegistrationToken = Guid.NewGuid();
            this.StreamName = streamName;
            this.StreamAdapter = streamAdapter;
            this.CursorEpsilon = cursorEpsilon;
            this.Callback = callback;
        }

        /// <summary>
        /// Gets the registration token.
        /// </summary>
        public Guid RegistrationToken { get; private set; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        public string StreamName { get; }

        /// <summary>
        /// Gets the stream binding.
        /// </summary>
        public IStreamAdapter StreamAdapter { get; }

        /// <summary>
        /// Gets or sets the cursor epsilon to be used when reading data from the stream.
        /// </summary>
        public RelativeTimeInterval CursorEpsilon { get; set; }

        /// <summary>
        /// Gets the method to call when new data from the stream is available.
        /// </summary>
        public Action<object, IndexEntry> Callback { get; }
    }
}
