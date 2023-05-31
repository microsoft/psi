// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;

    /// <summary>
    /// Represents an object that provides an X value range.
    /// </summary>
    public interface IXValueRangeProvider
    {
        /// <summary>
        /// Fires when the X value range has changed.
        /// </summary>
        event EventHandler<EventArgs> XValueRangeChanged;

        /// <summary>
        /// Gets the X value range.
        /// </summary>
        ValueRange<double> XValueRange { get; }
    }
}
